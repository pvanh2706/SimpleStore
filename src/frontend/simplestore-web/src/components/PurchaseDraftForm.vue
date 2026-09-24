<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { ProductListItem, ProductPage, Purchase, PurchaseWriteInput, SupplierPage } from '../api/types'

interface DraftLine { productId: string; quantity: number | null; unitPrice: number | null }
interface SelectedSupplier { id: string; name: string }

const props = defineProps<{
  searchSuppliers: (search: string, page: number) => Promise<SupplierPage>
  searchProducts: (search: string, page: number) => Promise<ProductPage>
  initial?: Purchase | null
  preselectedProduct?: ProductListItem | null
  disabled?: boolean
  saving?: boolean
}>()
const emit = defineEmits<{ save: [value: PurchaseWriteInput] }>()

const supplierId = ref(props.initial?.supplierId ?? '')
const selectedSupplier = ref<SelectedSupplier | null>(props.initial
  ? { id: props.initial.supplierId, name: props.initial.supplierName }
  : null)
const lines = ref<DraftLine[]>(props.initial?.lines.map((line) => ({
  productId: line.productId, quantity: line.quantity, unitPrice: line.unitPrice,
})) ?? (props.preselectedProduct
  ? [{ productId: props.preselectedProduct.id, quantity: null, unitPrice: null }]
  : []))
const selectedProducts = ref<Record<string, ProductListItem>>(
  Object.fromEntries([
    ...(props.initial?.lines ?? []).map((line) => [line.productId, {
      id: line.productId, sku: '', barcode: null, name: line.productName,
      unit: line.productUnit, salePrice: 0, isActive: true, quantityOnHand: 0,
    }] as const),
    ...(props.preselectedProduct
      ? [[props.preselectedProduct.id, props.preselectedProduct] as const]
      : []),
  ]),
)
const supplierQuery = ref('')
const productQuery = ref('')
const supplierResults = ref<SupplierPage | null>(null)
const productResults = ref<ProductPage | null>(null)
const loadingSuppliers = ref(false)
const loadingProducts = ref(false)
const error = ref('')

const roundMoney = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100
const lineAmount = (line: DraftLine) => roundMoney((line.quantity ?? 0) * (line.unitPrice ?? 0))
const total = computed(() => lines.value.reduce((sum, line) => sum + lineAmount(line), 0))
const productById = (id: string) => selectedProducts.value[id]

async function loadSuppliers(page = 1) {
  loadingSuppliers.value = true
  error.value = ''
  try {
    supplierResults.value = await props.searchSuppliers(supplierQuery.value.trim(), page)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể tìm nhà cung cấp.'
  } finally { loadingSuppliers.value = false }
}

async function loadProducts(page = 1) {
  loadingProducts.value = true
  error.value = ''
  try {
    productResults.value = await props.searchProducts(productQuery.value.trim(), page)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể tìm sản phẩm.'
  } finally { loadingProducts.value = false }
}

function chooseSupplier(supplier: SelectedSupplier) {
  supplierId.value = supplier.id
  selectedSupplier.value = supplier
}

function addProduct(product: ProductListItem) {
  error.value = ''
  if (lines.value.some((line) => line.productId === product.id)) {
    error.value = 'Sản phẩm đã có trong phiếu nhập.'
    return
  }
  selectedProducts.value[product.id] = product
  lines.value.push({ productId: product.id, quantity: 1, unitPrice: 0 })
}

function submit() {
  error.value = ''
  if (!supplierId.value) { error.value = 'Hãy chọn nhà cung cấp.'; return }
  if (lines.value.some(line => line.quantity === null || line.quantity <= 0
    || line.unitPrice === null || line.unitPrice < 0)) {
    error.value = 'Hãy nhập số lượng và giá nhập hợp lệ cho từng sản phẩm.'
    return
  }
  emit('save', {
    supplierId: supplierId.value,
    lines: lines.value.map(line => ({
      productId: line.productId,
      quantity: line.quantity!,
      unitPrice: line.unitPrice!,
    })),
  })
}

onMounted(() => Promise.all([loadSuppliers(), loadProducts()]))
</script>

<template>
  <form class="grid gap-5" @submit.prevent="submit">
    <fieldset :disabled="disabled || saving" class="grid gap-5">
      <section class="card grid gap-3">
        <div>
          <span class="text-sm font-semibold">Nhà cung cấp</span>
          <p v-if="selectedSupplier" class="mt-1" data-testid="selected-supplier">Đã chọn: <strong>{{ selectedSupplier.name }}</strong></p>
          <p v-else class="mt-1 text-sm text-slate-500">Chưa chọn nhà cung cấp.</p>
        </div>
        <div class="flex gap-2">
          <input v-model="supplierQuery" class="input" aria-label="Tìm nhà cung cấp" placeholder="Tên hoặc điện thoại" @keyup.enter.prevent="loadSuppliers(1)" />
          <button class="btn-secondary" type="button" :disabled="loadingSuppliers" @click="loadSuppliers(1)">Tìm nhà cung cấp</button>
        </div>
        <p v-if="supplierResults && supplierResults.items.length === 0" class="text-sm text-slate-500">Không tìm thấy nhà cung cấp đang hoạt động.</p>
        <ul v-else class="grid gap-2">
          <li v-for="supplier in supplierResults?.items" :key="supplier.id" class="flex items-center justify-between gap-3 border-t pt-2">
            <span>{{ supplier.name }}<span v-if="supplier.phone" class="text-slate-500"> · {{ supplier.phone }}</span></span>
            <button class="text-emerald-800" type="button" :aria-label="`Chọn nhà cung cấp ${supplier.name}`" @click="chooseSupplier(supplier)">Chọn</button>
          </li>
        </ul>
        <div v-if="supplierResults && supplierResults.totalPages > 1" class="flex items-center justify-between text-sm">
          <button class="btn-secondary" type="button" :disabled="supplierResults.page <= 1" @click="loadSuppliers(supplierResults.page - 1)">Trước</button>
          <span>Trang {{ supplierResults.page }} / {{ supplierResults.totalPages }}</span>
          <button class="btn-secondary" type="button" :disabled="supplierResults.page >= supplierResults.totalPages" @click="loadSuppliers(supplierResults.page + 1)">Sau</button>
        </div>
      </section>

      <section class="card grid gap-3">
        <span class="text-sm font-semibold">Thêm sản phẩm</span>
        <div class="flex gap-2">
          <input v-model="productQuery" class="input" aria-label="Tìm sản phẩm" placeholder="Tên, SKU hoặc barcode" @keyup.enter.prevent="loadProducts(1)" />
          <button class="btn-secondary" type="button" :disabled="loadingProducts" @click="loadProducts(1)">Tìm sản phẩm</button>
        </div>
        <p v-if="productResults && productResults.items.length === 0" class="text-sm text-slate-500">Không tìm thấy sản phẩm đang hoạt động.</p>
        <ul v-else class="grid gap-2">
          <li v-for="product in productResults?.items" :key="product.id" class="flex items-center justify-between gap-3 border-t pt-2">
            <span>{{ product.name }} · {{ product.sku }}<span v-if="product.barcode" class="text-slate-500"> · {{ product.barcode }}</span></span>
            <button class="text-emerald-800" type="button" :aria-label="`Thêm sản phẩm ${product.name}`" @click="addProduct(product)">Thêm</button>
          </li>
        </ul>
        <div v-if="productResults && productResults.totalPages > 1" class="flex items-center justify-between text-sm">
          <button class="btn-secondary" type="button" :disabled="productResults.page <= 1" @click="loadProducts(productResults.page - 1)">Trước</button>
          <span>Trang {{ productResults.page }} / {{ productResults.totalPages }}</span>
          <button class="btn-secondary" type="button" :disabled="productResults.page >= productResults.totalPages" @click="loadProducts(productResults.page + 1)">Sau</button>
        </div>
      </section>

      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <div class="card overflow-x-auto p-0">
        <p v-if="lines.length === 0" class="p-6 text-slate-500">Chưa có sản phẩm.</p>
        <table v-else class="w-full min-w-[680px] text-left text-sm">
          <thead class="border-b bg-stone-50"><tr><th class="p-4">Sản phẩm</th><th>Số lượng</th><th>Giá nhập</th><th>Thành tiền</th><th></th></tr></thead>
          <tbody><tr v-for="(line, index) in lines" :key="line.productId" class="border-b last:border-0"><td class="p-4 font-semibold">{{ productById(line.productId)?.name }}</td><td><input v-model.number="line.quantity" class="input w-28" type="number" min="0.001" step="0.001" aria-label="Số lượng" /></td><td><input v-model.number="line.unitPrice" class="input w-36" type="number" min="0" step="0.01" aria-label="Giá nhập" /></td><td>{{ new Intl.NumberFormat('vi-VN').format(lineAmount(line)) }} ₫</td><td><button class="text-red-700" type="button" @click="lines.splice(index, 1)">Xóa</button></td></tr></tbody>
        </table>
      </div>
      <div class="flex items-center justify-between"><strong>Tổng dự kiến: {{ new Intl.NumberFormat('vi-VN').format(total) }} ₫</strong><button class="btn-primary" type="submit">{{ saving ? 'Đang lưu…' : 'Lưu nháp' }}</button></div>
    </fieldset>
  </form>
</template>
