<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import type { InventoryMovement, Product, StockAdjustmentResult, StocktakeContext, StocktakeResult } from '../api/types'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const id = String(route.params.id)
const product = ref<Product | null>(null)
const movements = ref<InventoryMovement[]>([])
const loading = ref(true)
const error = ref('')
const deactivating = ref(false)
const auth = useAuthStore()
const adjustmentQuantity = ref<number | null>(null)
const adjustmentCost = ref<number | null>(null)
const adjustmentReason = ref('')
const adjustmentSubmitting = ref(false)
const adjustmentMessage = ref('')
const adjustmentAttempt = ref<{
  operationId: string
  productId: string
  quantityDelta: number
  adjustmentUnitCost: number | null
  reason: string
} | null>(null)
const stocktakeContext = ref<StocktakeContext | null>(null)
const countedQuantity = ref<number | null>(null)
const stocktakeCost = ref<number | null>(null)
const stocktakeNote = ref('')
const stocktakeSubmitting = ref(false)
const stocktakeMessage = ref('')
const stocktakeAttempt = ref<{
  operationId: string
  productId: string
  expectedQuantity: number
  expectedRevision: string
  countedQuantity: number
  adjustmentUnitCost: number | null
  note: string | null
} | null>(null)
const stocktakeDifference = computed(() =>
  stocktakeContext.value && countedQuantity.value !== null
    ? countedQuantity.value - stocktakeContext.value.expectedQuantity
    : 0)
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
const movementLabels: Record<string, string> = {
  OpeningBalance: 'Tồn đầu', Purchase: 'Nhập hàng', Sale: 'Bán hàng',
  ReturnRestock: 'Trả hàng nhập lại kho', SaleVoid: 'Hủy đơn bán',
  PurchaseVoid: 'Hủy phiếu nhập', Adjustment: 'Điều chỉnh tồn',
  StocktakeAdjustment: 'Chênh lệch kiểm kho',
}
const reliabilityLabels: Record<string, string> = {
  Reliable: 'Tin cậy', Estimated: 'Ước tính', Unavailable: 'Không có giá vốn',
}
const movementLabel = (type: string) => movementLabels[type] ?? type

async function load() {
  loading.value = true
  try {
    [product.value, movements.value] = await Promise.all([
      apiRequest<Product>(`/api/products/${id}`),
      apiRequest<InventoryMovement[]>(`/api/products/${id}/movements`),
    ])
  } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải sản phẩm.' }
  finally { loading.value = false }
}

async function deactivate() {
  if (!confirm('Ngừng bán sản phẩm này?')) return
  deactivating.value = true
  try { await apiRequest<void>(`/api/products/${id}/deactivate`, { method: 'POST', body: '{}' }); await load() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể ngừng bán.' }
  finally { deactivating.value = false }
}

async function submitAdjustment() {
  if (adjustmentQuantity.value === null || adjustmentQuantity.value === 0 || !adjustmentReason.value.trim()) return
  adjustmentSubmitting.value = true; error.value = ''; adjustmentMessage.value = ''
  const attempt = adjustmentAttempt.value ?? {
    operationId: crypto.randomUUID(),
    productId: id,
    quantityDelta: adjustmentQuantity.value,
    adjustmentUnitCost: adjustmentQuantity.value > 0 && !product.value?.hasAverageCost ? adjustmentCost.value : null,
    reason: adjustmentReason.value.trim(),
  }
  adjustmentAttempt.value = attempt
  try {
    const result = await apiRequest<StockAdjustmentResult>('/api/inventory/adjustments', {
      method: 'POST',
      body: JSON.stringify(attempt),
    })
    adjustmentMessage.value = `Đã điều chỉnh ${result.quantityDelta > 0 ? '+' : ''}${result.quantityDelta}; giá vốn ${reliabilityLabels[result.costReliability]}.`
    adjustmentAttempt.value = null
    adjustmentQuantity.value = null; adjustmentCost.value = null; adjustmentReason.value = ''
    await load()
  } catch (reason) {
    if (reason instanceof ApiError) adjustmentAttempt.value = null
    error.value = reason instanceof ApiError
      ? reason.message
      : 'Chưa xác định được kết quả. Giữ nguyên phiếu này và thử gửi lại.'
  }
  finally { adjustmentSubmitting.value = false }
}

async function loadStocktakeContext(clearMessage = true) {
  error.value = ''
  if (clearMessage) stocktakeMessage.value = ''
  try {
    stocktakeContext.value = await apiRequest<StocktakeContext>(`/api/inventory/stocktakes/context/${id}`)
    countedQuantity.value = null
    stocktakeCost.value = null
  } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải số tồn để kiểm kho.' }
}

async function submitStocktake() {
  if (!stocktakeContext.value || countedQuantity.value === null) return
  stocktakeSubmitting.value = true; error.value = ''; stocktakeMessage.value = ''
  const context = stocktakeContext.value
  const attempt = stocktakeAttempt.value ?? {
    operationId: crypto.randomUUID(),
    productId: id,
    expectedQuantity: context.expectedQuantity,
    expectedRevision: context.expectedRevision,
    countedQuantity: countedQuantity.value,
    adjustmentUnitCost: stocktakeDifference.value > 0 && !context.hasAverageCost ? stocktakeCost.value : null,
    note: stocktakeNote.value.trim() || null,
  }
  stocktakeAttempt.value = attempt
  try {
    const result = await apiRequest<StocktakeResult>('/api/inventory/stocktakes', {
      method: 'POST',
      body: JSON.stringify(attempt),
    })
    stocktakeMessage.value = result.difference === 0
      ? 'Đã ghi nhận kiểm kho khớp, không tạo biến động giả.'
      : `Đã ghi nhận chênh lệch ${result.difference > 0 ? '+' : ''}${result.difference}.`
    stocktakeAttempt.value = null
    stocktakeNote.value = ''
    await Promise.all([load(), loadStocktakeContext(false)])
  } catch (reason) {
    if (reason instanceof ApiError && reason.problem.code === 'stocktake-stale') {
      stocktakeAttempt.value = null
      error.value = 'Tồn kho đã thay đổi trong lúc đếm. Hãy tải lại số tồn và kiểm đếm lại trước khi gửi phiếu mới.'
      stocktakeContext.value = null
      countedQuantity.value = null
    } else {
      if (reason instanceof ApiError) stocktakeAttempt.value = null
      error.value = reason instanceof ApiError
        ? reason.message
        : 'Chưa xác định được kết quả. Giữ nguyên phiếu kiểm kho và thử gửi lại.'
    }
  } finally { stocktakeSubmitting.value = false }
}

onMounted(load)
</script>

<template>
  <section>
    <RouterLink class="text-sm font-semibold text-emerald-800" to="/products">← Danh sách sản phẩm</RouterLink>
    <p v-if="loading" class="mt-6 text-slate-500">Đang tải…</p><p v-if="error" class="error mt-5" role="alert">{{ error }}</p>
    <template v-if="product">
      <div class="mt-3 flex flex-wrap items-start justify-between gap-4"><div><h1 class="text-3xl font-black">{{ product.name }}</h1><p class="mt-1 text-slate-500">{{ product.sku }} · {{ product.barcode || 'Không có barcode' }}</p></div><div v-if="auth.session.roles.includes('Owner')" class="flex gap-2"><RouterLink class="btn-secondary" :to="`/products/${id}/edit`">Sửa</RouterLink><button v-if="product.isActive" class="btn-secondary" :disabled="deactivating" @click="deactivate">Ngừng bán</button></div></div>
      <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4"><div class="card"><p class="text-sm text-slate-500">Tồn hiện tại</p><p class="mt-1 text-2xl font-black">{{ product.quantityOnHand }} {{ product.unit }}</p></div><div class="card"><p class="text-sm text-slate-500">Giá trị tồn</p><p class="mt-1 text-2xl font-black">{{ money(product.inventoryValue) }} ₫</p></div><div class="card"><p class="text-sm text-slate-500">Giá vốn bình quân</p><p class="mt-1 text-2xl font-black">{{ product.hasAverageCost ? `${money(product.averageCost)} ₫` : 'Chưa đủ tin cậy' }}</p></div><div class="card"><p class="text-sm text-slate-500">Giá bán</p><p class="mt-1 text-2xl font-black">{{ money(product.salePrice) }} ₫</p></div></div>

      <div v-if="auth.session.roles.includes('Owner') && product.isActive" class="mt-6 grid gap-5 lg:grid-cols-2">
        <form class="card grid gap-4" @submit.prevent="submitAdjustment">
          <div><h2 class="text-xl font-black">Điều chỉnh tồn</h2><p class="text-sm text-slate-500">Ghi nguồn bất biến cùng lý do và giá vốn áp dụng.</p></div>
          <div class="field"><label for="adjustment-quantity">Số lượng thay đổi</label><input id="adjustment-quantity" v-model.number="adjustmentQuantity" class="input" type="number" step="0.001" required :disabled="adjustmentAttempt !== null" /></div>
          <div v-if="adjustmentQuantity !== null && adjustmentQuantity > 0 && !product.hasAverageCost" class="field"><label for="adjustment-cost">Giá vốn đơn vị cho lượng tăng</label><input id="adjustment-cost" v-model.number="adjustmentCost" class="input" type="number" min="0" step="0.0001" required :disabled="adjustmentAttempt !== null" /><p class="text-xs text-amber-700">Giá này chỉ xác nhận movement mới; không tự làm tồn lịch sử trở nên tin cậy.</p></div>
          <div class="field"><label for="adjustment-reason">Lý do</label><input id="adjustment-reason" v-model.trim="adjustmentReason" class="input" maxlength="500" required :disabled="adjustmentAttempt !== null" /></div>
          <p v-if="adjustmentMessage" class="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800">{{ adjustmentMessage }}</p>
          <button class="btn-primary" type="submit" :disabled="adjustmentSubmitting">Ghi điều chỉnh</button>
        </form>

        <div class="card grid gap-4">
          <div><h2 class="text-xl font-black">Kiểm kho</h2><p class="text-sm text-slate-500">Tải số tồn, đếm thực tế rồi gửi với revision hiện tại.</p></div>
          <button v-if="!stocktakeContext" class="btn-secondary" type="button" @click="() => loadStocktakeContext()">Bắt đầu kiểm kho</button>
          <form v-else class="grid gap-4" @submit.prevent="submitStocktake">
            <p class="rounded-lg bg-stone-100 px-3 py-2">Số tồn lúc bắt đầu: <strong>{{ stocktakeContext.expectedQuantity }} {{ product.unit }}</strong></p>
            <div class="field"><label for="counted-quantity">Số lượng đếm được</label><input id="counted-quantity" v-model.number="countedQuantity" class="input" type="number" step="0.001" required :disabled="stocktakeAttempt !== null" /></div>
            <p v-if="countedQuantity !== null" class="text-sm">Chênh lệch: <strong>{{ stocktakeDifference > 0 ? '+' : '' }}{{ stocktakeDifference }}</strong></p>
            <div v-if="stocktakeDifference > 0 && !stocktakeContext.hasAverageCost" class="field"><label for="stocktake-cost">Giá vốn đơn vị cho lượng tăng</label><input id="stocktake-cost" v-model.number="stocktakeCost" class="input" type="number" min="0" step="0.0001" required :disabled="stocktakeAttempt !== null" /></div>
            <div class="field"><label for="stocktake-note">Ghi chú</label><input id="stocktake-note" v-model.trim="stocktakeNote" class="input" maxlength="500" :disabled="stocktakeAttempt !== null" /></div>
            <div class="flex gap-2"><button class="btn-primary" type="submit" :disabled="stocktakeSubmitting">Ghi kiểm kho</button><button class="btn-secondary" type="button" :disabled="stocktakeAttempt !== null" @click="() => loadStocktakeContext()">Tải lại số tồn</button></div>
          </form>
          <p v-if="stocktakeMessage" class="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800">{{ stocktakeMessage }}</p>
        </div>
      </div>

      <div class="card mt-6"><h2 class="text-xl font-black">Nguồn tồn kho</h2><p v-if="movements.length === 0" class="mt-4 text-slate-500">Sản phẩm chưa có biến động tồn.</p><div v-for="movement in movements" :key="movement.id" class="mt-4 flex flex-wrap justify-between gap-3 border-t pt-4"><div><p class="font-semibold">{{ movementLabel(movement.type) }}</p><p v-if="movement.reason" class="text-sm text-slate-700">{{ movement.reason }}</p><p v-if="movement.stocktakeExpectedQuantity !== null" class="text-sm text-slate-500">Dự kiến {{ movement.stocktakeExpectedQuantity }} · Đếm {{ movement.stocktakeCountedQuantity }}</p><p class="text-sm text-slate-500">{{ new Date(movement.occurredAt).toLocaleString('vi-VN') }} · Người thực hiện {{ movement.performedByUserId }}</p></div><div class="text-right"><p class="font-semibold">{{ movement.quantityDelta > 0 ? '+' : '' }}{{ movement.quantityDelta }} {{ product.unit }}</p><p class="text-sm text-slate-500">{{ money(movement.inventoryValueDelta) }} ₫</p><p v-if="movement.costReliability" class="text-xs text-slate-500">{{ reliabilityLabels[movement.costReliability] }} · {{ money(movement.unitCost) }} ₫/đv</p></div></div></div>
    </template>
  </section>
</template>
