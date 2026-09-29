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
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)

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
  <section class="sales-page">
    <header class="sales-page__header no-print">
      <div>
        <p class="sales-page__eyebrow">QUẦY BÁN HÀNG</p>
        <h1>{{ completed ? 'Hoàn tất bán hàng' : 'Bán hàng' }}</h1>
        <p class="sales-page__description">{{ completed ? 'Giao dịch đã được lưu. Bạn có thể in hóa đơn hoặc bắt đầu đơn tiếp theo.' : 'Tìm sản phẩm, kiểm tra giỏ hàng và hoàn tất giao dịch tại quầy.' }}</p>
      </div>
      <RouterLink class="btn-secondary" to="/sales">Lịch sử bán hàng</RouterLink>
    </header>

    <p v-if="error" class="error mt-5" role="alert">{{ error }}</p>
    <p v-else-if="!settings" class="sales-page__loading no-print" role="status">Đang tải thiết lập bán hàng…</p>

    <SaleCheckoutForm
      v-if="settings && !completed"
      class="mt-5"
      :allow-negative-stock="settings.allowNegativeStock"
      :search-products="searchProducts"
      :search-customers="searchCustomers"
      :create-customer="createCustomer"
      :complete-sale="completeSale"
      :check-operation="checkOperation"
      :load-sale="loadSale"
      @completed="completed = $event"
    />

    <div v-if="completed" class="sales-complete mt-5">
      <section class="sales-complete__summary no-print" aria-live="polite">
        <div class="sales-complete__heading">
          <span class="sales-complete__icon" aria-hidden="true">✓</span>
          <div>
            <p class="sales-page__eyebrow">GIAO DỊCH THÀNH CÔNG</p>
            <h2>Đơn bán đã hoàn tất</h2>
            <p>Việc in hóa đơn không thay đổi trạng thái giao dịch.</p>
          </div>
        </div>
        <dl class="sales-complete__facts">
          <div><dt>Mã đơn</dt><dd class="sales-complete__id">{{ completed.id }}</dd></div>
          <div><dt>Thời gian</dt><dd>{{ new Date(completed.completedAt).toLocaleString('vi-VN') }}</dd></div>
          <div><dt>Tổng cộng</dt><dd>{{ money(completed.totalAmount) }} ₫</dd></div>
          <div><dt>Đã thu</dt><dd>{{ money(completed.paidAmount) }} ₫</dd></div>
          <div><dt>Còn nợ</dt><dd>{{ money(completed.outstandingAmount) }} ₫</dd></div>
        </dl>
        <div class="sales-complete__next">
          <button class="btn-secondary" type="button" @click="completed = null">Đơn bán mới</button>
        </div>
      </section>
      <SaleReceipt class="mt-5" :sale="completed" />
    </div>
  </section>
</template>

<style scoped>
.sales-page { min-width: 0; }
.sales-page__header {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
}
.sales-page__header h1 {
  color: var(--text);
  font-size: clamp(1.75rem, 2.5vw, 2.25rem);
  font-weight: 800;
  letter-spacing: -0.035em;
  line-height: 1.15;
}
.sales-page__eyebrow {
  color: var(--brand-primary-hover);
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.08em;
}
.sales-page__description { margin-top: 0.4rem; color: var(--text-muted); font-size: 0.9rem; }
.sales-page__loading { margin-top: 1.5rem; color: var(--text-muted); }
.sales-complete__summary {
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  box-shadow: 0 1px 3px rgb(14 43 33 / 5%);
  padding: clamp(1rem, 2vw, 1.5rem);
}
.sales-complete__heading { display: flex; align-items: start; gap: 0.875rem; }
.sales-complete__heading h2 { font-size: 1.35rem; font-weight: 800; }
.sales-complete__heading p:last-child { margin-top: 0.25rem; color: var(--text-muted); font-size: 0.9rem; }
.sales-complete__icon {
  display: grid;
  flex: none;
  width: 2.5rem;
  height: 2.5rem;
  place-items: center;
  border-radius: 50%;
  background: var(--surface-muted);
  color: var(--positive);
  font-size: 1.5rem;
  font-weight: 800;
}
.sales-complete__facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(9rem, 1fr));
  gap: 0.875rem;
  margin-top: 1.25rem;
  border-top: 1px solid var(--border);
  padding-top: 1.25rem;
}
.sales-complete__facts dt { color: var(--text-muted); font-size: 0.75rem; font-weight: 650; }
.sales-complete__facts dd { margin-top: 0.2rem; font-size: 1rem; font-weight: 750; font-variant-numeric: tabular-nums; }
.sales-complete__id { overflow-wrap: anywhere; }
.sales-complete__next { display: flex; justify-content: flex-end; margin-top: 1.25rem; }
@media (max-width: 560px) {
  .sales-page__header .btn-secondary { width: 100%; }
  .sales-complete__facts { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
</style>
