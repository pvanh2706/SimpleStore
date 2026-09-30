<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '../api/client'
import type { Customer, CustomerPage, OperationStatus, ProductListItem, ProductPage, Sale } from '../api/types'

type PaymentInput = { amount: number; method: 'Cash' | 'Transfer' }
type CartLine = { product: ProductListItem; quantity: number }
type State = 'idle' | 'completing' | 'checking' | 'retryable' | 'completed'
interface AttemptSnapshot {
  operationId: string
  customerId: string | null
  lines: Array<{ productId: string; quantity: number }>
  payments: PaymentInput[]
}

const props = defineProps<{
  allowNegativeStock: boolean
  searchProducts: (search: string, page: number) => Promise<ProductPage>
  searchCustomers: (search: string, page: number) => Promise<CustomerPage>
  createCustomer: (name: string, phone: string | null) => Promise<Customer>
  completeSale: (attempt: AttemptSnapshot) => Promise<Sale>
  checkOperation: (operationId: string) => Promise<OperationStatus | null>
  loadSale: (saleId: string) => Promise<Sale>
}>()
const emit = defineEmits<{ completed: [sale: Sale] }>()

const productSearch = ref('')
const productQuery = ref('')
const products = ref<ProductPage | null>(null)
const productPage = ref(1)
const productLoading = ref(false)
const productError = ref('')
let productRequestId = 0

const cart = ref<CartLine[]>([])
const amount = ref(0)
const method = ref<'Cash' | 'Transfer'>('Cash')
const payments = ref<PaymentInput[]>([])

const customerSearch = ref('')
const customerQuery = ref('')
const customers = ref<CustomerPage | null>(null)
const customerPage = ref(1)
const customerLoading = ref(false)
const customerError = ref('')
const creatingCustomer = ref(false)
let customerRequestId = 0
const customer = ref<Customer | null>(null)
const newCustomerName = ref('')
const newCustomerPhone = ref('')

const state = ref<State>('idle')
const attempt = ref<AttemptSnapshot | null>(null)
const message = ref('')

function lineAmount(line: CartLine) {
  if (!Number.isFinite(line.quantity) || line.quantity <= 0) return 0
  return Math.round(line.quantity * line.product.salePrice * 100) / 100
}
const total = computed(() => cart.value.reduce((sum, line) => sum + lineAmount(line), 0))
const paid = computed(() => payments.value.reduce((sum, payment) => sum + payment.amount, 0))
const outstanding = computed(() => Math.max(0, total.value - paid.value))
const locked = computed(() => ['completing', 'checking', 'retryable', 'completed'].includes(state.value))
const money = (value: number) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(value)

async function findProducts(page = 1) {
  if (locked.value) return
  const query = page === 1 ? productSearch.value.trim() : productQuery.value
  if (page === 1) productQuery.value = query
  const requestId = ++productRequestId
  productLoading.value = true
  productError.value = ''
  products.value = null
  try {
    const result = await props.searchProducts(query, page)
    if (requestId !== productRequestId) return
    products.value = result
    productPage.value = page
  } catch (reason) {
    if (requestId !== productRequestId) return
    productError.value = reason instanceof Error
      ? 'Không thể tìm sản phẩm. ' + reason.message
      : 'Không thể tìm sản phẩm. Vui lòng thử lại.'
  } finally {
    if (requestId === productRequestId) productLoading.value = false
  }
}

function addProduct(product: ProductListItem) {
  if (locked.value) return
  message.value = ''
  if (cart.value.some(line => line.product.id === product.id)) {
    message.value = 'Sản phẩm đã có trong giỏ hàng.'
    return
  }
  cart.value.push({ product, quantity: 1 })
}

function removeProduct(productId: string) {
  if (locked.value) return
  cart.value = cart.value.filter(line => line.product.id !== productId)
}

function changeQuantity(line: CartLine, delta: number) {
  if (locked.value) return
  const current = Number.isFinite(line.quantity) ? line.quantity : 0
  line.quantity = Math.max(0.001, Math.round((current + delta) * 1000) / 1000)
}

function addPayment() {
  if (locked.value) return
  message.value = ''
  if (!Number.isFinite(amount.value) || amount.value <= 0) {
    message.value = 'Số tiền phải lớn hơn 0.'
    return
  }
  if (paid.value + amount.value > total.value) {
    message.value = 'Tổng thanh toán không được vượt tổng đơn.'
    return
  }
  payments.value.push({ amount: amount.value, method: method.value })
  amount.value = 0
}

function removePayment(index: number) {
  if (locked.value) return
  payments.value.splice(index, 1)
}

async function findCustomers(page = 1) {
  if (locked.value) return
  const query = page === 1 ? customerSearch.value.trim() : customerQuery.value
  if (page === 1) customerQuery.value = query
  const requestId = ++customerRequestId
  customerLoading.value = true
  customerError.value = ''
  customers.value = null
  try {
    const result = await props.searchCustomers(query, page)
    if (requestId !== customerRequestId) return
    customers.value = result
    customerPage.value = page
  } catch (reason) {
    if (requestId !== customerRequestId) return
    customerError.value = reason instanceof Error
      ? 'Không thể tìm khách hàng. ' + reason.message
      : 'Không thể tìm khách hàng. Vui lòng thử lại.'
  } finally {
    if (requestId === customerRequestId) customerLoading.value = false
  }
}

function selectCustomer(item: Customer) {
  if (locked.value || creatingCustomer.value) return
  customer.value = item
}

async function createAndSelectCustomer() {
  if (locked.value || creatingCustomer.value || !newCustomerName.value.trim()) return
  creatingCustomer.value = true
  customerError.value = ''
  try {
    const created = await props.createCustomer(
      newCustomerName.value.trim(),
      newCustomerPhone.value.trim() || null,
    )
    if (locked.value) return
    customer.value = created
    newCustomerName.value = ''
    newCustomerPhone.value = ''
  } catch (reason) {
    customerError.value = reason instanceof Error
      ? 'Không thể tạo khách hàng. ' + reason.message
      : 'Không thể tạo khách hàng. Vui lòng thử lại.'
  } finally {
    creatingCustomer.value = false
  }
}

async function complete() {
  if (state.value === 'completing' || state.value === 'checking' || state.value === 'completed') return
  if (creatingCustomer.value) return
  message.value = ''

  // Retry always uses the saved attempt. Validate mutable inputs only for a new attempt.
  if (!attempt.value) {
    if (cart.value.length === 0) {
      message.value = 'Thêm ít nhất một sản phẩm.'
      return
    }
    if (cart.value.some(line => !Number.isFinite(line.quantity) || line.quantity <= 0)) {
      message.value = 'Số lượng bán phải lớn hơn 0.'
      return
    }
    if (paid.value > total.value) {
      message.value = 'Tổng thanh toán không được vượt tổng đơn.'
      return
    }
    if (outstanding.value > 0 && !customer.value) {
      message.value = 'Chọn khách hàng khi đơn còn công nợ.'
      return
    }
    attempt.value = {
      operationId: crypto.randomUUID(),
      customerId: customer.value?.id ?? null,
      lines: cart.value.map(line => ({ productId: line.product.id, quantity: line.quantity })),
      payments: payments.value.map(payment => ({ ...payment })),
    }
  }

  const current = attempt.value
  state.value = 'completing'
  try {
    const sale = await props.completeSale({
      ...current,
      lines: current.lines.map(line => ({ ...line })),
      payments: current.payments.map(payment => ({ ...payment })),
    })
    state.value = 'completed'
    emit('completed', sale)
  } catch (reason) {
    const ambiguous = !(reason instanceof ApiError)
      || reason.status === 408
      || reason.status >= 500
      || reason.problem.code === 'operation-lock-timeout'
    if (!ambiguous) {
      state.value = 'idle'
      attempt.value = null
      message.value = reason instanceof Error ? reason.message : 'Không thể hoàn tất đơn bán.'
      return
    }

    state.value = 'checking'
    try {
      const operation = await props.checkOperation(current.operationId)
      if (operation?.status === 'Completed'
        && operation.operationType === 'CompleteSale'
        && operation.resultReference) {
        const sale = await props.loadSale(operation.resultReference)
        state.value = 'completed'
        emit('completed', sale)
        return
      }
    } catch { /* Keep the exact attempt because the outcome remains ambiguous. */ }

    state.value = 'retryable'
    message.value = 'Chưa xác định được kết quả. Giỏ hàng, khách hàng và thanh toán đã được khóa để thử lại đúng thao tác.'
  }
}

onMounted(() => { void findProducts(1) })
defineExpose({ state, attempt, cart, payments, customer, total, paid, outstanding })
</script>

<template>
  <div class="sales-pos no-print">
    <section class="sales-pos__products" aria-labelledby="sales-products-heading">
      <div v-if="allowNegativeStock" class="sales-pos__warning" role="note">
        <strong>Lưu ý về tồn kho</strong>
        <span>Cửa hàng đang cho phép bán âm tồn. Giá vốn có thể ở trạng thái ước tính hoặc chưa xác định.</span>
      </div>

      <div class="sales-pos__section-heading">
        <h2 id="sales-products-heading">Chọn sản phẩm</h2>
        <p>Tìm theo tên, SKU hoặc quét mã vạch.</p>
      </div>

      <form class="sales-pos__search" role="search" @submit.prevent="findProducts(1)">
        <label for="sales-product-search">Tìm hoặc quét sản phẩm</label>
        <div class="sales-pos__search-controls">
          <div class="sales-pos__search-input">
            <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><circle cx="10.8" cy="10.8" r="6.8"/><path d="m16 16 4.5 4.5"/></svg>
            <input
              id="sales-product-search"
              v-model="productSearch"
              class="input"
              type="text"
              autocomplete="off"
              aria-label="Tìm hoặc quét sản phẩm"
              placeholder="Tên sản phẩm, SKU hoặc mã vạch..."
              :disabled="locked"
            />
          </div>
          <button class="btn-secondary sales-pos__search-button" type="submit" aria-label="Tìm sản phẩm" :disabled="locked">
            <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="10.8" cy="10.8" r="6.8"/><path d="m16 16 4.5 4.5"/></svg>
            <span>Tìm sản phẩm</span>
          </button>
        </div>
      </form>

      <div class="sales-pos__results-heading">
        <div>
          <h3>Sản phẩm</h3>
        </div>
        <p v-if="products" role="status">{{ products.totalCount }} kết quả · Trang {{ productPage }}/{{ products.totalPages || 1 }}</p>
      </div>

      <p v-if="productLoading" class="sales-pos__empty" role="status">Đang tìm sản phẩm…</p>
      <p v-else-if="productError" class="error" role="alert">{{ productError }}</p>
      <p v-else-if="products && products.items.length === 0" class="sales-pos__empty" role="status">Không tìm thấy sản phẩm phù hợp.</p>
      <p v-else-if="!products" class="sales-pos__empty" role="status">Nhập tên, SKU hoặc barcode để tìm sản phẩm.</p>
      <div v-else class="sales-pos__product-grid" aria-live="polite">
        <article v-for="product in products.items" :key="product.id" class="sales-pos__product-card">
          <div class="sales-pos__product-main">
            <h4>{{ product.name }}</h4>
            <p class="sales-pos__meta">{{ product.sku }} · {{ product.unit }}</p>
          </div>
          <div class="sales-pos__product-bottom">
            <strong class="sales-pos__product-price">{{ money(product.salePrice) }} ₫</strong>
            <span class="sales-pos__stock" :class="{ 'sales-pos__stock--empty': product.quantityOnHand <= 0 }">Tồn {{ money(product.quantityOnHand) }} {{ product.unit }}</span>
            <button
              class="sales-pos__add"
              type="button"
              :disabled="locked"
              :aria-label="'Thêm sản phẩm ' + product.name"
              @click="addProduct(product)"
            ><svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M12 5v14M5 12h14"/></svg><span>Thêm</span></button>
          </div>
        </article>
      </div>
      <nav v-if="products && products.totalPages > 1" class="sales-pos__pager" aria-label="Trang sản phẩm">
        <button class="btn-secondary" type="button" :disabled="productPage <= 1 || locked || productLoading" @click="findProducts(productPage - 1)">Trước</button>
        <span>Trang {{ productPage }}/{{ products.totalPages }}</span>
        <button class="btn-secondary" type="button" :disabled="productPage >= products.totalPages || locked || productLoading" @click="findProducts(productPage + 1)">Sau</button>
      </nav>
    </section>

    <section class="sales-pos__checkout" aria-labelledby="sales-checkout-heading">
      <div class="sales-pos__checkout-header">
        <div class="sales-pos__checkout-title">
          <span class="sales-pos__checkout-icon" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 4h2l2.1 10h11.7L21 7H6"/><circle cx="9" cy="19" r="1"/><circle cx="18" cy="19" r="1"/></svg></span>
          <div>
            <h2 id="sales-checkout-heading">Đơn bán</h2>
            <span class="sales-pos__status">Đang bán</span>
          </div>
        </div>
        <span class="sales-pos__count">{{ cart.length }} sản phẩm</span>
      </div>

      <div class="sales-pos__checkout-body">
        <section class="sales-pos__cart" aria-label="Sản phẩm trong giỏ">
          <p v-if="cart.length === 0" class="sales-pos__cart-empty">Chưa có sản phẩm. Tìm sản phẩm ở bên trái để bắt đầu đơn bán.</p>
          <div v-for="line in cart" :key="line.product.id" class="sales-pos__cart-line">
            <div class="sales-pos__line-heading">
              <div>
                <strong>{{ line.product.name }}</strong>
                <p>{{ money(line.product.salePrice) }} ₫ / {{ line.product.unit }}</p>
              </div>
              <button
                class="sales-pos__remove"
                type="button"
                :disabled="locked"
                :aria-label="'Xóa ' + line.product.name"
                @click="removeProduct(line.product.id)"
              ><svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 7h16M9 7V5h6v2m3 0-.7 12H6.7L6 7M10 11v5m4-5v5"/></svg><span class="sales-pos__visually-hidden">Xóa</span></button>
            </div>
            <div class="sales-pos__line-bottom">
              <div class="sales-pos__quantity">
                <button type="button" :disabled="locked" :aria-label="'Giảm số lượng ' + line.product.name" @click="changeQuantity(line, -1)">−</button>
                <input
                  :id="'sales-quantity-' + line.product.id"
                  v-model.number="line.quantity"
                  class="input"
                  type="number"
                  min="0.001"
                  step="0.001"
                  inputmode="decimal"
                  :aria-label="'Số lượng ' + line.product.name"
                  :disabled="locked"
                />
                <button type="button" :disabled="locked" :aria-label="'Tăng số lượng ' + line.product.name" @click="changeQuantity(line, 1)">+</button>
              </div>
              <strong class="sales-pos__line-total">{{ money(lineAmount(line)) }} ₫</strong>
            </div>
          </div>
        </section>

        <section class="sales-pos__panel" aria-labelledby="sales-payment-heading">
          <div class="sales-pos__panel-heading">
            <h3 id="sales-payment-heading">Thanh toán</h3>
            <p>Tiền mặt hoặc chuyển khoản · Có thể thêm nhiều lần.</p>
          </div>
          <div class="sales-pos__payment-fields">
            <div class="sales-pos__method" role="group" aria-label="Phương thức thanh toán">
              <button type="button" :class="{ 'is-active': method === 'Cash' }" :aria-pressed="method === 'Cash'" :disabled="locked" @click="method = 'Cash'">
                <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="6" width="18" height="12" rx="2"/><path d="M3 10h18"/></svg>
                Tiền mặt
              </button>
              <button type="button" :class="{ 'is-active': method === 'Transfer' }" :aria-pressed="method === 'Transfer'" :disabled="locked" @click="method = 'Transfer'">
                <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 20h16M5 10h14M12 3l8 5H4l8-5Z"/></svg>
                Chuyển khoản
              </button>
            </div>
            <div class="sales-pos__field">
              <label for="sales-payment-amount">Số tiền thanh toán</label>
              <input id="sales-payment-amount" v-model.number="amount" class="input" type="number" min="0.01" step="0.01" inputmode="decimal" aria-label="Số tiền thanh toán" :disabled="locked" />
            </div>
            <button class="btn-secondary" type="button" :disabled="locked" @click="addPayment">Thêm thanh toán</button>
          </div>
          <div v-if="payments.length" class="sales-pos__payments" aria-label="Các khoản đã nhập">
            <div v-for="(payment, index) in payments" :key="index" class="sales-pos__payment-row">
              <span>{{ payment.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }}</span>
              <strong>{{ money(payment.amount) }} ₫</strong>
              <button class="sales-pos__remove" type="button" :disabled="locked" :aria-label="'Xóa khoản thanh toán ' + (index + 1)" @click="removePayment(index)">Xóa</button>
            </div>
          </div>
        </section>
        <section v-if="outstanding > 0" class="sales-pos__panel" aria-labelledby="sales-customer-heading">
          <div class="sales-pos__panel-heading">
            <h3 id="sales-customer-heading">Khách hàng</h3>
            <p>Chọn khách hàng cho phần còn nợ.</p>
          </div>
          <div v-if="customer" class="sales-pos__selected-customer">
            <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 4 4L19 6"/></svg>
            <strong>{{ customer.name }}</strong>
            <span>{{ customer.phone || 'Không có số điện thoại' }}</span>
          </div>
          <form class="sales-pos__customer-search" @submit.prevent="findCustomers(1)">
            <label for="sales-customer-search">Tìm khách hàng</label>
            <div class="sales-pos__search-controls">
              <input
                id="sales-customer-search"
                v-model="customerSearch"
                class="input"
                type="text"
                autocomplete="off"
                aria-label="Tìm khách hàng"
                placeholder="Tên hoặc số điện thoại"
                :disabled="locked || creatingCustomer"
              />
              <button class="btn-secondary" type="submit" :disabled="locked || creatingCustomer || customerLoading">Tìm khách hàng</button>
            </div>
          </form>
          <p v-if="customerLoading" class="sales-pos__hint" role="status">Đang tìm khách hàng…</p>
          <p v-else-if="customerError" class="error" role="alert">{{ customerError }}</p>
          <p v-else-if="customers && customers.items.length === 0" class="sales-pos__hint" role="status">Không tìm thấy khách hàng phù hợp.</p>
          <div v-else-if="customers" class="sales-pos__customer-results">
            <button
              v-for="item in customers.items"
              :key="item.id"
              class="sales-pos__customer-result"
              type="button"
              :disabled="locked || creatingCustomer"
              :aria-label="'Chọn khách hàng ' + item.name"
              :aria-pressed="customer?.id === item.id"
              @click="selectCustomer(item)"
            >
              <strong>{{ item.name }}</strong>
              <span>{{ item.phone || 'Không có số điện thoại' }}</span>
            </button>
          </div>
          <nav v-if="customers && customers.totalPages > 1" class="sales-pos__pager" aria-label="Trang khách hàng">
            <button class="btn-secondary" type="button" :disabled="customerPage <= 1 || locked || creatingCustomer || customerLoading" @click="findCustomers(customerPage - 1)">Trước</button>
            <span>Trang {{ customerPage }}/{{ customers.totalPages }}</span>
            <button class="btn-secondary" type="button" :disabled="customerPage >= customers.totalPages || locked || creatingCustomer || customerLoading" @click="findCustomers(customerPage + 1)">Sau</button>
          </nav>
          <details class="sales-pos__create-customer">
            <summary>Thêm khách hàng mới</summary>
            <div class="sales-pos__create-customer-fields">
              <label for="sales-new-customer-name">Tên khách hàng</label>
              <input id="sales-new-customer-name" v-model="newCustomerName" class="input" aria-label="Tên khách hàng mới" placeholder="Tên khách hàng mới" :disabled="locked || creatingCustomer" />
              <label for="sales-new-customer-phone">Số điện thoại (không bắt buộc)</label>
              <input id="sales-new-customer-phone" v-model="newCustomerPhone" class="input" type="tel" aria-label="Số điện thoại khách hàng mới" placeholder="Số điện thoại" :disabled="locked || creatingCustomer" />
              <button class="btn-secondary" type="button" :disabled="locked || creatingCustomer || !newCustomerName.trim()" @click="createAndSelectCustomer">{{ creatingCustomer ? 'Đang tạo khách hàng…' : 'Tạo và chọn khách hàng' }}</button>
            </div>
          </details>
        </section>

      </div>

      <div class="sales-pos__checkout-footer">
        <div class="sales-pos__total-row"><span>Tổng đơn</span><strong>{{ money(total) }} ₫</strong></div>
        <div class="sales-pos__total-row"><span>Đã thanh toán</span><strong>{{ money(paid) }} ₫</strong></div>
        <div class="sales-pos__total-row sales-pos__total-row--outstanding"><span>Còn nợ</span><strong>{{ money(outstanding) }} ₫</strong></div>
        <a v-if="outstanding > 0 && !customer" class="sales-pos__customer-cue" href="#sales-customer-heading">Cần chọn khách hàng cho phần còn nợ <span aria-hidden="true">↓</span></a>
        <p v-if="paid > total" class="sales-pos__overpaid">Đã thanh toán vượt tổng đơn. Xóa khoản thanh toán hoặc điều chỉnh số lượng.</p>
        <p class="sales-pos__preview-note">Giá và tổng chính thức được xác nhận khi hoàn tất.</p>
        <p v-if="message" class="error" role="alert">{{ message }}</p>
        <button
          class="btn-primary sales-pos__complete"
          type="button"
          :disabled="state === 'completing' || state === 'checking' || state === 'completed' || creatingCustomer"
          @click="complete"
        ><svg v-if="state === 'idle'" aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 4 4L19 6"/></svg>{{ state === 'retryable' ? 'Thử lại đúng thao tác' : state === 'completing' || state === 'checking' ? 'Đang xác nhận…' : 'Hoàn tất bán hàng' }}</button>
      </div>
    </section>
  </div>
</template>

<style scoped>
.sales-pos {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(22rem, 23.5rem);
  align-items: start;
  gap: clamp(1rem, 1.7vw, 1.5rem);
  min-width: 0;
}
.sales-pos__products,
.sales-pos__checkout { min-width: 0; }
.sales-pos__products { display: grid; align-content: start; gap: 1.15rem; }
.sales-pos__warning {
  display: grid;
  gap: 0.2rem;
  border-left: 3px solid var(--warning);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--warning) 8%, var(--surface));
  padding: 0.7rem 0.9rem;
  color: var(--text);
  font-size: 0.8rem;
  line-height: 1.45;
}
.sales-pos__warning strong { color: var(--warning); }
.sales-pos__section-heading h2 {
  font-size: clamp(1.15rem, 1.6vw, 1.4rem);
  font-weight: 760;
  letter-spacing: -0.025em;
  line-height: 1.2;
}
.sales-pos__section-heading p { margin-top: 0.2rem; color: var(--text-muted); font-size: 0.84rem; }
.sales-pos__search { display: grid; gap: 0.45rem; }
.sales-pos__search label {
  color: var(--text);
  font-size: 0.82rem;
  font-weight: 700;
}
.sales-pos__search-controls { display: flex; min-width: 0; gap: 0.5rem; }
.sales-pos__search-input {
  position: relative;
  display: flex;
  min-width: 0;
  flex: 1;
  align-items: center;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-lg);
  background: var(--surface);
  box-shadow: 0 3px 12px rgb(14 43 33 / 5%);
}
.sales-pos__search-input:focus-within {
  border-color: var(--brand-primary);
  outline: 3px solid var(--focus-ring);
  outline-offset: 2px;
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--brand-primary) 12%, transparent);
}
.sales-pos__search-input svg {
  width: 1.3rem;
  height: 1.3rem;
  flex: none;
  margin-left: 0.9rem;
  color: var(--brand-primary-hover);
}
.sales-pos__search-input .input {
  min-width: 0;
  min-height: 3.4rem;
  flex: 1;
  border: 0;
  background: transparent;
  box-shadow: none;
  padding-inline: 0.75rem;
  font-size: 0.95rem;
}
.sales-pos__search-input .input:focus { outline: none; }
.sales-pos__search-input .input::placeholder { color: var(--text-subtle); }
.sales-pos__search-controls > .btn-secondary {
  flex: none;
  min-height: 3.4rem;
  border-color: var(--border);
  padding-inline: 1rem;
}
.sales-pos__results-heading {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.75rem;
  padding-top: 0.2rem;
}
.sales-pos__results-heading h3 { font-size: 1rem; font-weight: 770; letter-spacing: -0.015em; }
.sales-pos__results-heading p { color: var(--text-muted); font-size: 0.76rem; text-align: right; }
.sales-pos__empty {
  border: 1px dashed var(--border-strong);
  border-radius: var(--radius-lg);
  background: color-mix(in srgb, var(--surface) 85%, var(--surface-page));
  padding: 2.5rem 1rem;
  color: var(--text-muted);
  font-size: 0.88rem;
  text-align: center;
}
.sales-pos__product-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 10.5rem), 1fr));
  gap: 0.7rem;
}
.sales-pos__product-card {
  display: flex;
  min-width: 0;
  min-height: 10.5rem;
  flex-direction: column;
  justify-content: space-between;
  gap: 0.9rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  padding: 0.95rem;
  box-shadow: 0 1px 2px rgb(14 43 33 / 4%);
  transition: border-color 140ms ease, box-shadow 140ms ease, transform 140ms ease;
}
.sales-pos__product-card:hover,
.sales-pos__product-card:focus-within {
  border-color: var(--brand-primary);
  box-shadow: 0 5px 16px rgb(14 43 33 / 8%);
}
.sales-pos__product-card h4,
.sales-pos__cart-line strong,
.sales-pos__customer-result strong,
.sales-pos__selected-customer strong { overflow-wrap: anywhere; }
.sales-pos__product-card h4 {
  color: var(--text);
  font-size: 0.92rem;
  font-weight: 740;
  line-height: 1.36;
  letter-spacing: -0.015em;
}
.sales-pos__meta { margin-top: 0.4rem; color: var(--text-subtle); font-size: 0.72rem; line-height: 1.35; }
.sales-pos__product-bottom {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: end;
  gap: 0.45rem 0.3rem;
}
.sales-pos__product-price {
  grid-column: 1 / -1;
  color: var(--brand-primary-hover);
  font-size: 1.16rem;
  font-weight: 790;
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.025em;
  white-space: nowrap;
}
.sales-pos__stock {
  min-width: 0;
  color: var(--text-muted);
  font-size: 0.73rem;
  line-height: 1.25;
  overflow-wrap: anywhere;
}
.sales-pos__stock--empty { color: var(--warning); font-weight: 700; }
.sales-pos__add {
  display: inline-flex;
  min-height: 2.75rem;
  align-items: center;
  justify-content: center;
  gap: 0.22rem;
  border: 1px solid color-mix(in srgb, var(--brand-primary) 18%, var(--surface));
  border-radius: var(--radius-sm);
  background: var(--surface-muted);
  padding: 0.3rem 0.55rem;
  color: var(--brand-primary-hover);
  font-size: 0.78rem;
  font-weight: 770;
  white-space: nowrap;
}
.sales-pos__add svg { width: 0.92rem; height: 0.92rem; }
.sales-pos__add:hover:not(:disabled) { background: color-mix(in srgb, var(--brand-primary) 14%, var(--surface)); }
.sales-pos__add:disabled { cursor: not-allowed; opacity: 0.5; }
.sales-pos__pager {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.7rem;
  color: var(--text-muted);
  font-size: 0.78rem;
}
.sales-pos__pager .btn-secondary { min-height: 2.4rem; padding-inline: 0.7rem; }
.sales-pos__checkout {
  position: sticky;
  top: 1rem;
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
  max-height: calc(100dvh - 8.5rem);
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface-elevated);
  box-shadow: 0 10px 30px rgb(14 43 33 / 9%), 0 1px 3px rgb(14 43 33 / 5%);
}
.sales-pos__checkout-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  border-bottom: 1px solid var(--border);
  padding: 1rem 1.1rem;
}
.sales-pos__checkout-title { display: flex; min-width: 0; align-items: center; gap: 0.6rem; }
.sales-pos__checkout-icon {
  display: grid;
  width: 2rem;
  height: 2rem;
  flex: none;
  place-items: center;
  border-radius: var(--radius-sm);
  background: var(--surface-muted);
  color: var(--brand-primary-hover);
}
.sales-pos__checkout-icon svg { width: 1.15rem; height: 1.15rem; }
.sales-pos__checkout-header h2 { font-size: 1.12rem; font-weight: 790; letter-spacing: -0.025em; }
.sales-pos__count {
  flex: none;
  color: var(--text-muted);
  font-size: 0.76rem;
  font-weight: 650;
  white-space: nowrap;
}
.sales-pos__checkout-body { min-height: 0; overflow-y: auto; overscroll-behavior: contain; }
.sales-pos__cart { padding: 1rem 1.1rem; }
.sales-pos__cart-empty {
  border-radius: var(--radius-md);
  background: var(--surface-page);
  padding: 1.1rem;
  color: var(--text-muted);
  font-size: 0.83rem;
  line-height: 1.5;
}
.sales-pos__cart-line {
  display: grid;
  gap: 0.45rem;
  border-bottom: 1px solid var(--border);
  padding: 0.75rem 0;
}
.sales-pos__cart-line:first-child { padding-top: 0; }
.sales-pos__cart-line:last-child { border-bottom: 0; padding-bottom: 0; }
.sales-pos__line-heading,
.sales-pos__line-bottom { display: flex; align-items: start; justify-content: space-between; gap: 0.5rem; }
.sales-pos__line-heading > div { min-width: 0; }
.sales-pos__line-heading strong { display: block; font-size: 0.85rem; font-weight: 740; line-height: 1.34; }
.sales-pos__line-heading p { margin-top: 0.17rem; color: var(--text-muted); font-size: 0.72rem; }
.sales-pos__remove {
  display: inline-grid;
  width: 2.75rem;
  min-width: 2.75rem;
  height: 2.75rem;
  flex: none;
  place-items: center;
  border-radius: var(--radius-sm);
  color: var(--text-subtle);
}
.sales-pos__remove svg { width: 1rem; height: 1rem; }
.sales-pos__remove:hover:not(:disabled) {
  background: color-mix(in srgb, var(--danger) 8%, var(--surface));
  color: var(--danger);
}
.sales-pos__remove:disabled { cursor: not-allowed; opacity: 0.5; }
.sales-pos__visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
}
.sales-pos__line-bottom { align-items: end; }
.sales-pos__quantity { display: grid; width: 5.4rem; gap: 0.16rem; }
.sales-pos__quantity label,
.sales-pos__customer-search label,
.sales-pos__field label,
.sales-pos__create-customer label {
  color: var(--text-muted);
  font-size: 0.72rem;
  font-weight: 650;
}
.sales-pos__quantity .input { min-height: 2.1rem; padding: 0.3rem 0.5rem; font-size: 0.83rem; font-variant-numeric: tabular-nums; }
.sales-pos__line-total {
  padding-bottom: 0.35rem;
  font-size: 0.9rem;
  font-weight: 770;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}
.sales-pos__panel { border-top: 1px solid var(--border); padding: 0.9rem 1.1rem; }
.sales-pos__panel-heading h3 { font-size: 0.91rem; font-weight: 770; }
.sales-pos__panel-heading p { margin-top: 0.15rem; color: var(--text-muted); font-size: 0.72rem; line-height: 1.35; }
.sales-pos__payment-fields {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 0.55rem;
  margin-top: 0.75rem;
}
.sales-pos__field { display: grid; min-width: 0; align-content: start; gap: 0.25rem; }
.sales-pos__field .input { min-width: 0; font-size: 0.83rem; font-variant-numeric: tabular-nums; }
.sales-pos__payment-fields > .btn-secondary {
  grid-column: 1 / -1;
  min-height: 2.75rem;
  border-color: var(--border);
  color: var(--brand-primary-hover);
  font-size: 0.82rem;
}
.sales-pos__payments { display: grid; gap: 0.35rem; margin-top: 0.8rem; }
.sales-pos__payment-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 0.4rem;
  border-radius: var(--radius-sm);
  background: var(--surface-page);
  padding: 0.25rem 0.35rem 0.25rem 0.65rem;
  font-size: 0.79rem;
}
.sales-pos__payment-row strong { font-variant-numeric: tabular-nums; white-space: nowrap; }
.sales-pos__payment-row .sales-pos__remove { width: auto; min-width: 2.75rem; padding-inline: 0.25rem; font-size: 0.73rem; color: var(--text-muted); }
.sales-pos__payment-row .sales-pos__remove:hover:not(:disabled) { color: var(--danger); }
.sales-pos__selected-customer {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 0.1rem 0.45rem;
  margin-top: 0.65rem;
  border: 1px solid color-mix(in srgb, var(--brand-primary) 16%, var(--surface));
  border-radius: var(--radius-sm);
  background: var(--surface-muted);
  padding: 0.55rem 0.65rem;
  font-size: 0.79rem;
}
.sales-pos__selected-customer svg { grid-row: 1 / 3; width: 0.95rem; height: 0.95rem; margin-top: 0.13rem; color: var(--brand-primary); }
.sales-pos__selected-customer span { color: var(--text-muted); font-size: 0.72rem; }
.sales-pos__customer-search { display: grid; gap: 0.35rem; margin-top: 0.7rem; }
.sales-pos__customer-search .sales-pos__search-controls { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 0.4rem; }
.sales-pos__customer-search .btn-secondary { min-height: 2.65rem; padding-inline: 0.65rem; font-size: 0.78rem; }
.sales-pos__customer-search .input { min-width: 0; font-size: 0.82rem; }
.sales-pos__hint { margin-top: 0.65rem; color: var(--text-muted); font-size: 0.78rem; }
.sales-pos__customer-results { display: grid; gap: 0.3rem; max-height: 11rem; overflow-y: auto; margin-top: 0.65rem; }
.sales-pos__customer-result {
  display: grid;
  gap: 0.1rem;
  min-height: 2.6rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  padding: 0.45rem 0.6rem;
  font-size: 0.79rem;
  text-align: left;
}
.sales-pos__customer-result span { color: var(--text-muted); font-size: 0.71rem; }
.sales-pos__customer-result[aria-pressed="true"] { border-color: var(--brand-primary); background: var(--surface-muted); color: var(--brand-primary-hover); }
.sales-pos__panel .sales-pos__pager { margin-top: 0.65rem; }
.sales-pos__create-customer { margin-top: 0.55rem; border-top: 1px solid var(--border); }
.sales-pos__create-customer summary { min-height: 2.4rem; padding: 0.65rem 0.1rem 0.45rem; color: var(--brand-primary-hover); font-size: 0.78rem; font-weight: 730; cursor: pointer; }
.sales-pos__create-customer-fields { display: grid; gap: 0.35rem; padding-top: 0.3rem; }
.sales-pos__create-customer .btn-secondary { margin-top: 0.25rem; }
.sales-pos__checkout-footer {
  display: grid;
  gap: 0.42rem;
  border-top: 1px solid var(--border);
  background: color-mix(in srgb, var(--surface-muted) 58%, var(--surface));
  padding: 0.95rem 1.1rem 1.05rem;
  box-shadow: 0 -4px 16px rgb(14 43 33 / 3%);
}
.sales-pos__total-row { display: flex; justify-content: space-between; gap: 0.75rem; font-size: 0.81rem; line-height: 1.4; }
.sales-pos__total-row strong { font-weight: 750; font-variant-numeric: tabular-nums; white-space: nowrap; }
.sales-pos__total-row:first-child {
  align-items: baseline;
  margin-bottom: 0.15rem;
  color: var(--text);
  font-size: 0.93rem;
  font-weight: 760;
}
.sales-pos__total-row:first-child strong {
  color: var(--brand-primary-hover);
  font-size: 1.55rem;
  font-weight: 820;
  letter-spacing: -0.045em;
}
.sales-pos__total-row--outstanding { color: var(--text-muted); }
.sales-pos__customer-cue {
  display: flex;
  min-height: 2.75rem;
  align-items: center;
  justify-content: space-between;
  gap: 0.4rem;
  border-left: 2px solid var(--warning);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--warning) 7%, var(--surface));
  padding: 0.4rem 0.6rem;
  color: var(--text);
  font-size: 0.75rem;
  font-weight: 680;
  line-height: 1.3;
  text-decoration: none;
}
.sales-pos__customer-cue:hover { color: var(--brand-primary-hover); text-decoration: underline; }
.sales-pos__preview-note { color: var(--text-subtle); font-size: 0.68rem; line-height: 1.35; }
.sales-pos__overpaid { color: var(--danger); font-size: 0.77rem; font-weight: 700; }
.sales-pos__complete {
  width: 100%;
  min-height: 3.3rem;
  margin-top: 0.35rem;
  box-shadow: 0 3px 9px rgb(4 120 87 / 17%);
  font-size: 0.96rem;
  font-weight: 780;
}
.sales-pos__complete svg { width: 1.05rem; height: 1.05rem; }
@media (min-width: 1081px) and (max-width: 1279px) {
  .sales-pos { grid-template-columns: minmax(0, 1fr) 21.5rem; }
}
@media (max-width: 1080px) {
  .sales-pos { grid-template-columns: minmax(0, 1fr); }
  .sales-pos__checkout { position: static; max-height: none; overflow: visible; }
  .sales-pos__checkout-body { overflow: visible; }
}
@media (max-width: 600px) {
  .sales-pos { gap: 1.3rem; }
  .sales-pos__products { gap: 0.9rem; }
  .sales-pos__search-controls { display: grid; grid-template-columns: minmax(0, 1fr); }
  .sales-pos__search-controls > .btn-secondary { width: 100%; min-height: 2.75rem; }
  .sales-pos__product-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0.5rem; }
  .sales-pos__product-card { min-height: 10.2rem; padding: 0.75rem; }
  .sales-pos__product-card h4 { font-size: 0.86rem; }
  .sales-pos__product-price { font-size: 1.05rem; }
  .sales-pos__product-bottom { grid-template-columns: minmax(0, 1fr) auto; }
  .sales-pos__add { min-width: 2.75rem; padding-inline: 0.45rem; }
  .sales-pos__add span { display: none; }
  .sales-pos__add svg { width: 1.15rem; height: 1.15rem; }
  .sales-pos__checkout-header,
  .sales-pos__cart,
  .sales-pos__panel,
  .sales-pos__checkout-footer { padding-inline: 0.9rem; }
  .sales-pos__pager { justify-content: center; }
}
@media (max-width: 360px) {
  .sales-pos__product-grid { grid-template-columns: minmax(0, 1fr); }
}
@media (prefers-reduced-motion: reduce) {
  .sales-pos__product-card { transition: none; }
}

/* D-106 Sales visual reference alignment. Business behavior remains in the script above. */
.sales-pos {
  grid-template-columns: minmax(0, 1fr) 25.25rem;
  gap: 0.875rem;
}
.sales-pos__products { gap: 0.875rem; }
.sales-pos__section-heading {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
}
.sales-pos__search { gap: 0; }
.sales-pos__search > label {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
}
.sales-pos__search-controls { gap: 0.75rem; }
.sales-pos__search-input {
  height: 3.25rem;
  border: 2px solid rgb(92 117 255 / 26%);
  border-radius: 0.625rem;
  box-shadow: 0 6px 16px rgb(61 84 179 / 6%);
}
.sales-pos__search-input .input { min-height: 3rem; font-size: 0.875rem; }
.sales-pos__search-controls > .sales-pos__search-button {
  min-width: 9.5rem;
  min-height: 3.25rem;
  border-color: rgb(4 120 87 / 24%);
  border-radius: 0.625rem;
  color: var(--brand-primary-hover);
  font-size: 0.84rem;
  font-weight: 780;
}
.sales-pos__search-button svg { width: 1rem; height: 1rem; }
.sales-pos__results-heading {
  min-height: 2rem;
  padding-top: 0;
}
.sales-pos__results-heading h3 { font-size: 0.95rem; font-weight: 780; }
.sales-pos__product-grid {
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.75rem;
}
.sales-pos__product-card {
  min-height: 14.75rem;
  gap: 0.75rem;
  border-color: #e3e9ef;
  border-radius: 0.875rem;
  padding: 1rem;
  box-shadow: 0 1px 0 rgb(20 38 63 / 2%);
}
.sales-pos__product-card:hover,
.sales-pos__product-card:focus-within {
  border-color: #ccd9e0;
  box-shadow: 0 7px 18px rgb(25 48 72 / 6%);
  transform: translateY(-1px);
}
.sales-pos__product-card h4 {
  font-size: 0.97rem;
  font-weight: 780;
  line-height: 1.24;
  overflow-wrap: break-word;
  word-break: normal;
}
.sales-pos__meta { margin-top: 0.55rem; color: #637493; font-size: 0.77rem; }
.sales-pos__product-bottom { gap: 0.55rem 0.4rem; }
.sales-pos__product-price {
  color: var(--text);
  font-size: 1rem;
  font-weight: 820;
}
.sales-pos__stock { color: #596a82; font-size: 0.77rem; }
.sales-pos__add {
  width: 2.375rem;
  min-width: 2.375rem;
  min-height: 2.375rem;
  border: 0;
  border-radius: 0.625rem;
  background: #e6f7ee;
  padding: 0;
  color: #0aa36b;
}
.sales-pos__add span { display: none; }
.sales-pos__add svg { width: 1.2rem; height: 1.2rem; }
.sales-pos__checkout {
  top: 0.75rem;
  min-height: calc(100dvh - 5.5rem);
  max-height: calc(100dvh - 5.5rem);
  border-color: #e3e9ef;
  border-radius: 1rem;
  background: #fff;
  box-shadow: 0 7px 22px rgb(16 39 68 / 6%);
}
.sales-pos__checkout-header {
  min-height: 4.125rem;
  border-bottom-color: #ecf0f4;
  padding: 0.8rem 1.125rem;
}
.sales-pos__checkout-title { gap: 0; }
.sales-pos__checkout-icon { display: none; }
.sales-pos__checkout-title > div { display: flex; align-items: center; gap: 0.5rem; }
.sales-pos__checkout-header h2 { font-size: 1.125rem; font-weight: 850; }
.sales-pos__status {
  display: inline-flex;
  height: 1.5rem;
  align-items: center;
  border-radius: 999px;
  background: #edf9f3;
  padding: 0 0.55rem;
  color: #0d8b5b;
  font-size: 0.68rem;
  font-weight: 800;
}
.sales-pos__count { font-size: 0.72rem; }
.sales-pos__cart { padding: 0.85rem 1.125rem; }
.sales-pos__cart-line { gap: 0.45rem; padding: 0.65rem 0; }
.sales-pos__line-heading strong {
  font-size: 0.84rem;
  font-weight: 780;
  line-height: 1.25;
  overflow-wrap: break-word;
  word-break: normal;
}
.sales-pos__line-heading p { font-size: 0.72rem; }
.sales-pos__remove {
  width: 2.25rem;
  min-width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.625rem;
}
.sales-pos__quantity {
  display: grid;
  width: 7.25rem;
  height: 2.125rem;
  grid-template-columns: 1fr 1.25fr 1fr;
  gap: 0;
  overflow: hidden;
  border: 1px solid var(--border-strong);
  border-radius: 0.625rem;
  background: #fff;
}
.sales-pos__quantity button {
  display: grid;
  min-width: 0;
  place-items: center;
  color: var(--text);
  font-size: 1rem;
  font-weight: 800;
}
.sales-pos__quantity button:hover:not(:disabled) { background: var(--surface-muted); }
.sales-pos__quantity button:disabled { cursor: not-allowed; opacity: 0.5; }
.sales-pos__quantity .input {
  min-height: 0;
  border-width: 0 1px;
  border-color: var(--border);
  border-radius: 0;
  padding: 0.2rem;
  font-weight: 800;
  text-align: center;
  -moz-appearance: textfield;
}
.sales-pos__quantity .input::-webkit-outer-spin-button,
.sales-pos__quantity .input::-webkit-inner-spin-button { margin: 0; -webkit-appearance: none; }
.sales-pos__line-total { padding-bottom: 0.35rem; font-size: 0.92rem; font-weight: 850; }
.sales-pos__panel { padding: 0.8rem 1.125rem; }
.sales-pos__panel-heading h3 { font-size: 0.86rem; font-weight: 780; }
.sales-pos__panel-heading p { margin-top: 0.12rem; }
.sales-pos__payment-fields {
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 0.55rem;
  margin-top: 0.65rem;
}
.sales-pos__method {
  display: grid;
  grid-column: 1 / -1;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.55rem;
}
.sales-pos__method button {
  display: inline-flex;
  min-height: 2.75rem;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  border: 1.5px solid var(--border-strong);
  border-radius: 0.625rem;
  background: #fff;
  color: #2c3d54;
  font-size: 0.82rem;
  font-weight: 800;
}
.sales-pos__method button.is-active {
  border-color: rgb(10 163 107 / 45%);
  background: #eff9f3;
  color: #0a915f;
}
.sales-pos__method button:disabled { cursor: not-allowed; opacity: 0.5; }
.sales-pos__method svg { width: 1rem; height: 1rem; }
.sales-pos__field .input { min-height: 2.75rem; border-radius: 0.625rem; }
.sales-pos__payment-fields > .btn-secondary {
  grid-column: auto;
  min-height: 2.75rem;
  border-color: rgb(10 163 107 / 28%);
  border-radius: 0.625rem;
  color: #0f915f;
  font-weight: 800;
}
.sales-pos__payment-row { border-radius: 0.625rem; }
.sales-pos__selected-customer { border-radius: 0.625rem; }
.sales-pos__customer-cue { border-radius: 0.625rem; }
.sales-pos__checkout-footer {
  gap: 0.45rem;
  border-top-color: #ecf0f4;
  background: #fff;
  padding: 0.85rem 1.125rem 1rem;
  box-shadow: 0 -4px 16px rgb(16 39 68 / 3%);
}
.sales-pos__total-row { font-size: 0.8rem; }
.sales-pos__total-row:first-child { margin: 0.2rem 0 0.35rem; font-size: 1rem; }
.sales-pos__total-row:first-child strong {
  color: #078b5d;
  font-size: 1.65rem;
  font-weight: 900;
}
.sales-pos__complete {
  min-height: 3.25rem;
  border-radius: 0.625rem;
  background: #0aa06a;
  box-shadow: 0 12px 22px rgb(10 163 107 / 20%);
  font-size: 0.95rem;
  font-weight: 850;
}

@media (min-width: 1081px) and (max-width: 1280px) {
  .sales-pos { grid-template-columns: minmax(0, 1fr) 21.875rem; }
  .sales-pos__product-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
}
@media (max-width: 1080px) {
  .sales-pos { grid-template-columns: minmax(0, 1fr); }
  .sales-pos__product-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
  .sales-pos__checkout { position: static; min-height: 0; max-height: none; overflow: visible; }
  .sales-pos__checkout-body { overflow: visible; }
}
@media (max-width: 600px) {
  .sales-pos { gap: 1rem; }
  .sales-pos__search-controls { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 0.5rem; }
  .sales-pos__search-controls > .sales-pos__search-button { width: 3.25rem; min-width: 3.25rem; min-height: 3.25rem; padding: 0; }
  .sales-pos__search-button span { display: none; }
  .sales-pos__search-button svg { width: 1.15rem; height: 1.15rem; }
  .sales-pos__product-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .sales-pos__product-card { min-height: 11.5rem; }
  .sales-pos__payment-fields { grid-template-columns: minmax(0, 1fr); }
  .sales-pos__payment-fields > .btn-secondary { grid-column: 1; }
  .sales-pos__method { grid-column: 1; }
}
@media (max-width: 360px) {
  .sales-pos__product-grid { grid-template-columns: minmax(0, 1fr); }
}
</style>
