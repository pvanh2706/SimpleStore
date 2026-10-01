<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import type { InventoryMovement, Product, ProductInput, ProductListItem, ProductPage, StocktakeContext } from '../api/types'
import LineIcon from '../components/ui/LineIcon'
import { productCategory, productCategories, type ProductCategory } from '../sales/catalog'
import { productDemoEnabled, referenceCategoryCounts, referenceProducts, type ProductPreview } from '../products/demo'
import { useAuthStore } from '../stores/auth'

type Filter = { status: 'active' | 'inactive' | 'all'; stock: 'all' | 'low' | 'out'; minPrice: string; maxPrice: string; category: ProductCategory }
const defaultFilter = (): Filter => ({ status: 'all', stock: 'all', minPrice: '', maxPrice: '', category: 'Tất cả' })
const auth = useAuthStore()
const route = useRoute()
const inventoryView = computed(() => route.query.view === 'inventory')
const isOwner = computed(() => auth.session.roles.includes('Owner'))
const result = ref<ProductPage | null>(null)
const mockRows = ref<ProductPreview[]>(referenceProducts.map(item => ({ ...item })))
const search = ref('')
const page = ref(1)
const pageSize = ref(10)
const loading = ref(false)
const error = ref('')
const notice = ref('')
const activeCategory = ref<ProductCategory>('Tất cả')
const filter = ref<Filter>(defaultFilter())
const draft = ref<Filter>(defaultFilter())
const drawer = ref<'filter' | 'detail' | 'create' | 'edit' | null>(null)
const modal = ref<'adjust' | 'stocktake' | 'history' | null>(null)
const menuId = ref<string | null>(null)
const selectedId = ref<string | null>(null)
const checkedIds = ref<string[]>([])
const blankForm = () => ({ name: '', sku: '', barcode: '', unit: 'Chai', category: 'Khác' as ProductCategory, price: 0, cost: 0 as number | null, stock: 0, openingCost: 0 })
const form = ref(blankForm())
const adjustment = ref(0)
const adjustmentCost = ref<number | null>(null)
const counted = ref(0)
const stocktakeCost = ref<number | null>(null)
const stockStep = ref(1)
const adjustmentReason = ref('')
const adjustmentNote = ref('')
const stockNote = ref('')
const liveProduct = ref<Product | null>(null)
const stockContext = ref<StocktakeContext | null>(null)
const movements = ref<InventoryMovement[]>([])
const overlayLoading = ref(false)
const submitting = ref(false)
const overlayError = ref('')
const adjustmentAttempt = ref<{ operationId: string; productId: string; quantityDelta: number; adjustmentUnitCost: number | null; reason: string } | null>(null)
const stocktakeAttempt = ref<{ operationId: string; productId: string; expectedQuantity: number; expectedRevision: string; countedQuantity: number; adjustmentUnitCost: number | null; note: string | null } | null>(null)
const historyType = ref('Tất cả')
const historyFrom = ref('01/12/2024')
const historyTo = ref('16/12/2024')
let searchTimer: ReturnType<typeof setTimeout> | undefined
let loadSequence = 0

const money = (value: number) => `${new Intl.NumberFormat('vi-VN').format(value)} đ`
const formatUpdatedAt = (value?: string) => {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}
const updatedDate = (value: string) => /^\d{2}\//.test(value) ? value.split(' ')[0] : value || '—'
const updatedTime = (value: string) => /^\d{2}\//.test(value) ? value.split(' ')[1] ?? '' : ''
const liveRows = computed<ProductPreview[]>(() => (result.value?.items ?? []).map(item => ({
  ...item, category: productCategory(item), updatedAt: formatUpdatedAt(item.updatedAt), image: '', referenceCost: 0,
})))
const sourceRows = computed(() => productDemoEnabled.value ? mockRows.value : liveRows.value)
const selected = computed(() => sourceRows.value.find(item => item.id === selectedId.value) ?? null)
const expectedStock = computed(() => productDemoEnabled.value ? selected.value?.quantityOnHand ?? 0 : stockContext.value?.expectedQuantity ?? selected.value?.quantityOnHand ?? 0)
const historyLabels: Record<string, string> = { OpeningBalance: 'Tồn đầu', Purchase: 'Nhập hàng', Sale: 'Bán hàng', ReturnRestock: 'Trả hàng', SaleVoid: 'Hủy bán hàng', PurchaseVoid: 'Hủy nhập hàng', Adjustment: 'Điều chỉnh', StocktakeAdjustment: 'Kiểm kho' }
const demoHistory = [
  { date: '16/12/2024 10:23', type: 'Bán hàng', quantity: -3, cost: 7500, stock: '24', actor: 'Thu ngân', note: '#HD000123' },
  { date: '15/12/2024 14:10', type: 'Nhập hàng', quantity: 20, cost: 7000, stock: '27', actor: 'Việt Anh', note: '#PN000045' },
  { date: '14/12/2024 09:15', type: 'Bán hàng', quantity: -5, cost: 7500, stock: '7', actor: 'Thu ngân', note: '#HD000122' },
  { date: '12/12/2024 16:40', type: 'Điều chỉnh', quantity: -2, cost: 7500, stock: '12', actor: 'Việt Anh', note: 'Hàng hỏng' },
  { date: '10/12/2024 08:30', type: 'Kiểm kho', quantity: 1, cost: 7500, stock: '14', actor: 'Việt Anh', note: 'Kiểm kho định kỳ' },
]
const historyRows = computed(() => {
  const rows = productDemoEnabled.value ? demoHistory : movements.value.map(item => ({
    date: formatUpdatedAt(item.occurredAt), type: historyLabels[item.type] ?? item.type,
    quantity: item.quantityDelta, cost: item.unitCost, stock: '—',
    actor: item.performedByUserId.slice(0, 8), note: item.reason ?? '—',
  }))
  const toIso = (value: string) => /^\d{2}\/\d{2}\/\d{4}$/.test(value) ? value.split('/').reverse().join('-') : ''
  const from = toIso(historyFrom.value)
  const to = toIso(historyTo.value)
  return rows.filter(item => {
    const [day, month, year] = item.date.split(' ')[0]!.split('/')
    const date = `${year}-${month}-${day}`
    return (!from || date >= from) && (!to || date <= to) && (historyType.value === 'Tất cả' || item.type === historyType.value)
  })
})
const statusLabel = (item: ProductPreview) => !item.isActive ? 'Ngừng bán' : item.quantityOnHand <= 8 ? 'Tồn kho thấp' : 'Bình thường'
const statusTone = (item: ProductPreview) => !item.isActive ? 'off' : item.quantityOnHand <= 8 ? 'low' : 'ok'
const filteredRows = computed(() => sourceRows.value.filter(item => {
  const query = search.value.trim().toLocaleLowerCase('vi-VN')
  if (productDemoEnabled.value && query && !`${item.name} ${item.sku} ${item.barcode ?? ''}`.toLocaleLowerCase('vi-VN').includes(query)) return false
  if (activeCategory.value !== 'Tất cả' && item.category !== activeCategory.value) return false
  if (filter.value.category !== 'Tất cả' && item.category !== filter.value.category) return false
  if (filter.value.status === 'active' && !item.isActive) return false
  if (filter.value.status === 'inactive' && item.isActive) return false
  if (filter.value.stock === 'low' && item.quantityOnHand > 8) return false
  if (filter.value.stock === 'out' && item.quantityOnHand !== 0) return false
  if (filter.value.minPrice !== '' && item.salePrice < Number(filter.value.minPrice)) return false
  if (filter.value.maxPrice !== '' && item.salePrice > Number(filter.value.maxPrice)) return false
  return true
}))
const visibleRows = computed(() => productDemoEnabled.value
  ? filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value)
  : filteredRows.value)
const localFilterActive = computed(() => activeCategory.value !== 'Tất cả' || filter.value.category !== 'Tất cả' || filter.value.stock !== 'all' || filter.value.minPrice !== '' || filter.value.maxPrice !== '')
const totalCount = computed(() => productDemoEnabled.value || localFilterActive.value ? filteredRows.value.length : result.value?.totalCount ?? 0)
const totalPages = computed(() => productDemoEnabled.value ? Math.ceil(totalCount.value / pageSize.value) : localFilterActive.value ? Math.min(1, totalCount.value) : result.value?.totalPages ?? 0)
const firstIndex = computed(() => totalCount.value ? (page.value - 1) * pageSize.value + 1 : 0)
const lastIndex = computed(() => totalCount.value ? Math.min(firstIndex.value + visibleRows.value.length - 1, totalCount.value) : 0)
const allChecked = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(item => checkedIds.value.includes(item.id)))
const categoryCount = (category: ProductCategory) => productDemoEnabled.value
  ? referenceCategoryCounts[category]
  : category === 'Tất cả' ? result.value?.totalCount ?? 0 : sourceRows.value.filter(item => item.category === category).length

async function load(requestedPage = 1) {
  if (productDemoEnabled.value) { page.value = requestedPage; return }
  const sequence = ++loadSequence
  loading.value = true
  error.value = ''
  page.value = requestedPage
  const params = new URLSearchParams({ page: String(page.value), pageSize: String(pageSize.value) })
  if (search.value.trim()) params.set('search', search.value.trim())
  if (filter.value.status !== 'all') params.set('isActive', String(filter.value.status === 'active'))
  try {
    const response = await apiRequest<ProductPage>(`/api/products?${params}`)
    if (sequence === loadSequence) result.value = response
  } catch (reason) {
    if (sequence === loadSequence) error.value = reason instanceof Error ? reason.message : 'Không thể tải sản phẩm.'
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}
watch(search, () => {
  page.value = 1
  if (searchTimer) clearTimeout(searchTimer)
  if (!productDemoEnabled.value) searchTimer = setTimeout(() => load(1), 300)
})
watch(() => form.value.name, name => {
  if (!productDemoEnabled.value) form.value.category = productCategory({ name } as ProductListItem)
})
watch(productDemoEnabled, enabled => {
  ++loadSequence
  if (searchTimer) clearTimeout(searchTimer)
  if (enabled) mockRows.value = referenceProducts.map(item => ({ ...item }))
  search.value = ''
  page.value = 1
  checkedIds.value = []
  closeOverlays()
  selectedId.value = enabled ? 'preview-coke' : null
  if (!enabled) load(1)
})
watch(() => route.query.view, () => { page.value = 1; closeOverlays() })
function closeOverlays() { drawer.value = null; modal.value = null; menuId.value = null; overlayError.value = '' }
function openFilter() { draft.value = { ...filter.value }; drawer.value = 'filter'; menuId.value = null }
function applyFilter() {
  filter.value = { ...draft.value }
  activeCategory.value = draft.value.category
  drawer.value = null
  checkedIds.value = []
  load(1)
}
function clearFilter() { draft.value = defaultFilter(); filter.value = defaultFilter(); activeCategory.value = 'Tất cả'; drawer.value = null; load(1) }
function chooseCategory(category: ProductCategory) { activeCategory.value = category; filter.value.category = category; checkedIds.value = []; load(1) }
function toggleAll() { checkedIds.value = allChecked.value ? [] : visibleRows.value.map(item => item.id) }
function toggleChecked(id: string) { checkedIds.value = checkedIds.value.includes(id) ? checkedIds.value.filter(value => value !== id) : [...checkedIds.value, id] }
function openCreate() {
  if (!isOwner.value && !productDemoEnabled.value) return
  form.value = blankForm()
  overlayError.value = ''
  liveProduct.value = null
  drawer.value = 'create'
}
async function openAction(action: 'detail' | 'edit' | 'adjust' | 'stocktake' | 'history' | 'stop', item: ProductPreview) {
  selectedId.value = item.id
  menuId.value = null
  overlayError.value = ''
  if (action === 'stop') {
    if (productDemoEnabled.value) { item.isActive = false; notice.value = 'Đã chuyển sản phẩm sang Ngừng bán trong dữ liệu mẫu.'; return }
    if (!isOwner.value) return
    submitting.value = true
    try {
      await apiRequest<void>(`/api/products/${item.id}/deactivate`, { method: 'POST', body: '{}' })
      notice.value = 'Đã ngừng kinh doanh sản phẩm.'
      await load(page.value)
    } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể ngừng kinh doanh sản phẩm.' }
    finally { submitting.value = false }
    return
  }
  if (action === 'detail') drawer.value = 'detail'
  else if (action === 'edit') drawer.value = 'edit'
  else modal.value = action
  if (action === 'stocktake') stockContext.value = null
  if (action === 'history') { movements.value = []; historyType.value = 'Tất cả'; historyFrom.value = productDemoEnabled.value ? '01/12/2024' : ''; historyTo.value = productDemoEnabled.value ? '16/12/2024' : '' }
  if (!productDemoEnabled.value) {
    overlayLoading.value = true
    try {
      if (liveProduct.value?.id !== item.id) liveProduct.value = await apiRequest<Product>(`/api/products/${item.id}`)
      if (action === 'history') movements.value = await apiRequest<InventoryMovement[]>(`/api/products/${item.id}/movements`)
      if (action === 'stocktake') {
        stockContext.value = await apiRequest<StocktakeContext>(`/api/inventory/stocktakes/context/${item.id}`)
        counted.value = stockContext.value.expectedQuantity
      }
    } catch (reason) { overlayError.value = reason instanceof Error ? reason.message : 'Không thể tải thông tin sản phẩm.' }
    finally { overlayLoading.value = false }
  }
  if (action === 'edit') {
    const product = productDemoEnabled.value ? null : liveProduct.value
    form.value = { name: product?.name ?? item.name, sku: product?.sku ?? item.sku, barcode: product?.barcode ?? item.barcode ?? '', unit: product?.unit ?? item.unit, category: item.category, price: product?.salePrice ?? item.salePrice, cost: product ? product.referencePurchaseCost : item.referenceCost, stock: item.quantityOnHand, openingCost: 0 }
    return
  }
  if (action === 'adjust') { adjustment.value = 0; adjustmentCost.value = null; adjustmentReason.value = ''; adjustmentNote.value = ''; adjustmentAttempt.value = null }
  if (action === 'stocktake') { if (productDemoEnabled.value) counted.value = item.quantityOnHand; stocktakeCost.value = null; stockStep.value = 1; stockNote.value = ''; stocktakeAttempt.value = null }
}
async function saveProduct() {
  overlayError.value = ''
  if (!form.value.name.trim() || !form.value.sku.trim() || !form.value.unit.trim()) { overlayError.value = 'Nhập tên, mã SKU và đơn vị tính trước khi lưu.'; return }
  if (form.value.price < 0 || (form.value.cost !== null && form.value.cost < 0) || form.value.stock < 0 || form.value.openingCost < 0) { overlayError.value = 'Giá và số lượng không được âm.'; return }
  if (productDemoEnabled.value) {
    if (mockRows.value.some(item => item.sku === form.value.sku.trim() && item.id !== selectedId.value)) { overlayError.value = 'Mã SKU đã tồn tại trong dữ liệu mẫu.'; return }
    if (drawer.value === 'edit' && selected.value) {
      Object.assign(selected.value, { name: form.value.name.trim(), sku: form.value.sku.trim(), barcode: form.value.barcode.trim() || null, unit: form.value.unit, category: form.value.category, salePrice: Number(form.value.price) || 0, referenceCost: Number(form.value.cost) || 0, updatedAt: 'Hôm nay' })
    } else {
      mockRows.value.unshift({ id: `preview-${Date.now()}`, name: form.value.name.trim(), sku: form.value.sku.trim(), barcode: form.value.barcode.trim() || null, unit: form.value.unit, category: form.value.category, salePrice: Number(form.value.price) || 0, referenceCost: Number(form.value.cost) || 0, quantityOnHand: Number(form.value.stock) || 0, isActive: true, updatedAt: 'Hôm nay', image: '' })
    }
  } else {
    const editing = drawer.value === 'edit'
    if (editing && liveProduct.value?.id !== selectedId.value) { overlayError.value = 'Chưa tải được sản phẩm để sửa. Hãy đóng và mở lại.'; return }
    const input: ProductInput = {
      name: form.value.name.trim(), sku: form.value.sku.trim(), barcode: form.value.barcode.trim() || null,
      unit: form.value.unit.trim(), salePrice: Number(form.value.price),
      referencePurchaseCost: form.value.cost === null ? null : Number(form.value.cost),
      ...(!editing ? { openingQuantity: Number(form.value.stock), openingCost: form.value.stock > 0 ? Number(form.value.openingCost) : null } : {}),
    }
    submitting.value = true
    try {
      await apiRequest<Product>(editing ? `/api/products/${selectedId.value}` : '/api/products', { method: editing ? 'PUT' : 'POST', body: JSON.stringify(input) })
      await load(1)
    } catch (reason) { overlayError.value = reason instanceof Error ? reason.message : 'Không thể lưu sản phẩm.'; return }
    finally { submitting.value = false }
  }
  drawer.value = null
  if (productDemoEnabled.value) { activeCategory.value = 'Tất cả'; filter.value.category = 'Tất cả' }
  page.value = 1
  notice.value = productDemoEnabled.value ? 'Đã lưu trong dữ liệu mẫu của phiên này.' : 'Đã lưu sản phẩm.'
}
async function confirmAdjust() {
  if (!selected.value || adjustment.value === 0 || !adjustmentReason.value) return
  if (productDemoEnabled.value) {
    selected.value.quantityOnHand = Math.max(0, selected.value.quantityOnHand + Number(adjustment.value))
    selected.value.updatedAt = 'Hôm nay'
  } else {
    if (adjustment.value > 0 && !liveProduct.value?.hasAverageCost && adjustmentCost.value === null) { overlayError.value = 'Nhập giá vốn đơn vị cho lượng tăng.'; return }
    const attempt = adjustmentAttempt.value ?? {
      operationId: crypto.randomUUID(), productId: selected.value.id, quantityDelta: Number(adjustment.value),
      adjustmentUnitCost: adjustment.value > 0 && !liveProduct.value?.hasAverageCost ? adjustmentCost.value : null,
      reason: [adjustmentReason.value, adjustmentNote.value.trim()].filter(Boolean).join(' — '),
    }
    adjustmentAttempt.value = attempt
    submitting.value = true
    overlayError.value = ''
    try { await apiRequest('/api/inventory/adjustments', { method: 'POST', body: JSON.stringify(attempt) }); adjustmentAttempt.value = null; liveProduct.value = null; await load(page.value) }
    catch (reason) { if (reason instanceof ApiError) adjustmentAttempt.value = null; overlayError.value = reason instanceof ApiError ? reason.message : 'Chưa rõ kết quả điều chỉnh. Hãy thử gửi lại cùng phiếu này.'; return }
    finally { submitting.value = false }
  }
  modal.value = null
  notice.value = productDemoEnabled.value ? 'Đã điều chỉnh tồn trong dữ liệu mẫu.' : 'Đã điều chỉnh tồn kho.'
}
async function confirmStocktake() {
  if (!selected.value) return
  if (productDemoEnabled.value) {
    selected.value.quantityOnHand = Math.max(0, Number(counted.value))
    selected.value.updatedAt = 'Hôm nay'
  } else {
    if (!stockContext.value) { overlayError.value = 'Tải lại số tồn trước khi kiểm kho.'; return }
    const difference = Number(counted.value) - stockContext.value.expectedQuantity
    if (difference > 0 && !stockContext.value.hasAverageCost && stocktakeCost.value === null) { overlayError.value = 'Nhập giá vốn đơn vị cho lượng tăng.'; return }
    const attempt = stocktakeAttempt.value ?? {
      operationId: crypto.randomUUID(), productId: selected.value.id,
      expectedQuantity: stockContext.value.expectedQuantity, expectedRevision: stockContext.value.expectedRevision,
      countedQuantity: Number(counted.value),
      adjustmentUnitCost: difference > 0 && !stockContext.value.hasAverageCost ? stocktakeCost.value : null,
      note: stockNote.value.trim() || null,
    }
    stocktakeAttempt.value = attempt
    submitting.value = true
    overlayError.value = ''
    try { await apiRequest('/api/inventory/stocktakes', { method: 'POST', body: JSON.stringify(attempt) }); stocktakeAttempt.value = null; liveProduct.value = null; await load(page.value) }
    catch (reason) {
      if (reason instanceof ApiError) stocktakeAttempt.value = null
      if (reason instanceof ApiError && reason.problem.code === 'stocktake-stale') { stockContext.value = null; stockStep.value = 1; overlayError.value = 'Tồn kho đã thay đổi. Tải lại số tồn và kiểm đếm lại.' }
      else overlayError.value = reason instanceof ApiError ? reason.message : 'Chưa rõ kết quả kiểm kho. Hãy thử gửi lại cùng phiếu này.'
      return
    } finally { submitting.value = false }
  }
  modal.value = null
  notice.value = productDemoEnabled.value ? 'Đã ghi nhận kiểm kho trong dữ liệu mẫu.' : 'Đã ghi nhận kiểm kho.'
}
async function advanceStocktake() {
  if (!productDemoEnabled.value && !stockContext.value && selected.value) {
    overlayLoading.value = true
    try { stockContext.value = await apiRequest<StocktakeContext>(`/api/inventory/stocktakes/context/${selected.value.id}`); counted.value = stockContext.value.expectedQuantity; overlayError.value = '' }
    catch (reason) { overlayError.value = reason instanceof Error ? reason.message : 'Không thể tải số tồn.' }
    finally { overlayLoading.value = false }
    return
  }
  if (stockStep.value < 3) stockStep.value += 1
  else confirmStocktake()
}
function onKeydown(event: KeyboardEvent) { if (event.key === 'Escape') closeOverlays() }
onMounted(() => { load(); window.addEventListener('keydown', onKeydown) })
onUnmounted(() => { if (searchTimer) clearTimeout(searchTimer); ++loadSequence; window.removeEventListener('keydown', onKeydown) })
</script>

<template>
  <section class="product-page" @click.self="menuId = null">
    <div class="product-panel">
      <div class="product-heading">
        <div><h1>{{ inventoryView ? 'Tồn kho' : 'Sản phẩm' }}</h1><p>{{ inventoryView ? 'Theo dõi số lượng hàng hóa tại Kho chính' : 'Quản lý danh mục và thông tin sản phẩm' }}</p></div>
        <div class="heading-actions"><RouterLink v-if="isOwner && !productDemoEnabled" class="product-secondary-link" to="/import">Nhập CSV</RouterLink><button v-if="isOwner || productDemoEnabled" class="product-primary" type="button" @click="openCreate"><LineIcon name="plus" />Thêm sản phẩm</button></div>
      </div>
      <div class="product-toolbar"><label class="product-search"><LineIcon name="search" /><input v-model="search" aria-label="Tìm theo tên, mã SKU, mã vạch" placeholder="Tìm theo tên, mã SKU, mã vạch..." /><span class="product-scan" aria-hidden="true"><LineIcon name="barcode" /></span></label><button class="product-filter-button" type="button" @click="openFilter"><span aria-hidden="true">▽</span> Bộ lọc</button></div>
      <div class="product-chips" aria-label="Danh mục sản phẩm"><button v-for="category in productCategories" :key="category" class="product-chip" :class="{ active: activeCategory === category }" type="button" :aria-pressed="activeCategory === category" @click="chooseCategory(category)">{{ category }} ({{ categoryCount(category) }})</button></div>
      <p v-if="!productDemoEnabled && activeCategory !== 'Tất cả'" class="product-scope-note">Danh mục được ước tính theo tên và lọc trong trang hiện tại.</p>
      <p v-if="error && !productDemoEnabled" class="product-error" role="alert">{{ error }}</p>
      <p v-if="notice" class="product-notice" role="status">{{ notice }} <button type="button" aria-label="Đóng thông báo" @click="notice = ''">×</button></p>
      <div class="product-table-wrap"><table class="product-table">
        <colgroup><col class="col-check" /><col class="col-name" /><col class="col-sku" /><col class="col-category" /><col class="col-price" /><col class="col-stock" /><col class="col-status" /><col class="col-updated" /><col class="col-actions" /></colgroup>
        <thead><tr><th><input type="checkbox" aria-label="Chọn tất cả sản phẩm trên trang" :checked="allChecked" @change="toggleAll" /></th><th>Sản phẩm ↕</th><th>Mã SKU ↕</th><th>Danh mục ↕</th><th>Giá bán ↕</th><th>Tồn kho ↕</th><th>Trạng thái ↕</th><th>Cập nhật ↕</th><th><span class="sr-only">Thao tác</span></th></tr></thead>
        <tbody>
          <tr v-if="loading && !productDemoEnabled"><td colspan="9" class="product-empty">Đang tải sản phẩm…</td></tr>
          <tr v-else-if="visibleRows.length === 0"><td colspan="9" class="product-empty">Chưa có sản phẩm phù hợp.</td></tr>
          <tr v-for="item in loading && !productDemoEnabled ? [] : visibleRows" :key="item.id" :class="{ selected: selectedId === item.id }">
            <td><input type="checkbox" :aria-label="`Chọn ${item.name}`" :checked="checkedIds.includes(item.id)" @change="toggleChecked(item.id)" /></td>
            <td><div class="product-identity"><img v-if="item.image" :src="item.image" :alt="item.name" /><span v-else class="product-placeholder"><LineIcon name="product" /></span><div><button class="product-name" type="button" @click="openAction('detail', item)">{{ item.name }}</button><small>{{ item.unit }}</small></div></div></td>
            <td>{{ item.sku }}</td><td>{{ item.category }}</td><td class="product-price">{{ money(item.salePrice) }}</td><td class="product-stock">{{ item.quantityOnHand }}</td><td><span class="product-status" :class="statusTone(item)"><span class="product-dot" />{{ statusLabel(item) }}</span></td><td class="product-updated">{{ updatedDate(item.updatedAt) }}<br v-if="updatedTime(item.updatedAt)" />{{ updatedTime(item.updatedAt) }}</td>
            <td class="product-actions-cell"><button class="product-more" type="button" :aria-label="`Thao tác ${item.name}`" :aria-expanded="menuId === item.id" @click.stop="menuId = menuId === item.id ? null : item.id"><LineIcon name="kebab" /></button>
              <div v-if="menuId === item.id" class="product-action-menu"><button type="button" @click="openAction('detail', item)">Xem chi tiết</button><button v-if="isOwner || productDemoEnabled" type="button" @click="openAction('edit', item)">Sửa</button><button v-if="(isOwner && item.isActive) || productDemoEnabled" type="button" @click="openAction('adjust', item)">Điều chỉnh tồn kho</button><button v-if="(isOwner && item.isActive) || productDemoEnabled" type="button" @click="openAction('stocktake', item)">Kiểm kho</button><button type="button" @click="openAction('history', item)">Lịch sử tồn kho</button><button v-if="(isOwner || productDemoEnabled) && item.isActive" class="danger" type="button" @click="openAction('stop', item)">Ngừng kinh doanh</button></div>
            </td>
          </tr>
        </tbody>
      </table></div>
      <div class="product-footer"><span>Hiển thị {{ firstIndex }} – {{ lastIndex }} / {{ totalCount }} sản phẩm</span><div class="product-pagination"><button type="button" aria-label="Trang trước" :disabled="page <= 1" @click="load(page - 1)">‹</button><button v-for="number in Math.min(totalPages, 5)" :key="number" type="button" :class="{ active: page === number }" @click="load(number)">{{ number }}</button><span v-if="totalPages > 5">…</span><button v-if="totalPages > 5" type="button" :class="{ active: page === totalPages }" @click="load(totalPages)">{{ totalPages }}</button><button type="button" aria-label="Trang sau" :disabled="page >= totalPages" @click="load(page + 1)">›</button><select v-model.number="pageSize" aria-label="Số sản phẩm mỗi trang" @change="load(1)"><option :value="10">10 / trang</option><option :value="20">20 / trang</option><option :value="50">50 / trang</option></select></div></div>
    </div>
    <div v-if="drawer || modal" class="product-overlay" @click.self="closeOverlays" />
    <aside v-if="drawer" class="product-drawer" :class="{ wide: drawer !== 'filter' }" role="dialog" aria-modal="true" :aria-label="drawer === 'filter' ? 'Bộ lọc sản phẩm' : drawer === 'detail' ? 'Chi tiết sản phẩm' : drawer === 'create' ? 'Thêm sản phẩm' : 'Sửa sản phẩm'">
      <div class="product-drawer-head"><h2>{{ drawer === 'filter' ? 'Bộ lọc sản phẩm' : drawer === 'detail' ? 'Chi tiết sản phẩm' : drawer === 'create' ? 'Thêm sản phẩm' : 'Sửa sản phẩm' }}</h2><button type="button" aria-label="Đóng" @click="drawer = null">×</button></div>
      <p v-if="overlayLoading" class="product-overlay-message">Đang tải thông tin sản phẩm…</p>
      <p v-if="overlayError" class="product-error product-overlay-message" role="alert">{{ overlayError }}</p>
      <div v-if="drawer === 'filter'" class="product-drawer-body">
        <div class="product-section"><h3>Trạng thái</h3><label><input v-model="draft.status" type="radio" value="active" /> Đang bán</label><label><input v-model="draft.status" type="radio" value="inactive" /> Ngừng bán</label><label><input v-model="draft.status" type="radio" value="all" /> Tất cả</label></div>
        <div class="product-section"><h3>Tồn kho</h3><label><input v-model="draft.stock" type="radio" value="all" /> Tất cả</label><label><input v-model="draft.stock" type="radio" value="low" /> Tồn kho thấp (≤ 8)</label><label><input v-model="draft.stock" type="radio" value="out" /> Hết hàng</label></div>
        <div class="product-section"><h3>Khoảng giá bán</h3><div class="product-two-fields"><label>Từ<input v-model="draft.minPrice" type="number" min="0" placeholder="0 đ" /></label><label>Đến<input v-model="draft.maxPrice" type="number" min="0" placeholder="Không giới hạn" /></label></div></div>
        <div class="product-section"><h3>Danh mục</h3><label v-for="category in productCategories" :key="category"><input v-model="draft.category" type="radio" :value="category" /> {{ category }}</label></div>
      </div>
      <div v-else-if="drawer === 'detail' && selected" class="product-drawer-body"><div class="product-detail-identity"><img v-if="selected.image" :src="selected.image" :alt="selected.name" /><div><h3>{{ selected.name }}</h3><p>{{ selected.sku }} · {{ selected.unit }}</p></div></div><div class="product-detail-grid"><div><small>Giá bán</small><strong>{{ money(selected.salePrice) }}</strong></div><div><small>Tồn kho</small><strong>{{ selected.quantityOnHand }} {{ selected.unit.toLowerCase() }}</strong></div><div><small>Danh mục</small><strong>{{ selected.category }}</strong></div><div><small>Trạng thái</small><strong>{{ selected.isActive ? 'Đang bán' : 'Ngừng bán' }}</strong></div></div><div class="product-detail-buttons"><button v-if="isOwner || productDemoEnabled" type="button" @click="openAction('edit', selected)">Sửa sản phẩm</button><button v-if="(isOwner && selected.isActive) || productDemoEnabled" type="button" @click="openAction('adjust', selected)">Điều chỉnh tồn kho</button><button v-if="(isOwner && selected.isActive) || productDemoEnabled" type="button" @click="openAction('stocktake', selected)">Kiểm kho</button><button type="button" @click="openAction('history', selected)">Lịch sử tồn kho</button></div></div>
      <form v-else-if="(drawer === 'create' || drawer === 'edit') && !overlayLoading && (productDemoEnabled || drawer === 'create' || liveProduct?.id === selectedId)" class="product-drawer-body product-form" @submit.prevent="saveProduct">
        <h3>Thông tin cơ bản</h3>
        <div class="product-two-fields"><label>Tên sản phẩm <span>*</span><input v-model="form.name" required placeholder="Nhập tên sản phẩm" maxlength="160" /></label><label>Mã SKU <span>*</span><input v-model="form.sku" required placeholder="Nhập mã SKU" maxlength="64" /></label></div>
        <div class="product-two-fields"><label>Mã vạch<input v-model="form.barcode" placeholder="Nhập mã vạch" maxlength="64" /></label><label>Đơn vị tính <span>*</span><input v-model="form.unit" required placeholder="Chai, lon, gói..." maxlength="32" /></label></div>
        <label>Danh mục<select v-model="form.category" :disabled="!productDemoEnabled"><option v-for="category in productCategories.filter(value => value !== 'Tất cả')" :key="category">{{ category }}</option></select></label>
        <p v-if="!productDemoEnabled" class="product-preview-hint">Danh mục đang được ước tính theo tên; dữ liệu thật chưa hỗ trợ lưu danh mục.</p>
        <h3>Giá và chi phí</h3>
        <div class="product-two-fields"><label>Giá bán (đ)<input v-model.number="form.price" type="number" min="0" step="0.01" /></label><label>Giá nhập tham chiếu (đ)<input v-model.number="form.cost" type="number" min="0" step="0.01" /></label></div>
        <template v-if="drawer === 'create'"><h3>Tồn kho ban đầu (tùy chọn)</h3><div class="product-two-fields"><label>Số lượng<input v-model.number="form.stock" type="number" min="0" step="0.001" /></label><label>Đơn giá vốn (đ)<input v-model.number="form.openingCost" type="number" min="0" step="0.01" /></label></div></template>
        <p v-if="productDemoEnabled" class="product-preview-hint">Dữ liệu mẫu chỉ lưu trong phiên trình duyệt này.</p>
        <div class="product-drawer-foot"><button class="product-secondary-link" type="button" @click="drawer = null">Hủy</button><button class="product-primary" type="submit" :disabled="submitting || overlayLoading">{{ submitting ? 'Đang lưu…' : 'Lưu sản phẩm' }}</button></div>
      </form>
      <div v-if="drawer === 'filter'" class="product-drawer-foot"><button class="product-secondary-link" type="button" @click="clearFilter">Xóa bộ lọc</button><button class="product-primary" type="button" @click="applyFilter">Áp dụng</button></div>
    </aside>
    <div v-if="modal && selected" class="product-modal-layer" role="presentation" @click.self="modal = null">
      <section class="product-modal" :class="{ wide: modal !== 'adjust' }" role="dialog" aria-modal="true" :aria-label="modal === 'adjust' ? 'Điều chỉnh tồn kho' : modal === 'stocktake' ? 'Kiểm kho' : 'Lịch sử tồn kho'">
        <div class="product-drawer-head"><h2>{{ modal === 'adjust' ? 'Điều chỉnh tồn kho' : modal === 'stocktake' ? 'Kiểm kho' : 'Lịch sử tồn kho' }}</h2><button type="button" aria-label="Đóng" @click="modal = null">×</button></div>
        <div class="product-modal-body">
          <p v-if="overlayLoading" class="product-overlay-message">Đang tải thông tin sản phẩm…</p>
          <p v-if="overlayError" class="product-error" role="alert">{{ overlayError }}</p>
          <div class="product-detail-identity"><img v-if="selected.image" :src="selected.image" :alt="selected.name" /><div><h3>{{ selected.name }}</h3><p>{{ selected.sku }} · {{ selected.unit }}</p></div></div>
          <template v-if="overlayLoading"></template>
          <template v-else-if="modal === 'adjust'">
            <label>Số lượng điều chỉnh <span class="required">*</span><input v-model.number="adjustment" type="number" step="0.001" :disabled="submitting || adjustmentAttempt !== null" /></label>
            <div class="product-info-note">↑ Số dương: Tăng tồn kho<br />↓ Số âm: Giảm tồn kho</div>
            <label v-if="!productDemoEnabled && adjustment > 0 && !liveProduct?.hasAverageCost">Giá vốn đơn vị cho lượng tăng (đ) <span class="required">*</span><input v-model.number="adjustmentCost" type="number" min="0" step="0.0001" :disabled="submitting || adjustmentAttempt !== null" /></label>
            <label>Lý do điều chỉnh <span class="required">*</span><select v-model="adjustmentReason" :disabled="submitting || adjustmentAttempt !== null"><option value="">Chọn lý do</option><option>Hàng hỏng</option><option>Mất mát</option><option>Cân chỉnh sổ sách</option><option>Khác</option></select></label>
            <label>Ghi chú<textarea v-model="adjustmentNote" placeholder="Nhập ghi chú..." :disabled="submitting || adjustmentAttempt !== null" /></label>
          </template>
          <template v-else-if="modal === 'stocktake'">
            <div class="product-stepper"><div class="product-steps"><span v-for="step in 3" :key="step" :class="{ active: step === stockStep, done: step < stockStep }"><b>{{ step }}</b>{{ ['Nhập số đếm', 'Xem chênh lệch', 'Xác nhận'][step - 1] }}</span></div>
              <div v-if="stockStep === 1" class="product-step-content"><label>Số lượng thực đếm <span class="required">*</span><input v-model.number="counted" type="number" min="0" step="0.001" :disabled="submitting || stocktakeAttempt !== null" /></label><div class="product-info-note">Tồn kho hiện tại: <strong>{{ expectedStock }} {{ selected.unit.toLowerCase() }}</strong><br />Chênh lệch: <strong>{{ counted - expectedStock }}</strong></div><label>Ghi chú (tùy chọn)<textarea v-model="stockNote" placeholder="Nhập ghi chú..." :disabled="submitting || stocktakeAttempt !== null" /></label></div>
              <div v-else-if="stockStep === 2" class="product-info-note">Xác nhận chênh lệch kiểm kho<br /><br />Tồn hệ thống: {{ expectedStock }}<br />Số thực đếm: {{ counted }}<br />Chênh lệch: <strong>{{ counted - expectedStock }}</strong></div>
              <div v-else class="product-step-content"><div class="product-info-note green"><strong>Sẵn sàng xác nhận.</strong><br />Thao tác sẽ cập nhật tồn kho theo chênh lệch vừa kiểm{{ productDemoEnabled ? ' trong dữ liệu mẫu' : '' }}.</div><label v-if="!productDemoEnabled && counted > expectedStock && !stockContext?.hasAverageCost">Giá vốn đơn vị cho lượng tăng (đ) <span class="required">*</span><input v-model.number="stocktakeCost" type="number" min="0" step="0.0001" :disabled="submitting || stocktakeAttempt !== null" /></label></div>
            </div>
          </template>
          <template v-else>
            <div class="product-history-filters"><label>Từ ngày<input v-model="historyFrom" placeholder="dd/mm/yyyy" /></label><label>Đến ngày<input v-model="historyTo" placeholder="dd/mm/yyyy" /></label><label>Loại giao dịch<select v-model="historyType"><option>Tất cả</option><option>Bán hàng</option><option>Nhập hàng</option><option>Điều chỉnh</option><option>Kiểm kho</option></select></label></div>
            <div class="product-history-wrap"><table class="product-history"><thead><tr><th>Ngày</th><th>Loại giao dịch</th><th>Số lượng</th><th>Đơn giá vốn</th><th>Tồn sau GD</th><th>Người tạo</th><th>Ghi chú</th></tr></thead><tbody>
              <tr v-for="(entry, index) in historyRows" :key="index"><td>{{ entry.date }}</td><td>{{ entry.type }}</td><td :class="entry.quantity >= 0 ? 'plus' : 'minus'">{{ entry.quantity > 0 ? '+' : '' }}{{ entry.quantity }}</td><td>{{ money(entry.cost) }}</td><td>{{ entry.stock }}</td><td>{{ entry.actor }}</td><td>{{ entry.note }}</td></tr>
              <tr v-if="historyRows.length === 0"><td colspan="7">Chưa có lịch sử tồn kho phù hợp.</td></tr>
            </tbody></table></div>
          </template>
        </div>
        <div class="product-modal-foot"><button class="product-secondary-link" type="button" @click="modal = null">{{ modal === 'history' ? 'Đóng' : 'Hủy' }}</button><button v-if="modal === 'stocktake' && stockStep > 1" class="product-secondary-link" type="button" :disabled="submitting || stocktakeAttempt !== null" @click="stockStep -= 1">Quay lại</button><button v-if="modal === 'adjust'" class="product-primary" type="button" :disabled="submitting || overlayLoading || adjustment === 0 || !adjustmentReason || (!productDemoEnabled && liveProduct?.id !== selectedId)" @click="confirmAdjust">Xác nhận</button><button v-if="modal === 'stocktake'" class="product-primary" type="button" :disabled="submitting || overlayLoading || counted < 0" @click="advanceStocktake">{{ !productDemoEnabled && !stockContext ? 'Tải lại số tồn' : stockStep === 3 ? 'Xác nhận' : 'Tiếp theo →' }}</button></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.product-page{color:#172033}.product-panel{min-height:calc(100vh - 74px);padding:12px;background:#fff;border:1px solid #dfe7ee;border-radius:14px;box-shadow:0 10px 30px #1f304714}.product-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:16px;padding:2px 2px 14px}.product-heading h1{margin:0 0 2px;font-size:22px;font-weight:800;letter-spacing:-.02em}.product-heading p{margin:0;color:#66758d;font-size:12px}.heading-actions{display:flex;align-items:center;gap:8px}.product-primary,.product-secondary-link{display:inline-flex;min-height:38px;align-items:center;justify-content:center;gap:7px;border-radius:8px;padding:0 17px;font-size:13px;font-weight:800;text-decoration:none;cursor:pointer}.product-primary{border:0;background:linear-gradient(180deg,#10a971,#078d5d);color:#fff;box-shadow:0 8px 16px #0a9c6729}.product-primary:hover{background:#087b55}.product-primary .line-icon{width:16px;height:16px}.product-secondary-link{border:1px solid #cdd9e4;background:#fff;color:#2d3c54}.product-toolbar{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:10px}.product-search{display:flex;height:36px;align-items:center;gap:10px;border:1.5px solid #b8cef8;border-radius:8px;padding:0 10px;color:#53617b}.product-search>.line-icon{width:14px;height:14px}.product-search input{flex:1;min-width:0;border:0;outline:0;color:#44536a;font-size:12px}.product-scan{display:grid;width:28px;height:24px;place-items:center;border:1px solid #dfe7ee;border-radius:6px;background:#f8fbff}.product-scan .line-icon{width:16px;height:16px}.product-filter-button{height:36px;border:1px solid #cdd9e4;border-radius:8px;background:#fff;color:#263852;font-size:12px;font-weight:700;cursor:pointer}.product-chips{display:flex;flex-wrap:wrap;gap:8px;padding:12px 0}.product-chip{height:30px;border:1px solid #dfe7ee;border-radius:999px;background:#fff;padding:0 12px;color:#32435e;font-size:11px;font-weight:700;white-space:nowrap;cursor:pointer}.product-chip.active{border-color:transparent;background:linear-gradient(180deg,#14aa72,#078d5e);color:#fff}.product-scope-note{margin:0 0 8px;color:#66758d;font-size:11px}.product-error,.product-notice{margin:0 0 10px;padding:8px 10px;border-radius:8px;font-size:12px}.product-error{background:#fff0f1;color:#a94448}.product-notice{display:flex;justify-content:space-between;background:#eaf8f2;color:#087e57}.product-notice button{cursor:pointer}.product-table-wrap{overflow:visible;border:1px solid #dfe7ee;border-radius:9px;background:#fff}.product-table{width:100%;border-collapse:collapse;table-layout:fixed}.col-check{width:34px}.col-name{width:26%}.col-sku{width:11%}.col-category{width:14%}.col-price{width:10%}.col-stock{width:7%}.col-status{width:12%}.col-updated{width:13%}.col-actions{width:42px}.product-table th{height:34px;border-bottom:1px solid #dfe7ee;background:#f7fafc;padding:0 9px;color:#56657b;font-size:10.5px;font-weight:750;text-align:left;white-space:nowrap}.product-table td{height:53px;border-bottom:1px solid #edf1f4;padding:0 9px;color:#2b3a51;font-size:11px;vertical-align:middle}.product-table tbody tr:last-child td{border-bottom:0}.product-table tbody tr:hover{background:#fafdff}.product-table tbody tr.selected{background:#f2fbf7}.product-table input[type=checkbox]{width:14px;height:14px;margin:0;accent-color:#0a9c67;cursor:pointer}.product-identity{display:flex;min-width:0;align-items:center;gap:9px}.product-identity img,.product-placeholder{width:28px;height:36px;flex:none;object-fit:contain}.product-placeholder{display:grid;place-items:center;border-radius:6px;background:#edf7f3;color:#0a9c67}.product-placeholder .line-icon{width:18px;height:18px}.product-identity>div{min-width:0}.product-name{display:block;overflow:hidden;max-width:100%;padding:0;color:#172238;font-size:11px;font-weight:800;text-align:left;text-overflow:ellipsis;white-space:nowrap;cursor:pointer}.product-name:hover{color:#087b55;text-decoration:underline}.product-identity small{display:block;margin-top:1px;color:#617089;font-size:10px}.product-price{font-weight:800}.product-stock{text-align:center;font-weight:800}.product-status{display:inline-flex;height:22px;align-items:center;gap:5px;border-radius:7px;padding:0 8px;font-size:10px;font-weight:800;white-space:nowrap}.product-status.ok{background:#e9f8f2;color:#087e57}.product-status.low{background:#fff0d7;color:#c76e06}.product-status.off{background:#f1f3f6;color:#7a8696}.product-dot{width:6px;height:6px;border-radius:50%;background:currentColor}.product-updated{color:#52627a}.product-actions-cell{position:relative}.product-more{display:grid;width:28px;height:28px;place-items:center;border:1px solid #dfe7ee;border-radius:7px;background:#fff;color:#44536b;cursor:pointer}.product-more .line-icon{width:14px;height:14px;transform:rotate(90deg)}.product-action-menu{position:absolute;z-index:15;top:42px;right:4px;width:190px;border:1px solid #dfe7ee;border-radius:10px;background:#fff;padding:6px;box-shadow:0 14px 36px #17203324}.product-action-menu button{display:block;width:100%;height:34px;border:0;border-radius:7px;background:transparent;padding:0 10px;color:#2d3c54;font-size:12px;text-align:left;cursor:pointer}.product-action-menu button:hover{background:#f4f8fa}.product-action-menu button.danger{color:#d33d45}.product-empty{height:100px!important;color:#66758d!important;text-align:center}.product-footer{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:12px 4px 2px;color:#5d6b80;font-size:11px}.product-pagination{display:flex;align-items:center;gap:5px}.product-pagination button,.product-pagination select{min-width:30px;height:30px;border:1px solid #dfe7ee;border-radius:7px;background:#fff;padding:0 7px;color:#42516a;font-size:11px;cursor:pointer}.product-pagination button.active{border-color:#d5eee4;background:#e9f8f2;color:#07875b;font-weight:800}.product-pagination button:disabled{opacity:.45;cursor:default}.product-pagination select{margin-left:3px}.product-overlay{position:fixed;z-index:39;inset:0;background:#20303d2e}.product-drawer{position:fixed;z-index:40;top:44px;right:0;bottom:0;display:flex;width:min(300px,100vw);flex-direction:column;border-left:1px solid #dfe7ee;background:#fff;box-shadow:-12px 0 30px #1f304717}.product-drawer.wide{width:min(420px,100vw)}.product-drawer-head{display:flex;min-height:54px;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #dfe7ee;padding:0 16px}.product-drawer-head h2{margin:0;font-size:18px;font-weight:800}.product-drawer-head button{border:0;background:transparent;color:#536178;font-size:24px;cursor:pointer}.product-drawer-body{flex:1;overflow:auto;padding:14px 16px 20px}.product-section{margin-bottom:16px;border-bottom:1px solid #edf1f4;padding-bottom:16px}.product-section h3,.product-form h3{margin:0 0 12px;font-size:13px;font-weight:800}.product-section label{display:flex;align-items:center;gap:7px;margin:8px 0;color:#3f4f66;font-size:11px}.product-section input[type=radio]{accent-color:#0a9c67}.product-two-fields{display:grid;grid-template-columns:1fr 1fr;gap:10px}.product-section .product-two-fields label,.product-form label,.product-modal-body label{display:grid;align-items:start;gap:6px;color:#3e4e66;font-size:11px;font-weight:700}.product-form{display:flex;flex-direction:column;gap:12px}.product-form label span{color:#d63e45}.product-form input,.product-form select,.product-section input[type=number],.product-modal-body input{width:100%;height:34px;border:1px solid #cdd9e4;border-radius:7px;background:#fff;padding:0 9px;color:#31415a;font-size:11px}.product-drawer-foot,.product-modal-foot{display:flex;gap:10px;border-top:1px solid #dfe7ee;padding:12px 16px}.product-drawer-foot>*{flex:1}.product-detail-identity{display:flex;align-items:center;gap:12px;margin-bottom:14px;border-bottom:1px solid #dfe7ee;padding-bottom:14px}.product-detail-identity img{width:46px;height:56px;object-fit:contain}.product-detail-identity h3{margin:0 0 3px;font-size:15px}.product-detail-identity p,.product-modal-body p{margin:0;color:#66758d;font-size:11px}.product-detail-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px}.product-detail-grid>div{display:grid;gap:4px;border:1px solid #dfe7ee;border-radius:9px;background:#fbfdfe;padding:10px}.product-detail-grid small{color:#66758d;font-size:10px}.product-detail-grid strong{font-size:14px}.product-detail-buttons{display:grid;gap:8px;margin-top:14px}.product-detail-buttons button{height:38px;border:1px solid #cdd9e4;border-radius:8px;background:#fff;padding:0 11px;color:#304057;font-size:12px;font-weight:700;text-align:left;cursor:pointer}.product-preview-hint{color:#738197;font-size:10px;line-height:1.45}.product-modal-layer{position:fixed;z-index:50;inset:0;display:grid;place-items:center;padding:20px}.product-modal{width:min(420px,100%);border:1px solid #dfe7ee;border-radius:13px;background:#fff;box-shadow:0 24px 60px #19283c38}.product-modal-body{display:grid;gap:12px;padding:16px}.product-modal-foot{justify-content:flex-end}.product-modal-body label{margin-top:6px}@media(max-width:1100px){.product-table-wrap{overflow-x:auto}.product-table{min-width:1000px}}@media(max-width:700px){.product-panel{min-height:calc(100vh - 76px)}.product-heading{align-items:stretch;flex-direction:column}.heading-actions{justify-content:flex-end}.product-toolbar{grid-template-columns:1fr}.product-chips{flex-wrap:nowrap;overflow-x:auto}.product-footer{align-items:flex-start;flex-direction:column}.product-drawer{top:0}.product-two-fields{grid-template-columns:1fr}}
.product-heading{padding-bottom:12px}.product-heading h1{line-height:1.2}.product-heading p{line-height:1.3}
.product-modal.wide{width:min(92vw,720px)}.product-modal:has(.product-history){width:min(96vw,920px)}.product-detail-identity h3{font-weight:800}.product-modal-body textarea,.product-modal-body select{width:100%;min-height:34px;border:1px solid #cdd9e4;border-radius:7px;background:#fff;padding:7px 9px;color:#31415a;font-size:11px}.product-modal-body textarea{min-height:72px}.product-modal-body .required{color:#d63e45}.product-info-note{border:1px solid #cbe1ff;border-radius:8px;background:#f4f8ff;padding:9px 10px;color:#36537d;font-size:11px;line-height:1.5}.product-info-note.green{border-color:#cfeadd;background:#f3fbf7;color:#2a6c54}.product-stepper{display:grid;grid-template-columns:120px 1fr;gap:18px}.product-steps{display:flex;flex-direction:column;gap:16px}.product-steps span{display:flex;align-items:center;gap:9px;color:#7b889a;font-size:11px;font-weight:700}.product-steps b{display:grid;width:22px;height:22px;flex:none;place-items:center;border-radius:50%;background:#aeb8c8;color:#fff;font-size:10px}.product-steps .active{color:#087e57}.product-steps .active b{background:#0a9c67}.product-steps .done b{background:#78bf9f}.product-step-content{display:grid;gap:12px}.product-history-filters{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}.product-history-wrap{overflow:auto}.product-history{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}.product-history th{border-bottom:1px solid #dfe7ee;background:#f7fafc;padding:8px;color:#59677c;text-align:left}.product-history td{border-bottom:1px solid #edf1f4;padding:8px;color:#35455b}.product-history .plus{color:#0a9c67;font-weight:800}.product-history .minus{color:#d93f45;font-weight:800}@media(max-width:700px){.product-stepper{grid-template-columns:1fr}.product-steps{flex-direction:row;overflow:auto}.product-history-filters{grid-template-columns:1fr}}
.product-form .product-drawer-foot{position:sticky;bottom:-20px;z-index:1;margin:auto -16px -20px;background:#fff}
.product-primary:disabled{opacity:.55;cursor:not-allowed}
.product-overlay-message{margin:10px 16px;color:#66758d;font-size:11px}.product-form select:disabled{background:#f1f3f6;color:#7a8696}.product-modal-body .product-error{margin:0}
</style>
