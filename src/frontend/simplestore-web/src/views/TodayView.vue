<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import type { RouteLocationRaw } from 'vue-router'
import { apiRequest } from '../api/client'
import { recordC14EventBestEffort } from '../api/c14Telemetry'
import type { C14AttentionItem, C14AttentionKind, C14AttentionList, CostReliability, TodayExplanation, TodayMetricId, TodaySourceNavigation, TodaySummary } from '../api/types'
import LineIcon from '../components/ui/LineIcon'
import type { IconName } from '../components/ui/icons'
import {
  count, demoChart, demoDebt, demoDrawer, demoHeader, demoProducts, demoTotals, demoTransactions, displayDate, money, referenceCategories,
  referenceDate, referenceStock, referenceTrends, shiftOptions, todayDemoDate, todayDemoEnabled, todayLiveDate, weekdayLabel,
  type ChartMetric, type PaymentKind, type Shift, type TodayDrawer,
} from '../today/demo'

/**
 * Live data shows only what /api/today supports. The reference's date history, shifts, comparisons, hourly chart,
 * categories, best sellers and opening state are illustrations (D-096) and exist only in the preview.
 */
interface Kpi { key: string; label: string; icon: IconName; tone: 'green' | 'blue' | 'orange'; value: string; trend?: string; note: string; warn?: boolean; info?: boolean; open: string }

const demo = todayDemoEnabled
const router = useRouter()

/* Live */
const summary = ref<TodaySummary | null>(null)
const attention = ref<C14AttentionList | null>(null)
const explanation = ref<TodayExplanation | null>(null)
const loading = ref(!demo.value)
const attentionLoading = ref(!demo.value)
const explanationLoading = ref(false)
const message = ref('')
const attentionMessage = ref('')
const explanationMessage = ref('')
const signalAttempts = new Set<string>()
const observedItems = new Map<Element, C14AttentionItem>()
let visibilityObserver: IntersectionObserver | null = null
let todayOpenedAttempted = false
let explanationSequence = 0

/* Preview */
const shift = ref<Shift>('morning')
const chartMetric = ref<ChartMetric>('revenue')
const category = ref<string | null>(null)
const selectedBar = ref<number | null>(null)
const storeClosed = ref(false)

const drawerType = ref<string | null>(null)
const toastText = ref('')
const now = ref(new Date())
let toastTimer: ReturnType<typeof setTimeout> | undefined
let clockTimer: ReturnType<typeof setInterval> | undefined

const attentionLabels: Record<C14AttentionKind, string> = {
  NegativeStock: 'Tồn kho đang âm',
  OutOfStock: 'Đã hết hàng',
  LowStockRisk: 'Có nguy cơ sắp hết hàng',
}
const metricLabels: Record<TodayMetricId, string> = {
  revenue: 'Doanh thu',
  collected: 'Tiền thu thuần',
  'estimated-gross-profit': 'Lãi gộp ước tính',
  'sale-count': 'Số hóa đơn',
  'customer-debt-created': 'Công nợ khách mới phát sinh',
  'supplier-debt-created': 'Công nợ nhà cung cấp mới phát sinh',
}
const reliabilityLabels: Record<CostReliability, string> = { Reliable: 'Tin cậy', Estimated: 'Ước tính', Unavailable: 'Không khả dụng' }
/** Wording follows D-049/D-050/D-053/D-069/D-070; the backend computes every figure. */
const metricExplanations: Record<TodayMetricId, { title: string; definition: string; formula?: string[]; bullets: string[] }> = {
  revenue: {
    title: 'Cách tính doanh thu',
    definition: 'Doanh thu là giá trị các đơn bán hoàn tất trong ngày kinh doanh, theo ngày phát sinh của bán hàng, trả hàng và hủy giao dịch.',
    formula: ['Doanh thu = Tổng giá trị Sale hoàn tất', '− Giá trị Return', '− Giá trị Sale bị Void'],
    bullets: ['Gồm đơn bán trả bằng tiền mặt, chuyển khoản hoặc công nợ.', 'Thu nợ khách hàng là tiền thu, không tạo doanh thu.'],
  },
  collected: {
    title: 'Cách tính tiền thu thuần',
    definition: 'Tiền thu thuần là số tiền thực nhận trong ngày; khác với doanh thu.',
    formula: ['Tiền thu thuần = Thanh toán đơn bán', '+ Thu nợ khách hàng', '− Hoàn tiền khách hàng'],
    bullets: ['Phần bán chịu chưa thu không được tính.', 'Thu nợ khách hàng được tính vào ngày nhận tiền.'],
  },
  'estimated-gross-profit': {
    title: 'Cách tính lãi gộp ước tính',
    definition: 'Lãi gộp ước tính dựa trên giá vốn lịch sử của từng dòng bán tại thời điểm bán.',
    formula: ['Lãi gộp ước tính = Doanh thu thuần', '− Giá vốn lịch sử'],
    bullets: ['Không phải lợi nhuận kế toán; chưa trừ chi phí vận hành.', 'Độ tin cậy phụ thuộc vào giá vốn của hàng đã bán.'],
  },
  'sale-count': {
    title: 'Cách đếm số hóa đơn',
    definition: 'Số hóa đơn là số đơn bán hoàn tất trong ngày kinh doanh.',
    bullets: ['Đơn bị hủy cùng ngày không được tính.', 'Trả hàng không làm giảm số hóa đơn.', 'Hủy đơn của ngày trước không làm số này âm.'],
  },
  'customer-debt-created': {
    title: 'Cách tính công nợ khách mới',
    definition: 'Nghĩa vụ nợ mới của khách hàng phát sinh từ đơn bán trong ngày.',
    bullets: ['Không phải tổng dư nợ của khách hàng.', 'Thu nợ khách hàng không làm giảm chỉ số này.', 'Trả hàng hoặc hủy cùng ngày làm giảm phần nợ của đơn đó.'],
  },
  'supplier-debt-created': {
    title: 'Cách tính công nợ NCC mới',
    definition: 'Nghĩa vụ nợ mới với nhà cung cấp phát sinh từ phiếu nhập trong ngày.',
    bullets: ['Không phải tổng dư nợ nhà cung cấp.', 'Trả nợ nhà cung cấp không làm giảm chỉ số này.'],
  },
}

function storeTime(instant: Date | string, timeZone: string) {
  return new Intl.DateTimeFormat('en-GB', { timeZone, hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).format(new Date(instant))
}

/* Header and KPIs */
const selection = computed(() => ({ date: todayDemoDate.value, shift: shift.value }))
const subtitle = computed(() => {
  if (demo.value) return demoHeader(selection.value)
  const current = summary.value
  if (!current) return ''
  const end = now.value.getTime() < Date.parse(current.endUtc) ? storeTime(now.value, current.timeZoneId) : '24:00'
  return `${weekdayLabel(current.businessDate)}, ${displayDate(current.businessDate)}  (00:00 – ${end})`
})
const scopeHint = computed(() => demo.value ? 'Theo múi giờ của cửa hàng' : summary.value ? `Múi giờ cửa hàng: ${summary.value.timeZoneId}` : '')
const kpis = computed<Kpi[]>(() => {
  if (demo.value) {
    const totals = demoTotals(selection.value)
    const note = 'so với hôm qua'
    return [
      { key: 'revenue', label: 'Doanh thu', icon: 'register', tone: 'green', value: money(totals.revenue), trend: referenceTrends.revenue, note, info: true, open: 'revenue' },
      { key: 'invoices', label: 'Số hóa đơn', icon: 'receipt', tone: 'blue', value: count(totals.invoices), trend: referenceTrends.invoices, note, open: 'invoices' },
      { key: 'units', label: 'Số sản phẩm bán', icon: 'shoppingBag', tone: 'orange', value: count(totals.units), trend: referenceTrends.units, note, open: 'units' },
      { key: 'profit', label: 'Lợi nhuận ước tính', icon: 'coins', tone: 'orange', value: money(totals.profit), trend: referenceTrends.profit, note, open: 'profit' },
    ]
  }
  const current = summary.value
  if (!current) return []
  const reliability = current.estimatedGrossProfit.costReliability
  return [
    { key: 'revenue', label: metricLabels.revenue, icon: 'register', tone: 'green', value: money(current.salesRevenue), note: 'Đơn bán hoàn tất, đã trừ trả hàng', info: true, open: 'metric:revenue' },
    { key: 'sale-count', label: metricLabels['sale-count'], icon: 'receipt', tone: 'blue', value: count(current.saleCount), note: 'Đơn bán hoàn tất trong ngày', open: 'metric:sale-count' },
    { key: 'collected', label: metricLabels.collected, icon: 'money', tone: 'orange', value: money(current.netCollected), note: 'Thanh toán + thu nợ − hoàn tiền', open: 'metric:collected' },
    {
      key: 'estimated-gross-profit', label: metricLabels['estimated-gross-profit'], icon: 'coins', tone: 'orange', value: money(current.estimatedGrossProfit.amount),
      note: reliability === 'Unavailable' ? 'Giá vốn chưa đủ tin cậy' : `Độ tin cậy giá vốn: ${reliabilityLabels[reliability]}`, warn: reliability === 'Unavailable',
      open: 'metric:estimated-gross-profit',
    },
  ]
})

/* Preview panels */
const chart = computed(() => demoChart(selection.value, chartMetric.value))
const products = computed(() => demoProducts(selection.value).slice(0, 5))
const transactions = computed(() => demoTransactions(selection.value).slice(0, 5))
const debt = computed(() => demoDebt(selection.value))
const donutGradient = (() => {
  let start = 0
  return `conic-gradient(${referenceCategories.map(item => { const stop = `${item.color} ${start}% ${start + item.pct}%`; start += item.pct; return stop }).join(',')})`
})()
const donut = computed(() => {
  const revenue = demoTotals(selection.value).revenue
  const picked = referenceCategories.find(item => item.name === category.value)
  return { value: money(picked ? Math.round(revenue * picked.pct / 100) : revenue), caption: picked ? picked.name : 'Tổng doanh thu' }
})
const paymentClass = (payment: PaymentKind) => payment === 'Tiền mặt' ? 'cash' : payment === 'Công nợ' ? 'debt' : 'transfer'

/* Drawer */
const drawer = computed<TodayDrawer | null>(() => {
  const type = drawerType.value
  if (!type) return null
  if (demo.value) return demoDrawer(type, selection.value)
  return type.startsWith('metric:') ? liveDrawer(type.slice(7) as TodayMetricId) : null
})

function liveDrawer(metric: TodayMetricId): TodayDrawer {
  const copy = metricExplanations[metric]
  const current = summary.value
  const blocks: TodayDrawer['blocks'] = [{ kind: 'card', icon: 'receipt', title: '1. Định nghĩa', text: [copy.definition] }]
  let step = 2
  if (copy.formula) blocks.push({ kind: 'card', icon: 'register', title: `${step++}. Công thức`, formula: copy.formula })
  if (metric === 'estimated-gross-profit' && current) {
    const profit = current.estimatedGrossProfit
    blocks.push({ kind: 'card', icon: 'coins', title: `${step++}. Giá vốn`, pairs: [
      ['Doanh thu thuần', money(profit.netSalesRevenue)], ['Giá vốn lịch sử', money(profit.historicalCogs)], ['Độ tin cậy giá vốn', reliabilityLabels[profit.costReliability]],
    ] })
  }
  if (current) {
    blocks.push({ kind: 'card', icon: 'calendar', title: `${step++}. Phạm vi thời gian`, tone: 'blue',
      text: [`Từ 00:00 đến hiện tại, ngày ${displayDate(current.businessDate)} (múi giờ ${current.timeZoneId}).`] })
  }
  blocks.push({ kind: 'card', icon: 'checkCircle', title: `${step++}. Lưu ý khi đọc`, bullets: copy.bullets })
  blocks.push({ kind: 'sources' })
  blocks.push({ kind: 'notice', title: 'Lưu ý', text: 'Số liệu có thể thay đổi nếu có giao dịch được hoàn tất, trả hàng hoặc hủy trong ngày.' })
  return { title: copy.title, blocks }
}

function sourceRoute(navigation: TodaySourceNavigation): RouteLocationRaw {
  const type = navigation.type
  switch (type) {
    case 'Sale':
      return { name: 'sale-detail', params: { id: navigation.id } }
    case 'Return':
      return { name: 'return-detail', params: { id: navigation.id } }
    case 'Purchase':
      return { name: 'purchase-detail', params: { id: navigation.id } }
    default:
      return assertNever(type)
  }
}

function assertNever(value: never): never {
  throw new Error(`Unsupported Today source navigation type: ${String(value)}`)
}

async function loadSummary() {
  loading.value = true
  message.value = ''
  try {
    summary.value = await apiRequest<TodaySummary>('/api/today')
    todayLiveDate.value = summary.value.businessDate
    await nextTick()
    if (!todayOpenedAttempted) {
      todayOpenedAttempted = true
      void recordC14EventBestEffort('TodayOpened')
    }
  } catch (reason) {
    message.value = reason instanceof Error ? reason.message : 'Không thể tải thông tin hôm nay.'
  } finally {
    loading.value = false
  }
}

async function loadAttention() {
  attentionLoading.value = true
  attentionMessage.value = ''
  try {
    attention.value = await apiRequest<C14AttentionList>('/api/today/attention?page=1&pageSize=3')
  } catch (reason) {
    attentionMessage.value = reason instanceof Error ? reason.message : 'Không thể tải các mặt hàng cần chú ý.'
  } finally {
    attentionLoading.value = false
  }
}

function signalKey(item: C14AttentionItem) {
  return `${item.productId}:${item.attentionKind}`
}

function emitSignalShown(item: C14AttentionItem) {
  const key = signalKey(item)
  if (signalAttempts.has(key)) return
  signalAttempts.add(key)
  void recordC14EventBestEffort('SignalShown', item.productId, item.attentionKind)
}

function observeAttention(element: unknown, item: C14AttentionItem) {
  if (!(element instanceof Element)) return
  observedItems.set(element, item)
  if (visibilityObserver) visibilityObserver.observe(element)
  else emitSignalShown(item)
}

function openAttention(item: C14AttentionItem) {
  void recordC14EventBestEffort('WhyOpened', item.productId, item.attentionKind)
}

async function loadExplanation(metric: TodayMetricId, page: number) {
  const sequence = ++explanationSequence
  explanationLoading.value = true
  explanationMessage.value = ''
  try {
    const result = await apiRequest<TodayExplanation>(`/api/today/explanations/${metric}?page=${page}&pageSize=20`)
    if (sequence === explanationSequence) explanation.value = result
  } catch (reason) {
    if (sequence === explanationSequence) explanationMessage.value = reason instanceof Error ? reason.message : 'Không thể tải dữ liệu giải thích.'
  } finally {
    if (sequence === explanationSequence) explanationLoading.value = false
  }
}

function changeExplanationPage(page: number) {
  if (!explanation.value || page < 1 || page > explanation.value.totalPages) return
  void loadExplanation(explanation.value.metric, page)
}

function openDrawer(type: string) {
  drawerType.value = type
  if (!demo.value && type.startsWith('metric:')) {
    explanation.value = null
    void loadExplanation(type.slice(7) as TodayMetricId, 1)
  }
}
function closeDrawer() {
  ++explanationSequence
  drawerType.value = null
  explanationLoading.value = false
}
function toast(text: string) {
  toastText.value = text
  if (toastTimer) clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toastText.value = '' }, 2800)
}
function toggleStoreState() {
  storeClosed.value = !storeClosed.value
  toast(storeClosed.value ? 'Đã chuyển trạng thái cửa hàng sang đóng cửa.' : 'Cửa hàng đang mở cửa.')
}
function toggleCategory(name: string) { category.value = category.value === name ? null : name }
function toggleBar(hour: number) { selectedBar.value = selectedBar.value === hour ? null : hour }
function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') closeDrawer()
  if (event.key === 'F12') {
    event.preventDefault()
    void router?.push('/sales/new')
  }
}
function loadLive() {
  void loadSummary()
  void loadAttention()
}

watch(demo, enabled => {
  closeDrawer()
  todayDemoDate.value = referenceDate
  shift.value = 'morning'
  chartMetric.value = 'revenue'
  category.value = null
  selectedBar.value = null
  storeClosed.value = false
  if (!enabled && !summary.value && !loading.value) loadLive()
})
watch(todayDemoDate, () => { category.value = null })
watch(shift, () => { category.value = null; selectedBar.value = null })
watch(chartMetric, () => { selectedBar.value = null })

onMounted(() => {
  if (typeof IntersectionObserver !== 'undefined') {
    visibilityObserver = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue
        const item = observedItems.get(entry.target)
        if (item) emitSignalShown(item)
        visibilityObserver?.unobserve(entry.target)
      }
    })
  }
  window.addEventListener('keydown', onKeydown)
  clockTimer = setInterval(() => { now.value = new Date() }, 60_000)
  if (!demo.value) loadLive()
})

onBeforeUnmount(() => {
  visibilityObserver?.disconnect()
  window.removeEventListener('keydown', onKeydown)
  if (toastTimer) clearTimeout(toastTimer)
  if (clockTimer) clearInterval(clockTimer)
  todayLiveDate.value = ''
})
</script>

<template>
  <section class="today-page" :class="{ 'drawer-open': drawer }">
    <section class="today-heading-row">
      <div class="today-heading">
        <h1>Tổng quan hôm nay</h1>
        <div v-if="subtitle" class="today-subtitle">{{ subtitle }} <span class="today-subtitle-info" :title="scopeHint" :aria-label="scopeHint" role="img"><LineIcon name="info" /></span></div>
      </div>
      <div class="today-heading-tools">
        <template v-if="demo">
          <button class="today-status" :class="{ closed: storeClosed }" type="button" :aria-pressed="!storeClosed" @click="toggleStoreState"><span class="today-status-dot" />{{ storeClosed ? 'Đã đóng cửa' : 'Đang mở cửa' }}</button>
          <label class="today-shift"><span>Ca làm việc</span><select v-model="shift" aria-label="Ca làm việc"><option v-for="option in shiftOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select><LineIcon name="chevron" /></label>
        </template>
        <RouterLink class="today-sale-btn" to="/sales/new"><LineIcon name="todayCart" /><span>Bán hàng</span><kbd>F12</kbd></RouterLink>
      </div>
    </section>

    <p v-if="!demo && loading" class="today-state" role="status">Đang tải thông tin hôm nay…</p>
    <div v-if="!demo && message" class="today-error" role="alert">{{ message }} <button class="today-link-button" type="button" @click="loadSummary">Thử lại</button></div>

    <section v-if="kpis.length" class="today-cards" aria-label="Chỉ số chính">
      <button v-for="card in kpis" :key="card.key" class="today-kpi" :class="card.tone" type="button" @click="openDrawer(card.open)">
        <span class="today-kpi-icon"><LineIcon :name="card.icon" /></span>
        <span class="today-kpi-copy">
          <span class="today-kpi-label">{{ card.label }}<LineIcon v-if="card.info" name="info" /></span>
          <span class="today-kpi-value">{{ card.value }}</span>
          <span class="today-kpi-trend" :class="{ warn: card.warn }"><strong v-if="card.trend">{{ card.trend }}</strong> {{ card.note }}</span>
        </span>
      </button>
    </section>
    <p v-if="!demo && summary?.estimatedGrossProfit.costReliability === 'Unavailable'" class="today-warn-note">Dữ liệu giá vốn chưa đủ tin cậy; đây không phải lợi nhuận kế toán.</p>

    <!-- Preview: the reference dashboard -->
    <template v-if="demo">
      <section class="today-analytics">
        <article class="today-panel">
          <div class="today-panel-heading">
            <h2 class="today-panel-title">Doanh thu theo giờ <button class="today-info" type="button" aria-label="Giải thích biểu đồ doanh thu theo giờ" @click="openDrawer('hourInfo')"><LineIcon name="info" /></button></h2>
            <select v-model="chartMetric" class="today-chart-select" aria-label="Chỉ số biểu đồ"><option value="revenue">Doanh thu</option><option value="invoices">Hóa đơn</option><option value="units">Sản phẩm</option></select>
          </div>
          <div class="today-chart-area">
            <div class="today-y-axis"><span v-for="(tick, index) in chart.ticks" :key="index">{{ tick }}</span></div>
            <div class="today-plot">
              <div class="today-gridlines" aria-label="Biểu đồ theo giờ">
                <button v-for="bar in chart.bars" :key="bar.hour" class="today-bar" :class="{ selected: selectedBar === bar.hour }" type="button" :style="{ '--bar-height': `${bar.height}%` }" :aria-label="bar.tip.replace(' · ', ', ')" @click="toggleBar(bar.hour)"><span class="today-bar-tip">{{ bar.tip }}</span></button>
              </div>
              <div class="today-x-axis"><span v-for="bar in chart.bars" :key="bar.hour">{{ bar.axis }}</span></div>
            </div>
          </div>
        </article>
        <article class="today-panel">
          <div class="today-panel-heading"><h2 class="today-panel-title">Cơ cấu doanh thu <button class="today-info" type="button" aria-label="Giải thích cơ cấu doanh thu" @click="openDrawer('structureInfo')"><LineIcon name="info" /></button></h2></div>
          <div class="today-donut-wrap">
            <div class="today-donut" :class="{ focused: category }" :style="{ background: donutGradient }" role="img" aria-label="Biểu đồ cơ cấu doanh thu"><div class="today-donut-center"><strong>{{ donut.value }}</strong><span>{{ donut.caption }}</span></div></div>
            <div class="today-legend">
              <button v-for="item in referenceCategories" :key="item.name" class="today-legend-item" :class="{ selected: category === item.name }" type="button" :aria-pressed="category === item.name" @click="toggleCategory(item.name)"><span class="today-swatch" :style="{ background: item.color }" /><span class="today-legend-name">{{ item.name }}</span><span class="today-legend-pct">{{ item.pct }}%</span></button>
            </div>
          </div>
        </article>
      </section>

      <section class="today-lower">
        <article class="today-panel">
          <div class="today-panel-heading today-section-head"><h2 class="today-panel-title">Sản phẩm bán chạy <button class="today-info" type="button" aria-label="Giải thích sản phẩm bán chạy" @click="openDrawer('bestInfo')"><LineIcon name="info" /></button></h2><button class="today-link-button" type="button" @click="openDrawer('products')">Xem tất cả <LineIcon name="chevronRight" /></button></div>
          <button v-for="(item, index) in products" :key="item.sku" class="today-product-row" type="button" @click="openDrawer(`product:${index}`)">
            <span class="today-rank" :class="{ top: index < 3 }">{{ index + 1 }}</span>
            <span class="today-pack" :class="item.pack"><template v-for="(line, lineIndex) in item.short" :key="lineIndex">{{ line }}<br v-if="lineIndex < item.short.length - 1"></template></span>
            <span class="today-product-copy"><span class="today-product-name">{{ item.name }}</span><span class="today-product-meta">{{ item.sku }} | {{ item.unit }}</span></span>
            <span class="today-product-qty">{{ count(item.qty) }}</span>
            <span class="today-product-total">{{ money(item.total) }}</span>
          </button>
        </article>
        <article class="today-panel">
          <div class="today-panel-heading today-section-head"><h2 class="today-panel-title">Giao dịch gần đây</h2><button class="today-link-button" type="button" @click="openDrawer('transactions')">Xem tất cả <LineIcon name="chevronRight" /></button></div>
          <button v-for="(item, index) in transactions" :key="item.id" class="today-txn-row" type="button" @click="openDrawer(`transaction:${index}`)">
            <span class="today-txn-icon"><LineIcon name="money" /></span>
            <span><span class="today-txn-top"><strong>{{ item.id }}</strong><span class="today-txn-time">{{ item.time }}</span></span><span class="today-txn-meta">{{ item.items }} sản phẩm</span></span>
            <span class="today-txn-right"><span class="today-txn-amount">{{ money(item.amount) }}</span><span class="today-payment" :class="paymentClass(item.payment)"><LineIcon :name="item.payment === 'Tiền mặt' ? 'money' : 'bankBuilding'" />{{ item.payment }}</span></span>
          </button>
          <p v-if="transactions.length === 0" class="today-empty">Không có giao dịch trong ca này.</p>
        </article>
        <div class="today-right-stack">
          <article class="today-panel">
            <div class="today-panel-heading today-section-head"><h2 class="today-panel-title">Tình trạng tồn kho <button class="today-info" type="button" aria-label="Giải thích tình trạng tồn kho" @click="openDrawer('stockInfo')"><LineIcon name="info" /></button></h2><button class="today-link-button" type="button" @click="openDrawer('stock')">Xem tất cả <LineIcon name="chevronRight" /></button></div>
            <div class="today-stock-list">
              <button v-for="(item, index) in referenceStock" :key="item.label" class="today-stock-row" type="button" @click="openDrawer(`stock:${index}`)"><span class="today-stock-dot" :class="item.tone"><LineIcon :name="item.icon" /></span><span class="today-stock-label">{{ item.label }}</span><span class="today-stock-count">{{ item.count }}</span><LineIcon name="chevronRight" /></button>
            </div>
          </article>
          <article class="today-panel">
            <div class="today-panel-heading today-section-head"><h2 class="today-panel-title">Công nợ khách hàng <button class="today-info" type="button" aria-label="Giải thích công nợ khách hàng" @click="openDrawer('debtInfo')"><LineIcon name="info" /></button></h2><button class="today-link-button" type="button" @click="openDrawer('debt')">Xem tất cả</button></div>
            <div class="today-debt-body">
              <div class="today-debt-line"><span>Tổng công nợ</span><strong>{{ money(debt.amount) }}</strong></div>
              <div class="today-debt-line"><span>Số khách còn nợ</span><strong>{{ debt.customers }}</strong></div>
              <div class="today-debt-line overdue"><span>Quá hạn</span><strong>{{ debt.overdue }}</strong></div>
            </div>
          </article>
        </div>
      </section>
    </template>

    <!-- Live: C14 attention preview and today's new debt -->
    <section v-else-if="summary || attention || attentionLoading || attentionMessage" class="today-live-grid">
      <article class="today-panel" aria-labelledby="attention-heading">
        <div class="today-panel-heading today-section-head">
          <h2 id="attention-heading" class="today-panel-title">Cần chú ý</h2>
          <RouterLink v-if="attention && attention.totalAttentionCount > 3" class="today-link-button" :to="{ name: 'attention-list' }">Xem tất cả {{ attention.totalAttentionCount }} mặt hàng</RouterLink>
          <span v-else-if="attention" class="today-stock-count">{{ attention.totalAttentionCount }} mặt hàng</span>
        </div>
        <p v-if="attentionLoading" class="today-empty">Đang tải tín hiệu…</p>
        <p v-if="attentionMessage" class="today-error" role="alert">{{ attentionMessage }}</p>
        <template v-if="attention && !attentionLoading">
          <p v-if="attention.items.length === 0" class="today-empty">
            {{ attention.evaluationCoverage === 'PartialObservation'
              ? 'Chưa đủ 7 ngày lịch sử để đưa ra kết luận mạnh về nguy cơ sắp hết hàng.'
              : 'Hiện chưa có mặt hàng nào thỏa điều kiện tín hiệu C14.' }}
          </p>
          <ul v-else class="today-attention-list">
            <li v-for="item in attention.items" :key="`${item.productId}-${item.attentionKind}`" :ref="element => observeAttention(element, item)" class="today-attention-row" data-testid="attention-preview-item">
              <span class="today-stock-dot" :class="{ red: item.attentionKind !== 'LowStockRisk' }"><LineIcon :name="item.attentionKind === 'OutOfStock' ? 'shoppingBag' : 'alert'" /></span>
              <span class="today-attention-copy">
                <strong>{{ item.productName }}</strong>
                <span class="today-attention-kind" :class="item.attentionKind === 'LowStockRisk' ? 'risk' : 'fact'">{{ attentionLabels[item.attentionKind] }}</span>
                <span class="today-attention-meta">Tồn hiện tại: {{ item.currentStock }}<template v-if="item.averageDailySales !== null"> · Bán TB/ngày: {{ item.averageDailySales.toFixed(2) }}</template><template v-if="item.daysOfCover !== null"> · Chỉ số ngày tồn: {{ item.daysOfCover.toFixed(2) }}</template></span>
              </span>
              <RouterLink class="today-link-button" :to="{ name: 'attention-detail', params: { productId: item.productId } }" @click="openAttention(item)">Xem vì sao</RouterLink>
            </li>
          </ul>
        </template>
      </article>
      <article v-if="summary" class="today-panel">
        <div class="today-panel-heading today-section-head"><h2 class="today-panel-title">Công nợ phát sinh hôm nay</h2></div>
        <div class="today-debt-body">
          <button v-for="metric in (['customer-debt-created', 'supplier-debt-created'] as const)" :key="metric" class="today-debt-line today-debt-button" type="button" @click="openDrawer(`metric:${metric}`)">
            <span>{{ metricLabels[metric] }}</span><strong>{{ money(metric === 'customer-debt-created' ? summary.customerDebtCreated : summary.supplierDebtCreated) }}</strong>
          </button>
          <p class="today-debt-note">Nghĩa vụ nợ mới trong ngày, không phải tổng dư nợ. Chọn từng dòng để xem dữ liệu nguồn.</p>
        </div>
      </article>
    </section>

    <div v-if="drawer" class="today-scrim" @click="closeDrawer" />
    <aside v-if="drawer" class="today-drawer" role="dialog" :aria-label="drawer.title">
      <div class="today-drawer-head"><h2>{{ drawer.title }}</h2><button class="today-close" type="button" aria-label="Đóng" @click="closeDrawer"><LineIcon name="close" /></button></div>
      <div class="today-drawer-scroll">
        <template v-for="(block, blockIndex) in drawer.blocks" :key="`${drawerType}-${blockIndex}`">
          <p v-if="block.kind === 'intro'" class="today-detail-intro">{{ block.text }}</p>
          <section v-else-if="block.kind === 'card'" class="today-explain-card">
            <div class="today-explain-head"><span class="today-explain-icon" :class="block.tone"><LineIcon :name="block.icon" /></span><span>{{ block.title }}</span></div>
            <p v-for="line in block.text ?? []" :key="line">{{ line }}</p>
            <p v-if="block.value"><strong>{{ block.value.strong }}</strong>{{ block.value.text }}</p>
            <div v-if="block.formula" class="today-formula"><template v-for="(line, index) in block.formula" :key="index">{{ line }}<br v-if="index < block.formula.length - 1"></template></div>
            <ul v-if="block.bullets"><li v-for="line in block.bullets" :key="line">{{ line }}</li></ul>
            <div v-if="block.pairs" class="today-detail-grid"><template v-for="[label, value] in block.pairs" :key="label"><span>{{ label }}</span><strong>{{ value }}</strong></template></div>
            <button v-if="block.action" class="today-drawer-action" type="button" @click="openDrawer(block.action.open)"><LineIcon :name="block.action.icon" />{{ block.action.label }}</button>
          </section>
          <div v-else-if="block.kind === 'list'" class="today-detail-list">
            <button v-for="item in block.items" :key="item.open" class="today-detail-item" type="button" @click="openDrawer(item.open)"><strong>{{ item.title }}</strong><span>{{ item.text }}</span></button>
          </div>
          <div v-else-if="block.kind === 'detail'" class="today-detail-card">
            <h3>{{ block.title }}</h3>
            <p v-if="block.intro" class="today-detail-intro">{{ block.intro }}</p>
            <div class="today-detail-grid"><template v-for="[label, value] in block.pairs" :key="label"><span>{{ label }}</span><strong>{{ value }}</strong></template></div>
          </div>
          <button v-else-if="block.kind === 'action'" class="today-drawer-action" type="button" @click="openDrawer(block.open)"><LineIcon :name="block.icon" />{{ block.label }}</button>
          <div v-else-if="block.kind === 'notice'" class="today-notice"><LineIcon name="info" /><span><strong>{{ block.title }}</strong><br>{{ block.text }}</span></div>
          <section v-else class="today-explain-card" aria-live="polite">
            <div class="today-explain-head"><span class="today-explain-icon blue"><LineIcon name="list" /></span><span v-if="explanation">Dữ liệu nguồn · {{ explanation.totalCount }} mục</span><span v-else>Dữ liệu nguồn</span></div>
            <p v-if="explanationLoading">Đang tải dữ liệu nguồn…</p>
            <p v-if="explanationMessage" class="today-error" role="alert">{{ explanationMessage }}</p>
            <template v-if="explanation && !explanationLoading">
              <p v-if="explanation.items.length === 0">Chưa có dữ liệu nguồn trong ngày này.</p>
              <ul v-else class="today-source-list">
                <li v-for="item in explanation.items" :key="`${item.sourceType}-${item.sourceId}`" class="today-source-item">
                  <div class="today-source-top">
                    <span><strong>{{ item.title }}</strong><small>{{ item.sourceType }} · {{ item.sourceId }}</small></span>
                    <strong v-if="item.contributionAmount !== null">{{ money(item.contributionAmount) }}</strong>
                    <strong v-else-if="item.contributionCount !== null">{{ item.contributionCount }}</strong>
                  </div>
                  <div v-if="item.debtContribution" class="today-source-debt">
                    <span>Tổng giao dịch: {{ money(item.debtContribution.originalTotal) }}</span>
                    <span>Thanh toán trực tiếp: {{ money(item.debtContribution.directPayments) }}</span>
                    <span>Nghĩa vụ cơ sở: {{ money(item.debtContribution.baseDebt) }}</span>
                    <span>Return cùng ngày: {{ money(item.debtContribution.sameDayReturnObligationReduction) }}</span>
                    <span>Void cùng ngày: {{ item.debtContribution.sameDayVoided ? 'Có' : 'Không' }}</span>
                    <span>Đóng góp cuối: {{ money(item.debtContribution.finalContribution) }}</span>
                  </div>
                  <RouterLink v-if="item.navigation" class="today-link-button" :to="sourceRoute(item.navigation)">Xem giao dịch nguồn</RouterLink>
                </li>
              </ul>
              <div v-if="explanation.totalPages > 1" class="today-source-pager">
                <button class="today-drawer-action" type="button" :disabled="explanation.page <= 1" @click="changeExplanationPage(explanation.page - 1)">Trang trước</button>
                <span>Trang {{ explanation.page }}/{{ explanation.totalPages }}</span>
                <button class="today-drawer-action" type="button" :disabled="explanation.page >= explanation.totalPages" @click="changeExplanationPage(explanation.page + 1)">Trang sau</button>
              </div>
            </template>
          </section>
        </template>
      </div>
    </aside>
    <div v-if="toastText" class="today-toast" role="status" aria-live="polite">{{ toastText }}</div>
  </section>
</template>

<style scoped>
/* Values follow TemplateHTML/Today/index.html (v1.0) one-to-one. */
.today-page{color:#101838;transition:margin-right .24s ease}
.today-heading-row{display:flex;min-height:83px;align-items:flex-start;justify-content:space-between;gap:12px}
.today-heading h1{margin:0 0 5px;font-size:27px;font-weight:800;line-height:1.16;letter-spacing:-1.2px}
.today-subtitle{display:flex;align-items:center;gap:6px;color:#455581;font-size:14px}
.today-subtitle-info{display:inline-flex}
.today-subtitle-info .line-icon{width:16px;height:16px}
.today-heading-tools{display:flex;align-items:center;gap:10px}
.today-heading-tools>*{flex:none}
.today-status{display:flex;height:42px;align-items:center;gap:9px;border:1px solid #cfeee3;border-radius:8px;background:#e4f6ef;padding:0 12px;color:#007a56;font-size:13px;white-space:nowrap;cursor:pointer}
.today-status-dot{width:12px;height:12px;border-radius:50%;background:#00986b}
.today-status.closed{border-color:#e4e9ef;background:#f5f6f8;color:#66748a}
.today-status.closed .today-status-dot{background:#8c9aab}
.today-shift{position:relative;display:grid;width:185px;height:51px;grid-template-columns:1fr 18px;align-items:center;border:1px solid #e3ecf1;border-radius:8px;background:#fff;padding:4px 11px}
.today-shift span{grid-column:1;color:#526087;font-size:12px}
.today-shift select{grid-column:1;grid-row:2;width:100%;border:0;outline:none;background:transparent;color:#283866;font-size:12px;appearance:none;cursor:pointer}
.today-shift .line-icon{grid-column:2;grid-row:1/3;width:16px;pointer-events:none}
.today-sale-btn{display:flex;white-space:nowrap;height:51px;min-width:193px;align-items:center;justify-content:space-between;gap:18px;border-radius:7px;background:linear-gradient(135deg,#00966d,#00855e);padding:0 15px;color:#fff;font-weight:700;text-decoration:none;box-shadow:0 5px 13px #00895e21}
.today-sale-btn:hover{background:#007b58}
.today-sale-btn kbd{font-family:inherit;font-size:13px;font-weight:600}
.today-state{margin:0 0 14px;color:#58678d;font-size:14px}
.today-error{margin:0 0 14px;border-radius:8px;background:#fff0f1;padding:9px 11px;color:#a94448;font-size:13px}
.today-warn-note{margin:12px 0 0;border:1px solid #f2ddaa;border-radius:8px;background:#fff5df;padding:9px 11px;color:#8c620b;font-size:13px}

.today-cards{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px}
.today-kpi{display:flex;min-width:0;min-height:119px;align-items:flex-start;gap:13px;border:1px solid #e3ecf1;border-radius:11px;background:#fff;padding:16px 13px;color:inherit;text-align:left;cursor:pointer;transition:box-shadow .2s,border-color .2s,transform .2s}
.today-kpi:hover,.today-kpi:focus-visible{border-color:#96d7c1;box-shadow:0 14px 40px #1b354714;transform:translateY(-1px)}
.today-kpi-icon{display:grid;width:50px;height:53px;flex:none;place-items:center;border-radius:13px;background:#e1f6ec;color:#008f65}
.today-kpi-icon .line-icon{width:25px;height:25px}
.today-kpi.blue .today-kpi-icon{background:#eaf4ff;color:#1675ef}
.today-kpi.orange .today-kpi-icon{background:#fff4e4;color:#e98b00}
.today-kpi-copy{min-width:0;flex:1}
.today-kpi-label{display:flex;align-items:center;gap:5px;color:#293b6d;font-size:14px;white-space:nowrap}
.today-kpi-label .line-icon{width:17px;height:17px}
.today-kpi-value{display:block;margin:7px 0 6px;color:#0e1635;font-size:25px;font-weight:800;letter-spacing:-1px;white-space:nowrap}
.today-kpi-trend{display:block;overflow:hidden;color:#5c6d91;font-size:12px;text-overflow:ellipsis;white-space:nowrap}
.today-kpi-trend strong{margin-right:3px;color:#00a36c;font-size:14px}
.today-kpi-trend.warn{color:#c76e06}

.today-panel{min-width:0;border:1px solid #e3ecf1;border-radius:11px;background:#fff}
.today-panel-heading{display:flex;min-height:25px;align-items:center;justify-content:space-between;gap:8px}
.today-panel-title{display:flex;align-items:center;gap:6px;margin:0;font-size:17px;font-weight:800;letter-spacing:-.55px;white-space:nowrap}
.today-info{display:inline-grid;place-items:center;border:0;background:none;padding:0;color:#52679b;cursor:pointer}
.today-info .line-icon{width:16px;height:16px}
.today-section-head{border-bottom:1px solid #e7edf1;padding-bottom:8px}
.today-link-button{display:flex;align-items:center;gap:3px;border:0;background:transparent;padding:3px 0;color:#0059ca;font-size:12px;text-decoration:none;white-space:nowrap;cursor:pointer}
.today-link-button:hover{text-decoration:underline}
.today-link-button .line-icon{width:13px;height:13px}
.today-empty{display:flex;min-height:120px;align-items:center;justify-content:center;margin:0;color:#7381a0;font-size:13px;text-align:center}

.today-analytics{display:grid;grid-template-columns:1fr 1.03fr;gap:14px;margin-top:17px}
.today-analytics .today-panel{height:272px;padding:15px 16px}
.today-chart-select{min-width:112px;height:37px;border:1px solid #dce7ee;border-radius:7px;background:#fff;padding:0 9px;color:#1b2c5d;font-size:13px}
.today-chart-area{display:flex;height:195px;margin-top:8px}
.today-y-axis{display:flex;width:43px;flex-direction:column;justify-content:space-between;padding:4px 7px 27px 0;color:#344779;font-size:12px;text-align:right;white-space:nowrap}
.today-plot{display:flex;min-width:0;flex:1;flex-direction:column}
.today-gridlines{position:relative;display:flex;height:168px;align-items:flex-end;justify-content:space-around;gap:3px;border-bottom:1px solid #dfe8ed;background:repeating-linear-gradient(to bottom,#e9eef2 0 1px,transparent 1px 42px),repeating-linear-gradient(to right,#eef3f5 0 1px,transparent 1px 42px);padding:0 2px}
.today-bar{position:relative;height:var(--bar-height);min-height:0;max-width:22px;flex:1;border:0;border-radius:3px 3px 0 0;background:linear-gradient(180deg,#2db28e,#62d4b3);padding:0;cursor:pointer}
.today-bar:hover,.today-bar.selected{background:linear-gradient(180deg,#008f65,#2db58d)}
.today-bar:focus-visible{outline:2px solid #0a7c5c;outline-offset:2px}
.today-bar-tip{position:absolute;z-index:4;bottom:calc(100% + 9px);left:50%;display:none;border-radius:6px;background:#142342;padding:7px 9px;color:#fff;font-size:11px;white-space:nowrap;transform:translateX(-50%);box-shadow:0 6px 16px #2233}
.today-bar:hover .today-bar-tip,.today-bar:focus .today-bar-tip,.today-bar.selected .today-bar-tip{display:block}
.today-x-axis{display:flex;height:26px;align-items:flex-end;justify-content:space-around;color:#455783;font-size:11px}
.today-x-axis span{flex:1;text-align:center}
.today-donut-wrap{display:flex;height:207px;align-items:center;gap:23px}
.today-donut{position:relative;width:190px;height:190px;flex:none;border-radius:50%;box-shadow:0 0 0 2px #fff}
.today-donut.focused{filter:drop-shadow(0 3px 7px #1a375e22)}
.today-donut::after{position:absolute;inset:32px;border-radius:50%;background:#fff;content:""}
.today-donut-center{position:absolute;z-index:1;inset:48px 25px;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center}
.today-donut-center strong{font-size:16px;letter-spacing:-.6px;white-space:nowrap}
.today-donut-center span{color:#5a6b8e;font-size:12px;white-space:nowrap}
.today-legend{display:flex;min-width:0;flex:1;flex-direction:column;gap:12px}
.today-legend-item{display:flex;min-width:0;align-items:center;gap:11px;border:0;border-radius:5px;background:transparent;padding:1px 2px;color:#203365;font-size:13px;text-align:left;cursor:pointer}
.today-legend-item:hover,.today-legend-item.selected{background:#f1f8fa}
.today-swatch{width:12px;height:12px;flex:none;border-radius:50%}
.today-legend-name{overflow:hidden;flex:1;text-overflow:ellipsis;white-space:nowrap}
.today-legend-pct{font-weight:600;white-space:nowrap}

.today-lower{display:grid;grid-template-columns:1.13fr .95fr .9fr;gap:14px;margin-top:16px}
.today-lower>.today-panel,.today-right-stack .today-panel{overflow:hidden;padding:13px 13px 9px}
.today-lower>.today-panel{height:386px}
.today-product-row{display:grid;width:100%;height:64px;grid-template-columns:25px 34px minmax(0,1fr) 31px 83px;align-items:center;gap:7px;border:0;border-bottom:1px solid #edf1f3;background:#fff;padding:3px 0;color:#101838;text-align:left;cursor:pointer}
.today-product-row:last-child,.today-txn-row:last-of-type,.today-stock-row:last-child{border-bottom:0}
.today-product-row:hover,.today-txn-row:hover,.today-stock-row:hover{background:#f7fbfa}
.today-rank{display:grid;width:21px;height:21px;place-items:center;border-radius:50%;background:#8d98ad;color:#fff;font-size:12px;font-weight:700}
.today-rank.top{background:#009669}
.today-pack{position:relative;display:grid;width:27px;height:39px;place-items:center;border-radius:4px;background:linear-gradient(110deg,#ed2340,#aa0b1e);color:#fff;font-size:6px;font-weight:900;line-height:1.1;text-align:center;box-shadow:inset 3px 1px 5px #ffffff70,0 2px 4px #0002}
.today-pack::before{position:absolute;top:1px;right:3px;left:3px;height:3px;border-radius:50%;background:#eee9;content:""}
.today-pack.noodle{width:34px;height:30px;background:linear-gradient(140deg,#ef2639,#f6b329)}
.today-pack.water{width:18px;margin:auto;border-radius:5px;background:linear-gradient(90deg,#dbefff,#53a6ed,#e4f5ff);color:#1576bf}
.today-pack.milk{width:23px;background:linear-gradient(140deg,#428be9,#f3fbff,#b8e3fc);color:#13509a}
.today-pack.cookie{width:34px;height:25px;background:linear-gradient(140deg,#072b95,#1883e2);color:#fff}
.today-product-copy{min-width:0}
.today-product-name{display:block;overflow:hidden;font-size:12px;font-weight:600;text-overflow:ellipsis;white-space:nowrap}
.today-product-meta{display:block;margin-top:3px;color:#65759a;font-size:11px}
.today-product-qty,.today-product-total{font-size:13px;font-weight:700;text-align:right;white-space:nowrap}
.today-product-total{font-size:12px}
.today-txn-row{display:grid;width:100%;height:64px;grid-template-columns:36px minmax(0,1fr) auto;align-items:center;gap:8px;border:0;border-bottom:1px solid #edf1f3;background:#fff;padding:3px 0;color:#101838;text-align:left;cursor:pointer}
.today-txn-icon{display:grid;width:35px;height:37px;place-items:center;border-radius:8px;background:#dff6eb;color:#008f65}
.today-txn-icon .line-icon{width:18px;height:18px}
.today-txn-top{display:flex;align-items:center;gap:12px;font-size:12px}
.today-txn-time{color:#5b6c90}
.today-txn-meta{display:block;margin-top:8px;color:#68789d;font-size:12px}
.today-txn-right{display:flex;flex-direction:column;align-items:flex-end;gap:3px;text-align:right}
.today-txn-amount{display:block;font-size:12px;font-weight:700;white-space:nowrap}
.today-payment{display:inline-flex;align-items:center;gap:3px;border-radius:4px;padding:4px 5px;font-size:10px;white-space:nowrap}
.today-payment .line-icon{width:16px;height:16px}
.today-payment.cash{background:#dff6e9;color:#047f5b}
.today-payment.transfer{background:#e7f1ff;color:#0662cf}
.today-payment.debt{background:#fff0d8;color:#d77a00}
.today-right-stack{display:grid;grid-template-rows:216px 157px;gap:12px}
.today-stock-list{margin-top:4px}
.today-stock-row{display:flex;width:100%;height:38px;align-items:center;gap:8px;border:0;border-bottom:1px solid #edf1f3;background:#fff;padding:0;color:#283866;font-size:12px;text-align:left;cursor:pointer}
.today-stock-dot{display:grid;width:28px;height:28px;flex:none;place-items:center;border-radius:50%;background:#fff1d9;color:#ff9300}
.today-stock-dot .line-icon{width:16px;height:16px}
.today-stock-dot.red{background:#ffebee;color:#f43151}
.today-stock-dot.green{background:#e4f7ee;color:#0a9d6f}
.today-stock-label{flex:1}
.today-stock-count{color:#53668f;font-size:11px}
.today-stock-row>.line-icon{width:16px;height:16px;color:#8996b0}
.today-debt-body{padding:9px 2px 0}
.today-debt-line{display:flex;justify-content:space-between;gap:10px;margin-bottom:10px;color:#344677;font-size:14px}
.today-debt-line strong{color:#fc6500;font-size:14px}
.today-debt-line:nth-child(2) strong{color:#1454e9}
.today-debt-line.overdue{margin-bottom:0;color:#ff304e}
.today-debt-line.overdue strong{color:#ff304e}

/* Live layout */
.today-live-grid{display:grid;grid-template-columns:1.6fr 1fr;align-items:start;gap:14px;margin-top:17px}
.today-live-grid .today-panel{padding:13px 13px 9px}
.today-attention-list{margin:0;padding:0;list-style:none}
.today-attention-row{display:flex;align-items:center;gap:10px;border-bottom:1px solid #edf1f3;padding:10px 0}
.today-attention-row:last-child{border-bottom:0}
.today-attention-copy{display:grid;min-width:0;flex:1;gap:2px;font-size:12px}
.today-attention-copy strong{overflow:hidden;font-size:13px;text-overflow:ellipsis;white-space:nowrap}
.today-attention-kind.fact{color:#d6283f;font-weight:650}
.today-attention-kind.risk{color:#c76e06;font-weight:650}
.today-attention-meta{color:#65759a;font-size:11px}
.today-debt-button{width:100%;border:0;border-radius:6px;background:transparent;padding:4px 2px;text-align:left;cursor:pointer}
.today-debt-button:hover{background:#f7fbfa}
.today-debt-note{margin:4px 2px 0;color:#65759a;font-size:12px;line-height:1.45}

/* Drawer */
.today-scrim{position:fixed;z-index:35;inset:0;display:none;background:#18213a44}
.today-drawer{position:fixed;z-index:40;top:var(--app-topbar-height,0px);right:0;bottom:6px;display:flex;width:328px;flex-direction:column;overflow:hidden;border-radius:12px 0 0 12px;background:#fff;padding:10px 11px 12px;box-shadow:-8px 4px 36px #17354a17}
/* Slides in on open; a closed drawer leaves at once so two never coexist. */
.today-drawer{animation:today-drawer-in .24s ease}
@keyframes today-drawer-in{from{transform:translateX(105%)}}
.today-drawer-head{display:flex;height:43px;flex:none;align-items:center;justify-content:space-between;padding:0 8px 4px 12px}
.today-drawer-head h2{margin:0;font-size:20px;font-weight:700;letter-spacing:-.7px}
.today-close{display:grid;place-items:center;border:0;background:transparent;padding:5px;color:#18305f;cursor:pointer}
.today-drawer-scroll{flex:1;overflow:auto;padding:0 0 4px;scrollbar-width:thin}
.today-explain-card{margin-bottom:9px;border:1px solid #e3ecf1;border-radius:11px;background:#fff;padding:10px 11px}
.today-explain-head{display:flex;align-items:center;gap:10px;margin-bottom:8px;font-size:14px;font-weight:800}
.today-explain-icon{display:grid;width:34px;height:34px;flex:none;place-items:center;border-radius:8px;background:#e2f7ed;color:#008f65}
.today-explain-icon .line-icon{width:16px;height:16px}
.today-explain-icon.blue{background:#e8f2ff;color:#1880ef}
.today-explain-icon.red{background:#ffedf0;color:#fb3d5c}
.today-explain-card p,.today-explain-card li{color:#263963;font-size:12px;line-height:1.48}
.today-explain-card p{margin:3px 0}
.today-explain-card ul{margin:5px 0 1px;padding-left:18px;list-style:disc}
.today-explain-card li{margin:4px 0}
.today-formula{border-radius:8px;background:#fff6e9;padding:10px 11px;color:#0e1b3d;font-size:12px;line-height:1.65;text-align:center}
.today-drawer-action{display:flex;width:100%;height:36px;align-items:center;justify-content:center;gap:9px;margin-top:4px;border:1px solid #cbdbe6;border-radius:6px;background:#fff;color:#20345d;font-size:12px;font-weight:650;cursor:pointer}
.today-drawer-action .line-icon{width:16px;height:16px}
.today-drawer-action:hover{border-color:#9acbbd;background:#f0f8f6}
.today-drawer-action:disabled{opacity:.5;cursor:default}
.today-notice{display:flex;align-items:flex-start;gap:9px;border-radius:8px;background:#eaf5ff;padding:10px 9px;color:#21457d;font-size:10px;line-height:1.45}
.today-notice .line-icon{width:17px;height:17px;color:#1478e5;fill:#1478e5;stroke:#fff}
.today-detail-intro{margin:0 4px 11px;color:#5b6d90;font-size:13px;line-height:1.5}
.today-detail-card{margin-bottom:9px;border:1px solid #e3ecf1;border-radius:9px;padding:11px}
.today-detail-card h3{margin:0 0 8px;font-size:14px;font-weight:700}
.today-detail-card .today-detail-intro{margin:0 0 8px}
.today-detail-grid{display:grid;grid-template-columns:1fr auto;gap:8px 10px;color:#506184;font-size:12px}
.today-detail-grid strong{color:#152648;text-align:right}
.today-detail-list{display:flex;flex-direction:column;gap:7px}
.today-detail-item{border:1px solid #e3ecf1;border-radius:8px;background:#fff;padding:10px 11px;color:#172748;text-align:left;cursor:pointer}
.today-detail-item:hover{border-color:#abd7cb;background:#f8fcfa}
.today-detail-item strong{display:block;font-size:13px}
.today-detail-item span{display:block;margin-top:4px;color:#617398;font-size:12px}
.today-source-list{margin:6px 0 0;padding:0;list-style:none}
.today-source-item{display:grid;gap:6px;border-top:1px solid #edf1f3;padding:8px 0}
.today-source-top{display:flex;justify-content:space-between;gap:8px;font-size:12px}
.today-source-top small{display:block;color:#65759a;font-size:11px;overflow-wrap:anywhere}
.today-source-debt{display:grid;gap:2px;color:#506184;font-size:11px}
.today-source-pager{display:flex;align-items:center;justify-content:space-between;gap:8px;color:#506184;font-size:12px}
.today-source-pager .today-drawer-action{width:auto;padding:0 10px}
.today-toast{position:fixed;z-index:30;bottom:26px;left:50%;border-radius:9px;background:#122748;padding:11px 16px;color:#fff;font-size:13px;transform:translateX(-50%);box-shadow:0 14px 40px #1b354714}

@media(min-width:1251px){.today-page.drawer-open{margin-right:328px}}
@media(max-width:1250px){
  .today-scrim{display:block}
  .today-drawer{top:0;bottom:0}
  .today-heading-tools{gap:6px}
  .today-sale-btn{min-width:140px;gap:8px}
  .today-status{padding:0 8px}
  .today-shift{width:168px}
  .today-kpi{gap:8px;padding:13px 10px}
  .today-kpi-icon{width:42px;height:44px}
  .today-kpi-value{font-size:22px}
  .today-donut{width:160px;height:160px}
  .today-donut::after{inset:27px}
  .today-donut-center{inset:37px 20px}
  .today-donut-wrap{gap:12px}
  .today-legend{gap:9px}
}
@media(max-width:980px){
  .today-heading-row{flex-wrap:wrap;padding-bottom:10px}
  .today-heading-tools{width:100%;justify-content:flex-end}
  .today-analytics,.today-lower{grid-template-columns:1fr 1fr}
  .today-right-stack{grid-column:1/-1;grid-template-columns:1fr 1fr;grid-template-rows:216px}
  .today-donut{width:138px;height:138px}
  .today-donut-center strong{font-size:13px}
  .today-legend-item{gap:6px;font-size:11px}
  .today-live-grid{grid-template-columns:1fr}
}
@media(max-width:700px){
  .today-heading h1{font-size:24px}
  .today-heading-tools{justify-content:flex-start;overflow-x:auto;padding-bottom:3px}
  .today-status,.today-shift,.today-sale-btn{height:45px}
  .today-shift{min-width:160px}
  .today-sale-btn{min-width:140px}
  .today-cards{grid-template-columns:repeat(2,minmax(0,1fr));gap:9px}
  .today-kpi{min-height:105px}
  .today-analytics,.today-lower{grid-template-columns:1fr;gap:10px;margin-top:10px}
  .today-right-stack{grid-column:auto;grid-template-columns:1fr;grid-template-rows:216px 157px}
  .today-donut{width:175px;height:175px}
  .today-donut-center strong{font-size:15px}
  .today-legend-item{font-size:12px}
  .today-drawer{width:min(100vw,380px)}
}
@media(max-width:430px){
  .today-heading-tools{gap:5px}
  .today-kpi{gap:7px;padding:11px 8px}
  .today-kpi-icon{width:34px;height:36px;border-radius:8px}
  .today-kpi-icon .line-icon{width:19px}
  .today-kpi-label{font-size:11px}
  .today-kpi-label .line-icon{width:13px}
  .today-kpi-value{font-size:18px}
  .today-kpi-trend{font-size:10px}
  .today-donut-wrap{gap:8px}
  .today-donut{width:138px;height:138px}
  .today-donut::after{inset:23px}
  .today-donut-center{inset:32px 13px}
  .today-donut-center strong{font-size:12px}
  .today-legend{gap:7px}
  .today-legend-item{font-size:10px}
  .today-swatch{width:9px;height:9px}
  .today-product-row{grid-template-columns:22px 30px minmax(0,1fr) 25px 72px;gap:4px}
  .today-product-name,.today-product-total{font-size:11px}
}
</style>
