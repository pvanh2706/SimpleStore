<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import SaleCheckoutForm from '../components/SaleCheckoutForm.vue'
import SaleReceipt from '../components/SaleReceipt.vue'
import type { Customer, CustomerPage, OperationStatus, ProductPage, Sale, StoreOperationalSettings } from '../api/types'
import { salesDemoEnabled, searchDemoProducts, searchDemoCustomers } from '../sales/demo'
import { liveOrderBook } from '../sales/orders'

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
    <header v-if="completed" class="sales-page__header no-print">
      <div>
        <h1>{{ completed ? 'Hoàn tất bán hàng' : 'Bán hàng' }}</h1>
        <p class="sales-page__description">{{ completed ? 'Đơn đã được lưu. Bạn có thể in hóa đơn hoặc bắt đầu đơn tiếp theo.' : 'Tìm sản phẩm và hoàn tất đơn ngay tại quầy.' }}</p>
      </div>
      <RouterLink class="btn-secondary" to="/sales">Lịch sử bán hàng</RouterLink>
    </header>

    <p v-if="error && !salesDemoEnabled" class="error mt-5" role="alert">{{ error }}</p>
    <p v-else-if="!settings && !salesDemoEnabled" class="sales-page__loading no-print" role="status">Đang tải thiết lập bán hàng…</p>

    <SaleCheckoutForm
      v-if="settings && !completed"
      v-show="!salesDemoEnabled"
      :allow-negative-stock="settings.allowNegativeStock"
      :order-book="liveOrderBook"
      :search-products="searchProducts"
      :search-customers="searchCustomers"
      :create-customer="createCustomer"
      :complete-sale="completeSale"
      :check-operation="checkOperation"
      :load-sale="loadSale"
      @completed="completed = $event"
    />
    <SaleCheckoutForm
      v-if="salesDemoEnabled && !completed"
      preview-only
      :allow-negative-stock="false"
      :search-products="searchDemoProducts"
      :search-customers="searchDemoCustomers"
      :create-customer="async (name, phone) => ({ id: `demo-${name}`, name, phone, createdAt: '', updatedAt: '' })"
      :complete-sale="completeSale"
      :check-operation="checkOperation"
      :load-sale="loadSale"
    />

    <div v-if="completed" class="sales-complete">
      <section class="sales-complete__summary no-print" aria-live="polite">
        <div class="sales-complete__heading">
          <span class="sales-complete__icon" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 4 4L19 6"/></svg></span>
          <div>
            <p class="sales-complete__state">Giao dịch thành công</p>
            <h2>Đơn bán đã hoàn tất</h2>
            <p>Đơn đã được ghi nhận. Việc in hóa đơn không thay đổi trạng thái giao dịch.</p>
          </div>
        </div>
        <dl class="sales-complete__facts">
          <div class="sales-complete__detail"><dt>Mã đơn</dt><dd class="sales-complete__id">{{ completed.id }}</dd></div>
          <div class="sales-complete__detail"><dt>Thời gian</dt><dd>{{ new Date(completed.completedAt).toLocaleString('vi-VN') }}</dd></div>
          <div class="sales-complete__total"><dt>Tổng đơn</dt><dd>{{ money(completed.totalAmount) }} ₫</dd></div>
          <div><dt>Đã thu</dt><dd>{{ money(completed.paidAmount) }} ₫</dd></div>
          <div><dt>Còn nợ</dt><dd>{{ money(completed.outstandingAmount) }} ₫</dd></div>
        </dl>
        <div class="sales-complete__next">
          <button class="btn-secondary" type="button" @click="completed = null">Đơn bán mới</button>
        </div>
      </section>
      <SaleReceipt class="sales-complete__receipt" :sale="completed" />
    </div>
  </section>
</template>

<style scoped>
.sales-page { min-width: 0; }
:global(.app-main:has(> .sales-page)) { padding: 0.75rem 1.125rem 0.875rem; }
.sales-page__header {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
  min-height: 3.25rem;
  padding-bottom: 0.65rem;
}
.sales-page__header h1 {
  color: var(--text);
  font-size: clamp(1.35rem, 1.8vw, 1.65rem);
  font-weight: 810;
  letter-spacing: -0.04em;
  line-height: 1.15;
}
.sales-page__description { margin-top: 0.2rem; color: var(--text-muted); font-size: 0.8rem; }
.sales-page__header .btn-secondary { border-color: var(--border); font-size: 0.82rem; }
.sales-page__loading { margin-top: 1.5rem; color: var(--text-muted); }
.sales-complete { display: grid; grid-template-columns: minmax(0, 0.82fr) minmax(0, 1fr); align-items: start; gap: 0.875rem; padding-top: 0.25rem; }
.sales-complete__summary {
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  box-shadow: 0 5px 22px rgb(14 43 33 / 6%);
  padding: clamp(1.15rem, 2vw, 1.6rem);
}
.sales-complete__heading { display: flex; align-items: start; gap: 0.9rem; }
.sales-complete__heading h2 { margin-top: 0.2rem; font-size: 1.35rem; font-weight: 790; letter-spacing: -0.025em; line-height: 1.25; }
.sales-complete__heading p:last-child { margin-top: 0.4rem; color: var(--text-muted); font-size: 0.84rem; line-height: 1.45; }
.sales-complete__state { color: var(--positive); font-size: 0.78rem; font-weight: 760; }
.sales-complete__icon {
  display: grid;
  flex: none;
  width: 2.65rem;
  height: 2.65rem;
  place-items: center;
  border-radius: 50%;
  background: var(--surface-muted);
  color: var(--positive);
}
.sales-complete__icon svg { width: 1.3rem; height: 1.3rem; }
.sales-complete__facts {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.9rem 1rem;
  margin-top: 1.5rem;
  border-top: 1px solid var(--border);
  padding-top: 1.1rem;
}
.sales-complete__facts dt { color: var(--text-muted); font-size: 0.73rem; font-weight: 650; }
.sales-complete__facts dd { margin-top: 0.2rem; font-size: 0.94rem; font-weight: 750; font-variant-numeric: tabular-nums; }
.sales-complete__detail { min-width: 0; }
.sales-complete__total {
  grid-column: 1 / -1;
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 1rem;
  border-top: 1px solid var(--border);
  padding-top: 0.95rem;
}
.sales-complete__total dt { color: var(--text); font-size: 0.88rem; font-weight: 750; }
.sales-complete__total dd { color: var(--brand-primary-hover); font-size: 1.55rem; font-weight: 820; letter-spacing: -0.04em; white-space: nowrap; }
.sales-complete__id { overflow-wrap: anywhere; }
.sales-complete__next { margin-top: 1.4rem; }
.sales-complete__next .btn-secondary { width: 100%; border-color: var(--border-strong); }
@media screen {
  .sales-complete :deep(.sales-complete__receipt) { width: 100%; max-width: none; margin: 0; box-shadow: 0 5px 22px rgb(14 43 33 / 6%); }
}
@media (max-width: 900px) {
  :global(.app-main:has(> .sales-page)) { padding: 0.75rem; }
  .sales-complete { grid-template-columns: minmax(0, 1fr); }
}
@media (max-width: 560px) {
  .sales-page__header .btn-secondary { width: 100%; }
  .sales-complete__facts { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media print {
  .sales-complete { display: block; }
}
</style>
