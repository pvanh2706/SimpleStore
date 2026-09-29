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
        <div>
          <p class="sales-pos__eyebrow">SẢN PHẨM</p>
          <h2 id="sales-products-heading">Tìm và thêm sản phẩm</h2>
          <p>Tìm theo tên, SKU hoặc quét barcode để thêm vào đơn hiện tại.</p>
        </div>
      </div>

      <form class="sales-pos__search" role="search" @submit.prevent="findProducts(1)">
        <label for="sales-product-search">Tìm hoặc quét sản phẩm</label>
        <div class="sales-pos__search-controls">
          <input
            id="sales-product-search"
            v-model="productSearch"
            class="input"
            type="text"
            autocomplete="off"
            aria-label="Tìm hoặc quét sản phẩm"
            placeholder="Tên, SKU hoặc barcode"
            :disabled="locked"
          />
          <button class="btn-secondary" type="submit" :disabled="locked">Tìm sản phẩm</button>
        </div>
      </form>

      <div class="sales-pos__results-heading">
        <div>
          <h3>Kết quả sản phẩm</h3>
          <p v-if="products" role="status">{{ products.totalCount }} sản phẩm · Trang {{ productPage }}/{{ products.totalPages || 1 }}</p>
        </div>
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
            <span class="sales-pos__stock">Tồn {{ money(product.quantityOnHand) }} {{ product.unit }}</span>
            <button
              class="sales-pos__add"
              type="button"
              :disabled="locked"
              :aria-label="'Thêm sản phẩm ' + product.name"
              @click="addProduct(product)"
            >Thêm</button>
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
        <div>
          <p class="sales-pos__eyebrow">ĐƠN HIỆN TẠI</p>
          <h2 id="sales-checkout-heading">Giỏ hàng</h2>
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
              >Xóa</button>
            </div>
            <div class="sales-pos__line-bottom">
              <div class="sales-pos__quantity">
                <label :for="'sales-quantity-' + line.product.id">Số lượng</label>
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
              </div>
              <strong class="sales-pos__line-total">{{ money(lineAmount(line)) }} ₫</strong>
            </div>
          </div>
        </section>

        <section v-if="outstanding > 0" class="sales-pos__panel" aria-labelledby="sales-customer-heading">
          <div class="sales-pos__panel-heading">
            <h3 id="sales-customer-heading">Khách hàng công nợ</h3>
            <p>Đơn chưa thanh toán đủ cần có khách hàng.</p>
          </div>
          <div v-if="customer" class="sales-pos__selected-customer">
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

        <section class="sales-pos__panel" aria-labelledby="sales-payment-heading">
          <div class="sales-pos__panel-heading">
            <h3 id="sales-payment-heading">Thanh toán</h3>
            <p>Có thể thêm nhiều khoản tiền mặt hoặc chuyển khoản.</p>
          </div>
          <div class="sales-pos__payment-fields">
            <div class="sales-pos__field">
              <label for="sales-payment-method">Phương thức thanh toán</label>
              <select id="sales-payment-method" v-model="method" class="input" aria-label="Phương thức thanh toán" :disabled="locked">
                <option value="Cash">Tiền mặt</option>
                <option value="Transfer">Chuyển khoản</option>
              </select>
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
      </div>

      <div class="sales-pos__checkout-footer">
        <div class="sales-pos__total-row"><span>Tổng đơn (xem trước)</span><strong>{{ money(total) }} ₫</strong></div>
        <div class="sales-pos__total-row"><span>Đã thanh toán</span><strong>{{ money(paid) }} ₫</strong></div>
        <div class="sales-pos__total-row sales-pos__total-row--outstanding"><span>Còn nợ</span><strong>{{ money(outstanding) }} ₫</strong></div>
        <p v-if="paid > total" class="sales-pos__overpaid">Đã thanh toán vượt tổng đơn. Xóa khoản thanh toán hoặc điều chỉnh số lượng.</p>
        <p class="sales-pos__preview-note">Giá và tổng chính thức được xác nhận khi hoàn tất.</p>
        <p v-if="message" class="error" role="alert">{{ message }}</p>
        <button
          class="btn-primary sales-pos__complete"
          type="button"
          :disabled="state === 'completing' || state === 'checking' || state === 'completed' || creatingCustomer"
          @click="complete"
        >{{ state === 'retryable' ? 'Thử lại đúng thao tác' : state === 'completing' || state === 'checking' ? 'Đang xác nhận…' : 'Hoàn tất bán hàng' }}</button>
      </div>
    </section>
  </div>
</template>

<style scoped>
.sales-pos {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(21rem, 24rem);
  align-items: start;
  gap: clamp(1rem, 1.6vw, 1.5rem);
  min-width: 0;
}
.sales-pos__products,
.sales-pos__checkout { min-width: 0; }
.sales-pos__products { display: grid; align-content: start; gap: 1.1rem; }
.sales-pos__warning {
  display: grid;
  gap: 0.3rem;
  border: 1px solid #f8d9a4;
  border-radius: var(--radius-lg);
  background: #fff8e8;
  padding: 0.8rem 1rem;
  color: #7a4108;
  font-size: 0.875rem;
  line-height: 1.45;
}
.sales-pos__section-heading h2,
.sales-pos__checkout-header h2 {
  font-size: clamp(1.3rem, 1.7vw, 1.65rem);
  font-weight: 800;
  letter-spacing: -0.025em;
  line-height: 1.2;
}
.sales-pos__section-heading p:last-child { margin-top: 0.3rem; color: var(--text-muted); font-size: 0.875rem; }
.sales-pos__eyebrow {
  margin-bottom: 0.3rem;
  color: var(--brand-primary-hover);
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.09em;
}
.sales-pos__search {
  display: grid;
  gap: 0.5rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  padding: 1rem;
}
.sales-pos__search label,
.sales-pos__customer-search label,
.sales-pos__field label,
.sales-pos__create-customer label,
.sales-pos__quantity label {
  color: var(--text-muted);
  font-size: 0.78rem;
  font-weight: 700;
}
.sales-pos__search-controls { display: flex; gap: 0.5rem; min-width: 0; }
.sales-pos__search-controls .input { min-width: 0; flex: 1; }
.sales-pos__search-controls button { flex: none; }
.sales-pos__results-heading { display: flex; align-items: end; justify-content: space-between; }
.sales-pos__results-heading h3 { font-size: 1rem; font-weight: 750; }
.sales-pos__results-heading p { margin-top: 0.15rem; color: var(--text-muted); font-size: 0.78rem; }
.sales-pos__empty {
  border: 1px dashed var(--border-strong);
  border-radius: var(--radius-lg);
  background: var(--surface);
  padding: 2.5rem 1rem;
  color: var(--text-muted);
  text-align: center;
}
.sales-pos__product-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 13rem), 1fr));
  gap: 0.75rem;
}
.sales-pos__product-card {
  display: flex;
  min-width: 0;
  min-height: 10rem;
  flex-direction: column;
  justify-content: space-between;
  gap: 1rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  padding: 1rem;
  box-shadow: 0 1px 3px rgb(14 43 33 / 4%);
}
.sales-pos__product-card h4,
.sales-pos__cart-line strong,
.sales-pos__customer-result strong,
.sales-pos__selected-customer strong { overflow-wrap: anywhere; }
.sales-pos__product-card h4 { font-size: 0.95rem; font-weight: 750; line-height: 1.35; }
.sales-pos__meta { margin-top: 0.35rem; color: var(--text-muted); font-size: 0.75rem; }
.sales-pos__product-bottom { display: grid; grid-template-columns: minmax(0, 1fr) auto; align-items: end; gap: 0.35rem; }
.sales-pos__product-price { grid-column: 1 / -1; color: var(--brand-primary-hover); font-size: 1.1rem; font-variant-numeric: tabular-nums; }
.sales-pos__stock { min-width: 0; color: var(--text-muted); font-size: 0.75rem; overflow-wrap: anywhere; }
.sales-pos__add {
  min-height: 2.75rem;
  border: 1px solid #b9ddcd;
  border-radius: var(--radius-md);
  background: var(--surface-muted);
  padding: 0.5rem 0.85rem;
  color: var(--brand-primary-hover);
  font-size: 0.85rem;
  font-weight: 750;
}
.sales-pos__add:hover:not(:disabled) { background: #dcefe6; }
.sales-pos__add:disabled { cursor: not-allowed; opacity: 0.5; }
.sales-pos__pager { display: flex; align-items: center; justify-content: flex-end; gap: 0.65rem; color: var(--text-muted); font-size: 0.8rem; }
.sales-pos__pager .btn-secondary { min-height: 2.5rem; padding-inline: 0.7rem; }
.sales-pos__checkout {
  position: sticky;
  top: 1rem;
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
  max-height: calc(100dvh - 2rem);
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  box-shadow: var(--shadow-elevated);
}
.sales-pos__checkout-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  border-bottom: 1px solid var(--border);
  padding: 1rem 1.1rem;
}
.sales-pos__count {
  flex: none;
  border-radius: 999px;
  background: var(--surface-muted);
  padding: 0.35rem 0.65rem;
  color: var(--brand-primary-hover);
  font-size: 0.72rem;
  font-weight: 750;
}
.sales-pos__checkout-body { min-height: 0; overflow-y: auto; overscroll-behavior: contain; }
.sales-pos__cart,
.sales-pos__panel { padding: 1rem 1.1rem; }
.sales-pos__panel { border-top: 1px solid var(--border); }
.sales-pos__cart-empty { border: 1px dashed var(--border-strong); border-radius: var(--radius-md); padding: 1rem; color: var(--text-muted); font-size: 0.85rem; line-height: 1.5; }
.sales-pos__cart-line { display: grid; gap: 0.7rem; border-bottom: 1px solid var(--border); padding: 0.9rem 0; }
.sales-pos__cart-line:first-child { padding-top: 0; }
.sales-pos__cart-line:last-child { border-bottom: 0; padding-bottom: 0; }
.sales-pos__line-heading,
.sales-pos__line-bottom { display: flex; align-items: start; justify-content: space-between; gap: 0.75rem; }
.sales-pos__line-heading > div { min-width: 0; }
.sales-pos__line-heading strong { display: block; font-size: 0.875rem; line-height: 1.35; }
.sales-pos__line-heading p { margin-top: 0.2rem; color: var(--text-muted); font-size: 0.75rem; }
.sales-pos__remove {
  flex: none;
  min-width: 2.75rem;
  min-height: 2.75rem;
  border-radius: var(--radius-sm);
  color: var(--danger);
  font-size: 0.8rem;
  font-weight: 700;
}
.sales-pos__remove:hover:not(:disabled) { background: #fef3f2; }
.sales-pos__remove:disabled { cursor: not-allowed; opacity: 0.5; }
.sales-pos__line-bottom { align-items: end; }
.sales-pos__quantity { display: grid; width: 6.5rem; gap: 0.2rem; }
.sales-pos__quantity .input { min-height: 2.5rem; padding-block: 0.4rem; }
.sales-pos__line-total { padding-bottom: 0.6rem; font-size: 0.9rem; font-variant-numeric: tabular-nums; white-space: nowrap; }
.sales-pos__panel-heading h3 { font-size: 0.95rem; font-weight: 800; }
.sales-pos__panel-heading p { margin-top: 0.2rem; color: var(--text-muted); font-size: 0.75rem; line-height: 1.4; }
.sales-pos__selected-customer { display: grid; gap: 0.2rem; margin-top: 0.8rem; border-radius: var(--radius-md); background: var(--surface-muted); padding: 0.7rem; font-size: 0.82rem; }
.sales-pos__selected-customer span { color: var(--text-muted); }
.sales-pos__customer-search { display: grid; gap: 0.45rem; margin-top: 0.9rem; }
.sales-pos__customer-search .sales-pos__search-controls { display: grid; grid-template-columns: minmax(0, 1fr) auto; }
.sales-pos__hint { margin-top: 0.7rem; color: var(--text-muted); font-size: 0.8rem; }
.sales-pos__customer-results { display: grid; margin-top: 0.7rem; }
.sales-pos__customer-result { display: grid; gap: 0.2rem; min-height: 2.75rem; border-top: 1px solid var(--border); padding: 0.55rem 0.3rem; text-align: left; }
.sales-pos__customer-result span { color: var(--text-muted); font-size: 0.75rem; }
.sales-pos__customer-result[aria-pressed="true"] { background: var(--surface-muted); color: var(--brand-primary-hover); }
.sales-pos__panel .sales-pos__pager { margin-top: 0.7rem; }
.sales-pos__create-customer { margin-top: 0.9rem; border-top: 1px solid var(--border); }
.sales-pos__create-customer summary { min-height: 2.75rem; padding: 0.75rem 0.3rem; color: var(--brand-primary-hover); font-size: 0.82rem; font-weight: 750; cursor: pointer; }
.sales-pos__create-customer-fields { display: grid; gap: 0.45rem; padding-top: 0.3rem; }
.sales-pos__create-customer .btn-secondary { margin-top: 0.25rem; }
.sales-pos__payment-fields { display: grid; gap: 0.65rem; margin-top: 0.85rem; }
.sales-pos__field { display: grid; gap: 0.25rem; }
.sales-pos__payments { margin-top: 0.85rem; }
.sales-pos__payment-row { display: grid; grid-template-columns: minmax(0, 1fr) auto auto; align-items: center; gap: 0.5rem; border-top: 1px solid var(--border); font-size: 0.8rem; }
.sales-pos__payment-row strong { font-variant-numeric: tabular-nums; white-space: nowrap; }
.sales-pos__checkout-footer {
  display: grid;
  gap: 0.5rem;
  border-top: 1px solid var(--border);
  background: var(--surface);
  padding: 1rem 1.1rem;
}
.sales-pos__total-row { display: flex; justify-content: space-between; gap: 0.75rem; font-size: 0.84rem; }
.sales-pos__total-row strong { font-variant-numeric: tabular-nums; white-space: nowrap; }
.sales-pos__total-row--outstanding { color: var(--brand-primary-hover); font-weight: 750; }
.sales-pos__preview-note { color: var(--text-subtle); font-size: 0.7rem; line-height: 1.35; }
.sales-pos__overpaid { color: var(--danger); font-size: 0.78rem; font-weight: 700; }
.sales-pos__complete { width: 100%; margin-top: 0.25rem; }
@media (min-width: 1081px) and (max-width: 1279px) {
  .sales-pos { grid-template-columns: minmax(0, 1fr) 21rem; }
  .sales-pos__product-grid { grid-template-columns: repeat(auto-fill, minmax(min(100%, 12.5rem), 1fr)); }
}
@media (max-width: 1080px) {
  .sales-pos { grid-template-columns: minmax(0, 1fr); }
  .sales-pos__checkout { position: static; max-height: none; overflow: visible; }
  .sales-pos__checkout-body { overflow: visible; }
}
@media (max-width: 600px) {
  .sales-pos__search-controls,
  .sales-pos__customer-search .sales-pos__search-controls { display: grid; grid-template-columns: minmax(0, 1fr); }
  .sales-pos__search-controls button { width: 100%; }
  .sales-pos__checkout-header,
  .sales-pos__cart,
  .sales-pos__panel,
  .sales-pos__checkout-footer { padding-inline: 0.9rem; }
  .sales-pos__pager { justify-content: center; }
}
</style>
