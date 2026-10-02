<script setup lang="ts">
import { nextTick, onMounted, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import SaleCheckoutForm from '../components/SaleCheckoutForm.vue'
import SaleReceipt from '../components/SaleReceipt.vue'
import LineIcon from '../components/ui/LineIcon'
import type { Customer, CustomerPage, OperationStatus, ProductPage, Sale, StoreOperationalSettings } from '../api/types'
import { demoCheckout, salesDemoEnabled } from '../sales/demo'
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
const receipt = ref<InstanceType<typeof SaleReceipt> | null>(null)
const liveForm = ref<InstanceType<typeof SaleCheckoutForm> | null>(null)
const completedHeading = ref<HTMLElement | null>(null)
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
const methodLabel = (method: 'Cash' | 'Transfer') => method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản'
const paymentSummary = (sale: Sale) => sale.payments
  .map(payment => `${methodLabel(payment.method)} ${money(payment.amount)} ₫`)
  .join(' · ')

// The checkout (and its focused CTA) is replaced by the confirmed Sale: move focus to the success heading.
watch(completed, sale => {
  if (sale) void nextTick(() => completedHeading.value?.focus())
})

/** The next Sale starts in the Product search, ready for the next scan. */
async function startNewSale() {
  completed.value = null
  await nextTick()
  liveForm.value?.focusSearch()
}

/** Printing only opens the browser dialog; it never changes or repeats the completed Sale. */
function printReceipt() {
  receipt.value?.printReceipt()
}

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
    <p v-if="error && !salesDemoEnabled" class="error mt-5" role="alert">{{ error }}</p>
    <p v-else-if="!settings && !salesDemoEnabled" class="sales-page__loading no-print" role="status">Đang tải thiết lập bán hàng…</p>

    <SaleCheckoutForm
      v-if="settings && !completed"
      v-show="!salesDemoEnabled"
      ref="liveForm"
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
    <!-- Demo / Visual Reference never receives a live API callback: demoCheckout stays in the browser (D-107 G). -->
    <SaleCheckoutForm
      v-if="salesDemoEnabled && !completed"
      preview-only
      :allow-negative-stock="false"
      v-bind="demoCheckout"
    />

    <!-- The receipt stays a direct child of .sales-complete: the 80 mm print rules rely on that structure. -->
    <div v-if="completed" class="sales-complete">
      <section class="sales-complete__summary no-print" aria-labelledby="sales-complete-heading">
        <div class="sales-complete__heading">
          <span class="sales-complete__icon" aria-hidden="true"><LineIcon name="check" /></span>
          <div>
            <p class="sales-complete__state">Giao dịch thành công</p>
            <h1 id="sales-complete-heading" ref="completedHeading" tabindex="-1">Đơn bán đã hoàn tất</h1>
            <p class="sales-complete__lead">Đơn bán đã được ghi nhận. Việc in hóa đơn không thay đổi trạng thái giao dịch.</p>
            <p v-if="completed.wasAlreadyCompleted" class="sales-complete__recovered">Đơn này đã được ghi nhận từ lần gửi trước; không có đơn trùng.</p>
          </div>
        </div>

        <dl class="sales-complete__meta">
          <div><dt>Mã đơn</dt><dd class="sales-complete__id">{{ completed.id }}</dd></div>
          <div><dt>Thời gian</dt><dd>{{ new Date(completed.completedAt).toLocaleString('vi-VN') }}</dd></div>
        </dl>

        <dl class="sales-complete__amounts">
          <div class="sales-complete__total"><dt>Tổng đơn</dt><dd>{{ money(completed.totalAmount) }} ₫</dd></div>
          <div class="sales-complete__row">
            <dt>Đã thu<span v-if="completed.payments.length" class="sales-complete__sub">{{ paymentSummary(completed) }}</span></dt>
            <dd>{{ money(completed.paidAmount) }} ₫</dd>
          </div>
          <div class="sales-complete__row" :class="{ 'is-debt': completed.outstandingAmount > 0 }">
            <dt>Còn nợ<span v-if="completed.outstandingAmount > 0 && completed.customer" class="sales-complete__sub">Ghi công nợ cho {{ completed.customer.name }}</span></dt>
            <dd>{{ money(completed.outstandingAmount) }} ₫</dd>
          </div>
          <div v-if="completed.customer" class="sales-complete__row sales-complete__customer">
            <dt>Khách hàng</dt>
            <dd>{{ completed.customer.name }}<span v-if="completed.customer.phone" class="sales-complete__phone"> · {{ completed.customer.phone }}</span></dd>
          </div>
        </dl>

        <div class="sales-complete__print">
          <div class="sales-complete__print-text">
            <p class="sales-complete__print-title"><LineIcon name="receipt" />Hóa đơn 80 mm</p>
            <p>Có thể in lại bất cứ lúc nào. Hủy hoặc lỗi khi in không ảnh hưởng đơn đã ghi nhận.</p>
          </div>
          <button class="sales-complete__print-btn" type="button" @click="printReceipt"><LineIcon name="print" />In hóa đơn</button>
        </div>
        <p v-if="receipt?.printError" class="sales-complete__print-error" role="alert">{{ receipt.printError }}</p>

        <div class="sales-complete__next">
          <button class="sales-complete__new" type="button" @click="startNewSale"><LineIcon name="plus" />Đơn bán mới</button>
          <RouterLink class="sales-complete__history" to="/sales"><LineIcon name="clock" />Lịch sử bán hàng</RouterLink>
        </div>
      </section>
      <SaleReceipt ref="receipt" class="sales-complete__receipt" :sale="completed" hide-print-action />
    </div>
  </section>
</template>

<style scoped>
.sales-page { min-width: 0; }
:global(.app-main:has(> .sales-page)) { padding: 0.75rem 1.125rem 0.875rem; }
.sales-page__loading { margin-top: 1.5rem; color: var(--text-muted); }

/* Summary (success, facts, print, next actions) beside an 80 mm-like receipt preview on a tray. */
.sales-complete {
  display: grid;
  max-width: 1180px;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  align-items: start;
  gap: 0.875rem;
  margin: 0 auto;
  padding-top: 0.25rem;
}
.sales-complete::before {
  grid-row: 1;
  grid-column: 2;
  align-self: stretch;
  border: 1px solid var(--border);
  border-radius: 16px;
  background: #eef2f1;
  content: '';
}
.sales-complete__summary {
  grid-row: 1;
  grid-column: 1;
  display: grid;
  gap: 1.1rem;
  border: 1px solid var(--border);
  border-radius: 16px;
  background: var(--surface);
  box-shadow: 0 7px 22px rgb(16 39 68 / 5%);
  padding: clamp(1.15rem, 2vw, 1.5rem);
}
.sales-complete__heading { display: flex; align-items: start; gap: 0.9rem; }
/* Focus lands here programmatically after completion; it is not an interactive control. */
.sales-complete__heading h1:focus { outline: none; }
.sales-complete__heading h1 { margin-top: 0.15rem; font-size: 1.45rem; font-weight: 820; letter-spacing: -0.025em; line-height: 1.2; }
.sales-complete__state { color: var(--positive); font-size: 0.78rem; font-weight: 760; }
.sales-complete__lead { margin-top: 0.35rem; color: var(--text-muted); font-size: 0.86rem; line-height: 1.45; }
.sales-complete__recovered { margin-top: 0.5rem; border-radius: 8px; background: var(--surface-muted); padding: 0.4rem 0.6rem; color: var(--text); font-size: 0.8rem; font-weight: 650; }
.sales-complete__icon {
  display: grid;
  flex: none;
  width: 2.75rem;
  height: 2.75rem;
  place-items: center;
  border-radius: 50%;
  background: #dcf5e8;
  color: var(--positive);
}
.sales-complete__icon :deep(.line-icon) { width: 1.35rem; height: 1.35rem; stroke-width: 2.4; }
.sales-complete__meta {
  display: grid;
  grid-template-columns: minmax(0, 1.3fr) minmax(0, 1fr);
  gap: 0.75rem 1rem;
  border-top: 1px solid var(--border);
  padding-top: 1rem;
}
.sales-complete__meta dt,
.sales-complete__row dt { color: var(--text-muted); font-size: 0.78rem; font-weight: 650; }
.sales-complete__meta dd { margin-top: 0.2rem; font-size: 0.9rem; font-weight: 700; }
.sales-complete__meta .sales-complete__id { font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-size: 0.82rem; overflow-wrap: anywhere; }
.sales-complete__amounts { display: grid; gap: 0.55rem; border-top: 1px solid var(--border); padding-top: 1rem; }
.sales-complete__total { display: flex; flex-wrap: wrap; align-items: baseline; justify-content: space-between; gap: 0.25rem 1rem; padding-bottom: 0.2rem; }
.sales-complete__total dt { font-size: 1rem; font-weight: 800; }
.sales-complete__total dd { color: #078b5d; font-size: 1.9rem; font-weight: 900; letter-spacing: -0.03em; white-space: nowrap; font-variant-numeric: tabular-nums; }
.sales-complete__row { display: flex; align-items: baseline; justify-content: space-between; gap: 1rem; }
.sales-complete__row dt { min-width: 0; font-size: 0.86rem; }
.sales-complete__row dd { flex: none; font-size: 0.98rem; font-weight: 780; white-space: nowrap; font-variant-numeric: tabular-nums; }
.sales-complete__sub { display: block; margin-top: 0.1rem; color: var(--text-muted); font-size: 0.76rem; font-weight: 500; overflow-wrap: anywhere; }
.sales-complete__row.is-debt { margin: 0 -0.6rem; border-radius: 10px; background: #fff8ea; padding: 0.45rem 0.6rem; }
.sales-complete__row.is-debt dt,
.sales-complete__row.is-debt dd { color: #7d5300; }
.sales-complete__customer dd { min-width: 0; flex: 0 1 auto; overflow: hidden; text-align: right; text-overflow: ellipsis; }
.sales-complete__phone { color: var(--text-muted); font-weight: 600; }
.sales-complete__print {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.9rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: #f8fbfa;
  padding: 0.75rem 0.85rem;
}
.sales-complete__print-text { min-width: 0; color: var(--text-muted); font-size: 0.78rem; line-height: 1.4; }
.sales-complete__print-title { display: flex; align-items: center; gap: 0.4rem; margin-bottom: 0.15rem; color: var(--text); font-size: 0.88rem; font-weight: 760; }
.sales-complete__print-title :deep(.line-icon) { width: 1.05rem; height: 1.05rem; color: #60718a; }
.sales-complete__print-btn,
.sales-complete__new,
.sales-complete__history {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  border-radius: 10px;
  font-weight: 800;
  text-decoration: none;
  white-space: nowrap;
  transition: background-color 0.14s ease, border-color 0.14s ease, filter 0.14s ease;
}
.sales-complete__print-btn :deep(.line-icon),
.sales-complete__new :deep(.line-icon),
.sales-complete__history :deep(.line-icon) { width: 1.1rem; height: 1.1rem; flex: none; }
.sales-complete__print-btn {
  flex: none;
  min-height: 2.75rem;
  border: 1.5px solid rgb(10 163 107 / 40%);
  background: #fff;
  padding: 0 1rem;
  color: #0b7a52;
}
.sales-complete__print-btn:hover { border-color: rgb(10 163 107 / 70%); background: #f1faf5; }
.sales-complete__print-error { margin-top: -0.5rem; border: 1px solid #fbd0d0; border-radius: 10px; background: #fff0f0; padding: 0.5rem 0.75rem; color: var(--danger); font-size: 0.82rem; font-weight: 650; }
.sales-complete__next { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 0.75rem; }
.sales-complete__new {
  min-height: 3.25rem;
  border: 0;
  background: linear-gradient(180deg, #0baa6e, #068e5c);
  box-shadow: 0 12px 22px rgb(10 163 107 / 22%);
  padding: 0 1rem;
  color: #fff;
  font-size: 1rem;
}
.sales-complete__new:hover { filter: brightness(0.98); }
.sales-complete__history {
  min-height: 3.25rem;
  border: 1px solid var(--border-strong);
  background: var(--surface);
  padding: 0 1rem;
  color: #2f4158;
  font-size: 0.9rem;
}
.sales-complete__history:hover { background: var(--surface-muted); }
@media screen {
  /* The preview approximates the 76 mm printable width of the 80 mm roll. */
  .sales-complete :deep(.sales-complete__receipt) {
    position: relative;
    z-index: 1;
    grid-row: 1;
    grid-column: 2;
    justify-self: center;
    width: min(calc(100% - 2rem), 328px);
    max-width: none;
    margin: 1.25rem 0;
    border-color: #e3e9ef;
    border-radius: 6px;
    box-shadow: 0 10px 26px rgb(16 39 68 / 9%);
  }
}
@media (max-width: 900px) {
  :global(.app-main:has(> .sales-page)) { padding: 0.75rem; }
  .sales-complete { grid-template-columns: minmax(0, 1fr); }
  .sales-complete::before { grid-row: 2; grid-column: 1; }
}
@media screen and (max-width: 900px) {
  .sales-complete :deep(.sales-complete__receipt) { grid-row: 2; grid-column: 1; }
}
@media (max-width: 560px) {
  .sales-complete__meta { grid-template-columns: minmax(0, 1fr); }
  .sales-complete__print { flex-wrap: wrap; }
  .sales-complete__print-btn { width: 100%; }
  .sales-complete__next { grid-template-columns: minmax(0, 1fr); }
}
@media (prefers-reduced-motion: reduce) {
  .sales-complete__print-btn,
  .sales-complete__new,
  .sales-complete__history { transition: none; }
}
@media print {
  .sales-complete { display: block; }
  .sales-complete::before { display: none; }
}
</style>
