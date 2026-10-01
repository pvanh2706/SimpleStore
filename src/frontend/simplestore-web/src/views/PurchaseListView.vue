<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { apiRequest } from '../api/client'
import type { Purchase, PurchaseListItem, PurchasePage } from '../api/types'
import LineIcon from '../components/ui/LineIcon'
import type { IconName } from '../components/ui/icons'
import {
  chipOf, purchaseDemoEnabled, referenceChipCounts, referenceLines, referencePurchases, referenceSuppliers, referenceWizard,
  type PurchaseChip, type PurchaseLinePreview, type PurchasePreview, type PurchaseTone,
} from '../purchases/demo'
import { useAuthStore } from '../stores/auth'

type ListState = 'normal' | 'empty' | 'noresult' | 'loading' | 'error'
type StatusKey = 'done' | 'unpaid' | 'partial' | 'cancel'
type DetailTab = 'overview' | 'products' | 'payments' | 'history'
interface Filter { statuses: StatusKey[]; supplier: string; from: string; to: string; minTotal: string; maxTotal: string }
interface DetailModel {
  id: string
  code: string
  tone: PurchaseTone
  general: Array<{ label: string; value: string }>
  totals: Array<{ label: string; value: string; kind?: 'total' | 'green' | 'red' }>
  lines: Array<{ name: string; sku: string; unit: string; qty: number; cost: number; amount: number }>
  payments: Array<{ date: string; method: string; amount: number }>
  history: Array<{ time: string; event: string; actor: string }>
  voided: { reason: string; at: string } | null
  hint: string
}

const auth = useAuthStore()
const router = useRouter()
const isOwner = computed(() => auth.session.roles.includes('Owner'))
const demo = purchaseDemoEnabled

const clonePurchases = () => referencePurchases.map(item => ({ ...item, lines: item.lines.map(line => ({ ...line })) }))
/** The sample filter opens with the reference date range; neither bound excludes a reference row. */
const defaultFilter = (): Filter => demo.value
  ? { statuses: [], supplier: '', from: '01/12/2024', to: '16/12/2024', minTotal: '0 đ', maxTotal: '' }
  : { statuses: [], supplier: '', from: '', to: '', minTotal: '', maxTotal: '' }
const result = ref<PurchasePage | null>(null)
const liveCounts = ref<Partial<Record<PurchaseChip, number>>>({})
const mockRows = ref<PurchasePreview[]>(clonePurchases())
const search = ref('')
const chip = ref<PurchaseChip>('all')
const filter = ref<Filter>(defaultFilter())
const draft = ref<Filter>(defaultFilter())
const draftChip = ref<PurchaseChip>('all')
const page = ref(1)
const pageSize = ref(demo.value ? 5 : 20)
const loading = ref(false)
const error = ref('')
const forcedState = ref<ListState>('normal')
const demoMenuOpen = ref(false)
const drawer = ref<'filter' | 'detail' | 'edit' | null>(null)
const cancelOpen = ref(false)
const cancelReason = ref('')
const detailMenu = ref(false)
const detailTab = ref<DetailTab>('overview')
const selectedId = ref<string | null>(null)
const livePurchase = ref<Purchase | null>(null)
const detailLoading = ref(false)
const detailError = ref('')
const checkedIds = ref<string[]>([])
const editForm = ref({ supplier: '', date: '', note: '' })
const toastText = ref('')
const wizardOpen = ref(false)
let loadSequence = 0
let detailSequence = 0
let toastTimer: ReturnType<typeof setTimeout> | undefined

const money = (value: number) => `${new Intl.NumberFormat('vi-VN').format(value)} đ`
const pad = (part: number) => String(part).padStart(2, '0')
function formatDate(value: string | null) {
  const date = value ? new Date(value) : null
  return date && !Number.isNaN(date.getTime()) ? `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}` : '—'
}
function formatDateTime(value: string | null) {
  const date = value ? new Date(value) : null
  return date && !Number.isNaN(date.getTime()) ? `${formatDate(value)} ${pad(date.getHours())}:${pad(date.getMinutes())}` : '—'
}
const isoDate = (value: string) => /^\d{2}\/\d{2}\/\d{4}/.test(value.trim()) ? value.trim().slice(0, 10).split('/').reverse().join('-') : ''
const amountInput = (value: string) => value.replace(/\D/g, '') === '' ? null : Number(value.replace(/\D/g, ''))
const shortCode = (id: string) => `#${id.slice(0, 8).toUpperCase()}`
const toneLabel: Record<PurchaseTone, string> = { done: 'Đã nhập', unpaid: 'Chưa thanh toán', partial: 'Thanh toán một phần', cancel: 'Đã hủy', draft: 'Nháp' }
const statusOptions: Array<{ key: StatusKey; label: string }> = [
  { key: 'done', label: 'Đã nhập' }, { key: 'unpaid', label: 'Chưa thanh toán' },
  { key: 'partial', label: 'Thanh toán một phần' }, { key: 'cancel', label: 'Đã hủy' },
]

/** Payment wording is derived for a non-voided Completed Purchase only; Draft and Void stay distinct. */
function toneOf(item: Pick<PurchaseListItem, 'isVoided' | 'status' | 'paidAmount' | 'outstandingAmount'>): PurchaseTone {
  if (item.isVoided) return 'cancel'
  if (item.status === 'Draft') return 'draft'
  if (item.outstandingAmount <= 0) return 'done'
  return item.paidAmount <= 0 ? 'unpaid' : 'partial'
}
/** The list API filters by lifecycle only: "Đã nhập" is Completed and "Nháp" is Draft. */
const liveStatus = (key: PurchaseChip) => key === 'done' ? 'Completed' : key === 'processing' ? 'Draft' : ''

const liveRows = computed<PurchasePreview[]>(() => (result.value?.items ?? []).map(item => ({
  id: item.id, code: shortCode(item.id), date: formatDate(item.completedAt ?? item.createdAt), supplier: item.supplierName,
  total: item.totalAmount, paid: item.paidAmount, debt: item.outstandingAmount, tone: toneOf(item), creator: '—',
  updated: formatDateTime(item.completedAt ?? item.createdAt), note: '', payMethod: '', cancelReason: '', lines: [],
})))
const chips = computed(() => demo.value
  ? [{ key: 'all' as const, label: 'Tất cả' }, { key: 'done' as const, label: 'Đã nhập' }, { key: 'processing' as const, label: 'Đang xử lý' }, { key: 'cancel' as const, label: 'Đã hủy' }]
  : [{ key: 'all' as const, label: 'Tất cả' }, { key: 'done' as const, label: 'Đã nhập' }, { key: 'processing' as const, label: 'Nháp' }])
function bucketCount(rows: PurchasePreview[], key: PurchaseChip) {
  return key === 'all' ? rows.length : rows.filter(item => chipOf(item.tone) === key).length
}
/** Reference totals, moved by whatever was created or cancelled in this preview session. */
function chipCount(key: PurchaseChip) {
  return demo.value
    ? referenceChipCounts[key] + bucketCount(mockRows.value, key) - bucketCount(referencePurchases, key)
    : liveCounts.value[key]
}
const chipText = (key: PurchaseChip, label: string) => chipCount(key) === undefined ? label : `${label} (${chipCount(key)})`

const filteredRows = computed(() => {
  const query = search.value.trim().toLocaleLowerCase('vi-VN')
  const rows = demo.value ? mockRows.value : liveRows.value
  return rows.filter(item => {
    if (query && !`${item.code} ${item.supplier}`.toLocaleLowerCase('vi-VN').includes(query)) return false
    if (!demo.value) return true
    const current = filter.value
    if (chip.value !== 'all' && chipOf(item.tone) !== chip.value) return false
    if (current.statuses.length && !current.statuses.includes(item.tone as StatusKey)) return false
    if (current.supplier && item.supplier !== current.supplier) return false
    const date = isoDate(item.date)
    if (isoDate(current.from) && date < isoDate(current.from)) return false
    if (isoDate(current.to) && date > isoDate(current.to)) return false
    const min = amountInput(current.minTotal)
    const max = amountInput(current.maxTotal)
    if (min !== null && item.total < min) return false
    if (max !== null && item.total > max) return false
    return true
  })
})
const narrowed = computed(() => search.value.trim() !== '' || chip.value !== 'all' || JSON.stringify(filter.value) !== JSON.stringify(defaultFilter()))
const visibleRows = computed(() => demo.value
  ? filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value)
  : filteredRows.value)
const totalCount = computed(() => demo.value
  ? narrowed.value ? filteredRows.value.length : chipCount('all') ?? 0
  : result.value?.totalCount ?? 0)
const totalPages = computed(() => demo.value ? Math.ceil(totalCount.value / pageSize.value) : result.value?.totalPages ?? 0)
const firstIndex = computed(() => totalCount.value ? (page.value - 1) * pageSize.value + 1 : 0)
const lastIndex = computed(() => {
  if (!totalCount.value) return 0
  const shown = demo.value ? visibleRows.value.length : result.value?.items.length ?? 0
  return Math.min(firstIndex.value + shown - 1, totalCount.value)
})
const pagerItems = computed<Array<number | '…'>>(() => {
  const total = totalPages.value
  const current = page.value
  if (total <= 7) return Array.from({ length: total }, (_, index) => index + 1)
  if (current <= 4) return [1, 2, 3, 4, 5, '…', total]
  if (current >= total - 3) return [1, '…', total - 4, total - 3, total - 2, total - 1, total]
  return [1, '…', current - 1, current, current + 1, '…', total]
})
const allChecked = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(item => checkedIds.value.includes(item.id)))
const listState = computed<ListState>(() => {
  if (demo.value && forcedState.value !== 'normal') return forcedState.value
  if (!demo.value && loading.value) return 'loading'
  if (!demo.value && error.value) return 'error'
  if (visibleRows.value.length === 0) return narrowed.value ? 'noresult' : 'empty'
  return 'normal'
})
const stateCopy = computed(() => ({
  normal: null,
  empty: { icon: 'purchase' as IconName, title: 'Chưa có phiếu nhập nào', text: 'Hãy tạo phiếu nhập đầu tiên để bắt đầu quản lý nhập hàng.', action: 'Tạo phiếu nhập' },
  noresult: { icon: 'search' as IconName, title: 'Không tìm thấy phiếu nhập', text: 'Thử thay đổi từ khóa hoặc bộ lọc.', action: 'Xóa bộ lọc' },
  loading: { icon: null, title: 'Đang tải dữ liệu...', text: 'Vui lòng chờ trong giây lát.', action: '' },
  error: { icon: 'warning' as IconName, title: 'Không thể tải dữ liệu', text: 'Vui lòng thử lại sau hoặc liên hệ quản trị viên nếu lỗi vẫn tiếp diễn.', action: 'Thử lại' },
})[listState.value])
const selectedRow = computed(() => demo.value ? mockRows.value.find(item => item.id === selectedId.value) ?? null : null)

function minutesLater(value: string, minutes: number) {
  const [date, time = '00:00'] = value.split(' ')
  const [hour = 0, minute = 0] = time.split(':').map(Number)
  const total = hour * 60 + minute + minutes
  return `${date} ${pad(Math.floor(total / 60) % 24)}:${pad(total % 60)}`
}
const detail = computed<DetailModel | null>(() => {
  if (demo.value) {
    const row = selectedRow.value
    if (!row) return null
    const history = [
      { time: row.updated, event: 'Tạo phiếu nhập', actor: row.creator },
      { time: minutesLater(row.updated, 1), event: 'Ghi nhận nhập kho', actor: row.creator },
      ...(row.paid > 0 ? [{ time: minutesLater(row.updated, 2), event: 'Ghi nhận thanh toán', actor: row.creator }] : []),
      ...(row.tone === 'cancel' ? [{ time: 'Hôm nay', event: `Hủy phiếu nhập: ${row.cancelReason || '—'}`, actor: referenceWizard.creator }] : []),
    ]
    return {
      id: row.id, code: row.code, tone: row.tone,
      general: [
        { label: 'Mã phiếu', value: row.code }, { label: 'Ngày nhập', value: row.updated },
        { label: 'Nhà cung cấp', value: row.supplier }, { label: 'Trạng thái', value: toneLabel[row.tone] },
        { label: 'Người tạo', value: row.creator }, { label: 'Ghi chú', value: row.note || '—' },
      ],
      totals: [
        { label: 'Tổng tiền hàng', value: money(row.total) }, { label: 'Giảm giá', value: money(0) },
        { label: 'Tổng tiền', value: money(row.total), kind: 'total' }, { label: 'Đã thanh toán', value: money(row.paid), kind: 'green' },
        { label: 'Công nợ', value: money(row.debt), kind: 'red' },
      ],
      lines: row.lines.map(line => ({ ...line, amount: line.qty * line.cost })),
      payments: row.paid > 0 ? [{ date: row.date, method: row.payMethod, amount: row.paid }] : [],
      history,
      voided: row.tone === 'cancel' ? { reason: row.cancelReason || '—', at: 'Hôm nay' } : null,
      hint: '',
    }
  }
  const purchase = livePurchase.value
  if (!purchase) return null
  const tone = toneOf(purchase)
  const isDraft = purchase.status === 'Draft'
  const method = (value: 'Cash' | 'Transfer') => value === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản'
  const events = [
    { at: purchase.createdAt, event: 'Tạo phiếu nháp' },
    ...(purchase.completedAt ? [{ at: purchase.completedAt, event: 'Hoàn tất phiếu và ghi nhận nhập kho' }] : []),
    ...purchase.payments.map(payment => ({ at: payment.paidAt, event: `Thanh toán ${method(payment.method)} ${money(payment.amount)}` })),
    ...(purchase.void ? [{ at: purchase.void.voidedAt, event: `Hủy phiếu nhập: ${purchase.void.reason}` }] : []),
  ].sort((left, right) => left.at.localeCompare(right.at))
  return {
    id: purchase.id, code: shortCode(purchase.id), tone,
    general: [
      { label: 'Mã phiếu', value: shortCode(purchase.id) }, { label: 'Ngày tạo', value: formatDateTime(purchase.createdAt) },
      { label: 'Nhà cung cấp', value: purchase.supplierName }, { label: 'Trạng thái', value: toneLabel[tone] },
      { label: 'Hoàn tất lúc', value: purchase.completedAt ? formatDateTime(purchase.completedAt) : 'Chưa hoàn tất' },
    ],
    totals: [
      { label: 'Tổng tiền', value: money(purchase.totalAmount), kind: 'total' },
      { label: 'Đã thanh toán trong phiếu', value: isDraft ? '—' : money(purchase.paidAmount), kind: 'green' },
      { label: 'Còn nợ theo phiếu', value: isDraft ? '—' : money(purchase.outstandingAmount), kind: 'red' },
    ],
    lines: purchase.lines.map(line => ({ name: line.productName, sku: '', unit: line.productUnit, qty: line.quantity, cost: line.unitPrice, amount: line.lineAmount })),
    payments: purchase.payments.map(payment => ({ date: formatDateTime(payment.paidAt), method: method(payment.method), amount: payment.amount })),
    history: events.map(item => ({ time: formatDateTime(item.at), event: item.event, actor: '' })),
    voided: purchase.void ? { reason: purchase.void.reason, at: formatDateTime(purchase.void.voidedAt) } : null,
    hint: isDraft
      ? 'Phiếu nháp chưa ghi nhận nhập kho, thanh toán hay công nợ nhà cung cấp.'
      : 'Còn nợ theo phiếu là tổng tiền trừ các khoản trả trong phiếu; không phản ánh các khoản trả nợ nhà cung cấp phát sinh sau.',
  }
})
const detailTabs = computed(() => detail.value ? [
  { key: 'overview' as const, label: 'Tổng quan' },
  { key: 'products' as const, label: `Sản phẩm (${detail.value.lines.length})` },
  { key: 'payments' as const, label: `Thanh toán (${detail.value.payments.length})` },
  { key: 'history' as const, label: `Lịch sử (${detail.value.history.length})` },
] : [])
const showSku = computed(() => detail.value?.lines.some(line => line.sku) ?? false)
const showActor = computed(() => detail.value?.history.some(item => item.actor) ?? false)

async function load(requestedPage = 1) {
  if (demo.value) { page.value = requestedPage; return }
  const sequence = ++loadSequence
  loading.value = true
  error.value = ''
  const params = new URLSearchParams({ page: String(requestedPage), pageSize: String(pageSize.value) })
  const status = liveStatus(chip.value)
  if (status) params.set('status', status)
  try {
    const response = await apiRequest<PurchasePage>(`/api/purchases?${params}`)
    if (sequence !== loadSequence) return
    result.value = response
    page.value = requestedPage
    liveCounts.value = { ...liveCounts.value, [chip.value]: response.totalCount }
  } catch (reason) {
    if (sequence === loadSequence) error.value = reason instanceof Error ? reason.message : 'Không thể tải phiếu nhập.'
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}
/** Chip counts use the same supported lifecycle filter, one row per request. */
async function loadCounts() {
  const sequence = loadSequence
  const entries = await Promise.all((['all', 'done', 'processing'] as const).map(async key => {
    const params = new URLSearchParams({ page: '1', pageSize: '1' })
    const status = liveStatus(key)
    if (status) params.set('status', status)
    try { return [key, (await apiRequest<PurchasePage>(`/api/purchases?${params}`)).totalCount] as const }
    catch { return [key, undefined] as const }
  }))
  if (demo.value || sequence !== loadSequence) return
  liveCounts.value = Object.fromEntries(entries.filter(([, count]) => count !== undefined))
}

function toast(message: string) {
  toastText.value = message
  if (toastTimer) clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toastText.value = '' }, 2200)
}
function closeOverlays() {
  drawer.value = null
  cancelOpen.value = false
  detailMenu.value = false
  wizardOpen.value = false
  demoMenuOpen.value = false
}
function chooseChip(key: PurchaseChip) {
  chip.value = key
  page.value = 1
  checkedIds.value = []
  load(1)
}
function goPage(target: number) {
  if (target < 1 || target > totalPages.value || target === page.value) return
  if (demo.value && (target - 1) * pageSize.value >= filteredRows.value.length) {
    toast('Dữ liệu mẫu chỉ có trang đầu tiên giống bản thiết kế.')
    return
  }
  checkedIds.value = []
  load(target)
}
function changePageSize() {
  page.value = 1
  load(1)
}
function toggleAll() { checkedIds.value = allChecked.value ? [] : visibleRows.value.map(item => item.id) }
function toggleChecked(id: string) { checkedIds.value = checkedIds.value.includes(id) ? checkedIds.value.filter(value => value !== id) : [...checkedIds.value, id] }
function openFilter() {
  draft.value = { ...filter.value, statuses: [...filter.value.statuses] }
  draftChip.value = chip.value
  drawer.value = 'filter'
}
function toggleDraftStatus(key: StatusKey) {
  draft.value.statuses = draft.value.statuses.includes(key) ? draft.value.statuses.filter(value => value !== key) : [...draft.value.statuses, key]
}
function applyFilter() {
  drawer.value = null
  checkedIds.value = []
  if (demo.value) {
    filter.value = { ...draft.value, statuses: [...draft.value.statuses] }
    page.value = 1
    toast('Đã áp dụng bộ lọc')
  } else chooseChip(draftChip.value)
}
function clearAll() {
  search.value = ''
  filter.value = defaultFilter()
  forcedState.value = 'normal'
  drawer.value = null
  chooseChip('all')
}
function stateAction() {
  if (listState.value === 'empty') openCreate()
  else if (listState.value === 'noresult') clearAll()
  else if (demo.value) forcedState.value = 'normal'
  else { load(page.value); loadCounts() }
}
async function openDetail(item: PurchasePreview) {
  selectedId.value = item.id
  detailTab.value = 'overview'
  detailMenu.value = false
  drawer.value = 'detail'
  if (demo.value) return
  const sequence = ++detailSequence
  livePurchase.value = null
  detailError.value = ''
  detailLoading.value = true
  try {
    const purchase = await apiRequest<Purchase>(`/api/purchases/${item.id}`)
    if (sequence === detailSequence) livePurchase.value = purchase
  } catch (reason) {
    if (sequence === detailSequence) detailError.value = reason instanceof Error ? reason.message : 'Không thể tải phiếu nhập.'
  } finally {
    if (sequence === detailSequence) detailLoading.value = false
  }
}
function openEdit() {
  const row = selectedRow.value
  if (!row) return
  editForm.value = { supplier: row.supplier, date: row.date, note: row.note }
  detailMenu.value = false
  drawer.value = 'edit'
}
function saveEdit() {
  const row = selectedRow.value
  if (!row) return
  if (!isoDate(editForm.value.date)) { toast('Ngày nhập cần có dạng dd/mm/yyyy'); return }
  const time = row.updated.split(' ')[1] ?? '00:00'
  Object.assign(row, { supplier: editForm.value.supplier, date: editForm.value.date.trim(), note: editForm.value.note.trim(), updated: `${editForm.value.date.trim()} ${time}` })
  drawer.value = null
  toast('Đã lưu thay đổi')
}
function openCancel() {
  cancelReason.value = ''
  detailMenu.value = false
  cancelOpen.value = true
}
function confirmCancel() {
  const row = selectedRow.value
  if (!row) return
  if (!cancelReason.value.trim()) { toast('Vui lòng nhập lý do hủy'); return }
  Object.assign(row, { tone: 'cancel' as PurchaseTone, debt: 0, cancelReason: cancelReason.value.trim() })
  cancelOpen.value = false
  drawer.value = null
  toast('Đã hủy phiếu nhập trong dữ liệu mẫu')
}

/* Create wizard — sample data only; live creation keeps the supported Draft → Complete pages. */
const newWizard = () => ({
  step: 1, supplier: referenceWizard.supplier, note: referenceWizard.note, query: '',
  lines: referenceLines.slice(0, referenceWizard.lineCount).map(line => ({ ...line })),
  payType: referenceWizard.payType, payAmount: referenceWizard.payAmount, payMethod: referenceWizard.payMethod, payDate: referenceWizard.date,
  showLines: false,
})
const wizard = ref(newWizard())
const wizardSteps = ['Nhà cung cấp', 'Sản phẩm', 'Thanh toán', 'Xác nhận']
const payOptions = computed(() => [
  { key: 'full' as const, title: 'Thanh toán toàn bộ', hint: `Thanh toán đủ ${money(wizardTotal.value)}` },
  { key: 'partial' as const, title: 'Thanh toán một phần', hint: 'Thanh toán một phần, còn lại là công nợ' },
  { key: 'none' as const, title: 'Chưa thanh toán', hint: 'Ghi nhận công nợ' },
])
const wizardSupplier = computed(() => referenceSuppliers.find(item => item.name === wizard.value.supplier) ?? referenceSuppliers[0]!)
const wizardTotal = computed(() => wizard.value.lines.reduce((sum, line) => sum + line.qty * line.cost, 0))
const wizardQty = computed(() => wizard.value.lines.reduce((sum, line) => sum + line.qty, 0))
const wizardPaid = computed(() => wizard.value.payType === 'full' ? wizardTotal.value : wizard.value.payType === 'none' ? 0 : Math.max(0, Number(wizard.value.payAmount) || 0))
const wizardDebt = computed(() => Math.max(0, wizardTotal.value - wizardPaid.value))
const payTypeLabel = computed(() => payOptions.value.find(option => option.key === wizard.value.payType)!.title)
const productMatches = computed(() => {
  const query = wizard.value.query.trim().toLocaleLowerCase('vi-VN')
  if (!query) return []
  return referenceLines.filter(line => !wizard.value.lines.some(item => item.sku === line.sku)
    && `${line.name} ${line.sku}`.toLocaleLowerCase('vi-VN').includes(query))
})

function openCreate() {
  if (!demo.value) { router.push('/purchases/new'); return }
  wizard.value = newWizard()
  drawer.value = null
  wizardOpen.value = true
}
function addLine(line?: PurchaseLinePreview) {
  const match = line ?? productMatches.value[0]
  if (!match) { toast('Không tìm thấy sản phẩm phù hợp trong dữ liệu mẫu'); return }
  wizard.value.lines.push({ ...match })
  wizard.value.query = ''
}
function choosePayType(key: 'full' | 'partial' | 'none') {
  wizard.value.payType = key
}
function wizardBack() {
  if (wizard.value.step === 1) wizardOpen.value = false
  else wizard.value.step -= 1
}
function wizardNext() {
  const current = wizard.value
  if (current.step === 2 && (current.lines.length === 0 || current.lines.some(line => !(line.qty > 0) || line.cost < 0))) {
    toast('Thêm ít nhất một sản phẩm với số lượng lớn hơn 0')
    return
  }
  if (current.step === 3 && current.payType === 'partial' && !(wizardPaid.value > 0 && wizardPaid.value < wizardTotal.value)) {
    toast('Số tiền thanh toán một phần phải lớn hơn 0 và nhỏ hơn tổng tiền')
    return
  }
  if (current.step < 4) { current.step += 1; return }
  const next = Math.max(...mockRows.value.map(item => Number(item.code.replace(/\D/g, '')) || 0)) + 1
  const code = `PN${String(next).padStart(6, '0')}`
  const paid = Math.min(wizardPaid.value, wizardTotal.value)
  const created: PurchasePreview = {
    id: `preview-${code}`, code, date: current.payDate, supplier: current.supplier, total: wizardTotal.value, paid,
    debt: wizardTotal.value - paid, tone: paid === 0 ? 'unpaid' : paid >= wizardTotal.value ? 'done' : 'partial',
    creator: referenceWizard.creator, updated: code === referenceWizard.newCode ? referenceWizard.createdAt : `${current.payDate} 14:23`,
    note: current.note.trim(), payMethod: current.payMethod, cancelReason: '', lines: current.lines.map(line => ({ ...line })),
  }
  mockRows.value.unshift(created)
  wizardOpen.value = false
  search.value = ''
  chip.value = 'all'
  filter.value = defaultFilter()
  page.value = 1
  toast('Đã tạo phiếu nhập trong dữ liệu mẫu')
  openDetail(created)
}

function showDemoState(state: ListState) {
  forcedState.value = state
  demoMenuOpen.value = false
}
function onKeydown(event: KeyboardEvent) {
  if (event.key !== 'Escape') return
  if (detailMenu.value) detailMenu.value = false
  else closeOverlays()
}

watch(search, () => {
  checkedIds.value = []
  if (demo.value) page.value = 1
})
watch(demo, enabled => {
  ++loadSequence
  ++detailSequence
  mockRows.value = clonePurchases()
  search.value = ''
  chip.value = 'all'
  filter.value = defaultFilter()
  pageSize.value = enabled ? 5 : 20
  page.value = 1
  checkedIds.value = []
  forcedState.value = 'normal'
  selectedId.value = null
  loading.value = false
  error.value = ''
  closeOverlays()
  if (!enabled) { load(1); loadCounts() }
})
onMounted(() => {
  window.addEventListener('keydown', onKeydown)
  if (!demo.value) { load(1); loadCounts() }
})
onUnmounted(() => {
  ++loadSequence
  ++detailSequence
  if (toastTimer) clearTimeout(toastTimer)
  window.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <section class="purchase-page">
    <div class="purchase-panel">
      <div class="purchase-head">
        <div><h1>Nhập hàng</h1><p>Quản lý phiếu nhập hàng và công nợ nhà cung cấp</p></div>
        <button v-if="isOwner || demo" class="purchase-primary purchase-create" type="button" @click="openCreate"><LineIcon name="plus" />Tạo phiếu nhập</button>
      </div>

      <div class="purchase-toolbar">
        <label class="purchase-search"><LineIcon name="search" /><input v-model="search" aria-label="Tìm theo mã phiếu, tên nhà cung cấp" placeholder="Tìm theo mã phiếu, tên nhà cung cấp..." /><span class="purchase-scan" aria-hidden="true"><LineIcon name="barcode" /></span></label>
        <button class="purchase-filter-button" type="button" @click="openFilter"><span aria-hidden="true">▽</span> Bộ lọc</button>
      </div>
      <div class="purchase-chips" aria-label="Trạng thái phiếu nhập">
        <button v-for="item in chips" :key="item.key" class="purchase-chip" :class="{ active: chip === item.key }" type="button" :aria-pressed="chip === item.key" @click="chooseChip(item.key)">{{ chipText(item.key, item.label) }}</button>
      </div>
      <p v-if="!demo && search.trim()" class="purchase-scope-note">Đang tìm trong {{ result?.items.length ?? 0 }} phiếu của trang hiện tại; hệ thống chưa hỗ trợ tìm kiếm phiếu nhập trên toàn bộ dữ liệu.</p>

      <div v-if="stateCopy" class="purchase-state" :role="listState === 'error' ? 'alert' : 'status'">
        <div class="purchase-state-inner">
          <div v-if="listState === 'loading'" class="purchase-spinner" aria-hidden="true" />
          <span v-else-if="stateCopy.icon" class="purchase-state-icon" :class="{ danger: listState === 'error' }"><LineIcon :name="stateCopy.icon" /></span>
          <h3>{{ stateCopy.title }}</h3>
          <p>{{ stateCopy.text }}</p>
          <p v-if="listState === 'error' && error && !demo" class="purchase-state-detail">{{ error }}</p>
          <button v-if="stateCopy.action && (listState !== 'empty' || isOwner || demo)" :class="listState === 'empty' ? 'purchase-primary' : 'purchase-secondary'" type="button" @click="stateAction"><LineIcon v-if="listState === 'empty'" name="plus" />{{ stateCopy.action }}</button>
        </div>
      </div>
      <template v-else>
        <div class="purchase-table-wrap"><table class="purchase-table">
          <colgroup><col style="width:32px" /><col style="width:100px" /><col style="width:96px" /><col style="width:130px" /><col style="width:110px" /><col style="width:110px" /><col style="width:100px" /><col style="width:130px" /><col style="width:90px" /><col style="width:118px" /></colgroup>
          <thead><tr><th><input type="checkbox" aria-label="Chọn tất cả phiếu nhập trên trang" :checked="allChecked" @change="toggleAll" /></th><th>Mã phiếu ↕</th><th>Ngày nhập ↕</th><th>Nhà cung cấp ↕</th><th class="money">Tổng tiền ↕</th><th class="money">Đã thanh toán ↕</th><th class="money">Công nợ ↕</th><th>Trạng thái ↕</th><th>Người tạo ↕</th><th>Cập nhật ↕</th></tr></thead>
          <tbody>
            <tr v-for="item in visibleRows" :key="item.id" :class="{ selected: drawer === 'detail' && selectedId === item.id }">
              <td><input type="checkbox" :aria-label="`Chọn ${item.code}`" :checked="checkedIds.includes(item.id)" @change="toggleChecked(item.id)" /></td>
              <td><button class="purchase-code" type="button" @click="openDetail(item)">{{ item.code }}</button></td>
              <td>{{ item.date }}</td>
              <td class="purchase-ellipsis">{{ item.supplier }}</td>
              <td class="money">{{ money(item.total) }}</td>
              <td class="money">{{ item.tone === 'draft' ? '—' : money(item.paid) }}</td>
              <td class="money">{{ item.tone === 'draft' ? '—' : money(item.debt) }}</td>
              <td><span class="purchase-status" :class="item.tone"><span class="purchase-dot" />{{ toneLabel[item.tone] }}</span></td>
              <td>{{ item.creator }}</td>
              <td>{{ item.updated }}</td>
            </tr>
          </tbody>
        </table></div>
        <div class="purchase-footer">
          <span>Hiển thị {{ firstIndex }} - {{ lastIndex }} / {{ totalCount }} phiếu nhập</span>
          <div class="purchase-pager">
            <button type="button" aria-label="Trang trước" :disabled="page <= 1" @click="goPage(page - 1)">‹</button>
            <template v-for="(item, index) in pagerItems" :key="`${item}-${index}`">
              <span v-if="item === '…'">...</span>
              <button v-else type="button" :class="{ active: page === item }" :aria-current="page === item ? 'page' : undefined" @click="goPage(item)">{{ item }}</button>
            </template>
            <button type="button" aria-label="Trang sau" :disabled="page >= totalPages" @click="goPage(page + 1)">›</button>
            <select v-model.number="pageSize" aria-label="Số phiếu nhập mỗi trang" @change="changePageSize"><option :value="5">5 / trang</option><option :value="10">10 / trang</option><option :value="20">20 / trang</option><option :value="50">50 / trang</option></select>
          </div>
        </div>
      </template>
    </div>

    <div v-if="drawer" class="purchase-overlay" @click="closeOverlays" />

    <!-- Filter drawer -->
    <aside v-if="drawer === 'filter'" class="purchase-drawer" role="dialog" aria-modal="true" aria-label="Bộ lọc phiếu nhập">
      <div class="purchase-drawer-head"><h2>Bộ lọc</h2><button class="purchase-icon-button" type="button" aria-label="Đóng" @click="drawer = null">×</button></div>
      <div class="purchase-drawer-body">
        <template v-if="demo">
          <div class="purchase-section"><h3>Trạng thái</h3><label v-for="option in statusOptions" :key="option.key" class="purchase-check-row"><input type="checkbox" :checked="draft.statuses.includes(option.key)" @change="toggleDraftStatus(option.key)" />{{ option.label }}</label></div>
          <div class="purchase-section"><h3>Nhà cung cấp</h3><select v-model="draft.supplier" class="purchase-input" aria-label="Nhà cung cấp"><option value="">Chọn nhà cung cấp</option><option v-for="supplier in referenceSuppliers" :key="supplier.name">{{ supplier.name }}</option></select></div>
          <div class="purchase-section"><h3>Khoảng thời gian</h3><div class="purchase-grid2"><label class="purchase-field">Từ ngày<input v-model="draft.from" class="purchase-input" placeholder="dd/mm/yyyy" /></label><label class="purchase-field">Đến ngày<input v-model="draft.to" class="purchase-input" placeholder="dd/mm/yyyy" /></label></div></div>
          <div class="purchase-section"><h3>Khoảng tổng tiền</h3><div class="purchase-grid2"><label class="purchase-field">Từ<input v-model="draft.minTotal" class="purchase-input" placeholder="0 đ" /></label><label class="purchase-field">Đến<input v-model="draft.maxTotal" class="purchase-input" placeholder="Không giới hạn" /></label></div></div>
        </template>
        <template v-else>
          <div class="purchase-section"><h3>Trạng thái</h3><label v-for="item in chips" :key="item.key" class="purchase-check-row"><input v-model="draftChip" type="radio" name="purchase-lifecycle" :value="item.key" />{{ item.label }}</label></div>
          <p class="purchase-hint">Lọc theo nhà cung cấp, khoảng thời gian và tổng tiền chưa được hệ thống hỗ trợ.</p>
        </template>
      </div>
      <div class="purchase-drawer-foot"><button class="purchase-danger-outline" type="button" @click="clearAll(); toast('Đã xóa bộ lọc')">Xóa bộ lọc</button><button class="purchase-primary" type="button" @click="applyFilter">Áp dụng</button></div>
    </aside>

    <!-- Detail drawer -->
    <aside v-if="drawer === 'detail'" class="purchase-drawer xwide" role="dialog" aria-modal="true" aria-label="Chi tiết phiếu nhập">
      <div class="purchase-drawer-head">
        <div class="purchase-back-title"><button class="purchase-icon-button" type="button" aria-label="Đóng chi tiết" @click="drawer = null">‹</button><h2>Phiếu nhập {{ detail?.code ?? '' }}</h2><span v-if="detail" class="purchase-status" :class="detail.tone"><span class="purchase-dot" />{{ toneLabel[detail.tone] }}</span></div>
        <div v-if="detail" class="purchase-detail-actions">
          <button v-if="demo" class="purchase-small-button" type="button" @click="toast('Đã mô phỏng in phiếu')"><LineIcon name="print" />In phiếu</button>
          <button class="purchase-small-button" type="button" aria-label="Thao tác phiếu nhập" :aria-expanded="detailMenu" @click.stop="detailMenu = !detailMenu">•••</button>
          <div v-if="detailMenu" class="purchase-action-menu">
            <template v-if="demo">
              <button v-if="detail.tone !== 'cancel'" type="button" @click="openEdit">Sửa phiếu nhập</button>
              <button v-if="detail.tone !== 'cancel'" class="danger" type="button" @click="openCancel">Hủy phiếu nhập</button>
              <span v-if="detail.tone === 'cancel'" class="purchase-menu-note">Phiếu đã hủy không thể sửa.</span>
            </template>
            <template v-else>
              <RouterLink v-if="detail.tone === 'draft'" :to="`/purchases/${detail.id}/edit`">Sửa phiếu nháp</RouterLink>
              <RouterLink v-if="detail.tone === 'draft'" :to="`/purchases/${detail.id}`">Hoàn tất phiếu nhập</RouterLink>
              <RouterLink v-if="detail.tone !== 'draft' && detail.tone !== 'cancel'" class="danger" :to="`/purchases/${detail.id}`">Hủy phiếu nhập</RouterLink>
              <RouterLink :to="`/purchases/${detail.id}`">Mở trang chi tiết</RouterLink>
            </template>
          </div>
        </div>
      </div>
      <div class="purchase-drawer-body" @click="detailMenu = false">
        <p v-if="detailLoading" class="purchase-hint">Đang tải phiếu nhập…</p>
        <p v-else-if="detailError" class="purchase-error" role="alert">{{ detailError }}</p>
        <template v-if="detail">
          <div class="purchase-tabs" role="tablist">
            <button v-for="tab in detailTabs" :key="tab.key" class="purchase-tab" :class="{ active: detailTab === tab.key }" type="button" role="tab" :aria-selected="detailTab === tab.key" @click="detailTab = tab.key">{{ tab.label }}</button>
          </div>
          <div v-if="detail.voided" class="purchase-void-box"><strong>Đã hủy</strong> · {{ detail.voided.at }}<br />Lý do: {{ detail.voided.reason }}</div>
          <template v-if="detailTab === 'overview'">
            <div class="purchase-detail-grid">
              <div class="purchase-info-card"><h3>Thông tin chung</h3><div v-for="item in detail.general" :key="item.label" class="purchase-kv"><span>{{ item.label }}</span><b>{{ item.value }}</b></div></div>
              <div class="purchase-info-card"><h3>Tổng tiền</h3><div v-for="item in detail.totals" :key="item.label" class="purchase-kv" :class="item.kind"><span>{{ item.label }}</span><b>{{ item.value }}</b></div><p v-if="detail.hint" class="purchase-hint">{{ detail.hint }}</p></div>
            </div>
            <h3 class="purchase-subtitle">Danh sách sản phẩm ({{ detail.lines.length }})</h3>
          </template>
          <table v-if="detailTab === 'overview' || detailTab === 'products'" class="purchase-detail-table">
            <thead><tr><th>#</th><th>Sản phẩm</th><th v-if="showSku">Mã SKU</th><th>Đơn vị</th><th>Số lượng</th><th>Đơn giá nhập</th><th class="money">Thành tiền</th></tr></thead>
            <tbody><tr v-for="(line, index) in detail.lines" :key="index"><td>{{ index + 1 }}</td><td>{{ line.name }}</td><td v-if="showSku">{{ line.sku }}</td><td>{{ line.unit }}</td><td>{{ line.qty }}</td><td>{{ money(line.cost) }}</td><td class="money">{{ money(line.amount) }}</td></tr></tbody>
          </table>
          <template v-if="detailTab === 'payments'">
            <div v-for="(payment, index) in detail.payments" :key="index" class="purchase-info-card purchase-payment"><h3>Thanh toán</h3><div class="purchase-kv"><span>Ngày</span><b>{{ payment.date }}</b></div><div class="purchase-kv"><span>Phương thức</span><b>{{ payment.method }}</b></div><div class="purchase-kv green"><span>Số tiền</span><b>{{ money(payment.amount) }}</b></div></div>
            <p v-if="detail.payments.length === 0" class="purchase-hint">{{ detail.tone === 'draft' ? 'Thanh toán được ghi nhận khi hoàn tất phiếu.' : 'Chưa trả trong phiếu · toàn bộ là công nợ nhà cung cấp.' }}</p>
          </template>
          <table v-if="detailTab === 'history'" class="purchase-detail-table">
            <thead><tr><th>Thời gian</th><th>Sự kiện</th><th v-if="showActor">Người thực hiện</th></tr></thead>
            <tbody><tr v-for="(item, index) in detail.history" :key="index"><td>{{ item.time }}</td><td>{{ item.event }}</td><td v-if="showActor">{{ item.actor }}</td></tr></tbody>
          </table>
        </template>
      </div>
    </aside>

    <!-- Edit drawer (sample data) -->
    <aside v-if="drawer === 'edit' && selectedRow" class="purchase-drawer wide" role="dialog" aria-modal="true" aria-label="Sửa phiếu nhập">
      <div class="purchase-drawer-head"><h2>Sửa phiếu nhập {{ selectedRow.code }}</h2><button class="purchase-icon-button" type="button" aria-label="Đóng" @click="drawer = null">×</button></div>
      <form class="purchase-drawer-body" @submit.prevent="saveEdit">
        <div class="purchase-info-note">ℹ Chỉ có thể sửa khi phiếu chưa hủy. Việc sửa sẽ tạo thêm biên bản lịch sử.</div>
        <label class="purchase-field">
          <span>Nhà cung cấp <span class="required">*</span></span>
          <select v-model="editForm.supplier" class="purchase-input">
            <option v-for="supplier in referenceSuppliers" :key="supplier.name">{{ supplier.name }}</option>
            <option v-if="!referenceSuppliers.some(supplier => supplier.name === editForm.supplier)">{{ editForm.supplier }}</option>
          </select>
        </label>
        <div class="purchase-grid2"><label class="purchase-field"><span>Ngày nhập <span class="required">*</span></span><input v-model="editForm.date" class="purchase-input" placeholder="dd/mm/yyyy" /></label><label class="purchase-field">Ghi chú<input v-model="editForm.note" class="purchase-input" /></label></div>
      </form>
      <div class="purchase-drawer-foot"><button class="purchase-secondary" type="button" @click="drawer = null">Hủy</button><button class="purchase-primary" type="button" @click="saveEdit">Lưu thay đổi</button></div>
    </aside>

    <!-- Create wizard (sample data) -->
    <div v-if="wizardOpen" class="purchase-wizard">
      <section class="purchase-wizard-card" role="dialog" aria-modal="true" aria-label="Tạo phiếu nhập hàng">
        <div class="purchase-wizard-head"><h2>Tạo phiếu nhập hàng</h2><button class="purchase-icon-button" type="button" aria-label="Đóng" @click="wizardOpen = false">×</button></div>
        <div class="purchase-steps"><div v-for="(label, index) in wizardSteps" :key="label" class="purchase-step" :class="{ active: wizard.step === index + 1, done: wizard.step > index + 1 }"><span class="n">{{ index + 1 }}</span>{{ label }}</div></div>
        <div class="purchase-wizard-body">
          <div v-if="wizard.step === 1">
            <div class="purchase-field"><label for="purchase-supplier">Nhà cung cấp <span class="required">*</span></label><div class="purchase-supplier-row"><select id="purchase-supplier" v-model="wizard.supplier" class="purchase-input"><option v-for="supplier in referenceSuppliers" :key="supplier.name">{{ supplier.name }}</option></select><button class="purchase-small-button" type="button" @click="toast('Thêm nhà cung cấp chỉ minh họa trong dữ liệu mẫu')"><LineIcon name="plus" />Thêm nhà cung cấp</button></div></div>
            <div class="purchase-supplier-card"><h4>Thông tin nhà cung cấp</h4><div class="purchase-kv"><span>Tên nhà cung cấp:</span><b>{{ wizardSupplier.name }}</b></div><div class="purchase-kv"><span>Liên hệ:</span><b>{{ wizardSupplier.phone }}</b></div><div class="purchase-kv"><span>Địa chỉ:</span><b>{{ wizardSupplier.address }}</b></div><div class="purchase-kv red"><span>Công nợ hiện tại:</span><b>{{ money(wizardSupplier.debt) }}</b></div></div>
            <label class="purchase-field purchase-note-field">Ghi chú<textarea v-model="wizard.note" class="purchase-input purchase-textarea" placeholder="Nhập ghi chú (nếu có)..." /></label>
          </div>
          <div v-else-if="wizard.step === 2">
            <div class="purchase-mini-toolbar">
              <div class="purchase-product-search">
                <label class="purchase-search"><LineIcon name="search" /><input v-model="wizard.query" aria-label="Tìm sản phẩm" placeholder="Tìm sản phẩm (tên, mã SKU, mã vạch)..." @keydown.enter.prevent="addLine()" /><span class="purchase-scan" aria-hidden="true"><LineIcon name="barcode" /></span></label>
                <div v-if="productMatches.length" class="purchase-suggestions"><button v-for="line in productMatches" :key="line.sku" type="button" @click="addLine(line)">{{ line.name }} <small>{{ line.sku }}</small></button></div>
              </div>
              <button class="purchase-small-button purchase-add" type="button" :disabled="!wizard.query.trim()" @click="addLine()"><LineIcon name="plus" />Thêm</button>
            </div>
            <table class="purchase-line-table">
              <thead><tr><th>#</th><th>Sản phẩm</th><th>SKU</th><th>Đơn vị</th><th>Số lượng</th><th>Đơn giá nhập</th><th>Thành tiền</th><th><span class="sr-only">Xóa</span></th></tr></thead>
              <tbody>
                <tr v-for="(line, index) in wizard.lines" :key="line.sku"><td>{{ index + 1 }}</td><td>{{ line.name }}</td><td>{{ line.sku }}</td><td>{{ line.unit }}</td><td><input v-model.number="line.qty" class="purchase-num" type="number" min="0" :aria-label="`Số lượng ${line.name}`" /></td><td><input v-model.number="line.cost" class="purchase-num" type="number" min="0" :aria-label="`Đơn giá nhập ${line.name}`" /></td><td class="money">{{ money(line.qty * line.cost) }}</td><td><button class="purchase-remove" type="button" :aria-label="`Xóa ${line.name}`" @click="wizard.lines.splice(index, 1)">×</button></td></tr>
                <tr v-if="wizard.lines.length === 0"><td colspan="8" class="purchase-empty-line">Chưa có sản phẩm. Tìm và thêm sản phẩm ở ô phía trên.</td></tr>
              </tbody>
            </table>
            <div class="purchase-line-summary"><span>Tổng số lượng: <b>{{ wizardQty }}</b></span><span>Tổng tiền hàng: <b>{{ money(wizardTotal) }}</b></span></div>
          </div>
          <div v-else-if="wizard.step === 3" class="purchase-pay-layout">
            <div>
              <h3 class="purchase-subtitle purchase-subtitle--flush">Hình thức thanh toán</h3>
              <div class="purchase-pay-options" role="radiogroup" aria-label="Hình thức thanh toán">
                <button v-for="option in payOptions" :key="option.key" class="purchase-pay-option" :class="{ active: wizard.payType === option.key }" type="button" role="radio" :aria-checked="wizard.payType === option.key" @click="choosePayType(option.key)"><span class="purchase-radio" :class="{ on: wizard.payType === option.key }" /><span><b>{{ option.title }}</b><span class="purchase-hint">{{ option.hint }}</span></span></button>
              </div>
            </div>
            <div>
              <label class="purchase-field"><span>Số tiền thanh toán <span class="required">*</span></span><input v-if="wizard.payType === 'partial'" v-model.number="wizard.payAmount" class="purchase-input" type="number" min="0" /><input v-else class="purchase-input" :value="wizardPaid" readonly /></label>
              <label class="purchase-field"><span>Phương thức thanh toán <span class="required">*</span></span><select v-model="wizard.payMethod" class="purchase-input"><option>Tiền mặt</option><option>Chuyển khoản</option></select></label>
              <label class="purchase-field"><span>Ngày thanh toán <span class="required">*</span></span><input v-model="wizard.payDate" class="purchase-input" placeholder="dd/mm/yyyy" /></label>
              <div class="purchase-summary">
                <div class="purchase-summary-row"><span>Tổng tiền hàng</span><b>{{ money(wizardTotal) }}</b></div>
                <div class="purchase-summary-row"><span>Giảm giá</span><b>{{ money(0) }}</b></div>
                <div class="purchase-summary-row total"><span>Tổng tiền</span><b>{{ money(wizardTotal) }}</b></div>
                <div class="purchase-summary-row green"><span>Đã thanh toán</span><b>{{ money(wizardPaid) }}</b></div>
                <div class="purchase-summary-row red"><span>Công nợ sau nhập</span><b>{{ money(wizardDebt) }}</b></div>
              </div>
            </div>
          </div>
          <div v-else>
            <div class="purchase-confirm"><h3>Thông tin phiếu nhập</h3><div class="purchase-kv"><span>Nhà cung cấp</span><b>{{ wizard.supplier }}</b></div><div class="purchase-kv"><span>Ngày nhập</span><b>{{ wizard.payDate }}</b></div><div class="purchase-kv"><span>Ghi chú</span><b>{{ wizard.note.trim() || '—' }}</b></div></div>
            <div class="purchase-confirm">
              <button class="purchase-accordion" type="button" :aria-expanded="wizard.showLines" @click="wizard.showLines = !wizard.showLines"><span>Danh sách sản phẩm ({{ wizard.lines.length }})</span><LineIcon name="chevron" /></button>
              <table v-if="wizard.showLines" class="purchase-detail-table"><tbody><tr v-for="(line, index) in wizard.lines" :key="line.sku"><td>{{ index + 1 }}</td><td>{{ line.name }}</td><td>{{ line.qty }} {{ line.unit }}</td><td class="money">{{ money(line.qty * line.cost) }}</td></tr></tbody></table>
            </div>
            <div class="purchase-confirm"><h3>Thanh toán</h3><div class="purchase-kv"><span>Hình thức thanh toán</span><b>{{ payTypeLabel }}</b></div><div class="purchase-kv"><span>Số tiền thanh toán</span><b>{{ money(wizardPaid) }}</b></div><div class="purchase-kv"><span>Phương thức</span><b>{{ wizard.payMethod }}</b></div><div class="purchase-kv red"><span>Công nợ sau nhập</span><b>{{ money(wizardDebt) }}</b></div></div>
          </div>
        </div>
        <div class="purchase-wizard-foot">
          <button class="purchase-secondary" type="button" @click="wizardBack">{{ wizard.step === 1 ? 'Hủy' : '← Quay lại' }}</button>
          <button class="purchase-primary" type="button" @click="wizardNext"><template v-if="wizard.step === 4"><LineIcon name="check" />Tạo phiếu nhập</template><template v-else>Tiếp theo →</template></button>
        </div>
      </section>
    </div>

    <!-- Cancel modal (sample data) -->
    <div v-if="cancelOpen && selectedRow" class="purchase-modal-backdrop" @click.self="cancelOpen = false">
      <section class="purchase-modal" role="dialog" aria-modal="true" aria-label="Hủy phiếu nhập">
        <div class="purchase-modal-head"><h3>Hủy phiếu nhập</h3><button class="purchase-icon-button" type="button" aria-label="Đóng" @click="cancelOpen = false">×</button></div>
        <div class="purchase-modal-body">
          <span class="purchase-danger-icon"><LineIcon name="warning" /></span>
          <h4>Hủy phiếu nhập {{ selectedRow.code }}?</h4>
          <p>Phiếu nhập sẽ chuyển sang trạng thái Đã hủy. Hành động này không thể hoàn tác.</p>
          <label class="purchase-field purchase-left"><span>Lý do hủy <span class="required">*</span></span><input v-model="cancelReason" class="purchase-input" placeholder="Nhập lý do hủy..." @keydown.enter.prevent="confirmCancel" /></label>
        </div>
        <div class="purchase-modal-foot"><button class="purchase-secondary" type="button" @click="cancelOpen = false">Không hủy</button><button class="purchase-danger-outline" type="button" @click="confirmCancel">Hủy phiếu nhập</button></div>
      </section>
    </div>

    <div v-if="demo" class="purchase-demo">
      <button class="purchase-demo-button" type="button" :aria-expanded="demoMenuOpen" @click="demoMenuOpen = !demoMenuOpen">Demo trạng thái ▴</button>
      <div v-if="demoMenuOpen" class="purchase-demo-menu">
        <button type="button" @click="showDemoState('normal')">Danh sách bình thường</button>
        <button type="button" @click="showDemoState('empty')">Trạng thái rỗng</button>
        <button type="button" @click="showDemoState('noresult')">Không có kết quả</button>
        <button type="button" @click="showDemoState('loading')">Đang tải</button>
        <button type="button" @click="showDemoState('error')">Lỗi tải dữ liệu</button>
      </div>
    </div>
    <div v-if="toastText" class="purchase-toast" :class="{ raised: drawer || wizardOpen || cancelOpen }" role="status">{{ toastText }}</div>
  </section>
</template>

<style scoped>
.purchase-page{color:#172033}
.purchase-panel{min-height:calc(100vh - 74px);padding:12px;background:#fff;border:1px solid #dfe7ee;border-radius:14px;box-shadow:0 10px 30px #1f304714}
.purchase-head{display:flex;align-items:flex-start;justify-content:space-between;gap:18px;padding:2px 2px 12px}
.purchase-head h1{margin:0 0 3px;font-size:22px;font-weight:800;line-height:1.2;letter-spacing:-.02em}
.purchase-head p{margin:0;color:#66758d;font-size:12px}
.purchase-primary,.purchase-secondary,.purchase-danger-outline{display:inline-flex;height:38px;align-items:center;justify-content:center;gap:8px;border-radius:8px;padding:0 18px;font-weight:800;cursor:pointer;white-space:nowrap}
.purchase-primary{border:0;background:linear-gradient(180deg,#10a971,#078d5d);color:#fff;box-shadow:0 8px 16px #0a9c6729}
.purchase-primary:hover{background:#087b55}
.purchase-primary .line-icon{width:16px;height:16px}
.purchase-create{font-size:16px}
.purchase-drawer-foot button,.purchase-wizard-foot button,.purchase-modal-foot button,.purchase-state-inner button{font-size:13px}
.purchase-secondary{border:1px solid #ccd9e4;background:#fff;color:#2d3c54}
.purchase-danger-outline{border:1px solid #f4b3b7;background:#fff;color:#df3e46}
.purchase-toolbar{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:10px}
.purchase-search{display:flex;height:36px;align-items:center;gap:9px;border:1.5px solid #b8cef8;border-radius:8px;padding:0 10px;color:#53617b;background:#fff}
.purchase-search>.line-icon{width:13px;height:13px}
.purchase-search input{flex:1;min-width:0;border:0;outline:0;color:#44536a;font-size:12px}
.purchase-scan{display:grid;width:28px;height:24px;place-items:center;border:1px solid #dfe7ee;border-radius:6px;background:#f8fbff;color:#53617b}
.purchase-scan .line-icon{width:16px;height:16px}
.purchase-filter-button{height:36px;border:1px solid #ccd9e4;border-radius:8px;background:#fff;color:#263852;font-size:12px;font-weight:700;cursor:pointer}
.purchase-chips{display:flex;gap:8px;padding:12px 0}
.purchase-chip{height:30px;border:1px solid #dfe7ee;border-radius:999px;background:#fff;padding:0 12px;color:#32435e;font-size:11px;font-weight:700;white-space:nowrap;cursor:pointer}
.purchase-chip.active{border-color:transparent;background:linear-gradient(180deg,#14aa72,#078d5e);color:#fff}
.purchase-scope-note{margin:-4px 0 10px;color:#66758d;font-size:11px}
.purchase-table-wrap{position:relative;overflow:visible;border:1px solid #dfe7ee;border-radius:9px;background:#fff}
.purchase-table{width:100%;border-collapse:collapse;table-layout:fixed}
.purchase-table th{height:34px;border-bottom:1px solid #dfe7ee;background:#f7fafc;padding:0 8px;color:#56657b;font-size:10.5px;font-weight:750;text-align:left;white-space:nowrap}
.purchase-table td{height:48px;border-bottom:1px solid #edf1f4;padding:0 8px;color:#2b3a51;font-size:10.8px;vertical-align:middle}
.purchase-table tbody tr:last-child td{border-bottom:0}
.purchase-table tbody tr:hover{background:#fbfdff}
.purchase-table tbody tr.selected{background:#f2fbf7}
.purchase-table input[type=checkbox]{width:14px;height:14px;margin:0;accent-color:#0a9c67;cursor:pointer}
.purchase-table .money,.purchase-detail-table .money,.purchase-line-table .money{text-align:right;font-weight:800}
.purchase-ellipsis{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.purchase-code{padding:0;border:0;background:none;color:#2d405f;font-weight:800;cursor:pointer}
.purchase-code:hover{color:#087b55;text-decoration:underline}
.purchase-status{display:inline-flex;height:22px;align-items:center;gap:5px;border-radius:7px;padding:0 8px;font-size:9.8px;font-weight:800;white-space:nowrap}
.purchase-status.done{background:#e9f8f2;color:#087e57}
.purchase-status.unpaid{background:#ffe7e8;color:#d13f46}
.purchase-status.partial{background:#fff0d9;color:#cc7909}
.purchase-status.cancel{background:#f1f3f6;color:#7a8696}
.purchase-status.draft{background:#edf5ff;color:#2e6bd1}
.purchase-dot{width:6px;height:6px;border-radius:50%;background:currentColor}
.purchase-footer{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:12px 4px 2px;color:#5d6b80;font-size:11px}
.purchase-pager{display:flex;align-items:center;gap:5px}
.purchase-pager button,.purchase-pager select{min-width:30px;height:30px;border:1px solid #dfe7ee;border-radius:7px;background:#fff;padding:0 7px;color:#42516a;font-size:11px;cursor:pointer}
.purchase-pager button.active{border-color:#d5eee4;background:#e9f8f2;color:#07875b;font-weight:800}
.purchase-pager button:disabled{opacity:.45;cursor:default}
.purchase-pager select{margin-left:5px;padding:0 10px;color:#35455f}
.purchase-state{display:grid;min-height:430px;place-items:center;color:#67758b;text-align:center}
.purchase-state-inner{max-width:300px}
.purchase-state-inner h3{margin:0 0 6px;color:#263750;font-size:15px;font-weight:800}
.purchase-state-inner p{margin:0 0 12px;font-size:11px;line-height:1.5}
.purchase-state-inner .purchase-state-detail{color:#a94448}
.purchase-state-inner button{margin:auto}
.purchase-state-icon{display:grid;width:52px;height:52px;margin:0 auto 10px;place-items:center;border-radius:50%;background:#f1f6fb;color:#5c6d86}
.purchase-state-icon .line-icon{width:26px;height:26px}
.purchase-state-icon.danger{background:#fff0f1;color:#df3e46}
.purchase-spinner{width:34px;height:34px;margin:0 auto 10px;border:4px solid #dce8ff;border-top-color:#3475e5;border-radius:50%;animation:purchase-spin .8s linear infinite}
@keyframes purchase-spin{to{transform:rotate(360deg)}}

.purchase-overlay{position:fixed;z-index:55;inset:0;background:transparent}
.purchase-drawer{position:fixed;z-index:60;top:var(--app-topbar-height,0px);right:0;bottom:0;display:flex;width:min(320px,100vw);flex-direction:column;border-left:1px solid #dfe7ee;background:#fff;box-shadow:-12px 0 30px #1f30471a}
.purchase-drawer.wide{width:min(470px,100vw)}
.purchase-drawer.xwide{width:min(650px,100vw)}
.purchase-drawer-head{display:flex;min-height:54px;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #dfe7ee;padding:0 16px}
.purchase-drawer-head h2{margin:0;font-size:17px;font-weight:800;letter-spacing:-.02em}
.purchase-icon-button{display:grid;width:32px;height:32px;place-items:center;border:0;border-radius:8px;background:transparent;color:#536178;font-size:21px;cursor:pointer}
.purchase-icon-button:hover{background:#f4f8fa}
.purchase-drawer-body{flex:1;overflow:auto;padding:14px 16px 20px}
.purchase-drawer-foot{display:flex;gap:10px;border-top:1px solid #dfe7ee;padding:12px 16px}
.purchase-drawer-foot>button{flex:1}
.purchase-section{margin-bottom:14px;border-bottom:1px solid #edf1f4;padding-bottom:14px}
.purchase-section:last-child{border-bottom:0}
.purchase-section h3{margin:0 0 10px;font-size:12px;font-weight:800}
.purchase-check-row{display:flex;align-items:center;gap:8px;margin:8px 0;color:#3f4f66;font-size:11px;cursor:pointer}
.purchase-check-row input{width:15px;height:15px;margin:0;accent-color:#0a9c67}
.purchase-field{display:grid;gap:6px;margin-bottom:11px;color:#3e4e66;font-size:10.8px;font-weight:700}
.purchase-input{width:100%;height:34px;border:1px solid #ccd9e4;border-radius:7px;background:#fff;padding:0 9px;color:#31415a;font-size:11px;font-weight:400}
.purchase-input[readonly]{background:#f7fafc}
.purchase-textarea{min-height:64px;padding-top:8px;resize:vertical}
.purchase-grid2{display:grid;grid-template-columns:1fr 1fr;gap:10px}
.purchase-hint{display:block;margin:0;color:#748196;font-size:10px;font-weight:400;line-height:1.45}
.purchase-error{margin:0 0 10px;border-radius:8px;background:#fff0f1;padding:8px 10px;color:#a94448;font-size:12px}
.required{color:#d63e45}

.purchase-back-title{display:flex;min-width:0;align-items:center;gap:9px}
.purchase-back-title h2{white-space:nowrap}
.purchase-detail-actions{position:relative;display:flex;gap:8px}
.purchase-small-button{display:inline-flex;height:34px;align-items:center;gap:6px;border:1px solid #ccd9e4;border-radius:8px;background:#fff;padding:0 10px;color:#2d3c54;font-size:11px;font-weight:700;white-space:nowrap;cursor:pointer}
.purchase-small-button .line-icon{width:14px;height:14px}
.purchase-small-button:disabled{opacity:.55;cursor:default}
.purchase-action-menu{position:absolute;z-index:5;top:40px;right:0;width:180px;border:1px solid #dfe7ee;border-radius:9px;background:#fff;padding:5px;box-shadow:0 14px 34px #17203324}
.purchase-action-menu button,.purchase-action-menu a{display:flex;width:100%;height:32px;align-items:center;border:0;border-radius:6px;background:transparent;padding:0 9px;color:#314159;font-size:11px;text-align:left;text-decoration:none;cursor:pointer}
.purchase-action-menu button:hover,.purchase-action-menu a:hover{background:#f4f8fa}
.purchase-action-menu .danger{color:#df3e46}
.purchase-menu-note{display:block;padding:6px 9px;color:#748196;font-size:10.5px}
.purchase-tabs{display:flex;gap:16px;margin:-2px 0 12px;border-bottom:1px solid #dfe7ee}
.purchase-tab{height:36px;border:0;border-bottom:2px solid transparent;background:transparent;color:#66758d;font-size:11px;font-weight:700;cursor:pointer}
.purchase-tab.active{border-bottom-color:#0a9c67;color:#087e57}
.purchase-void-box{margin-bottom:12px;border:1px solid #f4c7ca;border-radius:9px;background:#fff6f6;padding:9px 11px;color:#a93d44;font-size:11px;line-height:1.5}
.purchase-detail-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px}
.purchase-info-card{border:1px solid #dfe7ee;border-radius:9px;background:#fff;padding:11px}
.purchase-info-card h3{margin:0 0 9px;font-size:12px;font-weight:800}
.purchase-payment{margin-bottom:10px}
.purchase-kv{display:grid;grid-template-columns:96px 1fr;gap:7px;margin:5px 0;font-size:10.8px}
.purchase-kv>span{color:#66758d}
.purchase-kv.total{border-top:1px solid #dfe7ee;padding-top:7px}
.purchase-kv.total b{font-size:14px}
.purchase-kv.green b{color:#087e57;font-weight:800}
.purchase-kv.red b{color:#d64047;font-weight:800}
.purchase-info-card .purchase-hint{margin-top:8px}
.purchase-subtitle{margin:14px 0 6px;font-size:12px;font-weight:800}
.purchase-subtitle--flush{margin-top:0}
.purchase-detail-table{width:100%;border-collapse:collapse}
.purchase-detail-table th,.purchase-detail-table td{border-bottom:1px solid #edf1f4;padding:7px;font-size:10px;text-align:left}
.purchase-detail-table th{background:#f7fafc;color:#627188}
.purchase-detail-table tr:last-child td{border-bottom:0}

.purchase-wizard{position:fixed;z-index:70;top:var(--app-topbar-height,0px);right:0;bottom:0;left:var(--app-sidebar-width,0px);display:grid;place-items:start center;overflow:auto;background:#f4f8fbf5;padding:20px 14px}
.purchase-wizard-card{width:min(760px,96vw);overflow:hidden;border:1px solid #dfe7ee;border-radius:14px;background:#fff;box-shadow:0 10px 30px #1f304714}
.purchase-wizard-head{display:flex;height:54px;align-items:center;justify-content:space-between;border-bottom:1px solid #dfe7ee;padding:0 16px}
.purchase-wizard-head h2{margin:0;font-size:16px;font-weight:800}
.purchase-steps{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;border-bottom:1px solid #edf1f4;padding:12px 16px}
.purchase-step{display:flex;align-items:center;gap:7px;color:#98a4b6;font-size:10.5px}
.purchase-step .n{display:grid;width:20px;height:20px;flex:none;place-items:center;border-radius:50%;background:#cbd4df;color:#fff;font-size:10px;font-weight:800}
.purchase-step.active{color:#087e57;font-weight:800}
.purchase-step.active .n,.purchase-step.done .n{background:#0a9c67}
.purchase-wizard-body{min-height:380px;padding:14px 16px 18px}
.purchase-wizard-foot{display:flex;justify-content:flex-end;gap:10px;border-top:1px solid #dfe7ee;padding:12px 16px}
.purchase-wizard-foot button{min-width:100px}
.purchase-supplier-row{display:grid;grid-template-columns:1fr auto;gap:8px}
.purchase-supplier-card{border:1px solid #cde6ff;border-radius:8px;background:#edf7ff;padding:10px;font-size:10.8px}
.purchase-supplier-card h4{margin:0 0 7px;font-size:11px;font-weight:800}
.purchase-supplier-card .purchase-kv{grid-template-columns:120px 1fr}
.purchase-note-field{margin-top:14px}
.purchase-mini-toolbar{display:grid;grid-template-columns:1fr 76px;gap:8px;margin-bottom:10px}
.purchase-product-search{position:relative}
.purchase-suggestions{position:absolute;z-index:3;top:40px;right:0;left:0;border:1px solid #dfe7ee;border-radius:8px;background:#fff;padding:4px;box-shadow:0 14px 34px #17203324}
.purchase-suggestions button{display:flex;width:100%;height:32px;align-items:center;justify-content:space-between;border:0;border-radius:6px;background:transparent;padding:0 9px;color:#314159;font-size:11px;cursor:pointer}
.purchase-suggestions button:hover{background:#f4f8fa}
.purchase-suggestions small{color:#748196}
.purchase-add{justify-content:center;border-color:transparent;background:#0a9c67;color:#fff}
.purchase-add:disabled{background:#bfe6d6;color:#fff}
.purchase-line-table{width:100%;overflow:hidden;border:1px solid #dfe7ee;border-radius:8px;border-collapse:separate;border-spacing:0}
.purchase-line-table th,.purchase-line-table td{border-bottom:1px solid #edf1f4;padding:7px;font-size:10px}
.purchase-line-table th{background:#f7fafc;color:#607087;text-align:left}
.purchase-line-table tr:last-child td{border-bottom:0}
.purchase-num{width:70px;height:28px;border:1px solid #ccd9e4;border-radius:6px;padding:0 7px;font-size:10px;text-align:right}
.purchase-remove{border:0;background:none;color:#d93f45;font-size:15px;cursor:pointer}
.purchase-empty-line{color:#748196;text-align:center}
.purchase-line-summary{display:flex;justify-content:space-between;margin-top:12px;font-size:11px}
.purchase-pay-layout{display:grid;grid-template-columns:1.3fr 1fr;gap:14px}
.purchase-pay-options{display:grid;gap:8px}
.purchase-pay-option{display:flex;align-items:flex-start;gap:9px;border:1px solid #dfe7ee;border-radius:8px;background:#fff;padding:10px;color:#172033;font-size:10.8px;text-align:left;cursor:pointer}
.purchase-pay-option.active{border-color:#b8d7ff;background:#f1f7ff}
.purchase-pay-option b{display:block;margin-bottom:2px}
.purchase-radio{position:relative;width:14px;height:14px;flex:none;margin-top:1px;border:1px solid #9db0c0;border-radius:50%;background:#fff}
.purchase-radio.on::after{position:absolute;inset:3px;border-radius:50%;background:#1b67e9;content:""}
.purchase-summary{border:1px solid #dfe7ee;border-radius:9px;background:#fbfdfe;padding:11px}
.purchase-summary-row{display:flex;justify-content:space-between;gap:10px;margin:8px 0;font-size:10.8px}
.purchase-summary-row.total{border-top:1px solid #dfe7ee;padding-top:9px;font-size:13px;font-weight:850}
.purchase-summary-row.green b{color:#087e57}
.purchase-summary-row.red b{color:#d64047}
.purchase-confirm{margin-bottom:10px;border:1px solid #dfe7ee;border-radius:9px;padding:11px}
.purchase-confirm h3{margin:0 0 8px;font-size:11px;font-weight:800}
.purchase-confirm .purchase-kv{grid-template-columns:140px 1fr}
.purchase-accordion{display:flex;width:100%;align-items:center;justify-content:space-between;border:0;background:none;padding:0;color:#172033;font-size:11px;font-weight:800;cursor:pointer}
.purchase-accordion .line-icon{width:14px;height:14px}
.purchase-accordion[aria-expanded="true"] .line-icon{transform:rotate(180deg)}
.purchase-confirm .purchase-detail-table{margin-top:8px}

.purchase-modal-backdrop{position:fixed;z-index:90;inset:0;display:grid;place-items:center;background:#202d3d33;padding:20px}
.purchase-modal{width:min(430px,94vw);overflow:hidden;border:1px solid #dfe7ee;border-radius:14px;background:#fff;box-shadow:0 22px 60px #1f30472e}
.purchase-modal-head{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #dfe7ee;padding:14px 16px}
.purchase-modal-head h3{margin:0;font-size:15px;font-weight:800}
.purchase-modal-body{padding:18px 16px;text-align:center}
.purchase-danger-icon{display:grid;width:45px;height:45px;margin:0 auto 10px;place-items:center;border-radius:50%;background:#fff0f1;color:#df3e46}
.purchase-danger-icon .line-icon{width:24px;height:24px}
.purchase-modal-body h4{margin:0 0 7px;color:#d83d45;font-size:14px;font-weight:800}
.purchase-modal-body p{margin:0 0 14px;color:#67758b;font-size:11px;line-height:1.5}
.purchase-left{text-align:left}
.purchase-modal-foot{display:flex;justify-content:flex-end;gap:9px;border-top:1px solid #dfe7ee;padding:12px 16px}
.purchase-modal-foot button{height:36px;padding:0 15px}
.purchase-info-note{margin-bottom:12px;border:1px solid #d1e6fa;border-radius:8px;background:#edf7ff;padding:8px 10px;color:#35608d;font-size:10.5px}

.purchase-demo{position:fixed;z-index:50;bottom:14px;left:calc(var(--app-sidebar-width,0px) + 16px)}
.purchase-demo-button{height:30px;border:1px solid #ccd9e4;border-radius:8px;background:#fff;padding:0 10px;color:#5b6980;font-size:10px;cursor:pointer}
.purchase-demo-menu{position:absolute;bottom:36px;left:0;width:170px;border:1px solid #dfe7ee;border-radius:9px;background:#fff;padding:5px;box-shadow:0 10px 30px #1f304714}
.purchase-demo-menu button{width:100%;height:30px;border:0;border-radius:6px;background:#fff;padding:0 8px;color:#394960;font-size:10.5px;text-align:left;cursor:pointer}
.purchase-demo-menu button:hover{background:#f4f8fa}
.purchase-toast{position:fixed;z-index:120;right:18px;bottom:18px;border-radius:9px;background:#15392f;padding:10px 14px;color:#fff;font-size:11px;box-shadow:0 10px 30px #1f304714}
.purchase-toast.raised{bottom:76px}

@media(max-width:1100px){.purchase-table-wrap{overflow-x:auto}.purchase-table{min-width:1000px}}
@media(max-width:1000px){.purchase-detail-grid,.purchase-pay-layout{grid-template-columns:1fr}}
@media(max-width:760px){
  .purchase-panel{min-height:auto}
  .purchase-head{align-items:center}
  .purchase-head h1{font-size:19px}
  .purchase-create{font-size:13px}
  .purchase-toolbar{grid-template-columns:1fr}
  .purchase-chips{overflow-x:auto}
  .purchase-footer{align-items:flex-start;flex-direction:column}
  .purchase-pager{flex-wrap:wrap}
  .purchase-drawer,.purchase-drawer.wide,.purchase-drawer.xwide{width:100%}
  .purchase-back-title h2{font-size:15px;white-space:normal}
  .purchase-wizard{padding:8px}
  .purchase-wizard-card{width:100%}
  .purchase-step{font-size:0}
  .purchase-step .n{margin:auto}
  .purchase-grid2{grid-template-columns:1fr}
  .purchase-line-table{display:block;overflow-x:auto}
}
</style>
