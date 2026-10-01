<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import type { C14AttentionDetail, C14AttentionKind, C14AttentionList, InventoryMovement, Product, ProductPage, StocktakeContext } from '../api/types'
import LineIcon from '../components/ui/LineIcon'
import type { IconName } from '../components/ui/icons'
import {
  inventoryDemoEnabled, previewTone, referenceActor, referenceChipCounts, referenceCounts, referenceInventory, referenceMovements,
  referenceNow, referencePageCount, referenceStocktake, type InventoryChip, type InventoryPreview, type MovementPreview, type StockTone,
} from '../inventory/demo'
import { useAuthStore } from '../stores/auth'

type ListState = 'normal' | 'empty' | 'noresult' | 'loading' | 'error'
type StockFilter = 'ok' | 'out' | 'low' | 'negative' | 'soon'
type ActionKind = 'adjust' | 'stocktake'
interface Filter { stock: StockFilter[]; category: string; unit: string; minStock: string; maxStock: string; minValue: string; maxValue: string; productStatus: 'all' | 'active' | 'inactive' }
interface Row { id: string; name: string; sku: string; barcode: string | null; unit: string; image: string; category: string; stock: number; avg: number | null; value: number | null; tone: StockTone; updated: string; isActive: boolean }
interface HistoryRow { date: string; type: string; quantity: number; cost: number; stock: string; actor: string; note: string }

const auth = useAuthStore()
const isOwner = computed(() => auth.session.roles.includes('Owner'))
const demo = inventoryDemoEnabled
const canAct = computed(() => demo.value || isOwner.value)

const cloneInventory = () => referenceInventory.map(item => ({ ...item }))
const defaultFilter = (): Filter => ({ stock: [], category: '', unit: '', minStock: '', maxStock: '', minValue: '', maxValue: '', productStatus: 'all' })
const mockRows = ref<InventoryPreview[]>(cloneInventory())
const demoMoves = ref<Record<string, MovementPreview[]>>({})
const result = ref<ProductPage | null>(null)
const costs = ref<Record<string, Product>>({})
const attention = ref<Record<string, C14AttentionKind>>({})
const liveSummary = ref<{ total: number | null; out: number | null; soon: number | null; negative: number | null }>({ total: null, out: null, soon: null, negative: null })
const search = ref('')
const chip = ref<InventoryChip>('all')
const filter = ref<Filter>(defaultFilter())
const draft = ref<Filter>(defaultFilter())
const page = ref(1)
const pageSize = ref(10)
const loading = ref(false)
const error = ref('')
const forcedState = ref<ListState>('normal')
const demoMenuOpen = ref(false)
const checkedIds = ref<string[]>([])
const menuId = ref<string | null>(null)
const drawer = ref<'filter' | 'detail' | null>(null)
const detailId = ref<string | null>(null)
const detailTab = ref<'overview' | 'history' | 'attention'>('overview')
const liveProduct = ref<Product | null>(null)
const liveMovements = ref<InventoryMovement[] | null>(null)
const liveAttention = ref<C14AttentionDetail | 'none' | 'error' | null>(null)
const detailLoading = ref(false)
const detailError = ref('')
const historyOpen = ref(false)
const historyFrom = ref('')
const historyTo = ref('')
const historyType = ref('Tất cả')
const toastText = ref('')
let searchTimer: ReturnType<typeof setTimeout> | undefined
let pickTimer: ReturnType<typeof setTimeout> | undefined
let toastTimer: ReturnType<typeof setTimeout> | undefined
let loadSequence = 0
let detailSequence = 0

const money = (value: number) => `${new Intl.NumberFormat('vi-VN').format(value)} đ`
const pad = (part: number) => String(part).padStart(2, '0')
function formatDateTime(value?: string | null) {
  const date = value ? new Date(value) : null
  return date && !Number.isNaN(date.getTime()) ? `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}` : '—'
}
const isoDate = (value: string) => /^\d{2}\/\d{2}\/\d{4}/.test(value.trim()) ? value.trim().slice(0, 10).split('/').reverse().join('-') : ''
const numberInput = (value: string) => value.trim() === '' || Number.isNaN(Number(value.replace(/\./g, ''))) ? null : Number(value.replace(/\./g, ''))
const signed = (value: number) => `${value > 0 ? '+' : ''}${value}`
const movementLabels: Record<string, string> = { OpeningBalance: 'Tồn đầu', Purchase: 'Nhập hàng', Sale: 'Bán hàng', ReturnRestock: 'Trả hàng', SaleVoid: 'Hủy bán hàng', PurchaseVoid: 'Hủy nhập hàng', Adjustment: 'Điều chỉnh', StocktakeAdjustment: 'Kiểm kho' }
const attentionLabels: Record<C14AttentionKind, string> = { NegativeStock: 'Tồn kho âm', OutOfStock: 'Hết hàng', LowStockRisk: 'Sắp hết hàng' }
const adjustReasons = ['Hàng hỏng', 'Mất mát', 'Cân chỉnh sổ sách', 'Khác']

function toneLabel(tone: StockTone) {
  return { ok: demo.value ? 'Bình thường' : 'Còn hàng', out: 'Hết hàng', low: 'Tồn kho thấp', negative: 'Tồn kho âm', soon: 'Sắp hết hàng', off: 'Ngừng bán' }[tone]
}
/** Live tone: factual zero/negative balance first, then the approved C14 LowStockRisk signal; no invented threshold. */
function liveTone(item: { id: string; isActive: boolean; quantityOnHand: number }): StockTone {
  if (!item.isActive) return 'off'
  if (item.quantityOnHand < 0) return 'negative'
  if (item.quantityOnHand === 0) return 'out'
  return attention.value[item.id] === 'LowStockRisk' ? 'soon' : 'ok'
}
const stockClass = (row: Row) => row.stock < 0 ? 'negative' : row.stock === 0 ? 'zero' : row.tone === 'low' || row.tone === 'soon' ? 'low' : ''

const rows = computed<Row[]>(() => demo.value
  ? mockRows.value.map(item => ({ ...item, tone: item.status }))
  : (result.value?.items ?? []).map(item => {
    const detail = costs.value[item.id]
    return {
      id: item.id, name: item.name, sku: item.sku, barcode: item.barcode, unit: item.unit, image: '', category: '',
      stock: item.quantityOnHand, avg: detail?.hasAverageCost ? detail.averageCost : null, value: detail ? detail.inventoryValue : null,
      tone: liveTone(item), updated: formatDateTime(item.updatedAt ?? detail?.updatedAt), isActive: item.isActive,
    }
  }))
const chips = computed(() => demo.value
  ? [['all', 'Tất cả'], ['out', 'Hết hàng'], ['low', 'Tồn kho thấp'], ['negative', 'Tồn kho âm'], ['soon', 'Sắp hết hàng'], ['other', 'Khác']] as Array<[InventoryChip, string]>
  : [['all', 'Tất cả'], ['out', 'Hết hàng'], ['negative', 'Tồn kho âm'], ...(isOwner.value ? [['soon', 'Sắp hết hàng']] : [])] as Array<[InventoryChip, string]>)
const stockOptions = computed(() => demo.value
  ? [['ok', 'Bình thường'], ['out', 'Hết hàng (tồn = 0)'], ['low', 'Tồn kho thấp'], ['negative', 'Tồn kho âm (tồn < 0)'], ['soon', 'Sắp hết hàng']] as Array<[StockFilter, string]>
  : [['out', 'Hết hàng (tồn = 0)'], ['negative', 'Tồn kho âm (tồn < 0)'], ...(isOwner.value ? [['soon', 'Sắp hết hàng (C14)']] : [])] as Array<[StockFilter, string]>)
const categories = computed(() => [...new Set(mockRows.value.map(item => item.category))])
const units = computed(() => [...new Set(mockRows.value.map(item => item.unit))])

function chipMatches(row: Row, key: InventoryChip | StockFilter) {
  if (key === 'all') return true
  if (key === 'other' || key === 'ok') return row.tone === 'ok'
  if (demo.value) return key === 'soon' ? row.stock > 0 && row.stock <= 8 : row.tone === key
  return key === 'out' ? row.isActive && row.stock === 0 : key === 'negative' ? row.isActive && row.stock < 0 : row.tone === key
}
function bucketCount(source: InventoryPreview[], key: InventoryChip) {
  return source.filter(item => chipMatches({ ...item, tone: item.status, avg: item.avg, value: item.value }, key)).length
}
/** Reference totals, moved by whatever changed in this preview session. */
function chipCount(key: InventoryChip): number | undefined {
  if (demo.value) return referenceChipCounts[key] + bucketCount(mockRows.value, key) - bucketCount(referenceInventory, key)
  return key === 'all' ? result.value?.totalCount : undefined
}
const chipText = (key: InventoryChip, label: string) => chipCount(key) === undefined ? label : `${label} (${chipCount(key)})`

const filteredRows = computed(() => {
  const query = search.value.trim().toLocaleLowerCase('vi-VN')
  const current = filter.value
  return rows.value.filter(row => {
    if (!chipMatches(row, chip.value)) return false
    if (current.stock.length && !current.stock.some(key => chipMatches(row, key))) return false
    if (!demo.value) return true
    if (query && !`${row.name} ${row.sku} ${row.barcode ?? ''}`.toLocaleLowerCase('vi-VN').includes(query)) return false
    if (current.category && row.category !== current.category) return false
    if (current.unit && row.unit !== current.unit) return false
    if (current.productStatus !== 'all' && row.isActive !== (current.productStatus === 'active')) return false
    const bounds = [[numberInput(current.minStock), numberInput(current.maxStock), row.stock], [numberInput(current.minValue), numberInput(current.maxValue), row.value ?? 0]] as const
    return bounds.every(([min, max, value]) => (min === null || value >= min) && (max === null || value <= max))
  })
})
const narrowed = computed(() => search.value.trim() !== '' || chip.value !== 'all' || JSON.stringify(filter.value) !== JSON.stringify(defaultFilter()))
/** Live search and product status go to the API; stock-state chips and filters only read the loaded page. */
const pageLocal = computed(() => !demo.value && (chip.value !== 'all' || filter.value.stock.length > 0))
const visibleRows = computed(() => demo.value ? filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value) : filteredRows.value)
const totalCount = computed(() => demo.value ? narrowed.value ? filteredRows.value.length : referencePageCount : result.value?.totalCount ?? 0)
const totalPages = computed(() => demo.value ? Math.ceil(totalCount.value / pageSize.value) : result.value?.totalPages ?? 0)
const firstIndex = computed(() => totalCount.value ? (page.value - 1) * pageSize.value + 1 : 0)
const lastIndex = computed(() => totalCount.value ? Math.min(firstIndex.value + (demo.value ? visibleRows.value.length : result.value?.items.length ?? 0) - 1, totalCount.value) : 0)
const pagerItems = computed<Array<number | '…'>>(() => {
  const total = totalPages.value
  const current = page.value
  if (total <= 7) return Array.from({ length: total }, (_, index) => index + 1)
  if (current <= 4) return [1, 2, 3, 4, 5, '…', total]
  if (current >= total - 3) return [1, '…', total - 4, total - 3, total - 2, total - 1, total]
  return [1, '…', current - 1, current, current + 1, '…', total]
})
const allChecked = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(row => checkedIds.value.includes(row.id)))
const listState = computed<ListState>(() => {
  if (demo.value && forcedState.value !== 'normal') return forcedState.value
  if (!demo.value && loading.value) return 'loading'
  if (!demo.value && error.value) return 'error'
  if (visibleRows.value.length === 0) return narrowed.value ? 'noresult' : 'empty'
  return 'normal'
})
const stateCopy = computed(() => ({
  normal: null,
  empty: { icon: 'inventory' as IconName, title: 'Chưa có sản phẩm trong kho', text: 'Thêm sản phẩm để bắt đầu theo dõi tồn kho tại Kho chính.', action: '' },
  noresult: { icon: 'search' as IconName, title: 'Không tìm thấy sản phẩm phù hợp', text: 'Thử thay đổi từ khóa hoặc bộ lọc.', action: 'Xóa bộ lọc' },
  loading: { icon: null, title: 'Đang tải tồn kho...', text: 'Vui lòng chờ trong giây lát.', action: '' },
  error: { icon: 'warning' as IconName, title: 'Không thể tải dữ liệu tồn kho', text: 'Vui lòng thử lại sau hoặc liên hệ quản trị viên nếu lỗi vẫn tiếp diễn.', action: 'Thử lại' },
})[listState.value])
const summaryCards = computed(() => {
  if (demo.value) {
    return [
      { key: 'all' as InventoryChip, label: 'Tổng số sản phẩm', value: String(referencePageCount), note: 'Sản phẩm đang sử dụng', tone: '' },
      { key: 'out' as InventoryChip, label: 'Hết hàng', value: String(chipCount('out')), note: 'Sản phẩm', tone: 'danger' },
      { key: 'low' as InventoryChip, label: 'Tồn kho thấp', value: String(chipCount('low')), note: 'Sản phẩm', tone: 'warning' },
      { key: 'negative' as InventoryChip, label: 'Tồn kho âm', value: String(chipCount('negative')), note: 'Sản phẩm', tone: 'negative' },
    ]
  }
  const c14 = (value: number | null) => !isOwner.value || value === null ? '—' : String(value)
  const note = isOwner.value ? 'Cần chú ý theo C14' : 'Chủ cửa hàng xem tín hiệu C14'
  return [
    { key: 'all' as InventoryChip, label: 'Tổng số sản phẩm', value: liveSummary.value.total === null ? '—' : String(liveSummary.value.total), note: 'Sản phẩm đang bán', tone: '' },
    { key: 'out' as InventoryChip, label: 'Hết hàng', value: c14(liveSummary.value.out), note, tone: 'danger' },
    { key: 'soon' as InventoryChip, label: 'Sắp hết hàng', value: c14(liveSummary.value.soon), note, tone: 'warning' },
    { key: 'negative' as InventoryChip, label: 'Tồn kho âm', value: c14(liveSummary.value.negative), note, tone: 'negative' },
  ]
})

/* Detail */
const detailRow = computed(() => rows.value.find(row => row.id === detailId.value) ?? null)
const demoHistory = (id: string): HistoryRow[] => [...(demoMoves.value[id] ?? []), ...referenceMovements].map(item => ({ ...item, note: '' }))
const liveHistory = computed<HistoryRow[]>(() => [...(liveMovements.value ?? [])]
  .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt))
  .map(item => ({ date: formatDateTime(item.occurredAt), type: movementLabels[item.type] ?? item.type, quantity: item.quantityDelta, cost: item.unitCost, stock: '—', actor: '', note: item.reason ?? '' })))
const detail = computed(() => {
  if (demo.value) {
    const row = mockRows.value.find(item => item.id === detailId.value)
    if (!row) return null
    const warnings: Partial<Record<StockTone, string>> = {
      out: 'Tồn kho hiện tại bằng 0; cần nhập thêm hàng.',
      negative: 'Số tồn đang âm; kiểm kho hoặc kiểm tra các giao dịch gần đây.',
      low: 'Còn ít hàng; cân nhắc nhập thêm.',
    }
    const warning = warnings[row.status]
    return {
      id: row.id, name: row.name, sku: row.sku, unit: row.unit, image: row.image, isActive: row.isActive, tone: row.status,
      stock: `${row.stock} ${row.unit}`, value: money(row.value), valueNegative: row.value < 0, avg: `${money(row.avg)} / ${row.unit}`,
      avgNote: `Giá bán tham chiếu ${money(row.salePrice)}`, updated: row.updated,
      info: [['Tên sản phẩm', row.name], ['Mã SKU', row.sku], ['Mã vạch', row.barcode], ['Đơn vị tính', row.unit], ['Danh mục', row.category]],
      c14: warning
        ? { tone: 'warn', title: toneLabel(row.status), text: warning, link: '' }
        : { tone: 'ok', title: 'Không có cảnh báo', text: 'Sản phẩm này đang ở trạng thái bình thường.', link: '' },
      history: [...(demoMoves.value[row.id] ?? []), ...[0, 1, 3].map(index => referenceMovements[index]!)].slice(0, 5)
        .map(item => ({ date: item.date.split(' ')[0]!, type: item.type, quantity: item.quantity, extra: item.stock })),
      historyExtra: 'Tồn sau GD',
    }
  }
  const product = liveProduct.value
  if (!product) return null
  const tone = liveTone(product)
  const c14 = !isOwner.value
    ? { tone: 'muted', title: 'Chỉ chủ cửa hàng xem được', text: 'Tín hiệu C14 dành cho chủ cửa hàng.', link: '' }
    : liveAttention.value === null
      ? { tone: 'muted', title: 'Đang tải tín hiệu C14…', text: '', link: '' }
      : liveAttention.value === 'error'
        ? { tone: 'muted', title: 'Không tải được tín hiệu C14', text: 'Thử mở lại chi tiết sản phẩm.', link: '' }
        : liveAttention.value === 'none'
          ? { tone: 'ok', title: 'Không có tín hiệu cần chú ý theo C14 hiện tại', text: 'C14 chỉ xét sản phẩm đang bán có bằng chứng bán hàng gần đây; không có tín hiệu không chứng minh tồn kho đủ.', link: '' }
          : { tone: 'warn', title: attentionLabels[liveAttention.value.attentionKind], text: `Tồn hiện tại ${liveAttention.value.currentStock} ${product.unit} · bán ròng ${liveAttention.value.netSoldQuantity} trong 7 ngày đã đóng.`, link: `/today/attention/${product.id}` }
  return {
    id: product.id, name: product.name, sku: product.sku, unit: product.unit, image: '', isActive: product.isActive, tone,
    stock: `${product.quantityOnHand} ${product.unit}`, value: money(product.inventoryValue), valueNegative: product.inventoryValue < 0,
    avg: product.hasAverageCost ? `${money(product.averageCost)} / ${product.unit}` : 'Chưa xác định',
    avgNote: product.hasAverageCost ? `Giá bán ${money(product.salePrice)}` : 'Chưa có giá vốn bình quân tin cậy',
    updated: formatDateTime(product.updatedAt),
    info: [['Tên sản phẩm', product.name], ['Mã SKU', product.sku], ['Mã vạch', product.barcode || 'Không có'], ['Đơn vị tính', product.unit], ['Giá bán', money(product.salePrice)], ['Giá nhập tham chiếu', product.referencePurchaseCost === null ? '—' : money(product.referencePurchaseCost)]],
    c14,
    history: liveHistory.value.slice(0, 5).map(item => ({ date: item.date, type: item.type, quantity: item.quantity, extra: money(item.cost) })),
    historyExtra: 'Đơn giá vốn',
  }
})
const historyRows = computed(() => {
  const source = demo.value ? demoHistory(detailId.value ?? '') : liveHistory.value
  const from = isoDate(historyFrom.value)
  const to = isoDate(historyTo.value)
  return source.filter(item => {
    const date = isoDate(item.date)
    return (!from || date >= from) && (!to || date <= to) && (historyType.value === 'Tất cả' || item.type === historyType.value)
  })
})

/* Adjustment and stocktake: one dialog, one Product per live submission. */
const actionKind = ref<ActionKind | null>(null)
const step = ref(1)
const pickId = ref<string | null>(null)
const pickQuery = ref('')
const pickResults = ref<Row[]>([])
const pickLoading = ref(false)
const actionProduct = ref<Product | null>(null)
const actionError = ref('')
const actionBusy = ref(false)
const adjustQty = ref(0)
const adjustReason = ref('')
const adjustNote = ref('')
const adjustCost = ref<number | null>(null)
const stName = ref('')
const stDate = ref('')
const stNote = ref('')
const stScope = ref<'all' | 'some'>('all')
const stCounts = ref<Record<string, number>>({})
const stQuery = ref('')
const stPage = ref(1)
const stContext = ref<StocktakeContext | null>(null)
const stCounted = ref(0)
const stCost = ref<number | null>(null)
const adjustAttempt = ref<{ operationId: string; productId: string; quantityDelta: number; adjustmentUnitCost: number | null; reason: string } | null>(null)
const stocktakeAttempt = ref<{ operationId: string; productId: string; expectedQuantity: number; expectedRevision: string; countedQuantity: number; adjustmentUnitCost: number | null; note: string | null } | null>(null)
const stepLabels = computed(() => actionKind.value === 'adjust' ? ['Chọn sản phẩm', 'Nhập thông tin', 'Xác nhận'] : ['Chuẩn bị', 'Nhập số đếm', 'Xem chênh lệch', 'Xác nhận'])
const lastStep = computed(() => stepLabels.value.length)
const locked = computed(() => actionBusy.value || adjustAttempt.value !== null || stocktakeAttempt.value !== null)
const showPicker = computed(() => step.value === 1 && (actionKind.value === 'adjust' || !demo.value))
const pickRows = computed(() => {
  if (!demo.value) return pickResults.value
  const query = pickQuery.value.trim().toLocaleLowerCase('vi-VN')
  return rows.value.filter(row => !query || `${row.name} ${row.sku} ${row.barcode ?? ''}`.toLocaleLowerCase('vi-VN').includes(query)).slice(0, 5)
})
const pickRow = computed(() => rows.value.find(row => row.id === pickId.value) ?? pickResults.value.find(row => row.id === pickId.value) ?? null)
const adjustBefore = computed(() => demo.value ? pickRow.value?.stock ?? 0 : actionProduct.value?.quantityOnHand ?? 0)
const needsAdjustCost = computed(() => !demo.value && adjustQty.value > 0 && actionProduct.value?.hasAverageCost === false)
const countRows = computed(() => {
  const query = stQuery.value.trim().toLocaleLowerCase('vi-VN')
  return mockRows.value.filter(row => !query || `${row.name} ${row.sku}`.toLocaleLowerCase('vi-VN').includes(query))
})
const countPageRows = computed(() => countRows.value.slice((stPage.value - 1) * 5, stPage.value * 5))
const countDiff = (row: InventoryPreview) => (Number(stCounts.value[row.id]) || 0) - row.stock
const differenceRows = computed(() => mockRows.value.filter(row => countDiff(row) !== 0))
const referenceMismatches = referenceInventory.filter(row => (referenceCounts[row.sku] ?? row.stock) !== row.stock).length
/** Reference KPIs (156 / 140 / 16), moved by the counts edited in this preview. */
const stocktakeKpis = computed(() => {
  const mismatched = referenceStocktake.mismatched + differenceRows.value.length - referenceMismatches
  return { total: referenceStocktake.total, matched: referenceStocktake.total - mismatched, mismatched }
})
const stDiff = computed(() => stContext.value ? Number(stCounted.value) - stContext.value.expectedQuantity : 0)
const needsStocktakeCost = computed(() => stDiff.value > 0 && stContext.value?.hasAverageCost === false)

async function load(requestedPage = 1) {
  if (demo.value) { page.value = requestedPage; return }
  const sequence = ++loadSequence
  loading.value = true
  error.value = ''
  const params = new URLSearchParams({ page: String(requestedPage), pageSize: String(pageSize.value) })
  if (search.value.trim()) params.set('search', search.value.trim())
  if (filter.value.productStatus !== 'all') params.set('isActive', String(filter.value.productStatus === 'active'))
  try {
    const response = await apiRequest<ProductPage>(`/api/products?${params}`)
    if (sequence !== loadSequence) return
    result.value = response
    page.value = requestedPage
    loading.value = false
    loadCosts(response, sequence)
  } catch (reason) {
    if (sequence === loadSequence) error.value = reason instanceof Error ? reason.message : 'Không thể tải tồn kho.'
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}
/** The list API has no cost fields; average cost and value come from each visible Product's own detail. */
async function loadCosts(response: ProductPage, sequence: number) {
  const products = await Promise.all(response.items.map(item => apiRequest<Product>(`/api/products/${item.id}`).catch(() => null)))
  if (sequence !== loadSequence) return
  costs.value = Object.fromEntries(products.filter((item): item is Product => item !== null).map(item => [item.id, item]))
}
async function loadSummary() {
  const sequence = loadSequence
  const active = await apiRequest<ProductPage>('/api/products?page=1&pageSize=1&isActive=true').then(page => page.totalCount).catch(() => null)
  let kinds: Record<string, C14AttentionKind> = {}
  if (isOwner.value) {
    try {
      for (let next = 1, pages = 1; next <= pages && next <= 5; next += 1) {
        const list = await apiRequest<C14AttentionList>(`/api/today/attention?page=${next}&pageSize=100`)
        pages = list.totalPages
        kinds = { ...kinds, ...Object.fromEntries(list.items.map(item => [item.productId, item.attentionKind])) }
      }
    } catch { kinds = {} }
  }
  if (demo.value || sequence !== loadSequence) return
  const count = (kind: C14AttentionKind) => isOwner.value ? Object.values(kinds).filter(value => value === kind).length : null
  attention.value = kinds
  liveSummary.value = { total: active, out: count('OutOfStock'), soon: count('LowStockRisk'), negative: count('NegativeStock') }
}

function toast(message: string) {
  toastText.value = message
  if (toastTimer) clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toastText.value = '' }, 2200)
}
function closeAll() {
  drawer.value = null
  historyOpen.value = false
  menuId.value = null
  demoMenuOpen.value = false
  if (!actionBusy.value) actionKind.value = null
}
function chooseChip(key: InventoryChip) {
  chip.value = key
  checkedIds.value = []
  page.value = 1
  if (demo.value) load(1)
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
function toggleAll() { checkedIds.value = allChecked.value ? [] : visibleRows.value.map(row => row.id) }
function toggleChecked(id: string) { checkedIds.value = checkedIds.value.includes(id) ? checkedIds.value.filter(value => value !== id) : [...checkedIds.value, id] }
function openFilter() { draft.value = { ...filter.value, stock: [...filter.value.stock] }; drawer.value = 'filter' }
function toggleDraftStock(key: StockFilter | 'all') {
  if (key === 'all') draft.value.stock = []
  else draft.value.stock = draft.value.stock.includes(key) ? draft.value.stock.filter(value => value !== key) : [...draft.value.stock, key]
}
function applyFilter() {
  const reload = draft.value.productStatus !== filter.value.productStatus
  filter.value = { ...draft.value, stock: [...draft.value.stock] }
  drawer.value = null
  checkedIds.value = []
  page.value = 1
  if (demo.value) toast('Đã áp dụng bộ lọc')
  else if (reload) load(1)
}
function clearAll() {
  const reload = !demo.value && (filter.value.productStatus !== 'all' || search.value.trim() !== '')
  filter.value = defaultFilter()
  draft.value = defaultFilter()
  chip.value = 'all'
  forcedState.value = 'normal'
  drawer.value = null
  page.value = 1
  if (search.value) search.value = ''
  else if (reload) load(1)
}
function stateAction() {
  if (listState.value === 'noresult') clearAll()
  else if (demo.value) forcedState.value = 'normal'
  else { load(page.value); loadSummary() }
}

async function openDetail(row: Row) {
  menuId.value = null
  detailId.value = row.id
  detailTab.value = 'overview'
  drawer.value = 'detail'
  if (demo.value) return
  const sequence = ++detailSequence
  liveProduct.value = null
  liveMovements.value = null
  liveAttention.value = null
  detailError.value = ''
  detailLoading.value = true
  const attentionRequest = isOwner.value
    ? apiRequest<C14AttentionDetail>(`/api/today/attention/${row.id}`).catch(reason => reason instanceof ApiError && reason.status === 404 ? 'none' as const : 'error' as const)
    : Promise.resolve(null)
  try {
    const [product, movements] = await Promise.all([
      apiRequest<Product>(`/api/products/${row.id}`),
      apiRequest<InventoryMovement[]>(`/api/products/${row.id}/movements`).catch(() => [] as InventoryMovement[]),
    ])
    if (sequence !== detailSequence) return
    liveProduct.value = product
    liveMovements.value = movements
    detailLoading.value = false
    const signal = await attentionRequest
    if (sequence === detailSequence) liveAttention.value = signal
  } catch (reason) {
    if (sequence === detailSequence) detailError.value = reason instanceof Error ? reason.message : 'Không thể tải sản phẩm.'
  } finally {
    if (sequence === detailSequence) detailLoading.value = false
  }
}
async function openHistory(row?: Row) {
  menuId.value = null
  if (row && row.id !== detailId.value) {
    detailId.value = row.id
    liveProduct.value = null
    liveMovements.value = null
  }
  historyType.value = 'Tất cả'
  historyFrom.value = demo.value ? '01/12/2024' : ''
  historyTo.value = demo.value ? '16/12/2024' : ''
  drawer.value = null
  historyOpen.value = true
  if (demo.value || liveMovements.value || !detailId.value) return
  const id = detailId.value
  try {
    const [product, movements] = await Promise.all([apiRequest<Product>(`/api/products/${id}`), apiRequest<InventoryMovement[]>(`/api/products/${id}/movements`)])
    if (detailId.value === id) { liveProduct.value = product; liveMovements.value = movements }
  } catch (reason) {
    toast(reason instanceof Error ? reason.message : 'Không thể tải lịch sử tồn kho.')
  }
}
const historyTitle = computed(() => {
  const row = rows.value.find(item => item.id === detailId.value)
  const product = liveProduct.value?.id === detailId.value ? liveProduct.value : null
  const name = product?.name ?? row?.name ?? ''
  return { name, meta: [name, product?.sku ?? row?.sku, product?.unit ?? row?.unit].filter(Boolean).join(' · '), image: row?.image ?? '' }
})

async function searchPick() {
  if (demo.value) return
  pickLoading.value = true
  const params = new URLSearchParams({ page: '1', pageSize: '5', isActive: 'true' })
  if (pickQuery.value.trim()) params.set('search', pickQuery.value.trim())
  try {
    const response = await apiRequest<ProductPage>(`/api/products?${params}`)
    pickResults.value = response.items.map(item => ({
      id: item.id, name: item.name, sku: item.sku, barcode: item.barcode, unit: item.unit, image: '', category: '',
      stock: item.quantityOnHand, avg: null, value: null, tone: liveTone(item), updated: '', isActive: item.isActive,
    }))
  } catch (reason) {
    actionError.value = reason instanceof Error ? reason.message : 'Không thể tìm sản phẩm.'
  } finally { pickLoading.value = false }
}
function openAction(kind: ActionKind, row?: Row | null) {
  if (!canAct.value) return
  menuId.value = null
  drawer.value = null
  actionKind.value = kind
  step.value = 1
  actionError.value = ''
  pickQuery.value = ''
  pickResults.value = row && !demo.value ? [row] : []
  pickId.value = row?.id ?? (demo.value ? detailId.value ?? mockRows.value[0]?.id ?? null : null)
  actionProduct.value = null
  adjustAttempt.value = null
  stocktakeAttempt.value = null
  adjustQty.value = 0
  adjustReason.value = ''
  adjustNote.value = ''
  adjustCost.value = null
  stName.value = referenceStocktake.name
  stDate.value = referenceStocktake.date
  stNote.value = ''
  stScope.value = 'all'
  stQuery.value = ''
  stPage.value = 1
  stCounts.value = Object.fromEntries(mockRows.value.map(item => [item.id, referenceCounts[item.sku] ?? item.stock]))
  stContext.value = null
  stCounted.value = 0
  stCost.value = null
  if (!demo.value && !row) searchPick()
}
function actionBack() {
  if (locked.value) return
  actionError.value = ''
  if (step.value === 1) actionKind.value = null
  else step.value -= 1
}
async function actionNext() {
  actionError.value = ''
  if (actionKind.value === 'adjust') {
    if (step.value === 1) {
      if (!pickId.value) { actionError.value = 'Chọn một sản phẩm để điều chỉnh.'; return }
      if (!demo.value) {
        actionBusy.value = true
        try { actionProduct.value = await apiRequest<Product>(`/api/products/${pickId.value}`) }
        catch (reason) { actionError.value = reason instanceof Error ? reason.message : 'Không thể tải sản phẩm.'; return }
        finally { actionBusy.value = false }
      }
      step.value = 2
    } else if (step.value === 2) {
      if (!adjustQty.value) { actionError.value = 'Nhập số lượng điều chỉnh khác 0.'; return }
      if (!adjustReason.value) { actionError.value = 'Chọn lý do điều chỉnh.'; return }
      if (needsAdjustCost.value && (adjustCost.value === null || adjustCost.value < 0)) { actionError.value = 'Nhập giá vốn đơn vị cho lượng tăng.'; return }
      step.value = 3
    } else await confirmAdjust()
    return
  }
  if (step.value === 1) {
    if (demo.value) {
      if (!stName.value.trim() || !isoDate(stDate.value)) { actionError.value = 'Nhập tên đợt và ngày kiểm kho (dd/mm/yyyy).'; return }
    } else {
      if (!pickId.value) { actionError.value = 'Chọn sản phẩm cần kiểm kho.'; return }
      actionBusy.value = true
      try {
        stContext.value = await apiRequest<StocktakeContext>(`/api/inventory/stocktakes/context/${pickId.value}`)
        stCounted.value = stContext.value.expectedQuantity
      } catch (reason) { actionError.value = reason instanceof Error ? reason.message : 'Không thể tải số tồn.'; return }
      finally { actionBusy.value = false }
    }
    step.value = 2
  } else if (step.value === 2) {
    const counts = demo.value ? mockRows.value.map(row => Number(stCounts.value[row.id])) : [Number(stCounted.value)]
    if (counts.some(value => Number.isNaN(value) || value < 0)) { actionError.value = 'Số đếm phải là số không âm.'; return }
    step.value = 3
  } else if (step.value === 3) {
    if (needsStocktakeCost.value && (stCost.value === null || stCost.value < 0)) { actionError.value = 'Nhập giá vốn đơn vị cho lượng tăng.'; return }
    step.value = 4
  } else await confirmStocktake()
}
function recordDemoMove(row: InventoryPreview, type: string, quantity: number) {
  row.stock += quantity
  row.value = row.stock * row.avg
  row.status = previewTone(row.stock)
  row.updated = referenceNow
  demoMoves.value = { ...demoMoves.value, [row.id]: [{ date: referenceNow, type, quantity, cost: row.avg, stock: String(row.stock), actor: referenceActor }, ...(demoMoves.value[row.id] ?? [])] }
}
async function confirmAdjust() {
  if (demo.value) {
    const row = mockRows.value.find(item => item.id === pickId.value)
    if (row) recordDemoMove(row, 'Điều chỉnh', Number(adjustQty.value))
    actionKind.value = null
    toast('Đã điều chỉnh tồn kho trong dữ liệu mẫu')
    return
  }
  const attempt = adjustAttempt.value ?? {
    operationId: crypto.randomUUID(), productId: pickId.value!, quantityDelta: Number(adjustQty.value),
    adjustmentUnitCost: needsAdjustCost.value ? adjustCost.value : null,
    reason: [adjustReason.value, adjustNote.value.trim()].filter(Boolean).join(' — '),
  }
  adjustAttempt.value = attempt
  actionBusy.value = true
  try {
    await apiRequest('/api/inventory/adjustments', { method: 'POST', body: JSON.stringify(attempt) })
    adjustAttempt.value = null
    actionKind.value = null
    toast('Đã điều chỉnh tồn kho')
    load(page.value)
    loadSummary()
  } catch (reason) {
    if (reason instanceof ApiError) adjustAttempt.value = null
    actionError.value = reason instanceof ApiError ? reason.message : 'Chưa rõ kết quả điều chỉnh. Bấm Xác nhận để gửi lại đúng thao tác này.'
  } finally { actionBusy.value = false }
}
async function confirmStocktake() {
  if (demo.value) {
    differenceRows.value.forEach(row => recordDemoMove(row, 'Kiểm kho', countDiff(row)))
    actionKind.value = null
    toast('Đã xác nhận kiểm kho trong dữ liệu mẫu')
    return
  }
  const context = stContext.value
  if (!context) { step.value = 1; return }
  const attempt = stocktakeAttempt.value ?? {
    operationId: crypto.randomUUID(), productId: context.productId, expectedQuantity: context.expectedQuantity,
    expectedRevision: context.expectedRevision, countedQuantity: Number(stCounted.value),
    adjustmentUnitCost: needsStocktakeCost.value ? stCost.value : null, note: stNote.value.trim() || null,
  }
  stocktakeAttempt.value = attempt
  actionBusy.value = true
  try {
    await apiRequest('/api/inventory/stocktakes', { method: 'POST', body: JSON.stringify(attempt) })
    stocktakeAttempt.value = null
    actionKind.value = null
    toast('Đã ghi nhận kiểm kho')
    load(page.value)
    loadSummary()
  } catch (reason) {
    if (reason instanceof ApiError) stocktakeAttempt.value = null
    if (reason instanceof ApiError && reason.problem.code === 'stocktake-stale') {
      stContext.value = null
      step.value = 1
      actionError.value = 'Tồn kho đã thay đổi trong lúc kiểm. Bấm Tiếp theo để tải lại số tồn và đếm lại.'
    } else actionError.value = reason instanceof ApiError ? reason.message : 'Chưa rõ kết quả kiểm kho. Bấm Xác nhận để gửi lại đúng thao tác này.'
  } finally { actionBusy.value = false }
}

function showDemoState(state: ListState) {
  forcedState.value = state
  demoMenuOpen.value = false
}
function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') closeAll()
}
function onDocumentClick(event: MouseEvent) {
  if (menuId.value && !(event.target as HTMLElement | null)?.closest('.inventory-actions-cell')) menuId.value = null
}

watch(search, () => {
  checkedIds.value = []
  page.value = 1
  if (searchTimer) clearTimeout(searchTimer)
  if (!demo.value) searchTimer = setTimeout(() => load(1), 300)
})
watch(pickQuery, () => {
  if (pickTimer) clearTimeout(pickTimer)
  if (!demo.value && actionKind.value) pickTimer = setTimeout(searchPick, 300)
})
watch(stQuery, () => { stPage.value = 1 })
watch(demo, enabled => {
  ++loadSequence
  ++detailSequence
  if (searchTimer) clearTimeout(searchTimer)
  mockRows.value = cloneInventory()
  demoMoves.value = {}
  search.value = ''
  chip.value = 'all'
  filter.value = defaultFilter()
  page.value = 1
  pageSize.value = 10
  checkedIds.value = []
  forcedState.value = 'normal'
  detailId.value = null
  loading.value = false
  error.value = ''
  adjustAttempt.value = null
  stocktakeAttempt.value = null
  actionKind.value = null
  closeAll()
  if (!enabled) { load(1); loadSummary() }
})
onMounted(() => {
  window.addEventListener('keydown', onKeydown)
  document.addEventListener('click', onDocumentClick)
  if (!demo.value) { load(1); loadSummary() }
})
onUnmounted(() => {
  ++loadSequence
  ++detailSequence
  for (const timer of [searchTimer, pickTimer, toastTimer]) if (timer) clearTimeout(timer)
  window.removeEventListener('keydown', onKeydown)
  document.removeEventListener('click', onDocumentClick)
})
</script>

<template>
  <section class="inventory-page">
    <div class="inventory-panel">
      <div class="inventory-head">
        <div><h1>Tồn kho</h1><p>Quản lý tồn kho, kiểm kho, điều chỉnh và lịch sử biến động</p></div>
        <div v-if="canAct" class="inventory-head-actions">
          <button class="inventory-outline" type="button" @click="openAction('stocktake')"><LineIcon name="clipboard" />Kiểm kho</button>
          <button class="inventory-outline" type="button" @click="openAction('adjust')"><LineIcon name="sliders" />Điều chỉnh tồn kho</button>
          <button class="inventory-primary" type="button" @click="openAction('adjust')"><LineIcon name="plus" />Điều chỉnh tồn kho</button>
        </div>
      </div>

      <div class="inventory-summary">
        <template v-for="card in summaryCards" :key="card.key">
          <button v-if="demo && card.key !== 'all'" class="inventory-summary-card" :class="card.tone" type="button" :aria-label="`Lọc ${card.label}`" @click="chooseChip(card.key)"><span>{{ card.label }}</span><strong>{{ card.value }}</strong><small>{{ card.note }}</small><LineIcon name="chevron" /></button>
          <RouterLink v-else-if="!demo && isOwner && card.key !== 'all'" class="inventory-summary-card" :class="card.tone" to="/today/attention"><span>{{ card.label }}</span><strong>{{ card.value }}</strong><small>{{ card.note }}</small><LineIcon name="chevron" /></RouterLink>
          <div v-else class="inventory-summary-card" :class="card.tone"><span>{{ card.label }}</span><strong>{{ card.value }}</strong><small>{{ card.note }}</small></div>
        </template>
      </div>

      <div class="inventory-toolbar">
        <label class="inventory-search"><LineIcon name="search" /><input v-model="search" aria-label="Tìm theo tên sản phẩm, mã SKU, mã vạch" placeholder="Tìm theo tên sản phẩm, mã SKU, mã vạch..." /><span class="inventory-scan" aria-hidden="true"><LineIcon name="barcode" /></span></label>
        <button class="inventory-filter-button" type="button" @click="openFilter"><span aria-hidden="true">▽</span> Bộ lọc</button>
      </div>
      <div class="inventory-chips" aria-label="Tình trạng tồn kho">
        <button v-for="[key, label] in chips" :key="key" class="inventory-chip" :class="{ active: chip === key }" type="button" :aria-pressed="chip === key" @click="chooseChip(key)">{{ chipText(key, label) }}</button>
      </div>
      <p v-if="pageLocal" class="inventory-scope-note">Tình trạng tồn kho được lọc trong trang hiện tại; hệ thống chưa có API lọc tồn kho.</p>

      <div v-if="stateCopy" class="inventory-state" :role="listState === 'error' ? 'alert' : 'status'">
        <div class="inventory-state-inner">
          <div v-if="listState === 'loading'" class="inventory-spinner" aria-hidden="true" />
          <span v-else-if="stateCopy.icon" class="inventory-state-icon" :class="{ danger: listState === 'error' }"><LineIcon :name="stateCopy.icon" /></span>
          <h3>{{ stateCopy.title }}</h3>
          <p>{{ stateCopy.text }}</p>
          <p v-if="listState === 'error' && error && !demo" class="inventory-state-detail">{{ error }}</p>
          <RouterLink v-if="listState === 'empty' && isOwner && !demo" class="inventory-primary" to="/products">Thêm sản phẩm</RouterLink>
          <button v-if="stateCopy.action" class="inventory-secondary" type="button" @click="stateAction">{{ stateCopy.action }}</button>
        </div>
      </div>
      <template v-else>
        <div class="inventory-table-wrap"><table class="inventory-table">
          <colgroup><col class="col-check" /><col class="col-name" /><col class="col-sku" /><col class="col-unit" /><col class="col-stock" /><col class="col-avg" /><col class="col-value" /><col class="col-status" /><col class="col-updated" /><col class="col-actions" /></colgroup>
          <thead><tr><th><input type="checkbox" aria-label="Chọn tất cả sản phẩm trên trang" :checked="allChecked" @change="toggleAll" /></th><th>Sản phẩm ↕</th><th>Mã SKU</th><th>Đơn vị</th><th class="center">Tồn kho hiện tại ↕</th><th>Giá vốn TB</th><th>Giá trị tồn kho</th><th>Trạng thái</th><th>Cập nhật</th><th><span class="sr-only">Thao tác</span></th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.id" :class="{ selected: drawer === 'detail' && detailId === row.id }">
              <td><input type="checkbox" :aria-label="`Chọn ${row.name}`" :checked="checkedIds.includes(row.id)" @change="toggleChecked(row.id)" /></td>
              <td><div class="inventory-identity"><img v-if="row.image" :src="row.image" :alt="row.name" /><span v-else class="inventory-placeholder"><LineIcon name="product" /></span><div><button class="inventory-name" type="button" @click="openDetail(row)">{{ row.name }}</button><small>{{ row.unit }}</small></div></div></td>
              <td class="muted">{{ row.sku }}</td>
              <td>{{ row.unit }}</td>
              <td class="inventory-stock" :class="stockClass(row)">{{ row.stock }}</td>
              <td class="inventory-price" :title="row.avg === null && !demo ? 'Chưa có giá vốn bình quân tin cậy' : undefined">{{ row.avg === null ? '—' : money(row.avg) }}</td>
              <td :class="{ negative: (row.value ?? 0) < 0 }">{{ row.value === null ? '—' : money(row.value) }}</td>
              <td><span class="inventory-status" :class="row.tone"><span class="inventory-dot" />{{ toneLabel(row.tone) }}</span></td>
              <td class="muted inventory-updated">{{ row.updated.split(' ')[0] }}<br v-if="row.updated.includes(' ')" />{{ row.updated.split(' ')[1] }}</td>
              <td class="inventory-actions-cell">
                <button class="inventory-more" type="button" :aria-label="`Thao tác ${row.name}`" :aria-expanded="menuId === row.id" @click="menuId = menuId === row.id ? null : row.id">•••</button>
                <div v-if="menuId === row.id" class="inventory-action-menu">
                  <button type="button" @click="openDetail(row)">Xem chi tiết</button>
                  <button v-if="canAct && row.isActive" type="button" @click="openAction('adjust', row)">Điều chỉnh tồn kho</button>
                  <button v-if="canAct && row.isActive" type="button" @click="openAction('stocktake', row)">Kiểm kho</button>
                  <button type="button" @click="openHistory(row)">Lịch sử biến động</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table></div>
        <div class="inventory-footer">
          <span>Hiển thị {{ firstIndex }} – {{ lastIndex }} / {{ totalCount }} sản phẩm</span>
          <div class="inventory-pager">
            <button type="button" aria-label="Trang trước" :disabled="page <= 1" @click="goPage(page - 1)">‹</button>
            <template v-for="(item, index) in pagerItems" :key="`${item}-${index}`">
              <span v-if="item === '…'" class="inventory-ellipsis">…</span>
              <button v-else type="button" :class="{ active: page === item }" :aria-current="page === item ? 'page' : undefined" @click="goPage(item)">{{ item }}</button>
            </template>
            <button type="button" aria-label="Trang sau" :disabled="page >= totalPages" @click="goPage(page + 1)">›</button>
            <select v-model.number="pageSize" aria-label="Số sản phẩm mỗi trang" @change="page = 1; load(1)"><option :value="10">10 / trang</option><option :value="20">20 / trang</option><option :value="50">50 / trang</option></select>
          </div>
        </div>
      </template>
    </div>

    <div v-if="drawer" class="inventory-overlay" @click="closeAll" />

    <!-- Filter drawer -->
    <aside v-if="drawer === 'filter'" class="inventory-drawer" role="dialog" aria-modal="true" aria-label="Bộ lọc tồn kho">
      <div class="inventory-drawer-head"><h2>Bộ lọc tồn kho</h2><button class="inventory-icon-button" type="button" aria-label="Đóng" @click="drawer = null">×</button></div>
      <div class="inventory-drawer-body">
        <div class="inventory-section"><h3>Tình trạng tồn kho</h3>
          <label class="inventory-check-row"><input type="checkbox" :checked="draft.stock.length === 0" @change="toggleDraftStock('all')" />Tất cả</label>
          <label v-for="[key, label] in stockOptions" :key="key" class="inventory-check-row"><input type="checkbox" :checked="draft.stock.includes(key)" @change="toggleDraftStock(key)" />{{ label }}</label>
        </div>
        <template v-if="demo">
          <div class="inventory-section"><h3>Danh mục sản phẩm</h3><select v-model="draft.category" class="inventory-input" aria-label="Danh mục sản phẩm"><option value="">Tất cả danh mục</option><option v-for="category in categories" :key="category">{{ category }}</option></select></div>
          <div class="inventory-section"><h3>Đơn vị tính</h3><select v-model="draft.unit" class="inventory-input" aria-label="Đơn vị tính"><option value="">Tất cả đơn vị</option><option v-for="unit in units" :key="unit">{{ unit }}</option></select></div>
          <div class="inventory-section"><h3>Khoảng tồn kho</h3><div class="inventory-grid2"><label class="inventory-field">Từ<input v-model="draft.minStock" class="inventory-input" placeholder="Số lượng" /></label><label class="inventory-field">Đến<input v-model="draft.maxStock" class="inventory-input" placeholder="Số lượng" /></label></div></div>
          <div class="inventory-section"><h3>Giá trị tồn kho</h3><div class="inventory-grid2"><label class="inventory-field">Từ<input v-model="draft.minValue" class="inventory-input" placeholder="Số tiền" /></label><label class="inventory-field">Đến<input v-model="draft.maxValue" class="inventory-input" placeholder="Số tiền" /></label></div></div>
        </template>
        <div class="inventory-section"><h3>Trạng thái sản phẩm</h3><select v-model="draft.productStatus" class="inventory-input" aria-label="Trạng thái sản phẩm"><option value="all">Tất cả</option><option value="active">Đang bán</option><option value="inactive">Ngừng bán</option></select></div>
        <p v-if="!demo" class="inventory-hint">Lọc theo danh mục, đơn vị, khoảng tồn kho và giá trị tồn kho chưa được hệ thống hỗ trợ.</p>
      </div>
      <div class="inventory-drawer-foot"><button class="inventory-danger-outline" type="button" @click="clearAll(); toast('Đã xóa bộ lọc')">Xóa bộ lọc</button><button class="inventory-primary" type="button" @click="applyFilter">Áp dụng</button></div>
    </aside>

    <!-- Detail drawer -->
    <aside v-if="drawer === 'detail'" class="inventory-drawer wide" role="dialog" aria-modal="true" aria-label="Chi tiết tồn kho sản phẩm">
      <div class="inventory-drawer-head">
        <div class="inventory-detail-identity"><img v-if="detail?.image" :src="detail.image" :alt="detail.name" /><span v-else class="inventory-placeholder large"><LineIcon name="product" /></span><div><h2>{{ detail?.name ?? detailRow?.name }}</h2><p class="inventory-hint">{{ detail?.sku ?? detailRow?.sku }} ・ {{ detail?.unit ?? detailRow?.unit }}</p></div></div>
        <button class="inventory-icon-button" type="button" aria-label="Đóng" @click="drawer = null">×</button>
      </div>
      <div class="inventory-drawer-body">
        <p v-if="detailLoading" class="inventory-hint">Đang tải sản phẩm…</p>
        <p v-else-if="detailError" class="inventory-error" role="alert">{{ detailError }}</p>
        <template v-if="detail">
          <div class="inventory-detail-actions">
            <span class="inventory-status" :class="detail.isActive ? 'ok' : 'off'"><span class="inventory-dot" />{{ detail.isActive ? 'Đang bán' : 'Ngừng bán' }}</span>
            <button v-if="canAct && detail.isActive" class="inventory-small-button" type="button" @click="openAction('adjust', detailRow)"><LineIcon name="sliders" />Điều chỉnh tồn kho</button>
            <button v-if="canAct && detail.isActive" class="inventory-small-button" type="button" @click="openAction('stocktake', detailRow)"><LineIcon name="clipboard" />Kiểm kho</button>
          </div>
          <div class="inventory-tabs" role="tablist">
            <button v-for="[key, label] in ([['overview', 'Tổng quan'], ['history', 'Lịch sử biến động'], ['attention', 'Cần chú ý (C14)']] as const)" :key="key" class="inventory-tab" :class="{ active: detailTab === key }" type="button" role="tab" :aria-selected="detailTab === key" @click="detailTab = key">{{ label }}</button>
          </div>
          <template v-if="detailTab === 'overview'">
            <div class="inventory-detail-grid">
              <div class="inventory-info-card"><span>Tồn kho hiện tại</span><strong>{{ detail.stock }}</strong><p class="inventory-hint">Giá trị tồn kho <b :class="{ negative: detail.valueNegative }">{{ detail.value }}</b></p></div>
              <div class="inventory-info-card"><span>Giá vốn trung bình</span><strong>{{ detail.avg }}</strong><p class="inventory-hint">{{ detail.avgNote }}</p></div>
              <div class="inventory-info-card"><span>Trạng thái</span><span class="inventory-status" :class="detail.tone"><span class="inventory-dot" />{{ toneLabel(detail.tone) }}</span><p class="inventory-hint inventory-updated-note">Cập nhật cuối<br /><b>{{ detail.updated }}</b></p></div>
            </div>
            <div class="inventory-grid2">
              <div class="inventory-info-card"><h3>Thông tin sản phẩm</h3><div v-for="[label, value] in detail.info" :key="label" class="inventory-kv"><span>{{ label }}</span><strong>{{ value }}</strong></div></div>
              <div class="inventory-info-card"><h3>Cần chú ý (C14)</h3><div class="inventory-notice" :class="detail.c14.tone"><b>{{ detail.c14.tone === 'ok' ? '✓ ' : detail.c14.tone === 'warn' ? '⚠ ' : '' }}{{ detail.c14.title }}</b><span v-if="detail.c14.text" class="inventory-hint">{{ detail.c14.text }}</span><RouterLink v-if="detail.c14.link" :to="detail.c14.link">Vì sao?</RouterLink></div></div>
            </div>
          </template>
          <template v-else-if="detailTab === 'history'">
            <table class="inventory-mini-table">
              <thead><tr><th>Ngày</th><th>Loại giao dịch</th><th>Số lượng</th><th>{{ detail.historyExtra }}</th></tr></thead>
              <tbody>
                <tr v-for="(item, index) in detail.history" :key="index"><td>{{ item.date }}</td><td>{{ item.type }}</td><td :class="item.quantity >= 0 ? 'plus' : 'minus'">{{ signed(item.quantity) }}</td><td>{{ item.extra }}</td></tr>
                <tr v-if="detail.history.length === 0"><td colspan="4" class="inventory-hint">Sản phẩm chưa có biến động tồn kho.</td></tr>
              </tbody>
            </table>
            <button class="inventory-link-button" type="button" @click="openHistory()">Xem toàn bộ lịch sử biến động →</button>
          </template>
          <div v-else class="inventory-notice" :class="detail.c14.tone"><b>{{ detail.c14.tone === 'ok' ? '✓ ' : detail.c14.tone === 'warn' ? '⚠ ' : '' }}{{ detail.c14.title }}</b><span v-if="detail.c14.text" class="inventory-hint">{{ detail.c14.text }}</span><RouterLink v-if="detail.c14.link" :to="detail.c14.link">Vì sao?</RouterLink></div>
        </template>
      </div>
    </aside>

    <!-- Adjustment / stocktake dialog -->
    <div v-if="actionKind" class="inventory-modal-layer">
      <section class="inventory-modal" :class="actionKind" role="dialog" aria-modal="true" :aria-label="actionKind === 'adjust' ? 'Điều chỉnh tồn kho' : 'Kiểm kho'">
        <div class="inventory-modal-head"><h2>{{ actionKind === 'adjust' ? 'Điều chỉnh tồn kho' : 'Kiểm kho' }}</h2><button class="inventory-icon-button" type="button" aria-label="Đóng" :disabled="actionBusy" @click="actionKind = null">×</button></div>
        <div class="inventory-modal-body">
          <div class="inventory-steps"><div v-for="(label, index) in stepLabels" :key="label" class="inventory-step" :class="{ active: step === index + 1, done: step > index + 1 }"><span class="n">{{ index + 1 }}</span>{{ label }}</div></div>
          <p v-if="actionError" class="inventory-error" role="alert">{{ actionError }}</p>

          <template v-if="showPicker">
            <label class="inventory-search inventory-picker-search"><LineIcon name="search" /><input v-model="pickQuery" aria-label="Tìm sản phẩm" placeholder="Tìm sản phẩm (tên, mã SKU, mã vạch)..." :disabled="locked" /></label>
            <div class="inventory-selection" role="radiogroup" :aria-label="actionKind === 'adjust' ? 'Sản phẩm cần điều chỉnh' : 'Sản phẩm cần kiểm kho'">
              <div class="inventory-selection-row head"><span /><span>Sản phẩm</span><span>Mã SKU</span><span>Đơn vị</span><span class="right">Tồn kho hiện tại</span></div>
              <button v-for="row in pickRows" :key="row.id" class="inventory-selection-row" :class="{ selected: pickId === row.id }" type="button" role="radio" :aria-checked="pickId === row.id" :disabled="locked" @click="pickId = row.id">
                <span class="inventory-check-box" :class="{ on: pickId === row.id }">{{ pickId === row.id ? '✓' : '' }}</span>
                <span class="inventory-identity"><img v-if="row.image" :src="row.image" alt="" /><span v-else class="inventory-placeholder"><LineIcon name="product" /></span><span class="inventory-name-text">{{ row.name }}</span></span>
                <span class="muted">{{ row.sku }}</span><span>{{ row.unit }}</span><strong class="right" :class="stockClass(row)">{{ row.stock }}</strong>
              </button>
              <p v-if="pickLoading" class="inventory-hint inventory-selection-note">Đang tìm sản phẩm…</p>
              <p v-else-if="pickRows.length === 0" class="inventory-hint inventory-selection-note">Không có sản phẩm phù hợp.</p>
            </div>
            <template v-if="actionKind === 'stocktake'">
              <label class="inventory-field inventory-gap">Ghi chú<textarea v-model="stNote" class="inventory-input inventory-textarea" placeholder="Nhập ghi chú (nếu có)..." :disabled="locked" /></label>
              <div class="inventory-notice info">ℹ Mỗi lần kiểm một sản phẩm. Kiểm kho toàn kho chưa được hệ thống hỗ trợ; số tồn được lấy tại thời điểm bắt đầu để so sánh với số đếm.</div>
            </template>
          </template>

          <template v-else-if="actionKind === 'adjust'">
            <div class="inventory-detail-identity bordered"><img v-if="pickRow?.image" :src="pickRow.image" :alt="pickRow.name" /><span v-else class="inventory-placeholder large"><LineIcon name="product" /></span><div><h3>{{ pickRow?.name ?? actionProduct?.name }}</h3><p class="inventory-hint">{{ pickRow?.sku ?? actionProduct?.sku }} ・ {{ pickRow?.unit ?? actionProduct?.unit }} ・ Tồn hiện tại {{ adjustBefore }}</p></div></div>
            <template v-if="step === 2">
              <div class="inventory-grid2">
                <label class="inventory-field"><span>Số lượng điều chỉnh <span class="required">*</span></span><input v-model.number="adjustQty" class="inventory-input" type="number" step="0.001" :disabled="locked" /></label>
                <label class="inventory-field"><span>Lý do điều chỉnh <span class="required">*</span></span><select v-model="adjustReason" class="inventory-input" :disabled="locked"><option value="">Chọn lý do</option><option v-for="reason in adjustReasons" :key="reason">{{ reason }}</option></select></label>
              </div>
              <p class="inventory-hint">↑ Số dương: tăng tồn kho &nbsp;&nbsp; ↓ Số âm: giảm tồn kho</p>
              <label v-if="needsAdjustCost" class="inventory-field inventory-gap"><span>Giá vốn đơn vị cho lượng tăng (đ) <span class="required">*</span></span><input v-model.number="adjustCost" class="inventory-input" type="number" min="0" step="0.0001" :disabled="locked" /></label>
              <label class="inventory-field inventory-gap">Ghi chú<textarea v-model="adjustNote" class="inventory-input inventory-textarea" placeholder="Nhập ghi chú..." :disabled="locked" /></label>
            </template>
            <div v-else class="inventory-info-card"><h3>Xác nhận điều chỉnh</h3>
              <div class="inventory-kv"><span>Sản phẩm</span><strong>{{ pickRow?.name ?? actionProduct?.name }}</strong></div>
              <div class="inventory-kv"><span>Tồn hiện tại</span><strong>{{ adjustBefore }}</strong></div>
              <div class="inventory-kv"><span>Điều chỉnh</span><strong :class="adjustQty >= 0 ? 'plus' : 'minus'">{{ signed(adjustQty) }}</strong></div>
              <div class="inventory-kv"><span>Tồn sau điều chỉnh</span><strong>{{ adjustBefore + Number(adjustQty) }}</strong></div>
              <div class="inventory-kv"><span>Lý do</span><strong>{{ [adjustReason, adjustNote.trim()].filter(Boolean).join(' — ') }}</strong></div>
              <div v-if="needsAdjustCost" class="inventory-kv"><span>Giá vốn lượng tăng</span><strong>{{ money(adjustCost ?? 0) }}</strong></div>
            </div>
          </template>

          <template v-else-if="demo && step === 1">
            <label class="inventory-field"><span>Tên đợt kiểm kho <span class="required">*</span></span><input v-model="stName" class="inventory-input" /></label>
            <div class="inventory-grid2">
              <label class="inventory-field"><span>Ngày kiểm kho <span class="required">*</span></span><input v-model="stDate" class="inventory-input" placeholder="dd/mm/yyyy" /></label>
              <label class="inventory-field">Ghi chú<textarea v-model="stNote" class="inventory-input inventory-textarea" placeholder="Nhập ghi chú (nếu có)..." /></label>
            </div>
            <div class="inventory-section"><h3>Phạm vi kiểm kho</h3>
              <label class="inventory-check-row"><input v-model="stScope" type="radio" value="all" />Tất cả sản phẩm</label>
              <div class="inventory-scope-row"><label class="inventory-check-row"><input v-model="stScope" type="radio" value="some" />Chỉ một số sản phẩm</label><button class="inventory-small-button" type="button" :disabled="stScope !== 'some'" @click="toast('Chọn sản phẩm chỉ minh họa trong dữ liệu mẫu')">Chọn sản phẩm</button></div>
            </div>
            <div class="inventory-notice info">ℹ Hệ thống sẽ lấy số liệu tồn kho hiện tại tại thời điểm bắt đầu kiểm kho để so sánh với số đếm.</div>
          </template>
          <template v-else-if="demo && step === 2">
            <label class="inventory-search inventory-picker-search"><LineIcon name="search" /><input v-model="stQuery" aria-label="Tìm sản phẩm kiểm kho" placeholder="Tìm sản phẩm (tên, mã SKU, mã vạch)..." /></label>
            <div class="inventory-count-grid">
              <div class="head">Sản phẩm</div><div class="head center">Tồn hiện tại</div><div class="head">Số đếm <span class="required">*</span></div><div class="head center">Chênh lệch</div>
              <template v-for="row in countPageRows" :key="row.id">
                <div class="inventory-identity"><img :src="row.image" alt="" /><span><span class="inventory-name-text">{{ row.name }}</span><small>{{ row.sku }}</small></span></div>
                <div class="center">{{ row.stock }}</div>
                <div><input v-model.number="stCounts[row.id]" class="inventory-input inventory-count-input" type="number" min="0" :aria-label="`Số đếm ${row.name}`" /></div>
                <div class="center" :class="countDiff(row) > 0 ? 'plus' : countDiff(row) < 0 ? 'minus' : ''">{{ signed(countDiff(row)) }}</div>
              </template>
            </div>
            <div class="inventory-count-footer"><span>Hiển thị {{ countRows.length ? (stPage - 1) * 5 + 1 : 0 }} - {{ Math.min(stPage * 5, countRows.length) }} / {{ countRows.length }} sản phẩm</span><div class="inventory-pager"><button v-for="number in Math.ceil(countRows.length / 5)" :key="number" type="button" :class="{ active: stPage === number }" @click="stPage = number">{{ number }}</button></div></div>
          </template>
          <template v-else-if="demo && step === 3">
            <div class="inventory-kpis"><div class="inventory-summary-card"><span>Tổng sản phẩm kiểm kho</span><strong>{{ stocktakeKpis.total }}</strong></div><div class="inventory-summary-card ok"><span>Khớp</span><strong>{{ stocktakeKpis.matched }}</strong></div><div class="inventory-summary-card warning"><span>Chênh lệch</span><strong>{{ stocktakeKpis.mismatched }}</strong></div></div>
            <table class="inventory-mini-table"><thead><tr><th>Sản phẩm</th><th>Tồn hiện tại</th><th>Số đếm</th><th>Chênh lệch</th></tr></thead><tbody>
              <tr v-for="row in differenceRows" :key="row.id"><td>{{ row.name }}</td><td>{{ row.stock }}</td><td>{{ stCounts[row.id] }}</td><td :class="countDiff(row) > 0 ? 'plus' : 'minus'">{{ signed(countDiff(row)) }}</td></tr>
              <tr v-if="differenceRows.length === 0"><td colspan="4" class="inventory-hint">Không có chênh lệch trong dữ liệu mẫu.</td></tr>
            </tbody></table>
            <div class="inventory-warning-box">⚠ Sau khi xác nhận, hệ thống sẽ tạo các bút toán điều chỉnh tồn kho. Vui lòng kiểm tra kỹ trước khi thực hiện.</div>
          </template>
          <div v-else-if="demo" class="inventory-info-card"><h3>Xác nhận kiểm kho</h3><p class="inventory-confirm-text">Bạn sắp xác nhận kết quả kiểm kho <b>{{ stName }}</b> ngày {{ stDate }}. Các sản phẩm có chênh lệch sẽ được cập nhật theo số đếm thực tế.</p><div class="inventory-notice ok">✓ {{ stocktakeKpis.matched }} sản phẩm khớp &nbsp;&nbsp; • &nbsp;&nbsp; {{ stocktakeKpis.mismatched }} sản phẩm có chênh lệch</div></div>

          <template v-else-if="stContext">
            <template v-if="step === 2">
              <div class="inventory-count-grid">
                <div class="head">Sản phẩm</div><div class="head center">Tồn hiện tại</div><div class="head">Số đếm <span class="required">*</span></div><div class="head center">Chênh lệch</div>
                <div class="inventory-identity"><span class="inventory-placeholder"><LineIcon name="product" /></span><span><span class="inventory-name-text">{{ stContext.productName }}</span><small>{{ stContext.productSku }}</small></span></div>
                <div class="center">{{ stContext.expectedQuantity }}</div>
                <div><input v-model.number="stCounted" class="inventory-input inventory-count-input" type="number" min="0" step="0.001" :aria-label="`Số đếm ${stContext.productName}`" :disabled="locked" /></div>
                <div class="center" :class="stDiff > 0 ? 'plus' : stDiff < 0 ? 'minus' : ''">{{ signed(stDiff) }}</div>
              </div>
            </template>
            <template v-else-if="step === 3">
              <div class="inventory-kpis"><div class="inventory-summary-card"><span>Tồn hệ thống</span><strong>{{ stContext.expectedQuantity }}</strong></div><div class="inventory-summary-card ok"><span>Số đếm</span><strong>{{ stCounted }}</strong></div><div class="inventory-summary-card warning"><span>Chênh lệch</span><strong>{{ signed(stDiff) }}</strong></div></div>
              <label v-if="needsStocktakeCost" class="inventory-field"><span>Giá vốn đơn vị cho lượng tăng (đ) <span class="required">*</span></span><input v-model.number="stCost" class="inventory-input" type="number" min="0" step="0.0001" :disabled="locked" /></label>
              <div class="inventory-warning-box">{{ stDiff === 0 ? 'Số đếm khớp tồn hệ thống; xác nhận chỉ ghi nhận kết quả kiểm kho, không tạo biến động tồn.' : '⚠ Sau khi xác nhận, hệ thống sẽ tạo bút toán kiểm kho cho sản phẩm này. Vui lòng kiểm tra kỹ trước khi thực hiện.' }}</div>
            </template>
            <div v-else class="inventory-info-card"><h3>Xác nhận kiểm kho</h3>
              <div class="inventory-kv"><span>Sản phẩm</span><strong>{{ stContext.productName }}</strong></div>
              <div class="inventory-kv"><span>Tồn hệ thống</span><strong>{{ stContext.expectedQuantity }} {{ stContext.unit }}</strong></div>
              <div class="inventory-kv"><span>Số đếm</span><strong>{{ stCounted }} {{ stContext.unit }}</strong></div>
              <div class="inventory-kv"><span>Chênh lệch</span><strong :class="stDiff > 0 ? 'plus' : stDiff < 0 ? 'minus' : ''">{{ signed(stDiff) }}</strong></div>
              <div class="inventory-kv"><span>Ghi chú</span><strong>{{ stNote.trim() || '—' }}</strong></div>
            </div>
          </template>
        </div>
        <div class="inventory-modal-foot">
          <button class="inventory-secondary" type="button" :disabled="locked" @click="actionBack">{{ step === 1 ? 'Hủy' : '← Quay lại' }}</button>
          <button class="inventory-primary" type="button" :disabled="actionBusy" @click="actionNext">{{ actionBusy ? 'Đang xử lý…' : step === lastStep ? 'Xác nhận' : 'Tiếp theo →' }}</button>
        </div>
      </section>
    </div>

    <!-- Movement history -->
    <div v-if="historyOpen" class="inventory-modal-layer" @click.self="historyOpen = false">
      <section class="inventory-modal history" role="dialog" aria-modal="true" aria-label="Lịch sử biến động tồn kho">
        <div class="inventory-modal-head"><div class="inventory-detail-identity"><img v-if="historyTitle.image" :src="historyTitle.image" alt="" /><div><h2>Lịch sử biến động tồn kho</h2><p class="inventory-hint">{{ historyTitle.meta }}</p></div></div><button class="inventory-icon-button" type="button" aria-label="Đóng" @click="historyOpen = false">×</button></div>
        <div class="inventory-modal-body">
          <div class="inventory-history-filters">
            <label class="inventory-field">Từ ngày<input v-model="historyFrom" class="inventory-input" placeholder="dd/mm/yyyy" /></label>
            <label class="inventory-field">Đến ngày<input v-model="historyTo" class="inventory-input" placeholder="dd/mm/yyyy" /></label>
            <label class="inventory-field">Loại giao dịch<select v-model="historyType" class="inventory-input"><option>Tất cả</option><option v-for="label in ['Bán hàng', 'Nhập hàng', 'Điều chỉnh', 'Kiểm kho', 'Trả hàng', 'Tồn đầu']" :key="label">{{ label }}</option></select></label>
          </div>
          <div class="inventory-history-wrap"><table class="inventory-mini-table">
            <thead><tr><th>Ngày</th><th>Loại giao dịch</th><th>Số lượng</th><th>Đơn giá vốn</th><th v-if="demo">Tồn sau GD</th><th v-if="demo">Người tạo</th><th v-else>Ghi chú</th></tr></thead>
            <tbody>
              <tr v-for="(item, index) in historyRows" :key="index"><td>{{ item.date }}</td><td>{{ item.type }}</td><td :class="item.quantity >= 0 ? 'plus' : 'minus'">{{ signed(item.quantity) }}</td><td>{{ money(item.cost) }}</td><td v-if="demo">{{ item.stock }}</td><td v-if="demo">{{ item.actor }}</td><td v-else>{{ item.note || '—' }}</td></tr>
              <tr v-if="historyRows.length === 0"><td :colspan="demo ? 6 : 5" class="inventory-hint">{{ !demo && liveMovements === null ? 'Đang tải lịch sử…' : 'Chưa có biến động tồn kho phù hợp.' }}</td></tr>
            </tbody>
          </table></div>
        </div>
        <div class="inventory-modal-foot"><button class="inventory-secondary" type="button" @click="historyOpen = false">Đóng</button></div>
      </section>
    </div>

    <div v-if="demo" class="inventory-demo">
      <button class="inventory-demo-button" type="button" :aria-expanded="demoMenuOpen" @click="demoMenuOpen = !demoMenuOpen">Demo trạng thái ▴</button>
      <div v-if="demoMenuOpen" class="inventory-demo-menu">
        <button type="button" @click="showDemoState('normal')">Danh sách bình thường</button>
        <button type="button" @click="showDemoState('empty')">Trạng thái rỗng</button>
        <button type="button" @click="showDemoState('noresult')">Không có kết quả</button>
        <button type="button" @click="showDemoState('loading')">Đang tải</button>
        <button type="button" @click="showDemoState('error')">Lỗi tải dữ liệu</button>
      </div>
    </div>
    <div v-if="toastText" class="inventory-toast" role="status">{{ toastText }}</div>
  </section>
</template>

<style scoped>
.inventory-page{color:#172033}
.inventory-panel{min-height:calc(100vh - 74px);padding:12px 12px 10px;background:#fff;border:1px solid #dfe7ee;border-radius:14px;box-shadow:0 10px 30px #1f304714}
.inventory-head{display:flex;align-items:flex-start;justify-content:space-between;gap:20px;padding:2px 2px 12px}
.inventory-head h1{margin:0 0 3px;font-size:22px;font-weight:800;line-height:1.2;letter-spacing:-.02em}
.inventory-head p{margin:0;color:#66758d;font-size:12px}
.inventory-head-actions{display:flex;flex-wrap:wrap;justify-content:flex-end;gap:8px}
.inventory-primary,.inventory-secondary,.inventory-outline,.inventory-danger-outline{display:inline-flex;height:38px;align-items:center;justify-content:center;gap:8px;border-radius:8px;padding:0 16px;font-size:13px;font-weight:800;text-decoration:none;white-space:nowrap;cursor:pointer}
.inventory-primary{border:0;background:linear-gradient(180deg,#10a971,#078d5d);color:#fff;box-shadow:0 8px 16px #0a9c6729}
.inventory-primary:hover{background:#087b55}
.inventory-primary:disabled,.inventory-secondary:disabled{opacity:.55;cursor:not-allowed}
.inventory-outline{border:1px solid #cdd9e4;background:#fff;color:#314159;font-weight:750}
.inventory-outline:hover{background:#f8fbfd}
.inventory-secondary{border:1px solid #cdd9e4;background:#fff;color:#2d3c54}
.inventory-danger-outline{border:1px solid #f5b4b7;background:#fff;color:#d93f45}
.inventory-primary .line-icon,.inventory-outline .line-icon{width:16px;height:16px}
.inventory-summary{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;margin-bottom:12px}
.inventory-summary-card{position:relative;display:block;min-height:82px;border:1px solid #dfe7ee;border-radius:10px;background:#fff;padding:12px 14px;color:#172033;text-align:left;text-decoration:none}
button.inventory-summary-card,a.inventory-summary-card{cursor:pointer}
button.inventory-summary-card:hover,a.inventory-summary-card:hover{border-color:#b9d8c9;background:#fbfefd}
.inventory-summary-card span{display:block;margin-bottom:7px;color:#66758d;font-size:10px}
.inventory-summary-card strong{display:block;font-size:24px;font-weight:850;line-height:1}
.inventory-summary-card small{display:block;margin-top:5px;color:#8a96a8;font-size:10px}
.inventory-summary-card .line-icon{position:absolute;top:50%;right:12px;width:14px;height:14px;color:#9aa7b8;transform:translateY(-50%) rotate(-90deg)}
.inventory-summary-card.danger strong{color:#e03d3d}
.inventory-summary-card.warning strong{color:#e07900}
.inventory-summary-card.negative strong{color:#df2d2d}
.inventory-summary-card.ok strong{color:#0a9c67}
.inventory-toolbar{display:grid;grid-template-columns:minmax(0,1fr) 98px;gap:10px}
.inventory-search{display:flex;height:36px;align-items:center;gap:10px;border:1.5px solid #b8cef8;border-radius:8px;background:#fff;padding:0 10px;color:#53617b}
.inventory-search>.line-icon{width:13px;height:13px}
.inventory-search input{flex:1;min-width:0;border:0;outline:0;background:transparent;color:#44536a;font-size:12px}
.inventory-scan{display:grid;width:28px;height:24px;place-items:center;border:1px solid #dfe7ee;border-radius:6px;background:#f8fbff}
.inventory-scan .line-icon{width:16px;height:16px}
.inventory-filter-button{height:36px;border:1px solid #cdd9e4;border-radius:8px;background:#fff;color:#263852;font-size:12px;font-weight:700;cursor:pointer}
.inventory-chips{display:flex;flex-wrap:wrap;gap:8px;padding:12px 0}
.inventory-chip{height:30px;border:1px solid #dfe7ee;border-radius:999px;background:#fff;padding:0 12px;color:#32435e;font-size:11px;font-weight:700;white-space:nowrap;cursor:pointer}
.inventory-chip.active{border-color:transparent;background:linear-gradient(180deg,#14aa72,#078d5e);color:#fff}
.inventory-scope-note{margin:-4px 0 10px;color:#66758d;font-size:11px}
/* Positioned so the absolutely placed sr-only header label stays inside the horizontal scroller. */
.inventory-table-wrap{position:relative;border:1px solid #dfe7ee;border-radius:9px;background:#fff}
.inventory-table{width:100%;border-collapse:collapse;table-layout:fixed}
.col-check{width:34px}.col-name{width:25%}.col-sku{width:10%}.col-unit{width:7%}.col-stock{width:11%}.col-avg{width:10%}.col-value{width:11%}.col-status{width:12%}.col-updated{width:9%}.col-actions{width:44px}
.inventory-table th{height:34px;border-bottom:1px solid #dfe7ee;background:#f7fafc;padding:0 9px;color:#56657b;font-size:10.5px;font-weight:750;text-align:left;white-space:nowrap}
.inventory-table td{height:53px;border-bottom:1px solid #edf1f4;padding:0 9px;color:#2b3a51;font-size:11px;vertical-align:middle}
.inventory-table tbody tr:last-child td{border-bottom:0}
.inventory-table tbody tr:hover{background:#fafdff}
.inventory-table tbody tr.selected{background:#f2fbf7}
.inventory-table input[type=checkbox]{width:14px;height:14px;margin:0;accent-color:#0a9c67;cursor:pointer}
.inventory-table .center{text-align:center}
.muted{color:#52627a}
.negative{color:#d93036}
.inventory-identity{display:flex;min-width:0;align-items:center;gap:9px}
.inventory-identity img{width:28px;height:36px;flex:none;object-fit:contain}
.inventory-identity>div,.inventory-identity>span:last-child{min-width:0}
.inventory-placeholder{display:grid;width:28px;height:32px;flex:none;place-items:center;border-radius:6px;background:#edf7f3;color:#0a9c67}
.inventory-placeholder .line-icon{width:16px;height:16px}
.inventory-placeholder.large{width:40px;height:46px}
.inventory-name{display:block;overflow:hidden;max-width:100%;padding:0;border:0;background:none;color:#172238;font-size:11px;font-weight:800;text-align:left;text-overflow:ellipsis;white-space:nowrap;cursor:pointer}
.inventory-name:hover{color:#087b55;text-decoration:underline}
.inventory-identity small{display:block;margin-top:1px;color:#617089;font-size:10px}
.inventory-name-text{display:block;overflow:hidden;color:#172238;font-weight:800;text-overflow:ellipsis;white-space:nowrap}
.inventory-stock{font-weight:800;text-align:center}
.inventory-stock.zero,.inventory-stock.negative,strong.zero,strong.negative{color:#e84141}
.inventory-stock.low,strong.low{color:#d97706}
.inventory-price{color:#162138;font-weight:800}
.inventory-updated{line-height:1.35}
.inventory-status{display:inline-flex;height:22px;align-items:center;gap:5px;border-radius:7px;padding:0 8px;font-size:10px;font-weight:800;white-space:nowrap}
.inventory-status.ok{background:#e9f8f2;color:#087e57}
.inventory-status.out{background:#ffe8e9;color:#d83f45}
.inventory-status.low,.inventory-status.soon{background:#fff0d7;color:#c76e06}
.inventory-status.negative{background:#fff0dd;color:#c76500}
.inventory-status.off{background:#f1f3f6;color:#7a8696}
.inventory-dot{width:6px;height:6px;border-radius:50%;background:currentColor}
.inventory-actions-cell{position:relative}
.inventory-more{width:28px;height:28px;border:1px solid #dfe7ee;border-radius:7px;background:#fff;color:#44536b;font-size:10px;font-weight:900;cursor:pointer}
.inventory-action-menu{position:absolute;z-index:15;top:42px;right:4px;width:184px;border:1px solid #dfe7ee;border-radius:10px;background:#fff;padding:6px;box-shadow:0 14px 36px #17203324}
.inventory-action-menu button{display:flex;width:100%;height:34px;align-items:center;border:0;border-radius:7px;background:transparent;padding:0 10px;color:#2d3c54;font-size:12px;text-align:left;cursor:pointer}
.inventory-action-menu button:hover{background:#f4f8fa}
.inventory-footer{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:12px 4px 2px;color:#5d6b80;font-size:11px}
.inventory-pager{display:flex;align-items:center;gap:5px}
.inventory-pager button,.inventory-pager select,.inventory-ellipsis{display:grid;min-width:30px;height:30px;place-items:center;border:1px solid #dfe7ee;border-radius:7px;background:#fff;padding:0 7px;color:#42516a;font-size:11px;cursor:pointer}
.inventory-ellipsis{cursor:default}
.inventory-pager button.active{border-color:#d5eee4;background:#e9f8f2;color:#07875b;font-weight:800}
.inventory-pager button:disabled{opacity:.45;cursor:default}
.inventory-pager select{display:block;margin-left:5px;padding:0 10px;color:#35455f}
.inventory-state{display:grid;min-height:400px;place-items:center;color:#617088;text-align:center}
.inventory-state-inner{max-width:320px}
.inventory-state-inner h3{margin:0 0 6px;color:#26364d;font-size:15px;font-weight:800}
.inventory-state-inner p{margin:0 0 12px;font-size:11px;line-height:1.5}
.inventory-state-inner .inventory-state-detail{color:#a94448}
.inventory-state-inner a,.inventory-state-inner button{margin:0 4px}
.inventory-state-icon{display:grid;width:52px;height:52px;margin:0 auto 10px;place-items:center;border-radius:50%;background:#f1f6fb;color:#5c6d86}
.inventory-state-icon .line-icon{width:26px;height:26px}
.inventory-state-icon.danger{background:#fff0f1;color:#d93f45}
.inventory-spinner{width:34px;height:34px;margin:0 auto 10px;border:4px solid #dce8ff;border-top-color:#3475e5;border-radius:50%;animation:inventory-spin .8s linear infinite}
@keyframes inventory-spin{to{transform:rotate(360deg)}}

.inventory-overlay{position:fixed;z-index:55;inset:0;background:transparent}
.inventory-drawer{position:fixed;z-index:60;top:var(--app-topbar-height,0px);right:0;bottom:0;display:flex;width:min(300px,100vw);flex-direction:column;border-left:1px solid #dfe7ee;background:#fff;box-shadow:-12px 0 30px #1f304717}
.inventory-drawer.wide{width:min(540px,100vw)}
.inventory-drawer-head{display:flex;min-height:54px;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #dfe7ee;padding:6px 16px}
.inventory-drawer-head h2,.inventory-modal-head h2{margin:0;font-size:18px;font-weight:800;letter-spacing:-.02em}
.inventory-detail-identity h2{font-size:15px}
.inventory-icon-button{display:grid;width:32px;height:32px;flex:none;place-items:center;border:0;border-radius:8px;background:transparent;color:#536178;font-size:22px;cursor:pointer}
.inventory-icon-button:hover{background:#f4f6f8}
.inventory-drawer-body{flex:1;overflow:auto;padding:14px 16px 20px}
.inventory-drawer-foot{display:flex;gap:10px;border-top:1px solid #dfe7ee;background:#fff;padding:12px 16px}
.inventory-drawer-foot>button{flex:1}
.inventory-section{margin-bottom:14px;border-bottom:1px solid #edf1f4;padding-bottom:14px}
.inventory-section:last-child{border-bottom:0}
.inventory-section h3{margin:0 0 10px;font-size:13px;font-weight:800}
.inventory-check-row{display:flex;align-items:center;gap:8px;margin:8px 0;color:#3f4f66;font-size:11px;cursor:pointer}
.inventory-check-row input{width:15px;height:15px;margin:0;accent-color:#0a9c67}
.inventory-field{display:grid;gap:6px;margin-bottom:12px;color:#3e4e66;font-size:11px;font-weight:700}
.inventory-gap{margin-top:10px}
.inventory-input{width:100%;height:34px;border:1px solid #cdd9e4;border-radius:7px;background:#fff;padding:0 9px;color:#31415a;font-size:11px;font-weight:400}
.inventory-input:disabled{background:#f4f7fa}
.inventory-textarea{min-height:72px;padding-top:8px;resize:vertical}
.inventory-grid2{display:grid;grid-template-columns:1fr 1fr;gap:10px}
.inventory-hint{display:block;margin:0;color:#738197;font-size:10px;font-weight:400;line-height:1.45}
.inventory-error{margin:0 0 10px;border-radius:8px;background:#fff0f1;padding:8px 10px;color:#a94448;font-size:12px}
.required{color:#d63e45}
.plus{color:#0a9c67;font-weight:800}
.minus{color:#d93f45;font-weight:800}

.inventory-detail-identity{display:flex;min-width:0;align-items:center;gap:10px}
.inventory-detail-identity img{width:40px;height:46px;flex:none;object-fit:contain}
.inventory-detail-identity h2,.inventory-detail-identity h3{margin:0 0 2px}
.inventory-detail-identity h3{font-size:15px;font-weight:800}
.inventory-detail-identity.bordered{margin-bottom:14px;border-bottom:1px solid #dfe7ee;padding-bottom:14px}
.inventory-detail-actions{display:flex;flex-wrap:wrap;align-items:center;gap:8px;margin-bottom:10px}
.inventory-small-button{display:inline-flex;height:32px;align-items:center;gap:6px;border:1px solid #cdd9e4;border-radius:8px;background:#fff;padding:0 10px;color:#263852;font-size:11px;font-weight:700;white-space:nowrap;cursor:pointer}
.inventory-small-button .line-icon{width:14px;height:14px}
.inventory-small-button:disabled{opacity:.5;cursor:default}
.inventory-tabs{display:flex;gap:18px;margin:0 -16px 14px;border-bottom:1px solid #dfe7ee;padding:0 16px}
.inventory-tab{height:36px;border:0;border-bottom:2px solid transparent;background:transparent;color:#5d6c82;font-size:11px;font-weight:700;cursor:pointer}
.inventory-tab.active{border-bottom-color:#0a9c67;color:#087b55}
.inventory-detail-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:10px;margin-bottom:12px}
.inventory-info-card{border:1px solid #dfe7ee;border-radius:9px;background:#fbfdfe;padding:12px}
.inventory-info-card>span:not(.inventory-status){display:block;margin-bottom:6px;color:#68768b;font-size:10px}
.inventory-info-card>strong{display:block;margin-bottom:4px;font-size:18px;font-weight:800}
.inventory-info-card h3{margin:0 0 8px;font-size:13px;font-weight:800}
.inventory-updated-note{margin-top:12px}
.inventory-kv{display:grid;grid-template-columns:120px 1fr;gap:8px;border-bottom:1px solid #eef2f5;padding:7px 0;font-size:11px}
.inventory-kv:last-child{border-bottom:0}
.inventory-kv span{color:#627087}
.inventory-kv strong{font-weight:700;overflow-wrap:anywhere}
.inventory-notice{display:grid;gap:4px;border:1px solid #dfe7ee;border-radius:9px;background:#f7fafc;padding:12px;color:#44536a;font-size:11px;line-height:1.45}
.inventory-notice.ok{border-color:#d9eee5;background:#f4fbf8;color:#17815d}
.inventory-notice.warn{border-color:#f2d29d;background:#fff7e7;color:#9b6605}
.inventory-notice.info{border-color:#cbe1ff;background:#f4f8ff;color:#36537d}
.inventory-notice a{color:#087b55;font-weight:700}
.inventory-mini-table{width:100%;border-collapse:collapse}
.inventory-mini-table th,.inventory-mini-table td{border-bottom:1px solid #edf1f4;padding:8px 6px;font-size:10.5px;text-align:left}
.inventory-mini-table th{background:#f8fafc;color:#607088}
.inventory-link-button{margin-top:10px;border:0;background:none;padding:0;color:#087b55;font-size:11px;font-weight:700;cursor:pointer}

.inventory-modal-layer{position:fixed;z-index:90;inset:0;display:grid;place-items:center;background:#202d3d2e;padding:24px}
.inventory-modal{display:flex;width:min(720px,94vw);max-height:88vh;flex-direction:column;overflow:hidden;border:1px solid #dfe7ee;border-radius:13px;background:#fff;box-shadow:0 24px 60px #19283c38}
.inventory-modal.stocktake,.inventory-modal.history{width:min(820px,95vw)}
.inventory-modal-head{display:flex;min-height:52px;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #dfe7ee;padding:6px 16px}
.inventory-modal-head h2{font-size:18px}
.inventory-modal-head .inventory-detail-identity img{width:34px;height:42px}
.inventory-modal-head .inventory-detail-identity h2{font-size:17px}
.inventory-modal-body{overflow:auto;padding:14px 16px}
.inventory-modal-foot{display:flex;justify-content:flex-end;gap:10px;border-top:1px solid #dfe7ee;padding:12px 16px}
.inventory-modal-foot button{min-width:92px}
.inventory-steps{display:flex;width:100%;align-items:center;margin:0 0 18px;overflow-x:auto}
.inventory-step{display:flex;min-width:0;flex:1 1 0;align-items:center;gap:7px;color:#9aa5b5;font-size:10px;font-weight:700;white-space:nowrap}
.inventory-step::after{display:block;min-width:20px;height:1px;flex:1 1 auto;margin:0 8px;background:#d8e0e7;content:""}
.inventory-step:last-child{flex:0 0 auto}
.inventory-step:last-child::after{display:none}
.inventory-step .n{display:grid;width:20px;height:20px;flex:0 0 20px;place-items:center;border-radius:50%;background:#cfd7e2;color:#fff}
.inventory-step.active{color:#0a8b60}
.inventory-step.active .n,.inventory-step.done .n{background:#0a9c67}
.inventory-step.done{color:#6b7b8d}
.inventory-picker-search{margin-bottom:10px}
.inventory-selection{overflow:hidden;border:1px solid #dfe7ee;border-radius:8px}
.inventory-selection-row{display:grid;width:100%;grid-template-columns:34px minmax(0,1fr) 88px 70px 96px;align-items:center;gap:8px;border:0;border-bottom:1px solid #edf1f4;background:#fff;padding:8px;color:#2b3a51;font-size:11px;text-align:left;cursor:pointer}
.inventory-selection-row:last-of-type{border-bottom:0}
.inventory-selection-row.head{background:#f8fafc;color:#66758b;font-size:10px;font-weight:750;cursor:default}
.inventory-selection-row.selected{background:#eef9f4}
.inventory-selection-row:disabled{cursor:default}
.inventory-selection-row .right{text-align:right}
.inventory-selection-note{padding:10px}
.inventory-check-box{display:grid;width:15px;height:15px;place-items:center;border:1px solid #9db0c0;border-radius:3px;background:#fff;color:#fff;font-size:10px}
.inventory-check-box.on{border-color:#0a9c67;background:#0a9c67}
.inventory-scope-row{display:flex;align-items:center;justify-content:space-between;gap:10px}
.inventory-count-grid{display:grid;grid-template-columns:minmax(0,1fr) 100px 110px 90px;align-items:center;border:1px solid #dfe7ee;border-radius:8px}
.inventory-count-grid>div{min-width:0;border-bottom:1px solid #eef2f4;padding:7px 8px;font-size:11px}
.inventory-count-grid>div.head{background:#f8fafc;color:#66758b;font-size:10px;font-weight:750}
.inventory-count-grid .center{text-align:center}
.inventory-count-grid .inventory-identity small{display:block;color:#617089;font-size:10px}
.inventory-count-input{height:30px}
.inventory-count-footer{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-top:10px;color:#5d6b80;font-size:11px}
.inventory-kpis{display:grid;grid-template-columns:repeat(3,1fr);gap:8px;margin-bottom:10px}
.inventory-kpis .inventory-summary-card{min-height:auto;padding:10px}
.inventory-kpis strong{font-size:20px}
.inventory-warning-box{margin-top:10px;border:1px solid #f2d29d;border-radius:8px;background:#fff7e7;padding:10px 11px;color:#9b6605;font-size:10.5px;line-height:1.45}
.inventory-confirm-text{margin:0 0 10px;font-size:12px;line-height:1.6}
.inventory-history-filters{display:grid;grid-template-columns:1fr 1fr 1fr;gap:8px;margin-bottom:4px}
.inventory-history-wrap{overflow-x:auto}

.inventory-demo{position:fixed;z-index:50;bottom:14px;left:calc(var(--app-sidebar-width,0px) + 16px)}
.inventory-demo-button{height:30px;border:1px solid #cdd9e4;border-radius:8px;background:#fff;padding:0 10px;color:#5b6980;font-size:10px;cursor:pointer}
.inventory-demo-menu{position:absolute;bottom:36px;left:0;width:170px;border:1px solid #dfe7ee;border-radius:9px;background:#fff;padding:5px;box-shadow:0 10px 30px #1f304714}
.inventory-demo-menu button{width:100%;height:30px;border:0;border-radius:6px;background:#fff;padding:0 8px;color:#394960;font-size:10.5px;text-align:left;cursor:pointer}
.inventory-demo-menu button:hover{background:#f4f8fa}
.inventory-toast{position:fixed;z-index:120;bottom:22px;left:50%;border-radius:9px;background:#173c31;padding:10px 14px;color:#fff;font-size:12px;font-weight:700;box-shadow:0 10px 30px #1f304714;transform:translateX(-50%)}

@media(max-width:1180px){.inventory-table-wrap{overflow-x:auto}.inventory-table{min-width:1040px}}
@media(max-width:1050px){.inventory-summary{grid-template-columns:repeat(2,1fr)}.inventory-detail-grid{grid-template-columns:1fr}}
@media(max-width:760px){
  .inventory-panel{min-height:auto}
  .inventory-head{flex-direction:column;align-items:stretch}
  .inventory-head-actions{justify-content:flex-start}
  .inventory-head h1{font-size:19px}
  .inventory-toolbar{grid-template-columns:1fr}
  .inventory-chips{flex-wrap:nowrap;overflow-x:auto}
  .inventory-footer,.inventory-count-footer{align-items:flex-start;flex-direction:column}
  .inventory-pager{flex-wrap:wrap}
  .inventory-drawer,.inventory-drawer.wide{width:100%}
  .inventory-grid2,.inventory-history-filters{grid-template-columns:1fr}
  .inventory-modal-layer{padding:8px}
  .inventory-selection-row{grid-template-columns:28px minmax(0,1fr) 64px}
  .inventory-selection-row>:nth-child(3),.inventory-selection-row>:nth-child(4){display:none}
  .inventory-count-grid{grid-template-columns:minmax(0,1fr) 64px 84px 64px}
  .inventory-step{flex:0 0 auto}
}
</style>
