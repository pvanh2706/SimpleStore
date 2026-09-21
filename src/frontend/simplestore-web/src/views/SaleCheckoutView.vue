<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import SaleCheckoutForm from '../components/SaleCheckoutForm.vue'
import SaleReceipt from '../components/SaleReceipt.vue'
import type { Customer, CustomerPage, OperationStatus, ProductPage, Sale, StoreOperationalSettings } from '../api/types'

type Attempt = {
  operationId: string
  customerId: string | null
  lines: Array<{ productId: string; quantity: number }>
  payments: Array<{ amount: number; method: 'Cash' | 'Transfer' }>
}
const settings = ref<StoreOperationalSettings | null>(null)
const completed = ref<Sale | null>(null)
const error = ref('')

async function searchProducts(search: string, page: number) {
  const params = new URLSearchParams({ search, isActive: 'true', page: String(page), pageSize: '20' })
  return apiRequest<ProductPage>(`/api/products?${params}`)
}
async function searchCustomers(search: string, page: number) {
  const params = new URLSearchParams({ search, page: String(page), pageSize: '20' })
  return apiRequest<CustomerPage>(`/api/customers?${params}`)
}
async function createCustomer(name: string, phone: string | null) {
  return apiRequest<Customer>('/api/customers', { method: 'POST', body: JSON.stringify({ name, phone }) })
}
async function completeSale(attempt: Attempt) {
  return apiRequest<Sale>('/api/sales/complete', { method: 'POST', body: JSON.stringify(attempt) })
}
async function checkOperation(operationId: string) {
  try { return await apiRequest<OperationStatus>(`/api/operations/${operationId}`) }
  catch (reason) { if (reason instanceof ApiError && reason.status === 404) return null; throw reason }
}
async function loadSale(saleId: string) { return apiRequest<Sale>(`/api/sales/${saleId}`) }
onMounted(async () => {
  try { settings.value = await apiRequest<StoreOperationalSettings>('/api/store/operational-settings') }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải thiết lập bán hàng.' }
})
</script>

<template>
  <section>
    <div class="flex flex-wrap items-end justify-between gap-3 no-print"><div><h1 class="text-3xl font-black">Bán hàng</h1><p class="mt-1 text-slate-500">Tạo đơn trực tiếp ở trạng thái hoàn tất.</p></div><RouterLink class="btn-secondary" to="/sales">Lịch sử bán hàng</RouterLink></div>
    <p v-if="error" class="error mt-5">{{ error }}</p>
    <SaleCheckoutForm v-if="settings && !completed" class="mt-6" :allow-negative-stock="settings.allowNegativeStock" :search-products="searchProducts" :search-customers="searchCustomers" :create-customer="createCustomer" :complete-sale="completeSale" :check-operation="checkOperation" :load-sale="loadSale" @completed="completed = $event" />
    <div v-if="completed" class="mt-6"><div class="no-print mb-5 rounded-xl bg-emerald-50 p-4 font-semibold text-emerald-900">Đơn bán đã hoàn tất. Việc in không thay đổi trạng thái giao dịch.</div><SaleReceipt :sale="completed" /><div class="no-print mt-5 flex justify-center"><RouterLink class="btn-secondary" to="/sales/new" @click="completed = null">Đơn bán mới</RouterLink></div></div>
  </section>
</template>
