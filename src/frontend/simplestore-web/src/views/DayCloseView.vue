<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { apiRequest } from '../api/client'
import type { CostReliability, EndOfDayReport, StoreInfo } from '../api/types'
import {
  dayCloseDemoEnabled, referenceActor, referenceCloseSummary, referenceCloseTime, referenceDayCount, referenceDays,
  referencePageCount, referenceReconciliation, referenceReport, type DayRow, type ReportModel,
} from '../dayclose/demo'

/**
 * Live mode is the D-101 report on a Store-local business date. Closing a day (status, actor, locked figures)
 * is future direction only (D-048), so the close flow exists solely in the browser-only preview.
 */
interface ListRow { key: string; date: string; weekday: string; time: string; closed: boolean; actor: string; isToday: boolean }

/** Days offered in the live list; the report's date arrows and picker reach any earlier day. */
const LIVE_DAYS = 30
const closeSteps = ['Xem số liệu', 'Kiểm tra', 'Xác nhận đóng ngày']

const demo = dayCloseDemoEnabled
const cloneDays = () => referenceDays.map(day => ({ ...day }))
const mockDays = ref<DayRow[]>(cloneDays())
const view = ref<'list' | 'report'>('list')
const today = ref('')
const storeError = ref('')
const sortAscending = ref(false)
const page = ref(1)
const pageSize = ref(10)
const reportDate = ref('')
const report = ref<EndOfDayReport | null>(null)
const loading = ref(false)
const error = ref('')
const metric = ref('sales')
const detailOpen = ref(false)
const explainOpen = ref(false)
const wizardOpen = ref(false)
const wizardStep = ref(1)
const closeNote = ref('')
const toastText = ref('')
let toastTimer: ReturnType<typeof setTimeout> | undefined
let loadSequence = 0

const money = (value: number) => `${new Intl.NumberFormat('vi-VN').format(value)} đ`
const minus = (value: number) => value ? `-${money(value)}` : money(0)
const reliabilityLabels: Record<CostReliability, string> = { Reliable: 'Tin cậy', Estimated: 'Ước tính', Unavailable: 'Không khả dụng' }

function storeToday(timeZone: string) {
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone, year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date())
  const value = Object.fromEntries(parts.map(part => [part.type, part.value]))
  return `${value.year}-${value.month}-${value.day}`
}
/** Calendar arithmetic on the business date itself, independent of the browser timezone. */
function shiftDate(iso: string, days: number) {
  const date = new Date(`${iso}T00:00:00Z`)
  date.setUTCDate(date.getUTCDate() + days)
  return date.toISOString().slice(0, 10)
}
const displayDate = (iso: string) => iso.split('-').reverse().join('/')
function weekday(iso: string) {
  const day = new Date(`${iso}T00:00:00Z`).getUTCDay()
  return day === 0 ? 'Chủ nhật' : `Thứ ${day + 1}`
}
function storeLocalTime(instant: string, timeZone: string) {
  const parts = new Intl.DateTimeFormat('en-GB', {
    timeZone, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
  }).formatToParts(new Date(instant))
  const value = Object.fromEntries(parts.map(part => [part.type, part.value]))
  return `${value.hour}:${value.minute} ${value.day}/${value.month}/${value.year}`
}

/* List */
const liveDays = computed(() => today.value ? Array.from({ length: LIVE_DAYS }, (_, index) => shiftDate(today.value, -index)) : [])
const rows = computed<ListRow[]>(() => {
  if (demo.value) {
    const days = mockDays.value.map((day, index) => ({ key: day.date, date: day.date, weekday: '', time: day.time, closed: day.closed, actor: day.actor, isToday: index === 0 }))
    return sortAscending.value ? days.reverse() : days
  }
  const days = sortAscending.value ? [...liveDays.value].reverse() : liveDays.value
  return days.slice((page.value - 1) * pageSize.value, page.value * pageSize.value)
    .map(iso => ({ key: iso, date: displayDate(iso), weekday: weekday(iso), time: '', closed: false, actor: '', isToday: iso === today.value }))
})
const totalCount = computed(() => demo.value ? referenceDayCount : liveDays.value.length)
const totalPages = computed(() => demo.value ? referencePageCount : Math.ceil(totalCount.value / pageSize.value))
const firstIndex = computed(() => rows.value.length ? (page.value - 1) * pageSize.value + 1 : 0)
const lastIndex = computed(() => rows.value.length ? firstIndex.value + rows.value.length - 1 : 0)
const pagerItems = computed<Array<number | '…'>>(() => {
  const total = totalPages.value
  const current = page.value
  if (total <= 7) return Array.from({ length: total }, (_, index) => index + 1)
  if (current <= 4) return [1, 2, 3, 4, 5, '…', total]
  if (current >= total - 3) return [1, '…', total - 4, total - 3, total - 2, total - 1, total]
  return [1, '…', current - 1, current, current + 1, '…', total]
})
/** The day the preview can close: the reference has exactly one, today. */
const openDay = computed(() => mockDays.value.find(day => !day.closed) ?? null)

/* Report */
const demoDayIndex = computed(() => mockDays.value.findIndex(day => day.date === reportDate.value))
const demoDay = computed(() => mockDays.value[demoDayIndex.value] ?? null)
const model = computed<ReportModel | null>(() => demo.value ? referenceReport : report.value ? liveModel(report.value) : null)
const canPrevious = computed(() => demo.value ? demoDayIndex.value < mockDays.value.length - 1 : reportDate.value !== '')
const canNext = computed(() => demo.value ? demoDayIndex.value > 0 : reportDate.value !== '' && reportDate.value < today.value)
const status = computed(() => {
  if (demo.value) return demoDay.value ? { tone: demoDay.value.closed ? 'closed' : 'open', label: demoDay.value.closed ? 'Đã đóng' : 'Chưa đóng' } : null
  return reportDate.value === today.value ? { tone: 'open', label: 'Đang diễn ra' } : null
})
const donutStyle = computed(() => {
  const slices = model.value?.payments.slices.filter(slice => slice.share > 0) ?? []
  if (!slices.length) return { background: '#eef2f5' }
  let start = 0
  const stops = slices.map((slice, index) => {
    const end = index === slices.length - 1 ? 100 : start + slice.share
    const stop = `${slice.color} ${start}% ${end}%`
    start = end
    return stop
  })
  return { background: `conic-gradient(${stops.join(',')})` }
})
const detail = computed(() => model.value?.details[metric.value] ?? null)
const explain = computed(() => model.value?.explains[metric.value] ?? null)

/** Only the aggregates EndOfDayReport exposes; cash/transfer split, categories, invoice count and new debt stay preview-only (D-101). */
function liveModel(source: EndOfDayReport): ReportModel {
  const collected = source.collected
  const profit = source.estimatedGrossProfit
  const supplier = source.supplierPayments
  const reliability = `Độ tin cậy giá vốn: ${reliabilityLabels[profit.costReliability]}`
  const unavailable = profit.costReliability === 'Unavailable'
  const received = collected.salePayments + collected.customerDebtPayments
  const share = (value: number) => received > 0 ? Math.round(value / received * 1000) / 10 : 0
  const amountWithShare = (value: number) => `${money(value)} (${share(value).toFixed(1)}%)`
  const scope = `${storeLocalTime(source.startUtc, source.timeZoneId)} đến trước ${storeLocalTime(source.endUtc, source.timeZoneId)}, theo múi giờ ${source.timeZoneId}.`
  const collectedRows = [
    { label: 'Thanh toán đơn bán', value: money(collected.salePayments) },
    { label: 'Thu nợ khách hàng', value: money(collected.customerDebtPayments) },
    { label: 'Hoàn tiền khách hàng', value: minus(collected.customerRefunds), tone: 'bad' as const },
  ]
  const profitRows = [
    { label: 'Doanh thu thuần', value: money(profit.netSalesRevenue) },
    { label: 'Giá vốn lịch sử', value: money(profit.historicalCogs) },
  ]
  const supplierRows = [
    { label: 'Thanh toán phiếu nhập', value: money(supplier.purchasePayments) },
    { label: 'Trả nợ nhà cung cấp', value: money(supplier.supplierDebtPayments) },
  ]
  const amountTable = (items: Array<{ label: string; value: string; tone?: 'good' | 'bad' }>, total?: { label: string; value: string; tone?: 'good' | 'bad' }) => ({
    columns: ['Khoản mục', 'Số tiền'], numeric: [1],
    rows: [
      ...items.map(item => ({ cells: [{ text: item.label }, { text: item.value, tone: item.tone }] })),
      ...(total ? [{ cells: [{ text: total.label }, { text: total.value, tone: total.tone }], total: true }] : []),
    ],
  })
  return {
    kpis: [
      { key: 'sales', label: 'Doanh thu bán hàng', value: money(source.salesRevenue), tone: 'green' },
      { key: 'cash', label: 'Tiền thu thuần', value: money(collected.netAmount), tone: 'green' },
      { key: 'debt', label: 'Công nợ khách hàng cuối ngày', value: money(source.customerOutstandingDebtAtEnd), tone: 'red' },
      { key: 'cogs', label: 'Giá vốn hàng bán', value: money(profit.historicalCogs), tone: 'red', note: reliability, warn: unavailable },
      { key: 'profit', label: 'Lãi gộp ước tính', value: money(profit.amount), tone: 'green', note: unavailable ? 'Giá vốn chưa đủ tin cậy; không phải lợi nhuận kế toán.' : reliability, warn: unavailable },
      { key: 'supplier', label: 'Thanh toán nhà cung cấp', value: money(supplier.totalAmount), tone: '' },
    ],
    payments: {
      title: 'Cơ cấu tiền thu', centerLabel: 'Tiền thu thuần', centerValue: money(collected.netAmount),
      slices: [
        { label: 'Thanh toán đơn bán', value: amountWithShare(collected.salePayments), color: '#0aa06a', share: share(collected.salePayments) },
        { label: 'Thu nợ khách hàng', value: amountWithShare(collected.customerDebtPayments), color: '#2f6fe4', share: share(collected.customerDebtPayments) },
        { label: 'Hoàn tiền khách hàng', value: minus(collected.customerRefunds), color: '#aab6c5', share: 0 },
      ],
    },
    categories: null,
    tables: [
      { title: 'Chi tiết tiền thu', table: amountTable(collectedRows, { label: 'Tiền thu thuần', value: money(collected.netAmount) }) },
      { title: 'Chi tiết lãi gộp ước tính', table: amountTable([...profitRows, { label: 'Độ tin cậy giá vốn', value: reliabilityLabels[profit.costReliability] }], { label: 'Lãi gộp ước tính', value: money(profit.amount), tone: 'good' }) },
      { title: 'Công nợ và thanh toán nhà cung cấp', table: amountTable([
        { label: 'Công nợ khách hàng cuối ngày', value: money(source.customerOutstandingDebtAtEnd), tone: 'bad' },
        { label: 'Công nợ nhà cung cấp cuối ngày', value: money(source.supplierOutstandingDebtAtEnd), tone: 'bad' },
        ...supplierRows,
      ], { label: 'Tổng thanh toán nhà cung cấp', value: money(supplier.totalAmount) }) },
    ],
    details: {
      sales: { title: 'Chi tiết doanh thu bán hàng', blocks: [
        { kind: 'pairs', rows: [{ label: 'Doanh thu bán hàng', value: money(source.salesRevenue), tone: 'good' }] },
        { kind: 'text', text: 'Tính theo ngày phát sinh của bán hàng, trả hàng và hủy giao dịch trong ngày kinh doanh.' },
      ] },
      cash: { title: 'Chi tiết tiền thu thuần', blocks: [
        { kind: 'pairs', rows: [...collectedRows, { label: 'Tiền thu thuần', value: money(collected.netAmount), tone: 'good', strong: true }] },
        { kind: 'note', tone: 'info', text: 'Thu nợ khách hàng là tiền thực thu nhưng không tạo doanh thu.' },
      ] },
      debt: { title: 'Chi tiết công nợ cuối ngày', blocks: [
        { kind: 'pairs', rows: [
          { label: 'Công nợ khách hàng cuối ngày', value: money(source.customerOutstandingDebtAtEnd), tone: 'bad' },
          { label: 'Công nợ nhà cung cấp cuối ngày', value: money(source.supplierOutstandingDebtAtEnd), tone: 'bad' },
        ] },
        { kind: 'note', tone: 'info', text: 'Là số dư công nợ tại thời điểm kết thúc ngày, không phải công nợ phát sinh trong ngày.' },
      ] },
      cogs: { title: 'Chi tiết giá vốn hàng bán', blocks: [
        { kind: 'note', tone: profit.costReliability === 'Reliable' ? 'info' : 'warn', text: 'Giá vốn lịch sử lấy theo giá vốn bình quân tại thời điểm phát sinh từng giao dịch.' },
        { kind: 'pairs', rows: [
          { label: 'Giá vốn lịch sử', value: money(profit.historicalCogs) },
          { label: 'Độ tin cậy giá vốn', value: reliabilityLabels[profit.costReliability] },
        ] },
      ] },
      profit: { title: 'Chi tiết lãi gộp ước tính', blocks: [
        { kind: 'pairs', rows: [...profitRows, { label: 'Lãi gộp ước tính', value: money(profit.amount), tone: 'good', strong: true }, { label: 'Độ tin cậy giá vốn', value: reliabilityLabels[profit.costReliability] }] },
        { kind: 'note', tone: unavailable ? 'warn' : 'info', text: 'Lãi gộp ước tính không phải lợi nhuận kế toán và phụ thuộc vào độ tin cậy của giá vốn.' },
      ] },
      supplier: { title: 'Chi tiết thanh toán nhà cung cấp', blocks: [
        { kind: 'pairs', rows: [...supplierRows, { label: 'Tổng thanh toán nhà cung cấp', value: money(supplier.totalAmount), strong: true }] },
        { kind: 'text', text: 'Trả nợ nhà cung cấp không làm thay đổi giá vốn của phiếu nhập.' },
      ] },
    },
    explains: {
      sales: { title: 'Giải thích Doanh thu bán hàng', definition: 'Doanh thu bán hàng là giá trị bán hàng trong ngày kinh doanh, đã trừ trả hàng và giao dịch bị hủy theo ngày phát sinh.' },
      cash: { title: 'Giải thích Tiền thu thuần', definition: 'Tiền thu thuần = Thanh toán đơn bán + Thu nợ khách hàng − Hoàn tiền khách hàng.', formula: { left: money(received), op: '−', right: money(collected.customerRefunds), result: money(collected.netAmount), captions: ['Thanh toán + thu nợ', 'Hoàn tiền', 'Tiền thu thuần'] } },
      debt: { title: 'Giải thích Công nợ khách hàng cuối ngày', definition: 'Tổng số dư nợ của khách hàng tại thời điểm kết thúc ngày kinh doanh.' },
      cogs: { title: 'Giải thích Giá vốn', definition: 'Giá vốn lịch sử của hàng đã bán, lấy theo giá vốn bình quân tại thời điểm phát sinh từng giao dịch.' },
      profit: { title: 'Giải thích Lãi gộp ước tính', definition: 'Lãi gộp ước tính = Doanh thu thuần − Giá vốn lịch sử.', formula: { left: money(profit.netSalesRevenue), op: '−', right: money(profit.historicalCogs), result: money(profit.amount), captions: ['Doanh thu thuần', 'Giá vốn lịch sử', 'Lãi gộp ước tính'] } },
      supplier: { title: 'Giải thích Thanh toán nhà cung cấp', definition: 'Thanh toán nhà cung cấp = Thanh toán phiếu nhập + Trả nợ nhà cung cấp.', formula: { left: money(supplier.purchasePayments), op: '+', right: money(supplier.supplierDebtPayments), result: money(supplier.totalAmount), captions: ['Phiếu nhập', 'Trả nợ NCC', 'Tổng'] } },
    },
    scope,
    scopeNote: 'Báo cáo được tính lại mỗi lần xem, không phải số liệu đã chốt; trả hàng hoặc hủy giao dịch ghi nhận sau có thể làm thay đổi số liệu của ngày này.',
  }
}

async function loadStore() {
  storeError.value = ''
  try {
    const store = await apiRequest<StoreInfo>('/api/store/current')
    today.value = storeToday(store.timeZoneId)
  } catch (reason) {
    storeError.value = reason instanceof Error ? reason.message : 'Không thể tải thông tin cửa hàng.'
  }
}
async function loadReport() {
  if (demo.value || !reportDate.value) return
  const sequence = ++loadSequence
  loading.value = true
  error.value = ''
  report.value = null
  try {
    const result = await apiRequest<EndOfDayReport>(`/api/reports/end-of-day?date=${encodeURIComponent(reportDate.value)}`)
    if (sequence === loadSequence) report.value = result
  } catch (reason) {
    if (sequence === loadSequence) error.value = reason instanceof Error ? reason.message : 'Không thể tải báo cáo.'
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}

function toast(message: string) {
  toastText.value = message
  if (toastTimer) clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toastText.value = '' }, 2200)
}
function closeOverlays() {
  detailOpen.value = false
  explainOpen.value = false
  wizardOpen.value = false
}
function goPage(target: number) {
  if (target < 1 || target > totalPages.value || target === page.value) return
  if (demo.value) { toast('Dữ liệu mẫu chỉ có trang đầu tiên giống bản thiết kế.'); return }
  page.value = target
}
function openReport(key: string) {
  closeOverlays()
  reportDate.value = key
  view.value = 'report'
  loadReport()
}
function stepDate(direction: -1 | 1) {
  closeOverlays()
  if (demo.value) {
    const day = mockDays.value[demoDayIndex.value - direction]
    if (day) reportDate.value = day.date
    return
  }
  reportDate.value = shiftDate(reportDate.value, direction)
  loadReport()
}
function backToList() {
  ++loadSequence
  closeOverlays()
  view.value = 'list'
}
function openDetail(key: string) {
  metric.value = key
  explainOpen.value = false
  detailOpen.value = true
}
function openWizard() {
  if (!openDay.value) return
  closeOverlays()
  wizardStep.value = 1
  closeNote.value = ''
  wizardOpen.value = true
}
function wizardNext() {
  if (wizardStep.value < closeSteps.length) { wizardStep.value += 1; return }
  const day = openDay.value
  if (!day) return
  Object.assign(day, { closed: true, time: referenceCloseTime, actor: referenceActor, note: closeNote.value.trim() })
  wizardOpen.value = false
  toast(`Đã đóng ngày ${day.date} trong dữ liệu mẫu`)
}
function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') closeOverlays()
}

watch(demo, enabled => {
  ++loadSequence
  mockDays.value = cloneDays()
  view.value = 'list'
  sortAscending.value = false
  page.value = 1
  pageSize.value = 10
  reportDate.value = ''
  report.value = null
  loading.value = false
  error.value = ''
  closeOverlays()
  if (!enabled && !today.value) loadStore()
})
watch(pageSize, () => { page.value = 1 })
onMounted(() => {
  window.addEventListener('keydown', onKeydown)
  if (!demo.value) loadStore()
})
onUnmounted(() => {
  ++loadSequence
  if (toastTimer) clearTimeout(toastTimer)
  window.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <section class="dayclose-page">
    <!-- Day list -->
    <div v-if="view === 'list'" class="dayclose-panel">
      <div class="dayclose-head">
        <div class="dayclose-title">
          <h1>Đóng ngày</h1>
          <p>{{ demo ? 'Xem báo cáo và thực hiện đóng ngày để chốt số liệu trong ngày.' : 'Xem báo cáo cuối ngày theo từng ngày kinh doanh của cửa hàng.' }}</p>
        </div>
        <button v-if="demo" class="dayclose-primary" type="button" :disabled="!openDay" @click="openWizard">＋ Đóng ngày hôm nay</button>
        <button v-else class="dayclose-primary" type="button" :disabled="!today" @click="openReport(today)">Xem báo cáo hôm nay</button>
      </div>
      <p v-if="!demo" class="dayclose-info-note dayclose-list-note">ℹ Chốt số liệu (đóng ngày) chưa được hệ thống hỗ trợ; báo cáo luôn tính theo dữ liệu hiện tại. Bật <b>Dữ liệu mẫu</b> để xem trước quy trình đóng ngày.</p>

      <div v-if="!demo && (storeError || !today)" class="dayclose-state" :role="storeError ? 'alert' : 'status'">
        <template v-if="storeError"><h3>Không thể tải danh sách ngày</h3><p>{{ storeError }}</p><button class="dayclose-secondary" type="button" @click="loadStore">Thử lại</button></template>
        <p v-else>Đang tải danh sách ngày…</p>
      </div>
      <template v-else>
        <div class="dayclose-table-wrap">
          <table class="dayclose-table">
            <thead>
              <tr>
                <th :aria-sort="sortAscending ? 'ascending' : 'descending'"><button class="dayclose-sort" type="button" @click="sortAscending = !sortAscending">Ngày ↕</button></th>
                <template v-if="demo"><th>Giờ đóng</th><th>Trạng thái</th><th>Người thực hiện</th></template>
                <th v-else>Thứ</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in rows" :key="row.key" :class="{ today: row.isToday }">
                <td><b v-if="row.isToday">{{ row.date }}</b><template v-else>{{ row.date }}</template></td>
                <template v-if="demo">
                  <td>{{ row.time || '-' }}</td>
                  <td><span class="dayclose-status" :class="row.closed ? 'closed' : 'open'">{{ row.closed ? 'Đã đóng' : 'Chưa đóng' }}</span></td>
                  <td>{{ row.actor || '-' }}</td>
                </template>
                <td v-else>{{ row.weekday }}<span v-if="row.isToday" class="dayclose-status open dayclose-inline-status">Hôm nay</span></td>
                <td>
                  <button v-if="demo && !row.closed" class="dayclose-primary dayclose-row-primary" type="button" @click="openWizard">Đóng ngày</button>
                  <button v-else class="dayclose-secondary" type="button" :aria-label="`Xem báo cáo ngày ${row.date}`" @click="openReport(row.key)">Xem báo cáo</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="dayclose-footer">
          <span>Hiển thị {{ firstIndex }} – {{ lastIndex }} / {{ totalCount }} ngày</span>
          <div class="dayclose-pager">
            <button type="button" aria-label="Trang trước" :disabled="page <= 1" @click="goPage(page - 1)">‹</button>
            <template v-for="(item, index) in pagerItems" :key="`${item}-${index}`">
              <button v-if="item === '…'" type="button" tabindex="-1" aria-hidden="true">…</button>
              <button v-else type="button" :class="{ active: page === item }" :aria-current="page === item ? 'page' : undefined" @click="goPage(item)">{{ item }}</button>
            </template>
            <button type="button" aria-label="Trang sau" :disabled="page >= totalPages" @click="goPage(page + 1)">›</button>
            <select v-model.number="pageSize" aria-label="Số ngày mỗi trang"><option :value="10">10 / trang</option><option :value="20">20 / trang</option></select>
          </div>
        </div>
      </template>
    </div>

    <!-- Report -->
    <div v-else class="dayclose-panel">
      <div class="dayclose-head">
        <div class="dayclose-title">
          <button class="dayclose-backlink" type="button" @click="backToList">← Danh sách ngày</button>
          <h1 class="dayclose-report-title">{{ demo ? 'Báo cáo đóng ngày' : 'Báo cáo cuối ngày' }}</h1>
        </div>
        <div v-if="demo" class="dayclose-meta">Giờ đóng: <b>{{ demoDay?.time || '-' }}</b><br>Người thực hiện: <b>{{ demoDay?.actor || '-' }}</b><template v-if="demoDay?.note"><br>Ghi chú: <b>{{ demoDay.note }}</b></template></div>
        <div v-else-if="report" class="dayclose-meta">Múi giờ: <b>{{ report.timeZoneId }}</b><br>Từ <b>{{ storeLocalTime(report.startUtc, report.timeZoneId) }}</b> đến trước <b>{{ storeLocalTime(report.endUtc, report.timeZoneId) }}</b></div>
      </div>
      <div class="dayclose-report">
        <div class="dayclose-report-top">
          <div class="dayclose-date-nav">
            <button type="button" aria-label="Ngày trước" :disabled="!canPrevious" @click="stepDate(-1)">‹</button>
            <div v-if="demo" class="dayclose-date-pill">{{ reportDate }}</div>
            <input v-else v-model="reportDate" class="dayclose-date-pill" type="date" :max="today" aria-label="Ngày kinh doanh" @change="closeOverlays(); loadReport()" />
            <button type="button" aria-label="Ngày sau" :disabled="!canNext" @click="stepDate(1)">›</button>
            <span v-if="status" class="dayclose-status" :class="status.tone">{{ status.label }}</span>
          </div>
        </div>

        <div v-if="!demo && (loading || error)" class="dayclose-state" :role="error ? 'alert' : 'status'">
          <template v-if="error"><h3>Không thể tải báo cáo</h3><p>{{ error }}</p><button class="dayclose-secondary" type="button" @click="loadReport">Thử lại</button></template>
          <p v-else>Đang tải báo cáo…</p>
        </div>
        <template v-if="model">
          <div class="dayclose-kpis">
            <div v-for="kpi in model.kpis" :key="kpi.key" class="dayclose-kpi" :class="kpi.tone">
              <span>{{ kpi.label }}</span>
              <strong>{{ kpi.value }}</strong>
              <small v-if="kpi.note" :class="{ warn: kpi.warn }">{{ kpi.note }}</small>
              <button class="dayclose-link" type="button" :aria-label="`Xem chi tiết ${kpi.label}`" @click="openDetail(kpi.key)">Xem chi tiết</button>
            </div>
          </div>

          <div class="dayclose-charts">
            <div class="dayclose-card">
              <h3>{{ model.payments.title }}</h3>
              <div class="dayclose-donut-wrap">
                <div class="dayclose-donut" :style="donutStyle" role="img" :aria-label="`${model.payments.centerLabel} ${model.payments.centerValue}`">
                  <div class="dayclose-donut-label"><div>{{ model.payments.centerLabel }}<br><b>{{ model.payments.centerValue }}</b></div></div>
                </div>
                <div class="dayclose-legend">
                  <div v-for="slice in model.payments.slices" :key="slice.label" class="dayclose-legend-row"><span class="dayclose-swatch" :style="{ background: slice.color }" /><span>{{ slice.label }}</span><b>{{ slice.value }}</b></div>
                </div>
              </div>
            </div>
            <div class="dayclose-card">
              <h3>Cơ cấu doanh thu theo nhóm hàng</h3>
              <div v-if="model.categories" class="dayclose-bars">
                <div v-for="bar in model.categories" :key="bar.label" class="dayclose-bar-row"><span>{{ bar.label }}</span><div class="dayclose-track"><div class="dayclose-fill" :style="{ width: `${bar.width}%`, background: bar.color }" /></div><b>{{ bar.value }}</b></div>
              </div>
              <p v-else class="dayclose-chart-empty">Hệ thống chưa phân loại doanh thu theo nhóm hàng.</p>
            </div>
          </div>

          <div class="dayclose-detail-grid">
            <div v-for="card in model.tables" :key="card.title" class="dayclose-card">
              <h3>{{ card.title }}</h3>
              <table class="dayclose-mini-table">
                <thead v-if="card.table.columns.length"><tr><th v-for="column in card.table.columns" :key="column">{{ column }}</th></tr></thead>
                <tbody><tr v-for="(row, rowIndex) in card.table.rows" :key="rowIndex" :class="{ total: row.total }"><td v-for="(item, index) in row.cells" :key="index" :class="[item.tone, { num: card.table.numeric.includes(index) }]">{{ item.text }}</td></tr></tbody>
              </table>
            </div>
          </div>
        </template>
      </div>
    </div>

    <div v-if="detailOpen || explainOpen" class="dayclose-overlay" @click="closeOverlays" />

    <!-- Metric detail -->
    <aside v-if="detailOpen && detail" class="dayclose-drawer" role="dialog" aria-modal="true" :aria-label="detail.title">
      <div class="dayclose-drawer-head"><h2>{{ detail.title }}</h2><button class="dayclose-x" type="button" aria-label="Đóng" @click="detailOpen = false">×</button></div>
      <div class="dayclose-drawer-body">
        <div v-for="(block, blockIndex) in detail.blocks" :key="blockIndex" :class="block.section ? 'dayclose-ex-section' : 'dayclose-block'">
          <h3 v-if="block.section">{{ block.section }}</h3>
          <table v-if="block.kind === 'pairs'" class="dayclose-confirm-table"><tbody><tr v-for="pair in block.rows" :key="pair.label"><td>{{ pair.label }}</td><td :class="[pair.tone, { strong: pair.strong }]">{{ pair.value }}</td></tr></tbody></table>
          <table v-else-if="block.kind === 'table'" class="dayclose-mini-table">
            <thead v-if="block.table.columns.length"><tr><th v-for="column in block.table.columns" :key="column">{{ column }}</th></tr></thead>
            <tbody><tr v-for="(row, rowIndex) in block.table.rows" :key="rowIndex" :class="{ total: row.total }"><td v-for="(item, index) in row.cells" :key="index" :class="[item.tone, { num: block.table.numeric.includes(index) }]">{{ item.text }}</td></tr></tbody>
          </table>
          <div v-else-if="block.kind === 'note'" :class="block.tone === 'warn' ? 'dayclose-warn-note' : 'dayclose-info-note'">{{ block.text }}</div>
          <p v-else class="dayclose-muted-text">{{ block.text }}</p>
        </div>
      </div>
      <div class="dayclose-drawer-foot"><button class="dayclose-secondary" type="button" @click="explainOpen = true">Giải thích số liệu</button><button class="dayclose-secondary" type="button" @click="detailOpen = false">Đóng</button></div>
    </aside>

    <!-- Explainability -->
    <aside v-if="explainOpen && explain && model" class="dayclose-drawer" role="dialog" aria-modal="true" :aria-label="explain.title">
      <div class="dayclose-drawer-head"><h2>{{ explain.title }}</h2><button class="dayclose-x" type="button" aria-label="Đóng" @click="explainOpen = false">×</button></div>
      <div class="dayclose-drawer-body">
        <div class="dayclose-ex-section"><h3>1. Định nghĩa</h3><p class="dayclose-text">{{ explain.definition }}</p></div>
        <div v-if="explain.formula" class="dayclose-ex-section"><h3>2. Công thức</h3>
          <div class="dayclose-formula">
            <div class="dayclose-formula-box blue">{{ explain.formula.left }}<small v-if="explain.formula.captions">{{ explain.formula.captions[0] }}</small></div><b>{{ explain.formula.op }}</b>
            <div class="dayclose-formula-box red">{{ explain.formula.right }}<small v-if="explain.formula.captions">{{ explain.formula.captions[1] }}</small></div><b>=</b>
            <div class="dayclose-formula-box green">{{ explain.formula.result }}<small v-if="explain.formula.captions">{{ explain.formula.captions[2] }}</small></div>
          </div>
        </div>
        <div class="dayclose-ex-section"><h3>{{ explain.formula ? 3 : 2 }}. Phạm vi thời gian</h3><p class="dayclose-text">{{ model.scope }}</p></div>
        <div class="dayclose-info-note">ℹ {{ model.scopeNote }}</div>
      </div>
      <div class="dayclose-drawer-foot"><button class="dayclose-secondary" type="button" @click="explainOpen = false">Đóng</button></div>
    </aside>

    <!-- Close-day wizard: browser-only preview -->
    <div v-if="wizardOpen && openDay" class="dayclose-modal-layer">
      <section class="dayclose-modal" role="dialog" aria-modal="true" aria-label="Quy trình đóng ngày">
        <div class="dayclose-modal-head"><h2>Quy trình đóng ngày</h2><button class="dayclose-x" type="button" aria-label="Đóng" @click="wizardOpen = false">×</button></div>
        <div class="dayclose-modal-body">
          <div class="dayclose-steps"><div v-for="(label, index) in closeSteps" :key="label" class="dayclose-step" :class="{ active: wizardStep === index + 1, done: wizardStep > index + 1 }"><span class="n">{{ index + 1 }}</span>{{ label }}</div></div>
          <div v-if="wizardStep === 1" class="dayclose-card">
            <h3>Số liệu ngày {{ openDay.date }}</h3>
            <table class="dayclose-confirm-table"><tbody><tr v-for="item in referenceCloseSummary" :key="item.label"><td>{{ item.label }}</td><td :class="item.tone">{{ item.value }}</td></tr></tbody></table>
          </div>
          <template v-else-if="wizardStep === 2">
            <div class="dayclose-warn-note">⚠ Hãy kiểm tra lại số liệu bán hàng, thực thu, công nợ và giá vốn trước khi đóng ngày. Sau khi đóng, số liệu ngày này sẽ được chốt.</div>
            <div class="dayclose-detail-grid two">
              <div v-for="item in referenceReconciliation" :key="item.title" class="dayclose-card"><h3>{{ item.title }}</h3><p class="dayclose-muted-text">{{ item.text }}</p></div>
            </div>
          </template>
          <template v-else>
            <h3 class="dayclose-confirm-title">Xác nhận đóng ngày {{ openDay.date }}</h3>
            <div class="dayclose-warn-note">Sau khi đóng ngày, số liệu của ngày này sẽ được chốt. Các giao dịch phát sinh sau giờ đóng sẽ thuộc về ngày tiếp theo và không làm thay đổi báo cáo ngày này.</div>
            <table class="dayclose-confirm-table spaced"><tbody><tr v-for="item in referenceCloseSummary" :key="item.label"><td>{{ item.label }}</td><td :class="item.tone">{{ item.value }}</td></tr></tbody></table>
            <label class="dayclose-field"><span>Ghi chú (không bắt buộc)</span><textarea v-model="closeNote" class="dayclose-textarea" placeholder="Nhập ghi chú..." /></label>
          </template>
        </div>
        <div class="dayclose-modal-foot">
          <button class="dayclose-secondary" type="button" :style="{ visibility: wizardStep === 1 ? 'hidden' : 'visible' }" @click="wizardStep -= 1">Quay lại</button>
          <button class="dayclose-primary" type="button" @click="wizardNext">{{ wizardStep === closeSteps.length ? '✓ Xác nhận đóng ngày' : 'Tiếp theo →' }}</button>
        </div>
      </section>
    </div>

    <div v-if="toastText" class="dayclose-toast" role="status">{{ toastText }}</div>
  </section>
</template>

<style scoped>
/* Values follow TemplateHTML/EndOfDay/index.html (v1.0) one-to-one. */
.dayclose-page{color:#172033}
.dayclose-panel{overflow:hidden;border:1px solid #dce7ed;border-radius:10px;background:#fff;box-shadow:0 2px 8px #122d410a}
.dayclose-head{display:flex;align-items:flex-start;justify-content:space-between;gap:12px;padding:14px 16px 10px}
.dayclose-title h1{margin:0;font-size:22px;font-weight:700;letter-spacing:-.03em}
.dayclose-title .dayclose-report-title{margin-top:4px}
.dayclose-title p{margin:4px 0 0;color:#68778c;font-size:10px}
.dayclose-primary{height:36px;flex:none;border:0;border-radius:8px;background:linear-gradient(#0ba46e,#078c5f);padding:0 15px;color:#fff;font-size:10px;font-weight:800;box-shadow:0 6px 14px #08976530;cursor:pointer}
.dayclose-primary:disabled{opacity:.55;cursor:not-allowed}
.dayclose-row-primary{height:30px}
.dayclose-secondary{height:34px;border:1px solid #cbd8e2;border-radius:7px;background:#fff;padding:0 12px;color:#33445b;font-size:9.5px;font-weight:700;cursor:pointer}
.dayclose-secondary:hover{background:#f8fbfd}
.dayclose-status{display:inline-flex;align-items:center;gap:5px;border-radius:999px;padding:4px 8px;font-size:8.5px;font-weight:750}
.dayclose-status::before{width:6px;height:6px;border-radius:50%;background:currentColor;content:""}
.dayclose-status.closed{background:#e7f8ef;color:#087c58}
.dayclose-status.open{background:#eef6ff;color:#3979c7}
.dayclose-inline-status{margin-left:8px}
.dayclose-list-note{margin:0 16px 10px}
.dayclose-table-wrap{overflow:hidden;margin:0 16px;border:1px solid #dce7ed;border-radius:8px}
.dayclose-table{width:100%;border-collapse:collapse;font-size:9.5px}
.dayclose-table th{border-bottom:1px solid #dce7ed;background:#f7fafc;padding:8px;color:#627187;font-size:9px;font-weight:700;text-align:left}
.dayclose-table td{border-bottom:1px solid #edf2f4;padding:8px}
.dayclose-table tbody tr:last-child td{border-bottom:0}
.dayclose-table tbody tr:hover{background:#fbfdfd}
.dayclose-table tr.today{background:#f0faf6}
.dayclose-sort{border:0;background:none;padding:0;color:inherit;font-weight:inherit;cursor:pointer}
.dayclose-footer{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:12px 16px;color:#67768a;font-size:9.5px}
.dayclose-pager{display:flex;flex-wrap:wrap;align-items:center;gap:5px}
.dayclose-pager button{width:30px;height:30px;border:1px solid #dce7ed;border-radius:7px;background:#fff;cursor:pointer}
.dayclose-pager button.active{background:#e9f7f2;color:#07805a}
.dayclose-pager button:disabled{cursor:default}
.dayclose-pager select{height:30px;border:1px solid #dce7ed;border-radius:7px;background:#fff;padding:0 8px;font-size:9px}
.dayclose-state{display:grid;min-height:220px;place-content:center;justify-items:center;gap:8px;margin:0 16px 16px;color:#67768a;font-size:10px;text-align:center}
.dayclose-state h3{margin:0;color:#26364d;font-size:13px;font-weight:800}
.dayclose-state p{margin:0}

/* Report */
.dayclose-backlink{border:0;background:transparent;padding:0;color:#1777cb;font-size:9px;cursor:pointer}
.dayclose-meta{color:#67768a;font-size:9px;line-height:1.6;text-align:right}
.dayclose-report{padding:0 16px 16px}
.dayclose-report-top{display:flex;align-items:center;justify-content:space-between;padding:2px 0 12px}
.dayclose-date-nav{display:flex;align-items:center;gap:7px}
.dayclose-date-nav button{width:30px;height:30px;border:1px solid #dce7ed;border-radius:7px;background:#fff;cursor:pointer}
.dayclose-date-nav button:disabled{opacity:.45;cursor:default}
.dayclose-date-pill{display:flex;height:30px;align-items:center;border:1px solid #dce7ed;border-radius:7px;background:#fff;padding:0 16px;font-size:10px;font-weight:750}
input.dayclose-date-pill{padding:0 10px}
.dayclose-kpis{display:grid;grid-template-columns:repeat(3,1fr);gap:10px}
.dayclose-kpi{display:flex;min-height:92px;flex-direction:column;align-items:flex-start;border:1px solid #dce7ed;border-radius:9px;background:#fff;padding:11px}
.dayclose-kpi .dayclose-link{margin-top:auto}
.dayclose-kpi>span{color:#67768a;font-size:9px}
.dayclose-kpi strong{display:block;margin:9px 0 12px;font-size:20px;font-weight:700}
.dayclose-kpi small{display:block;margin:-6px 0 8px;color:#67768a;font-size:9px}
.dayclose-kpi small.warn{color:#d67900}
.dayclose-kpi.red strong{color:#f03b43}
.dayclose-kpi.green strong{color:#087d58}
.dayclose-link{display:block;border:0;background:none;padding:0;color:#1777cb;font-size:9px;cursor:pointer}
.dayclose-link:hover{text-decoration:underline}
.dayclose-charts{display:grid;grid-template-columns:1fr 1.2fr;gap:12px;margin-top:12px}
.dayclose-card{border:1px solid #dce7ed;border-radius:9px;background:#fff;padding:12px}
.dayclose-card h3{margin:0 0 10px;font-size:11px;font-weight:700}
.dayclose-donut-wrap{display:flex;align-items:center;gap:16px}
.dayclose-donut{position:relative;width:150px;height:150px;flex:0 0 auto;border-radius:50%}
.dayclose-donut::after{position:absolute;inset:34px;border-radius:50%;background:#fff;content:""}
.dayclose-donut-label{position:absolute;z-index:2;inset:0;display:grid;place-items:center;font-size:9px;text-align:center}
.dayclose-donut-label b{font-size:13px}
.dayclose-legend{flex:1}
.dayclose-legend-row{display:grid;grid-template-columns:12px 1fr auto;align-items:center;gap:7px;padding:7px 0;font-size:9px}
.dayclose-swatch{width:9px;height:9px;border-radius:3px}
.dayclose-bars{display:grid;gap:11px}
.dayclose-bar-row{display:grid;grid-template-columns:72px 1fr 110px;align-items:center;gap:8px;font-size:9px}
.dayclose-track{overflow:hidden;height:16px;border-radius:3px;background:#eef2f5}
.dayclose-fill{height:100%;border-radius:3px}
.dayclose-chart-empty{display:grid;min-height:150px;place-items:center;margin:0;color:#67768a;font-size:10px}
.dayclose-detail-grid{display:grid;grid-template-columns:1.05fr 1fr 1fr;gap:12px;margin-top:12px}
.dayclose-detail-grid.two{grid-template-columns:1fr 1fr;margin-top:10px}
.dayclose-mini-table{width:100%;border-collapse:collapse;font-size:9px}
.dayclose-mini-table th{border-bottom:1px solid #dce7ed;background:#f7fafc;padding:7px;color:#627187;font-weight:700;text-align:left}
.dayclose-mini-table td{border-bottom:1px solid #edf2f4;padding:7px}
.dayclose-mini-table td.num{font-weight:700;text-align:right}
.dayclose-mini-table tr.total td{font-weight:800}
.good{color:#079b67!important}
.bad{color:#ed3740!important}
.dayclose-info-note{border:1px solid #d8eaf9;border-radius:7px;background:#eef7ff;padding:9px;color:#35617f;font-size:9px;line-height:1.5}
.dayclose-warn-note{border:1px solid #f2ddaa;border-radius:7px;background:#fff5df;padding:9px;color:#8c620b;font-size:9px;line-height:1.5}
.spaced{margin-top:10px}
.dayclose-text{margin:0;font-size:10px;line-height:1.6}
.dayclose-muted-text{margin:10px 0;color:#68778c;font-size:10px}
.dayclose-block+.dayclose-block{margin-top:10px}
.dayclose-block>.dayclose-muted-text{margin:0}

/* Drawers and wizard */
.dayclose-overlay{position:fixed;z-index:75;inset:0;background:transparent}
.dayclose-drawer{position:fixed;z-index:80;top:var(--app-topbar-height,0px);right:0;bottom:0;display:flex;width:min(510px,100vw);flex-direction:column;border-left:1px solid #dce7ed;background:#fff;box-shadow:-12px 0 28px #1e364d1a}
.dayclose-drawer-head,.dayclose-modal-head{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #dce7ed;padding:14px 16px}
.dayclose-drawer-head h2{margin:0;font-size:17px;font-weight:700}
.dayclose-x{width:30px;height:30px;border:0;background:transparent;color:#48576c;font-size:20px;cursor:pointer}
.dayclose-drawer-body{flex:1;overflow:auto;padding:14px 16px}
.dayclose-drawer-foot,.dayclose-modal-foot{display:flex;justify-content:flex-end;gap:8px;border-top:1px solid #dce7ed;padding:12px 16px}
.dayclose-ex-section{margin-bottom:9px;border:1px solid #dce7ed;border-radius:8px;padding:11px}
.dayclose-ex-section h3{margin:0 0 7px;font-size:11px;font-weight:700}
.dayclose-formula{display:grid;grid-template-columns:1fr auto 1fr auto 1fr;align-items:center;gap:7px}
.dayclose-formula-box{border-radius:7px;padding:10px;font-size:9px;text-align:center}
.dayclose-formula-box small{display:block;margin-top:3px;font-size:8px;opacity:.8}
.dayclose-formula-box.blue{background:#edf6ff;color:#245f9f}
.dayclose-formula-box.red{background:#ffeded;color:#c5333b}
.dayclose-formula-box.green{background:#e9f8f2;color:#0a7a57}
.dayclose-modal-layer{position:fixed;z-index:90;inset:0;display:grid;place-items:center;background:#12202d45;padding:18px}
.dayclose-modal{display:flex;width:min(720px,95vw);max-height:94vh;flex-direction:column;overflow:hidden;border-radius:12px;background:#fff;box-shadow:0 20px 55px #152d433d}
.dayclose-modal-head h2{margin:0;font-size:16px;font-weight:700}
.dayclose-modal-body{overflow:auto;padding:14px 16px}
.dayclose-steps{display:flex;width:100%;align-items:center;margin-bottom:16px}
.dayclose-step{display:flex;flex:1;align-items:center;gap:7px;color:#8d9aad;font-size:9px;font-weight:700}
.dayclose-step::after{height:1px;flex:1;margin:0 8px;background:#d8e0e7;content:""}
.dayclose-step:last-child{flex:0 0 auto}
.dayclose-step:last-child::after{display:none}
.dayclose-step .n{display:grid;width:20px;height:20px;place-items:center;border-radius:50%;background:#ccd5e1;color:#fff}
.dayclose-step.active{color:#07885f}
.dayclose-step.active .n,.dayclose-step.done .n{background:#079b68}
.dayclose-confirm-title{margin:0 0 12px;font-size:14px;font-weight:700}
.dayclose-confirm-table{width:100%;border-collapse:collapse;font-size:10px}
.dayclose-confirm-table td{border-bottom:1px solid #edf2f4;padding:7px}
.dayclose-confirm-table td:last-child{font-weight:750;text-align:right}
.dayclose-confirm-table td.strong{font-weight:850}
.dayclose-field{display:grid;gap:5px;margin-top:10px}
.dayclose-field span{font-size:9.5px;font-weight:750}
.dayclose-textarea{width:100%;min-height:70px;border:1px solid #cbd8e2;border-radius:7px;padding:8px;font-size:10px;resize:vertical}
.dayclose-toast{position:fixed;z-index:200;bottom:22px;left:50%;border-radius:8px;background:#172033;padding:9px 14px;color:#fff;font-size:10px;transform:translateX(-50%)}

@media(max-width:980px){.dayclose-kpis{grid-template-columns:repeat(2,1fr)}.dayclose-charts,.dayclose-detail-grid{grid-template-columns:1fr}}
@media(max-width:700px){
  .dayclose-head{flex-direction:column;gap:10px}
  .dayclose-meta{text-align:left}
  .dayclose-table-wrap{overflow-x:auto}
  .dayclose-table{min-width:520px}
  .dayclose-footer{flex-direction:column;align-items:flex-start}
  .dayclose-donut-wrap{flex-direction:column;align-items:stretch}
  .dayclose-donut{margin:0 auto}
  .dayclose-formula{grid-template-columns:1fr}
  .dayclose-drawer{width:100vw}
  .dayclose-steps{overflow:auto}
  .dayclose-step{flex:0 0 auto}
  .dayclose-step::after{width:35px;flex:0 0 35px}
  .dayclose-detail-grid.two{grid-template-columns:1fr}
}
</style>
