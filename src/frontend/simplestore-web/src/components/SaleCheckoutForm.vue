<script setup lang="ts">
import { computed, inject, onMounted, onUnmounted, ref } from 'vue'
import { routerKey } from 'vue-router'
import { ApiError } from '../api/client'
import type { Customer, CustomerPage, OperationStatus, ProductListItem, ProductPage, Sale } from '../api/types'
import LineIcon from './ui/LineIcon'
import type { IconName } from './ui/icons'
import { factualStockState, productCategories, productCategory, stockStatus, type ProductCategory } from '../sales/catalog'
import { createDemoOrderBook, demoImageById } from '../sales/demo'
import {
  createOrderBook, lineAmount, lineDiscount, orderTotal,
  type OrderBook, type PaymentInput, type PayMode,
} from '../sales/orders'

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
  previewOnly?: boolean
  /** Shared working orders; a standalone form keeps its own. */
  orderBook?: OrderBook
}>()
const emit = defineEmits<{ completed: [sale: Sale] }>()
const router = inject(routerKey, null)

const root = ref<HTMLElement | null>(null)
const searchInput = ref<HTMLInputElement | null>(null)
const customerPicker = ref<HTMLDetailsElement | null>(null)
const createPicker = ref<HTMLDetailsElement | null>(null)
const queueMenu = ref<HTMLDetailsElement | null>(null)
const idSuffix = props.previewOnly ? '-preview' : ''

const book = props.orderBook ?? (props.previewOnly ? createDemoOrderBook() : createOrderBook())
const orders = book.orders
const order = computed(() => book.active.value)
const cart = computed(() => order.value.cart)
const payments = computed(() => order.value.payments)
const customer = computed(() => order.value.customer)
const railOrders = computed(() => {
  const shown = orders.value.slice(0, 3)
  if (!shown.includes(order.value)) shown[2] = order.value
  return shown
})

const productSearch = ref('')
const productQuery = ref('')
const products = ref<ProductPage | null>(null)
const productPage = ref(1)
const productLoading = ref(false)
const productError = ref('')
const category = ref<ProductCategory>('Tất cả')
const selectedProductId = ref<string | null>(props.previewOnly ? 'demo-coke' : null)
/** Keyword Category is Demo / Visual Reference only (D-107); live shows every loaded Product. */
const shownProducts = computed(() => {
  const items = products.value?.items ?? []
  if (!props.previewOnly || category.value === 'Tất cả') return items
  return items.filter(item => productCategory(item) === category.value)
})
let productRequestId = 0

const amount = ref<number | null>(null)
const splitOpen = ref(false)
const splitShown = computed(() => splitOpen.value || payments.value.length > 0)
/** Live payment methods are Cash and Transfer only; `Bán nợ` stays a D-106 visual in Demo mode (D-107). */
const payModes: ReadonlyArray<{ id: PayMode; label: string; icon: IconName }> = [
  { id: 'Cash', label: 'Tiền mặt', icon: 'cash' },
  { id: 'Transfer', label: 'Chuyển khoản', icon: 'bank' },
  ...(props.previewOnly ? [{ id: 'Debt' as const, label: 'Bán nợ', icon: 'debt' as const }] : []),
]

const customerSearch = ref('')
const customerQuery = ref('')
const customers = ref<CustomerPage | null>(null)
const customerPage = ref(1)
const customerLoading = ref(false)
const customerError = ref('')
const creatingCustomer = ref(false)
let customerRequestId = 0
const newCustomerName = ref('')
const newCustomerPhone = ref('')

const state = ref<State>('idle')
const attempt = ref<AttemptSnapshot | null>(null)
const message = ref('')
const notice = ref('')

const itemCount = computed(() => cart.value.reduce((sum, line) => sum + (Number.isFinite(line.quantity) && line.quantity > 0 ? line.quantity : 0), 0))
const subtotal = computed(() => cart.value.reduce((sum, line) => sum + lineAmount(line), 0))
/** Discounts are a browser-only Demo visual; live totals never include them (D-107). */
const discount = computed(() => props.previewOnly ? cart.value.reduce((sum, line) => sum + lineDiscount(line), 0) : 0)
const total = computed(() => subtotal.value - discount.value)
const explicitPaid = computed(() => payments.value.reduce((sum, payment) => sum + payment.amount, 0))
/**
 * Entered amounts win. Without entered amounts, Tiền mặt/Chuyển khoản pays the whole order. Once the
 * cashier opens "Nhập số tiền", only actually entered payments count, possibly none, so live debt is
 * Outstanding = Total − Actual Payments without any Debt method (D-107). Demo keeps `Bán nợ` as no payment.
 */
const effectivePayments = computed<PaymentInput[]>(() => {
  if (payments.value.length) return payments.value
  if (total.value <= 0) return []
  const mode = order.value.payMode
  if (props.previewOnly ? mode === 'Debt' : splitOpen.value) return []
  return [{ amount: total.value, method: mode === 'Transfer' ? 'Transfer' : 'Cash' }]
})
const paid = computed(() => effectivePayments.value.reduce((sum, payment) => sum + payment.amount, 0))
const outstanding = computed(() => Math.max(0, total.value - paid.value))
const customerRequired = computed(() => cart.value.length > 0 && outstanding.value > 0)
const locked = computed(() => ['completing', 'checking', 'retryable', 'completed'].includes(state.value))
const busy = computed(() => locked.value || creatingCustomer.value)
const money = (value: number) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(value)
const unitLabel = (unit: string) => unit.charAt(0).toLocaleUpperCase('vi-VN') + unit.slice(1)
const methodLabel = (method: PaymentInput['method']) => method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản'

/** Sample images belong to Demo Products only; live Products always use the neutral placeholder (D-107). */
function productImage(product: ProductListItem): string | null {
  return props.previewOnly ? demoImageById[product.id] ?? null : null
}

function closeDetails(element: HTMLDetailsElement | null) {
  if (element) element.open = false
}

function resetFeedback() {
  message.value = ''
  notice.value = ''
}

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

async function quickAdd() {
  await findProducts(1)
  const items = products.value?.items ?? []
  if (productQuery.value && items.length === 1) addProduct(items[0]!)
}

function focusSearch() {
  searchInput.value?.focus()
  searchInput.value?.select()
}

function addProduct(product: ProductListItem) {
  if (locked.value) return
  resetFeedback()
  if (cart.value.some(line => line.product.id === product.id)) {
    message.value = 'Sản phẩm đã có trong giỏ hàng.'
    return
  }
  cart.value.push({ product, quantity: 1 })
  selectedProductId.value = product.id
}

function removeProduct(productId: string) {
  if (locked.value) return
  order.value.cart = cart.value.filter(line => line.product.id !== productId)
  if (selectedProductId.value === productId) selectedProductId.value = null
}

function changeQuantity(line: { quantity: number }, delta: number) {
  if (locked.value) return
  const current = Number.isFinite(line.quantity) ? line.quantity : 0
  line.quantity = Math.max(0.001, Math.round((current + delta) * 1000) / 1000)
}

function setPayMode(mode: PayMode) {
  if (locked.value) return
  order.value.payMode = mode
}

function addPayment() {
  if (locked.value) return
  resetFeedback()
  const value = amount.value
  if (typeof value !== 'number' || !Number.isFinite(value) || value <= 0) {
    message.value = 'Số tiền phải lớn hơn 0.'
    return
  }
  if (explicitPaid.value + value > total.value) {
    message.value = 'Tổng thanh toán không được vượt tổng đơn.'
    return
  }
  payments.value.push({ amount: value, method: order.value.payMode === 'Transfer' ? 'Transfer' : 'Cash' })
  amount.value = null
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

function selectCustomer(item: Customer | null) {
  if (busy.value) return
  order.value.customer = item
  closeDetails(customerPicker.value)
}

async function createAndSelectCustomer() {
  if (busy.value || !newCustomerName.value.trim()) return
  const target = order.value
  creatingCustomer.value = true
  customerError.value = ''
  try {
    const created = await props.createCustomer(
      newCustomerName.value.trim(),
      newCustomerPhone.value.trim() || null,
    )
    if (locked.value) return
    target.customer = created
    newCustomerName.value = ''
    newCustomerPhone.value = ''
    closeDetails(createPicker.value)
  } catch (reason) {
    customerError.value = reason instanceof Error
      ? 'Không thể tạo khách hàng. ' + reason.message
      : 'Không thể tạo khách hàng. Vui lòng thử lại.'
  } finally {
    creatingCustomer.value = false
  }
}

function switchOrder(number: number) {
  if (busy.value) return
  book.activate(number)
  resetFeedback()
  closeDetails(queueMenu.value)
}

function newOrder() {
  if (busy.value) return
  book.create()
  resetFeedback()
}

function holdOrder() {
  if (busy.value || !cart.value.length) return
  const held = order.value.number
  book.create()
  resetFeedback()
  notice.value = `Đã giữ Đơn ${held}. Chọn lại đơn trên thanh đơn hàng để tiếp tục.`
}

function clearOrder() {
  if (busy.value) return
  book.clearActive()
  selectedProductId.value = null
  splitOpen.value = false
  amount.value = null
  resetFeedback()
}

function invoiceDiscount() {
  resetFeedback()
  notice.value = 'Giảm giá hóa đơn sẽ dùng được khi backend hỗ trợ giảm giá.'
}

function openHistory(event: MouseEvent) {
  if (!router) return
  event.preventDefault()
  void router.push('/sales')
}

function finish(sale: Sale) {
  state.value = 'completed'
  book.completeActive()
  emit('completed', sale)
}

async function complete() {
  if (props.previewOnly) {
    message.value = 'Đây là dữ liệu mẫu để xem giao diện. Tắt Dữ liệu mẫu trên header để bán hàng thật.'
    return
  }
  if (state.value === 'completing' || state.value === 'checking' || state.value === 'completed') return
  if (creatingCustomer.value) return
  resetFeedback()

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
      payments: effectivePayments.value.map(payment => ({ ...payment })),
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
    finish(sale)
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
        finish(await props.loadSale(operation.resultReference))
        return
      }
    } catch { /* Keep the exact attempt because the outcome remains ambiguous. */ }

    state.value = 'retryable'
    message.value = 'Chưa xác định được kết quả. Giỏ hàng, khách hàng và thanh toán đã được khóa để thử lại đúng thao tác.'
  }
}

function onKeydown(event: KeyboardEvent) {
  // Both the live and the preview form can be mounted; only the visible one reacts.
  if (event.key !== 'F12' || !root.value?.offsetParent) return
  event.preventDefault()
  void complete()
}

function onPointerDown(event: PointerEvent) {
  root.value?.querySelectorAll('details[open]').forEach(details => {
    if (!details.contains(event.target as Node)) details.removeAttribute('open')
  })
}

onMounted(() => {
  window.addEventListener('keydown', onKeydown)
  document.addEventListener('pointerdown', onPointerDown)
  void findProducts(1)
})
onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
  document.removeEventListener('pointerdown', onPointerDown)
})
defineExpose({ state, attempt, cart, payments, customer, total, paid, outstanding })
</script>

<template>
  <div ref="root" class="sales-pos no-print" :class="{ 'sales-pos--demo': previewOnly }">
    <section class="sales-pos__products" :aria-labelledby="`sales-products-heading${idSuffix}`">
      <h2 :id="`sales-products-heading${idSuffix}`" class="sales-pos__sr-only">Chọn sản phẩm</h2>

      <!-- Multiple/held working orders are a D-106 visual; live Sales has one active order (D-107). -->
      <div v-if="previewOnly" class="sales-pos__order-rail" role="group" aria-label="Các đơn đang bán">
        <button
          v-for="(item, index) in railOrders"
          :key="item.number"
          class="sales-pos__order-pill"
          :class="{ 'is-active': item === order }"
          type="button"
          :aria-pressed="item === order"
          :aria-label="`Đơn ${item.number}: ${item.cart.length} sản phẩm, ${money(orderTotal(item))} đồng`"
          :disabled="busy"
          @click="switchOrder(item.number)"
        >
          <span v-if="item === order" class="sales-pos__order-badge">{{ item.number }}</span>
          <span class="sales-pos__pill-main">
            <span class="sales-pos__pill-icon"><LineIcon :name="item === order ? 'cart' : index % 2 ? 'bag' : 'box'" class="sales-pos__icon-sm" /></span>
            <span class="sales-pos__pill-text">
              <span class="sales-pos__pill-title">Đơn {{ item.number }}</span>
              <span class="sales-pos__pill-meta">{{ item.cart.length }} SP ・ {{ money(orderTotal(item)) }} đ</span>
            </span>
          </span>
          <span class="sales-pos__pill-more" aria-hidden="true"><LineIcon v-if="item === order" name="kebab" class="sales-pos__icon-sm" /><template v-else>⋮</template></span>
        </button>
        <span v-for="slot in 3 - railOrders.length" :key="`slot-${slot}`" aria-hidden="true" />
        <button class="sales-pos__ghost-pill" type="button" :disabled="busy" @click="newOrder"><LineIcon name="plus" class="sales-pos__icon-sm" /> Đơn mới</button>
        <details ref="queueMenu" class="sales-pos__queue">
          <summary class="sales-pos__order-pill sales-pos__queue-pill">
            <span class="sales-pos__pill-icon"><LineIcon name="clock" class="sales-pos__icon-sm" /></span>
            Danh sách đơn đang chờ ({{ orders.length }})
          </summary>
          <div class="sales-pos__dropdown sales-pos__dropdown--end">
            <button
              v-for="item in orders"
              :key="item.number"
              class="sales-pos__queue-item"
              :class="{ 'is-active': item === order }"
              type="button"
              :disabled="busy"
              @click="switchOrder(item.number)"
            ><strong>Đơn {{ item.number }}</strong><span>{{ item.cart.length }} SP ・ {{ money(orderTotal(item)) }} đ</span></button>
            <a class="sales-pos__queue-history" href="/sales" @click="openHistory">Lịch sử bán hàng</a>
          </div>
        </details>
      </div>

      <div v-if="allowNegativeStock" class="sales-pos__warning" role="note">
        <strong>Lưu ý về tồn kho</strong>
        <span>Cửa hàng đang cho phép bán âm tồn. Giá vốn có thể ở trạng thái ước tính hoặc chưa xác định.</span>
      </div>

      <form class="sales-pos__search" role="search" @submit.prevent="findProducts(1)">
        <label :for="`sales-product-search${idSuffix}`" class="sales-pos__sr-only">Tìm hoặc quét sản phẩm</label>
        <div class="sales-pos__search-box">
          <LineIcon name="search" />
          <input
            :id="`sales-product-search${idSuffix}`"
            ref="searchInput"
            v-model="productSearch"
            type="text"
            autocomplete="off"
            aria-label="Tìm hoặc quét sản phẩm"
            placeholder="Tìm sản phẩm theo tên, mã SKU hoặc quét mã vạch..."
            :disabled="locked"
          />
          <span class="sales-pos__barcode-chip" aria-hidden="true"><LineIcon name="barcode" class="sales-pos__icon-sm" /></span>
        </div>
        <button class="sales-pos__tool-btn" type="button" :disabled="locked" @click="quickAdd"><LineIcon name="plus" class="sales-pos__icon-sm" /> Thêm nhanh</button>
        <button class="sales-pos__tool-btn sales-pos__tool-btn--scan" type="button" :disabled="locked" @click="focusSearch"><LineIcon name="scan" class="sales-pos__icon-sm" />Quét mã</button>
      </form>

      <div v-if="previewOnly" class="sales-pos__categories" role="group" aria-label="Nhóm sản phẩm">
        <button
          v-for="item in productCategories"
          :key="item"
          class="sales-pos__chip"
          :class="{ 'is-active': category === item }"
          type="button"
          :aria-pressed="category === item"
          @click="category = item"
        >{{ item }}</button>
      </div>

      <p v-if="productLoading" class="sales-pos__empty" role="status">Đang tìm sản phẩm…</p>
      <p v-else-if="productError" class="error" role="alert">{{ productError }}</p>
      <p v-else-if="products && products.items.length === 0" class="sales-pos__empty" role="status">Không tìm thấy sản phẩm phù hợp.</p>
      <p v-else-if="!products" class="sales-pos__empty" role="status">Nhập tên, SKU hoặc barcode để tìm sản phẩm.</p>
      <p v-else-if="shownProducts.length === 0" class="sales-pos__empty" role="status">Không có sản phẩm thuộc nhóm “{{ category }}” trong kết quả hiện tại.</p>
      <div v-else class="sales-pos__product-grid" aria-live="polite">
        <article v-for="product in shownProducts" :key="product.id" class="sales-pos__product-card" :class="{ 'is-selected': selectedProductId === product.id }">
          <div class="sales-pos__product-image">
            <img v-if="productImage(product)" :src="productImage(product)!" :alt="product.name" />
            <svg v-else class="sales-pos__placeholder" aria-hidden="true" viewBox="0 0 80 80" fill="none"><rect x="17" y="21" width="46" height="42" rx="7" fill="#EAF3F1" /><path d="M17 32h46M30 21v42m22-42v42" stroke="#93B9AA" stroke-width="3" /><path d="M35 43h10" stroke="#0AA06A" stroke-width="3" stroke-linecap="round" /></svg>
          </div>
          <div class="sales-pos__product-body">
            <h3 class="sales-pos__product-name" :title="product.name">{{ product.name }}</h3>
            <p class="sales-pos__product-meta">{{ product.sku }} ・ {{ unitLabel(product.unit) }}</p>
            <p class="sales-pos__product-price">{{ money(product.salePrice) }} đ</p>
            <div class="sales-pos__product-footer">
              <span
                v-if="previewOnly && stockStatus(product.quantityOnHand)"
                class="sales-pos__stock-tag"
                :class="`is-${stockStatus(product.quantityOnHand)!.tone}`"
                :title="`Tồn ${money(product.quantityOnHand)} ${product.unit}`"
              >{{ stockStatus(product.quantityOnHand)!.label }}</span>
              <span
                v-else-if="!previewOnly && factualStockState(product.quantityOnHand) !== 'available'"
                class="sales-pos__stock-tag is-danger"
              >{{ factualStockState(product.quantityOnHand) === 'out' ? 'Hết hàng' : 'Tồn âm' }} · {{ money(product.quantityOnHand) }} {{ product.unit }}</span>
              <span v-else class="sales-pos__product-stock">Còn {{ money(product.quantityOnHand) }} {{ product.unit }}</span>
              <button class="sales-pos__add-btn" type="button" :disabled="locked" :aria-label="'Thêm sản phẩm ' + product.name" @click="addProduct(product)">+</button>
            </div>
          </div>
        </article>
      </div>
      <nav v-if="products && products.totalPages > 1" class="sales-pos__pager" aria-label="Trang sản phẩm">
        <button type="button" :disabled="productPage <= 1 || locked || productLoading" @click="findProducts(productPage - 1)">Trước</button>
        <span>Trang {{ productPage }}/{{ products.totalPages }}</span>
        <button type="button" :disabled="productPage >= products.totalPages || locked || productLoading" @click="findProducts(productPage + 1)">Sau</button>
      </nav>
    </section>

    <section class="sales-pos__checkout" :aria-labelledby="`sales-checkout-heading${idSuffix}`">
      <div class="sales-pos__checkout-top">
        <div class="sales-pos__checkout-title">
          <h2 :id="`sales-checkout-heading${idSuffix}`">Đơn {{ order.number }}</h2>
          <span class="sales-pos__status-chip">Đang bán</span>
        </div>
        <div class="sales-pos__checkout-tools">
          <div v-if="previewOnly" class="sales-pos__muted-row"><LineIcon name="kebab" class="sales-pos__icon-sm" /></div>
          <button class="sales-pos__danger-link" type="button" :disabled="busy || !cart.length" @click="clearOrder"><LineIcon name="trash" class="sales-pos__icon-sm" /> Xóa đơn</button>
        </div>
      </div>

      <div class="sales-pos__customer-head">
        <span class="sales-pos__field-label">Khách hàng</span>
        <span v-if="customerRequired" class="sales-pos__required-tag">Bắt buộc khi còn nợ</span>
      </div>
      <div class="sales-pos__field-row">
        <details ref="customerPicker" class="sales-pos__customer-picker">
          <summary class="sales-pos__select" :class="{ 'is-required': customerRequired && !customer }" :aria-label="`Khách hàng: ${customer?.name ?? 'Khách lẻ'}`">
            <span class="sales-pos__select-value">{{ customer?.name ?? 'Khách lẻ' }}</span>
            <LineIcon name="chevron" class="sales-pos__icon-sm" />
          </summary>
          <div class="sales-pos__dropdown sales-pos__dropdown--row">
            <form class="sales-pos__dropdown-search" @submit.prevent="findCustomers(1)">
              <label :for="`sales-customer-search${idSuffix}`" class="sales-pos__sr-only">Tìm khách hàng</label>
              <input :id="`sales-customer-search${idSuffix}`" v-model="customerSearch" class="sales-pos__control" type="text" autocomplete="off" aria-label="Tìm khách hàng" placeholder="Tên hoặc số điện thoại" :disabled="locked || creatingCustomer" />
              <button class="sales-pos__control-btn" type="submit" :disabled="locked || creatingCustomer || customerLoading">Tìm khách hàng</button>
            </form>
            <p v-if="customerLoading" class="sales-pos__hint" role="status">Đang tìm khách hàng…</p>
            <p v-else-if="customerError" class="sales-pos__hint sales-pos__hint--error" role="alert">{{ customerError }}</p>
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
              ><strong>{{ item.name }}</strong><span>{{ item.phone || 'Không có số điện thoại' }}</span></button>
            </div>
            <p v-else class="sales-pos__hint">Tìm theo tên hoặc số điện thoại để ghi công nợ.</p>
            <nav v-if="customers && customers.totalPages > 1" class="sales-pos__pager" aria-label="Trang khách hàng">
              <button type="button" :disabled="customerPage <= 1 || locked || creatingCustomer || customerLoading" @click="findCustomers(customerPage - 1)">Trước</button>
              <span>Trang {{ customerPage }}/{{ customers.totalPages }}</span>
              <button type="button" :disabled="customerPage >= customers.totalPages || locked || creatingCustomer || customerLoading" @click="findCustomers(customerPage + 1)">Sau</button>
            </nav>
            <button v-if="customer" class="sales-pos__walk-in" type="button" :disabled="locked || creatingCustomer" @click="selectCustomer(null)">Chuyển về Khách lẻ</button>
          </div>
        </details>
        <details ref="createPicker" class="sales-pos__create-customer">
          <summary class="sales-pos__add-customer-btn"><LineIcon name="plus" class="sales-pos__icon-sm" /> Thêm khách</summary>
          <div class="sales-pos__dropdown sales-pos__dropdown--end">
            <label :for="`sales-new-customer-name${idSuffix}`">Tên khách hàng</label>
            <input :id="`sales-new-customer-name${idSuffix}`" v-model="newCustomerName" class="sales-pos__control" aria-label="Tên khách hàng mới" placeholder="Tên khách hàng mới" :disabled="locked || creatingCustomer" />
            <label :for="`sales-new-customer-phone${idSuffix}`">Số điện thoại (không bắt buộc)</label>
            <input :id="`sales-new-customer-phone${idSuffix}`" v-model="newCustomerPhone" class="sales-pos__control" type="tel" aria-label="Số điện thoại khách hàng mới" placeholder="Số điện thoại" :disabled="locked || creatingCustomer" />
            <button class="sales-pos__control-btn" type="button" :disabled="locked || creatingCustomer || !newCustomerName.trim()" @click="createAndSelectCustomer">{{ creatingCustomer ? 'Đang tạo khách hàng…' : 'Tạo và chọn khách hàng' }}</button>
          </div>
        </details>
      </div>

      <!-- Outstanding makes the Customer a business requirement; a fully paid Sale keeps Khách lẻ (D-107). -->
      <p v-if="customerRequired" class="sales-pos__debt-note" :class="{ 'is-missing': !customer }" role="note">
        <strong>Còn nợ {{ money(outstanding) }} đ.</strong>
        {{ customer ? `Ghi nhận công nợ ${money(outstanding)} đ cho ${customer.name}.` : `Chọn khách hàng để ghi nhận công nợ ${money(outstanding)} đ.` }}
      </p>

      <template v-if="previewOnly">
        <label class="sales-pos__field-label" :for="`sales-order-note${idSuffix}`">Ghi chú đơn hàng <span class="sales-pos__optional">(tùy chọn)</span></label>
        <input :id="`sales-order-note${idSuffix}`" v-model="order.note" class="sales-pos__note" type="text" autocomplete="off" placeholder="Thêm ghi chú..." :disabled="locked" />
      </template>

      <div class="sales-pos__order-items" aria-label="Sản phẩm trong giỏ">
        <p v-if="cart.length === 0" class="sales-pos__cart-empty">Chưa có sản phẩm. Tìm hoặc quét sản phẩm ở bên trái để bắt đầu đơn bán.</p>
        <template v-for="line in cart" :key="line.product.id">
          <div class="sales-pos__line-item">
            <img v-if="productImage(line.product)" class="sales-pos__line-image" :src="productImage(line.product)!" :alt="line.product.name" />
            <span v-else class="sales-pos__line-image sales-pos__line-image--empty" aria-hidden="true"><LineIcon name="box" /></span>
            <div class="sales-pos__line-main">
              <div class="sales-pos__line-name">{{ line.product.name }}</div>
              <div class="sales-pos__line-price">{{ money(line.product.salePrice) }} đ</div>
              <div class="sales-pos__qty-box">
                <button type="button" :disabled="locked" :aria-label="'Giảm số lượng ' + line.product.name" @click="changeQuantity(line, -1)">−</button>
                <input
                  :id="`sales-quantity-${line.product.id}`"
                  v-model.number="line.quantity"
                  type="number"
                  min="0.001"
                  step="0.001"
                  inputmode="decimal"
                  :aria-label="'Số lượng ' + line.product.name"
                  :disabled="locked"
                />
                <button type="button" :disabled="locked" :aria-label="'Tăng số lượng ' + line.product.name" @click="changeQuantity(line, 1)">+</button>
              </div>
            </div>
            <div class="sales-pos__line-side">
              <button class="sales-pos__remove-mini" type="button" :disabled="locked" :aria-label="'Xóa ' + line.product.name" @click="removeProduct(line.product.id)"><LineIcon name="trashMini" class="sales-pos__icon-sm" /></button>
              <div class="sales-pos__line-amount">{{ money(lineAmount(line)) }} đ</div>
            </div>
          </div>
          <div v-if="previewOnly && line.discountPercent" class="sales-pos__line-discount">
            <span>🏷 Giảm giá</span>
            <span>{{ line.discountPercent }}% &nbsp; -{{ money(lineDiscount(line)) }} đ ›</span>
          </div>
        </template>
      </div>

      <button v-if="previewOnly" class="sales-pos__hint-card" type="button" :disabled="locked" @click="invoiceDiscount"><LineIcon name="plus" class="sales-pos__icon-sm" /> Thêm giảm giá hóa đơn</button>

      <div class="sales-pos__totals">
        <div class="sales-pos__totals-row"><span>Tạm tính ({{ money(itemCount) }} sản phẩm)</span><strong>{{ money(subtotal) }} đ</strong></div>
        <template v-if="previewOnly">
          <div class="sales-pos__totals-row"><span>Giảm giá sản phẩm</span><strong>{{ discount ? '-' : '' }}{{ money(discount) }} đ</strong></div>
          <div class="sales-pos__totals-row"><span>Giảm giá hóa đơn</span><strong>0 đ ›</strong></div>
        </template>
      </div>

      <div class="sales-pos__grand-total">
        <h3>Tổng cộng</h3>
        <div class="sales-pos__grand-value">{{ money(total) }} đ</div>
      </div>

      <div class="sales-pos__pay-head">
        <h3 :id="`sales-payment-heading${idSuffix}`" class="sales-pos__pay-title">Hình thức thanh toán</h3>
        <button v-if="!payments.length && !previewOnly" class="sales-pos__split-toggle" type="button" :disabled="locked" :aria-expanded="splitShown" :aria-controls="`sales-split${idSuffix}`" @click="splitOpen = !splitShown">{{ splitShown ? 'Thu đủ' : 'Nhập số tiền' }}</button>
      </div>
      <div class="sales-pos__payment-grid" :class="{ 'sales-pos__payment-grid--two': payModes.length === 2 }" role="group" :aria-labelledby="`sales-payment-heading${idSuffix}`">
        <button
          v-for="mode in payModes"
          :key="mode.id"
          class="sales-pos__pay-btn"
          :class="{ 'is-active': order.payMode === mode.id }"
          type="button"
          :aria-pressed="order.payMode === mode.id"
          :disabled="locked"
          @click="setPayMode(mode.id)"
        ><LineIcon :name="mode.icon" class="sales-pos__icon-sm" /> {{ mode.label }}</button>
      </div>
      <div v-show="splitShown" :id="`sales-split${idSuffix}`" class="sales-pos__split">
        <div class="sales-pos__split-row">
          <label :for="`sales-payment-amount${idSuffix}`" class="sales-pos__sr-only">Số tiền thanh toán</label>
          <input
            :id="`sales-payment-amount${idSuffix}`"
            v-model.number="amount"
            class="sales-pos__control"
            type="number"
            min="0.01"
            step="0.01"
            inputmode="decimal"
            aria-label="Số tiền thanh toán"
            :placeholder="`Khách trả (${order.payMode === 'Transfer' ? 'Chuyển khoản' : 'Tiền mặt'})`"
            :disabled="locked"
          />
          <button class="sales-pos__control-btn" type="button" :disabled="locked" @click="addPayment">Thêm thanh toán</button>
        </div>
        <ul v-if="payments.length" class="sales-pos__split-list" aria-label="Các khoản đã nhập">
          <li v-for="(payment, index) in payments" :key="index">
            <span>{{ methodLabel(payment.method) }}</span>
            <strong>{{ money(payment.amount) }} đ</strong>
            <button type="button" :disabled="locked" :aria-label="'Xóa khoản thanh toán ' + (index + 1)" @click="removePayment(index)">Xóa</button>
          </li>
        </ul>
        <p class="sales-pos__split-summary" aria-live="polite">Khách trả <strong>{{ money(paid) }} đ</strong> · Còn nợ <strong>{{ money(outstanding) }} đ</strong></p>
        <p class="sales-pos__hint">Chỉ các khoản đã nhập được ghi nhận là tiền thu; phần chưa thanh toán ghi công nợ cho khách hàng.</p>
      </div>

      <p v-if="notice" class="sales-pos__notice" role="status">{{ notice }}</p>
      <p v-if="message" class="sales-pos__message" role="alert">{{ message }}</p>

      <div class="sales-pos__actions" :class="{ 'sales-pos__actions--single': !previewOnly }">
        <button v-if="previewOnly" class="sales-pos__secondary-btn" type="button" :disabled="busy || !cart.length" @click="holdOrder"><LineIcon name="clock" class="sales-pos__icon-sm" /> Giữ đơn</button>
        <button
          class="sales-pos__primary-btn sales-pos__complete"
          type="button"
          :data-shortcut="state === 'idle' ? 'F12' : undefined"
          :disabled="state === 'completing' || state === 'checking' || state === 'completed' || creatingCustomer"
          @click="complete"
        ><LineIcon v-if="state === 'idle'" name="check" class="sales-pos__icon-sm" />{{ state === 'retryable' ? 'Thử lại đúng thao tác' : state === 'completing' || state === 'checking' ? 'Đang xác nhận…' : 'Hoàn tất bán hàng' }}</button>
      </div>
    </section>
  </div>
</template>

<style scoped>
/* Proportions follow TemplateHTML/Sales/index.html (approved 1536×1024 visual reference). */
.sales-pos {
  --pos-border: #e3e9ef;
  --pos-border-strong: #d1dce5;
  --pos-text: #151d2b;
  --pos-subtle: #7f8ba0;
  --pos-primary: #0aa06a;
  --pos-primary-strong: #078b5d;
  display: grid;
  max-width: 1536px;
  grid-template-columns: minmax(0, 1fr) 404px;
  align-items: start;
  gap: 14px;
  margin: 0 auto;
  color: var(--pos-text);
  font-size: 16px;
  line-height: normal;
}
.sales-pos button,
.sales-pos summary { cursor: pointer; transition: border-color 0.14s ease, background-color 0.14s ease, box-shadow 0.14s ease, transform 0.14s ease; }
.sales-pos button:disabled { cursor: not-allowed; }
.sales-pos summary { list-style: none; }
.sales-pos summary::-webkit-details-marker { display: none; }
/* Like the reference icons, these may shrink when a label wraps inside a narrow button. */
.sales-pos :deep(.line-icon) { width: 20px; height: 20px; flex: 0 1 auto; }
.sales-pos :deep(.sales-pos__icon-sm) { width: 18px; height: 18px; }
.sales-pos__sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
}
.sales-pos__products { min-width: 0; }

.sales-pos__order-rail {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 176px)) 118px minmax(188px, 1fr);
  gap: 10px;
  margin-bottom: 14px;
}
.sales-pos__order-pill,
.sales-pos__ghost-pill {
  position: relative;
  display: flex;
  height: 56px;
  min-width: 0;
  align-items: center;
  justify-content: space-between;
  border: 1.4px solid var(--pos-border-strong);
  border-radius: 10px;
  background: #fff;
  padding: 10px 14px;
  box-shadow: 0 1px 0 rgb(12 26 53 / 2%);
  color: var(--pos-text);
  text-align: left;
}
.sales-pos__order-pill.is-active {
  border-color: rgb(10 163 107 / 70%);
  background: linear-gradient(180deg, #f3fbf7, #eef9f4);
  box-shadow: inset 0 0 0 1px rgb(10 163 107 / 18%);
}
.sales-pos__order-pill:disabled:not(.is-active) { opacity: 0.6; }
.sales-pos__order-badge {
  position: absolute;
  top: -14px;
  left: -14px;
  display: grid;
  width: 30px;
  height: 30px;
  place-items: center;
  border: 3px solid #fff;
  border-radius: 999px;
  background: #ff2740;
  box-shadow: 0 8px 20px rgb(255 39 64 / 25%);
  color: #fff;
  font-weight: 800;
}
.sales-pos__pill-main { display: flex; min-width: 0; align-items: center; gap: 10px; }
.sales-pos__pill-icon {
  display: grid;
  width: 28px;
  height: 28px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 8px;
  background: #e8efff;
  color: #4258c9;
}
.sales-pos__order-pill.is-active .sales-pos__pill-icon { background: #0fa96f; color: #fff; }
.sales-pos__pill-text { display: block; min-width: 0; }
.sales-pos__pill-title { display: block; font-size: 14px; font-weight: 800; line-height: 1.15; }
.sales-pos__pill-meta { display: block; margin-top: 3px; color: #66758a; font-size: 12px; white-space: nowrap; }
.sales-pos__pill-more { color: #60718a; }
/* The reference renders these icons inline, so they sit on a text line box. */
.sales-pos__pill-more :deep(.line-icon),
.sales-pos__remove-mini :deep(.line-icon) { display: inline; vertical-align: baseline; }
.sales-pos__ghost-pill {
  justify-content: center;
  gap: 8px;
  border-color: rgb(10 163 107 / 28%);
  background: linear-gradient(180deg, #fff, #fbfffd);
  color: #0d9f67;
  font-weight: 800;
  text-align: left;
}
.sales-pos__queue { position: relative; min-width: 0; }
.sales-pos__queue-pill {
  justify-content: flex-start;
  gap: 10px;
  border-color: rgb(51 84 209 / 20%);
  background: #fbfcff;
  color: #3044a4;
  font-weight: 700;
}
.sales-pos__ghost-pill:hover:not(:disabled),
.sales-pos__tool-btn:hover:not(:disabled),
.sales-pos__pay-btn:hover:not(:disabled),
.sales-pos__add-customer-btn:hover { border-color: rgb(10 163 107 / 48%); background-color: #f8fdfa; }
.sales-pos__queue-item {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 10px;
  border-radius: 9px;
  padding: 9px 10px;
  font-size: 14px;
  text-align: left;
}
.sales-pos__queue-item span { color: #66758a; font-size: 12.5px; }
.sales-pos__queue-item:hover:not(:disabled),
.sales-pos__queue-item.is-active { background: #eff9f3; }
.sales-pos__queue-history { border-top: 1px solid var(--pos-border); padding: 10px 10px 2px; color: #3044a4; font-size: 13.5px; font-weight: 700; text-decoration: none; }
.sales-pos__queue-history:hover { text-decoration: underline; }

.sales-pos__warning {
  display: grid;
  gap: 2px;
  margin-bottom: 14px;
  border: 1px solid #f3ddb1;
  border-radius: 10px;
  background: #fff8ea;
  padding: 9px 12px;
  color: #9d6b06;
  font-size: 13px;
  line-height: 1.45;
}

.sales-pos__search {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 168px 72px;
  align-items: stretch;
  gap: 12px;
  margin-bottom: 16px;
}
.sales-pos__search-box {
  display: flex;
  height: 52px;
  min-width: 0;
  align-items: center;
  gap: 14px;
  border: 2px solid rgb(92 117 255 / 26%);
  border-radius: 10px;
  background: #fff;
  padding: 0 16px;
  box-shadow: 0 6px 16px rgb(61 84 179 / 6%);
}
.sales-pos__search-box:focus-within { border-color: rgb(77 103 238 / 45%); box-shadow: 0 0 0 3px rgb(77 103 238 / 7%); }
.sales-pos__search-box input {
  min-width: 0;
  flex: 1;
  border: 0;
  outline: none;
  background: transparent;
  padding: 1px 2px;
  color: var(--pos-text);
  font-size: 14px;
}
.sales-pos__search-box input::placeholder { color: var(--pos-text); opacity: 1; }
.sales-pos__barcode-chip {
  display: grid;
  width: 42px;
  height: 34px;
  flex: none;
  place-items: center;
  border: 1px solid var(--pos-border);
  border-radius: 10px;
  background: #fff;
  color: #405173;
}
.sales-pos__tool-btn {
  display: flex;
  height: 52px;
  align-items: center;
  justify-content: center;
  gap: 10px;
  border: 1.4px solid rgb(10 163 107 / 24%);
  border-radius: 10px;
  background: #fff;
  padding: 1px 6px;
  color: #118458;
  font-size: 14px;
  font-weight: 800;
}
.sales-pos__tool-btn--scan { flex-direction: column; gap: 5px; background: #f4fbf7; color: #16885d; font-size: 12px; }

.sales-pos__categories { display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 16px; }
.sales-pos__chip {
  display: inline-flex;
  height: 38px;
  align-items: center;
  border: 1px solid var(--pos-border-strong);
  border-radius: 999px;
  background: #fff;
  padding: 0 16px;
  color: #2d3e54;
  font-weight: 700;
}
.sales-pos__chip:hover:not(.is-active) { border-color: rgb(10 163 107 / 48%); }
.sales-pos__chip.is-active {
  border-color: transparent;
  background: linear-gradient(180deg, #12a86f, #09935e);
  box-shadow: 0 10px 18px rgb(10 163 107 / 20%);
  color: #fff;
}

.sales-pos__empty {
  border: 1px dashed var(--pos-border-strong);
  border-radius: 14px;
  background: #fff;
  padding: 40px 16px;
  color: #4e607a;
  font-size: 14px;
  text-align: center;
}
.sales-pos__product-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }
.sales-pos__product-card {
  display: flex;
  height: 236px;
  min-width: 0;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid var(--pos-border);
  border-radius: 14px;
  background: #fff;
  box-shadow: 0 1px 0 rgb(20 38 63 / 2%);
  transition: border-color 0.14s ease, box-shadow 0.14s ease, transform 0.14s ease;
}
.sales-pos__product-card:hover { border-color: #ccd9e0; box-shadow: 0 7px 18px rgb(25 48 72 / 5.5%); transform: translateY(-1px); }
.sales-pos__product-card.is-selected {
  border: 2px solid rgb(10 163 107 / 78%);
  background: linear-gradient(180deg, #fafdfe 0%, #f8fcfb 100%);
  box-shadow: 0 12px 24px rgb(10 163 107 / 8%);
}
.sales-pos__product-image {
  display: grid;
  height: 104px;
  place-items: center;
  overflow: hidden;
  background: linear-gradient(180deg, #f8fafc, #fbfcfd);
}
.sales-pos__product-image img { display: block; width: auto; max-width: 100%; height: 100%; object-fit: contain; }
.sales-pos__placeholder { width: 72px; height: 72px; }
/* As in the reference, the image band shrinks so the text body always fits the 236px card. */
.sales-pos__product-body { display: flex; flex: 1; flex-direction: column; gap: 5px; padding: 10px 16px 12px; }
.sales-pos__product-name {
  display: -webkit-box;
  min-height: 38px;
  overflow: hidden;
  font-size: 15.5px;
  font-weight: 780;
  line-height: 1.22;
  overflow-wrap: anywhere;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}
.sales-pos__product-meta { color: #637493; font-size: 12.5px; }
.sales-pos__product-price { font-size: 16px; font-weight: 820; }
.sales-pos__product-footer { display: flex; align-items: center; justify-content: space-between; margin-top: 4px; }
.sales-pos__product-stock { margin-top: auto; color: #596a82; font-size: 12.5px; }
.sales-pos__stock-tag { display: inline-flex; align-items: center; border-radius: 10px; padding: 6px 10px; font-size: 12px; font-weight: 800; }
.sales-pos__stock-tag.is-warn { background: #fff6e6; color: #f7a531; }
.sales-pos__stock-tag.is-danger { background: #ffecec; color: #eb5a49; }
.sales-pos__add-btn {
  display: grid;
  width: 38px;
  height: 38px;
  flex: none;
  place-items: center;
  border: 0;
  border-radius: 10px;
  background: #e6f7ee;
  padding: 1px 6px;
  color: #0aa36b;
  font-size: 30px;
  font-weight: 500;
  line-height: 0;
}
.sales-pos__add-btn:hover:not(:disabled) { background: #d9f3e6; transform: translateY(-1px); }
.sales-pos__add-btn:disabled { opacity: 0.5; }
.sales-pos__pager { display: flex; align-items: center; justify-content: flex-end; gap: 10px; margin-top: 12px; color: #4e607a; font-size: 13px; }
.sales-pos__pager button {
  height: 34px;
  border: 1px solid var(--pos-border-strong);
  border-radius: 9px;
  background: #fff;
  padding: 0 12px;
  color: #2c3d54;
  font-weight: 700;
}
.sales-pos__pager button:disabled { opacity: 0.5; }

/* Natural height like the reference; the flex column uses its collapsed block margins. */
.sales-pos__checkout {
  position: sticky;
  top: 70px;
  display: flex;
  min-width: 0;
  flex-direction: column;
  border: 1px solid var(--pos-border);
  border-radius: 16px;
  background: #fff;
  padding: 15px 18px 16px;
  box-shadow: 0 7px 22px rgb(16 39 68 / 5%);
}
.sales-pos__checkout > * { flex: none; }
.sales-pos__checkout-top { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; margin-bottom: 12px; }
.sales-pos__checkout-title { display: flex; align-items: center; gap: 8px; font-size: 18px; font-weight: 850; }
.sales-pos__status-chip {
  display: inline-flex;
  height: 24px;
  align-items: center;
  border-radius: 999px;
  background: #edf9f3;
  padding: 0 9px;
  color: #0d8b5b;
  font-size: 11px;
  font-weight: 800;
}
.sales-pos__checkout-tools { text-align: right; }
.sales-pos__muted-row { display: flex; justify-content: flex-end; gap: 12px; margin-bottom: 10px; color: #43546c; }
.sales-pos__danger-link { display: inline-flex; align-items: center; gap: 8px; color: #ed4343; font-weight: 800; }
.sales-pos__danger-link:disabled { opacity: 0.55; }
.sales-pos__field-label { display: block; margin-bottom: 7px; font-size: 13.5px; font-weight: 760; }
.sales-pos__customer-head { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; }
.sales-pos__required-tag { border-radius: 999px; background: #fff3dc; padding: 2px 8px; color: #9d6b06; font-size: 11.5px; font-weight: 800; white-space: nowrap; }
.sales-pos__optional { color: var(--pos-subtle); font-weight: 500; }
.sales-pos__field-row { display: grid; grid-template-columns: minmax(0, 1fr) 126px; gap: 10px; margin-bottom: 12px; }
.sales-pos__customer-picker,
.sales-pos__create-customer { position: relative; min-width: 0; }
.sales-pos__select,
.sales-pos__note,
.sales-pos__add-customer-btn {
  display: flex;
  height: 38px;
  align-items: center;
  justify-content: space-between;
  border: 1px solid var(--pos-border-strong);
  border-radius: 9px;
  background: #f9fbfc;
  padding: 0 14px;
  color: #25354d;
}
.sales-pos__select { gap: 8px; }
.sales-pos__select.is-required { border-color: #efb44a; background: #fffaf0; box-shadow: 0 0 0 3px rgb(239 180 74 / 16%); }
.sales-pos__select-value { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.sales-pos__add-customer-btn {
  justify-content: center;
  gap: 8px;
  border-color: rgb(10 163 107 / 28%);
  background: #fbfffd;
  color: #0f915f;
  font-weight: 800;
}
.sales-pos__note { width: 100%; margin-bottom: 12px; }
.sales-pos__note::placeholder { color: #9aa6b8; }
.sales-pos__dropdown {
  position: absolute;
  z-index: 30;
  top: calc(100% + 6px);
  left: 0;
  display: grid;
  width: 100%;
  min-width: 260px;
  gap: 10px;
  border: 1px solid var(--pos-border);
  border-radius: 12px;
  background: #fff;
  padding: 12px;
  box-shadow: 0 16px 32px rgb(16 39 68 / 14%);
  font-size: 14px;
}
.sales-pos__dropdown--row { width: calc(100% + 136px); }
.sales-pos__dropdown--end { right: 0; left: auto; width: 300px; }
.sales-pos__dropdown label { color: #4e607a; font-size: 12.5px; font-weight: 700; }
.sales-pos__dropdown-search { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 8px; }
.sales-pos__control {
  width: 100%;
  height: 38px;
  min-width: 0;
  border: 1px solid var(--pos-border-strong);
  border-radius: 9px;
  background: #f9fbfc;
  padding: 0 12px;
  color: #25354d;
  font-size: 14px;
}
.sales-pos__control:focus { border-color: rgb(10 163 107 / 60%); background: #fff; }
.sales-pos__control::placeholder { color: #9aa6b8; }
.sales-pos__control-btn {
  height: 38px;
  border: 1px solid rgb(10 163 107 / 28%);
  border-radius: 9px;
  background: #fbfffd;
  padding: 0 12px;
  color: #0f915f;
  font-size: 13.5px;
  font-weight: 800;
  white-space: nowrap;
}
.sales-pos__control-btn:disabled,
.sales-pos__walk-in:disabled { opacity: 0.5; }
.sales-pos__hint { color: #4e607a; font-size: 12.5px; line-height: 1.4; }
.sales-pos__hint--error { color: #b42318; }
.sales-pos__customer-results { display: grid; max-height: 220px; gap: 4px; overflow-y: auto; }
.sales-pos__customer-result {
  display: grid;
  gap: 2px;
  border: 1px solid var(--pos-border);
  border-radius: 9px;
  padding: 8px 10px;
  text-align: left;
}
.sales-pos__customer-result span { color: #66758a; font-size: 12.5px; }
.sales-pos__customer-result:hover:not(:disabled) { border-color: rgb(10 163 107 / 48%); }
.sales-pos__customer-result[aria-pressed="true"] { border-color: rgb(10 163 107 / 60%); background: #eff9f3; color: #0a915f; }
.sales-pos__dropdown .sales-pos__pager { margin-top: 0; }
.sales-pos__walk-in { justify-self: start; color: #0f915f; font-size: 13px; font-weight: 700; }

.sales-pos__order-items {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 9px;
}
.sales-pos__cart-empty { border-radius: 10px; background: #f8fbfa; padding: 14px 12px; color: #4e607a; font-size: 13px; line-height: 1.45; }
.sales-pos__line-item { display: grid; flex: none; grid-template-columns: 66px minmax(0, 1fr) auto; align-items: center; gap: 10px; }
.sales-pos__line-image {
  width: 58px;
  height: 58px;
  border: 1px solid var(--pos-border);
  border-radius: 8px;
  background: #fafcfd;
  object-fit: cover;
}
.sales-pos__line-image--empty { display: grid; place-items: center; color: #93b9aa; }
.sales-pos__line-main { min-width: 0; }
.sales-pos__line-name { font-size: 13.5px; font-weight: 780; line-height: 1.25; overflow-wrap: anywhere; }
.sales-pos__line-price { margin-top: 4px; color: #4d5f78; font-size: 12px; }
.sales-pos__qty-box {
  display: grid;
  width: 116px;
  height: 34px;
  grid-template-columns: 1fr 1fr 1fr;
  align-items: center;
  overflow: hidden;
  margin-top: 7px;
  border: 1px solid var(--pos-border-strong);
  border-radius: 10px;
  background: #fff;
}
.sales-pos__qty-box button,
.sales-pos__qty-box input { display: grid; height: 100%; min-width: 0; place-items: center; font-weight: 800; }
.sales-pos__qty-box button:hover:not(:disabled) { background: #f1f5f4; }
.sales-pos__qty-box button:disabled { opacity: 0.5; }
.sales-pos__qty-box input {
  width: 100%;
  height: 21px;
  border-inline: 1px solid var(--pos-border);
  outline-offset: -3px;
  background: transparent;
  text-align: center;
  -moz-appearance: textfield;
}
.sales-pos__qty-box input::-webkit-outer-spin-button,
.sales-pos__qty-box input::-webkit-inner-spin-button { margin: 0; -webkit-appearance: none; }
.sales-pos__line-side { display: grid; justify-items: end; }
.sales-pos__remove-mini { display: block; margin-bottom: 4px; color: #6b7a91; }
.sales-pos__remove-mini:hover:not(:disabled) { color: #df4040; }
.sales-pos__line-amount { color: #1f3047; font-size: 15px; font-weight: 850; white-space: nowrap; }
.sales-pos__line-discount {
  display: flex;
  flex: none;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin: -4px 0 0 72px;
  border-radius: 10px;
  background: #fff2f2;
  padding: 8px 10px;
  color: #d94a4a;
  font-size: 12px;
  font-weight: 750;
}

.sales-pos__hint-card {
  display: flex;
  width: 100%;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
  border: 1px dashed #d4ddea;
  border-radius: 10px;
  background: #fcfffd;
  padding: 10px 12px;
  color: #129363;
  font-weight: 800;
  text-align: left;
}
.sales-pos__hint-card:hover:not(:disabled) { border-color: rgb(10 163 107 / 48%); }
.sales-pos__debt-note {
  margin-bottom: 14px;
  border: 1px solid #f3ddb1;
  border-radius: 14px;
  background: #fff8ea;
  padding: 10px 12px;
  color: #9d6b06;
  font-size: 13px;
  line-height: 1.45;
}
.sales-pos__debt-note strong { color: #7d5300; }
.sales-pos__debt-note.is-missing { border-color: #efb44a; }
.sales-pos__totals { border-top: 1px solid #ecf0f4; padding-top: 12px; }
.sales-pos__totals-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 8px;
  color: #344762;
  font-size: 13px;
}
.sales-pos__totals-row strong { color: #1b2e48; white-space: nowrap; }
.sales-pos__grand-total { display: flex; align-items: end; justify-content: space-between; gap: 10px; margin: 6px 0 12px; }
.sales-pos__grand-total h3 { font-size: 18px; font-weight: 850; }
.sales-pos__grand-value { color: var(--pos-primary-strong); font-size: 26px; font-weight: 900; letter-spacing: -0.02em; white-space: nowrap; }
.sales-pos__pay-head { display: flex; align-items: baseline; justify-content: space-between; gap: 10px; margin-bottom: 9px; }
.sales-pos__pay-title { font-size: 13.5px; font-weight: 760; }
.sales-pos__split-toggle { color: #118458; font-size: 12.5px; font-weight: 700; }
.sales-pos__split-toggle:hover { text-decoration: underline; }
.sales-pos__payment-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px; margin-bottom: 16px; }
.sales-pos__payment-grid--two { grid-template-columns: repeat(2, minmax(0, 1fr)); }
.sales-pos__pay-btn {
  display: flex;
  height: 44px;
  min-width: 0;
  align-items: center;
  justify-content: center;
  gap: 10px;
  border: 1.5px solid var(--pos-border-strong);
  border-radius: 9px;
  background: #fff;
  padding: 0 14px;
  color: #2c3d54;
  font-weight: 800;
  text-align: left;
}
.sales-pos__pay-btn.is-active { border-color: rgb(10 163 107 / 45%); background: #eff9f3; color: #0a915f; }
.sales-pos__pay-btn:disabled { opacity: 0.6; }
.sales-pos__split { display: grid; gap: 8px; margin: -6px 0 14px; }
.sales-pos__split-row { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 8px; }
.sales-pos__split-list { display: grid; gap: 4px; }
.sales-pos__split-summary { color: #344762; font-size: 13px; }
.sales-pos__split-summary strong { color: #1b2e48; }
.sales-pos__split-list li {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 10px;
  border-radius: 9px;
  background: #f8fbfa;
  padding: 5px 6px 5px 10px;
  font-size: 13px;
}
.sales-pos__split-list button { border-radius: 7px; padding: 4px 8px; color: #6b7a91; font-size: 12.5px; font-weight: 700; }
.sales-pos__split-list button:hover:not(:disabled) { background: #fff0f0; color: #df4040; }
.sales-pos__notice,
.sales-pos__message { margin: -4px 0 12px; border-radius: 10px; padding: 8px 12px; font-size: 13px; font-weight: 650; line-height: 1.4; }
.sales-pos__notice { border: 1px solid var(--pos-border); background: #f1f5f4; color: #4e607a; }
.sales-pos__message { border: 1px solid #fbd0d0; background: #fff0f0; color: #b42318; }
.sales-pos__actions { display: grid; grid-template-columns: 120px minmax(0, 1fr); gap: 12px; }
.sales-pos__actions--single { grid-template-columns: minmax(0, 1fr); }
.sales-pos__secondary-btn,
.sales-pos__primary-btn {
  display: flex;
  height: 52px;
  align-items: center;
  justify-content: center;
  gap: 10px;
  border: 1.5px solid var(--pos-border-strong);
  border-radius: 9px;
  background: #fff;
  padding: 1px 6px;
  color: #2f4158;
  font-weight: 900;
}
.sales-pos__secondary-btn:hover:not(:disabled) { border-color: rgb(10 163 107 / 48%); }
.sales-pos__secondary-btn:disabled { opacity: 0.55; }
.sales-pos__primary-btn {
  border: 0;
  background: linear-gradient(180deg, #0baa6e, #068e5c);
  box-shadow: 0 14px 24px rgb(10 163 107 / 24%);
  color: #fff;
  font-size: 16px;
}
.sales-pos__primary-btn:hover:not(:disabled) { filter: brightness(0.98); transform: translateY(-1px); }
.sales-pos__primary-btn:disabled { opacity: 0.7; }
/* The shortcut hint stays out of the button's accessible text. */
.sales-pos__primary-btn[data-shortcut]::after { margin-left: auto; opacity: 0.9; content: attr(data-shortcut); }

@media (max-width: 1280px) {
  .sales-pos { grid-template-columns: minmax(0, 1fr) 350px; }
  .sales-pos__product-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
  .sales-pos__order-rail { grid-template-columns: repeat(3, minmax(0, 1fr)); }
  /* Empty slots only keep the five-column desktop rail aligned. */
  .sales-pos__order-rail > span[aria-hidden="true"] { display: none; }
}
@media (max-width: 1080px) {
  .sales-pos { grid-template-columns: minmax(0, 1fr); }
  .sales-pos__checkout { position: static; }
}
@media (max-width: 860px) {
  .sales-pos__search { grid-template-columns: minmax(0, 1fr); }
  .sales-pos__categories { display: none; }
  .sales-pos__product-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .sales-pos__field-row,
  .sales-pos__actions,
  .sales-pos__payment-grid { grid-template-columns: minmax(0, 1fr); }
  .sales-pos__order-rail { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .sales-pos__dropdown--row { width: 100%; }
}
@media (max-width: 560px) {
  .sales-pos { gap: 12px; }
  .sales-pos__product-grid,
  .sales-pos__order-rail { grid-template-columns: minmax(0, 1fr); }
  .sales-pos__checkout { padding: 16px; }
  .sales-pos__dropdown--end { width: 100%; min-width: 0; }
}
@media (prefers-reduced-motion: reduce) {
  .sales-pos button,
  .sales-pos summary,
  .sales-pos__product-card { transition: none; }
}
</style>
