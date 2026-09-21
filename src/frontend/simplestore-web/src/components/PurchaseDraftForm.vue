<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ProductListItem, Purchase, PurchaseWriteInput, Supplier } from '../api/types'

interface DraftLine { productId: string; quantity: number; unitPrice: number }

const props = defineProps<{
  suppliers: Supplier[]
  products: ProductListItem[]
  initial?: Purchase | null
  disabled?: boolean
  saving?: boolean
}>()
const emit = defineEmits<{ save: [value: PurchaseWriteInput] }>()

const supplierId = ref(props.initial?.supplierId ?? props.suppliers[0]?.id ?? '')
const lines = ref<DraftLine[]>(props.initial?.lines.map((line) => ({
  productId: line.productId,
  quantity: line.quantity,
  unitPrice: line.unitPrice,
})) ?? [])
const selectedProductId = ref('')
const error = ref('')

const roundMoney = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100
const total = computed(() => lines.value.reduce(
  (sum, line) => sum + roundMoney(line.quantity * line.unitPrice), 0))
const productById = (id: string) => props.products.find((product) => product.id === id)

function addProduct() {
  error.value = ''
  if (!selectedProductId.value) return
  if (lines.value.some((line) => line.productId === selectedProductId.value)) {
    error.value = 'Sản phẩm đã có trong phiếu nhập.'
    return
  }
  lines.value.push({
    productId: selectedProductId.value,
    quantity: 1,
    unitPrice: 0,
  })
  selectedProductId.value = ''
}

function submit() {
  error.value = ''
  if (!supplierId.value) { error.value = 'Hãy chọn nhà cung cấp.'; return }
  emit('save', { supplierId: supplierId.value, lines: lines.value.map((line) => ({ ...line })) })
}
</script>

<template>
  <form class="grid gap-5" @submit.prevent="submit">
    <fieldset :disabled="disabled || saving" class="grid gap-5">
      <label class="field"><span>Nhà cung cấp</span><select v-model="supplierId" class="input" aria-label="Nhà cung cấp"><option value="" disabled>Chọn nhà cung cấp</option><option v-for="supplier in suppliers" :key="supplier.id" :value="supplier.id">{{ supplier.name }}</option></select></label>
      <div class="card grid gap-3 md:grid-cols-[1fr_auto]"><select v-model="selectedProductId" class="input" aria-label="Sản phẩm"><option value="">Chọn sản phẩm để thêm</option><option v-for="product in products" :key="product.id" :value="product.id">{{ product.name }} · {{ product.sku }}</option></select><button class="btn-secondary" type="button" @click="addProduct">Thêm dòng</button></div>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <div class="card overflow-x-auto p-0"><p v-if="lines.length === 0" class="p-6 text-slate-500">Chưa có sản phẩm.</p><table v-else class="w-full min-w-[680px] text-left text-sm"><thead class="border-b bg-stone-50"><tr><th class="p-4">Sản phẩm</th><th>Số lượng</th><th>Giá nhập</th><th>Thành tiền</th><th></th></tr></thead><tbody><tr v-for="(line, index) in lines" :key="line.productId" class="border-b last:border-0"><td class="p-4 font-semibold">{{ productById(line.productId)?.name }}</td><td><input v-model.number="line.quantity" class="input w-28" type="number" min="0.001" step="0.001" aria-label="Số lượng" /></td><td><input v-model.number="line.unitPrice" class="input w-36" type="number" min="0" step="0.01" aria-label="Giá nhập" /></td><td>{{ new Intl.NumberFormat('vi-VN').format(roundMoney(line.quantity * line.unitPrice)) }} ₫</td><td><button class="text-red-700" type="button" @click="lines.splice(index, 1)">Xóa</button></td></tr></tbody></table></div>
      <div class="flex items-center justify-between"><strong>Tổng dự kiến: {{ new Intl.NumberFormat('vi-VN').format(total) }} ₫</strong><button class="btn-primary" type="submit">{{ saving ? 'Đang lưu…' : 'Lưu nháp' }}</button></div>
    </fieldset>
  </form>
</template>
