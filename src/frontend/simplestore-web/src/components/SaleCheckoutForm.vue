<script setup lang="ts">
import { computed, ref } from 'vue'
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
const products = ref<ProductPage | null>(null)
const productPage = ref(1)
const cart = ref<CartLine[]>([])
const amount = ref(0)
const method = ref<'Cash' | 'Transfer'>('Cash')
const payments = ref<PaymentInput[]>([])
const customerSearch = ref('')
const customers = ref<CustomerPage | null>(null)
const customerPage = ref(1)
const customer = ref<Customer | null>(null)
const newCustomerName = ref('')
const newCustomerPhone = ref('')
const state = ref<State>('idle')
const attempt = ref<AttemptSnapshot | null>(null)
const message = ref('')

const total = computed(() => cart.value.reduce(
  (sum, line) => sum + Math.round(line.quantity * line.product.salePrice * 100) / 100,
  0,
))
const paid = computed(() => payments.value.reduce((sum, payment) => sum + payment.amount, 0))
const outstanding = computed(() => Math.max(0, total.value - paid.value))
const locked = computed(() => ['completing', 'checking', 'retryable', 'completed'].includes(state.value))

async function findProducts(page = 1) {
  productPage.value = page
  products.value = await props.searchProducts(productSearch.value.trim(), page)
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
  if (amount.value <= 0) {
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
  customerPage.value = page
  customers.value = await props.searchCustomers(customerSearch.value.trim(), page)
}

async function createAndSelectCustomer() {
  if (locked.value || !newCustomerName.value.trim()) return
  customer.value = await props.createCustomer(
    newCustomerName.value.trim(),
    newCustomerPhone.value.trim() || null,
  )
  newCustomerName.value = ''
  newCustomerPhone.value = ''
}

async function complete() {
  if (state.value === 'completing' || state.value === 'checking' || state.value === 'completed') return
  message.value = ''
  if (cart.value.length === 0) {
    message.value = 'Thêm ít nhất một sản phẩm.'
    return
  }
  if (cart.value.some(line => line.quantity <= 0)) {
    message.value = 'Số lượng bán phải lớn hơn 0.'
    return
  }
  if (outstanding.value > 0 && !customer.value) {
    message.value = 'Chọn khách hàng khi đơn còn công nợ.'
    return
  }
  if (!attempt.value) {
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

defineExpose({ state, attempt, cart, payments, customer, total, paid, outstanding })
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
</script>

<template>
  <div class="grid gap-6 lg:grid-cols-[1.1fr_.9fr]">
    <section class="grid content-start gap-5">
      <div v-if="allowNegativeStock" class="rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900">
        Cửa hàng đang cho phép bán âm tồn. Giá vốn có thể ở trạng thái ước tính hoặc chưa xác định.
      </div>
      <form class="card grid gap-3" @submit.prevent="findProducts(1)">
        <h2 class="text-xl font-black">Tìm hoặc quét sản phẩm</h2>
        <div class="flex gap-2">
          <input v-model="productSearch" class="input flex-1" aria-label="Tìm hoặc quét sản phẩm" placeholder="Tên, SKU hoặc barcode" :disabled="locked" />
          <button class="btn-secondary" type="submit" :disabled="locked">Tìm sản phẩm</button>
        </div>
        <div v-for="product in products?.items" :key="product.id" class="flex items-center justify-between gap-3 border-t pt-3">
          <div><strong>{{ product.name }}</strong><p class="text-sm text-slate-500">{{ product.sku }} · tồn {{ product.quantityOnHand }} {{ product.unit }}</p></div>
          <button class="btn-secondary" type="button" :disabled="locked" :aria-label="`Thêm sản phẩm ${product.name}`" @click="addProduct(product)">Thêm</button>
        </div>
        <div v-if="products && products.totalPages > 1" class="flex items-center justify-end gap-2 text-sm">
          <button class="btn-secondary" type="button" :disabled="productPage <= 1 || locked" @click="findProducts(productPage - 1)">Trước</button>
          <span>Trang {{ productPage }}/{{ products.totalPages }}</span>
          <button class="btn-secondary" type="button" :disabled="productPage >= products.totalPages || locked" @click="findProducts(productPage + 1)">Sau</button>
        </div>
      </form>

      <section class="card">
        <h2 class="text-xl font-black">Giỏ hàng</h2>
        <p v-if="cart.length === 0" class="mt-3 text-slate-500">Chưa có sản phẩm.</p>
        <div v-for="line in cart" :key="line.product.id" class="mt-4 grid gap-3 border-t pt-4 sm:grid-cols-[1fr_130px_120px_auto] sm:items-center">
          <div><strong>{{ line.product.name }}</strong><p class="text-sm text-slate-500">{{ money(line.product.salePrice) }} ₫ / {{ line.product.unit }}</p></div>
          <input v-model.number="line.quantity" class="input" type="number" min="0.001" step="0.001" :aria-label="`Số lượng ${line.product.name}`" :disabled="locked" />
          <strong>{{ money(Math.round(line.quantity * line.product.salePrice * 100) / 100) }} ₫</strong>
          <button class="text-sm font-semibold text-red-700" type="button" :disabled="locked" :aria-label="`Xóa ${line.product.name}`" @click="removeProduct(line.product.id)">Xóa</button>
        </div>
      </section>
    </section>

    <section class="grid content-start gap-5">
      <div class="card grid gap-3">
        <h2 class="text-xl font-black">Thanh toán</h2>
        <div class="grid gap-2 sm:grid-cols-[1fr_1fr_auto]">
          <select v-model="method" class="input" aria-label="Phương thức thanh toán" :disabled="locked"><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select>
          <input v-model.number="amount" class="input" type="number" min="0.01" step="0.01" aria-label="Số tiền thanh toán" :disabled="locked" />
          <button class="btn-secondary" type="button" :disabled="locked" @click="addPayment">Thêm thanh toán</button>
        </div>
        <div v-for="(payment, index) in payments" :key="index" class="flex justify-between border-t pt-3 text-sm">
          <span>{{ payment.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }}</span>
          <span>{{ money(payment.amount) }} ₫ <button class="ml-2 text-red-700" type="button" :disabled="locked" @click="removePayment(index)">Xóa</button></span>
        </div>
      </div>

      <div v-if="outstanding > 0" class="card grid gap-3">
        <h2 class="text-xl font-black">Khách hàng công nợ</h2>
        <div v-if="customer" class="rounded-lg bg-emerald-50 p-3"><strong>{{ customer.name }}</strong><p class="text-sm">{{ customer.phone || 'Không có số điện thoại' }}</p></div>
        <form class="flex gap-2" @submit.prevent="findCustomers(1)">
          <input v-model="customerSearch" class="input flex-1" aria-label="Tìm khách hàng" placeholder="Tên hoặc số điện thoại" :disabled="locked" />
          <button class="btn-secondary" type="submit" :disabled="locked">Tìm khách hàng</button>
        </form>
        <button v-for="item in customers?.items" :key="item.id" class="flex justify-between border-t pt-3 text-left" type="button" :disabled="locked" :aria-label="`Chọn khách hàng ${item.name}`" @click="customer = item"><strong>{{ item.name }}</strong><span>{{ item.phone }}</span></button>
        <div v-if="customers && customers.totalPages > 1" class="flex items-center justify-end gap-2 text-sm">
          <button class="btn-secondary" type="button" :disabled="customerPage <= 1 || locked" @click="findCustomers(customerPage - 1)">Trước</button><span>Trang {{ customerPage }}/{{ customers.totalPages }}</span><button class="btn-secondary" type="button" :disabled="customerPage >= customers.totalPages || locked" @click="findCustomers(customerPage + 1)">Sau</button>
        </div>
        <div class="grid gap-2 border-t pt-3 sm:grid-cols-2"><input v-model="newCustomerName" class="input" aria-label="Tên khách hàng mới" placeholder="Tên khách hàng mới" :disabled="locked" /><input v-model="newCustomerPhone" class="input" aria-label="Số điện thoại khách hàng mới" placeholder="Số điện thoại (không bắt buộc)" :disabled="locked" /></div>
        <button class="btn-secondary" type="button" :disabled="locked || !newCustomerName.trim()" @click="createAndSelectCustomer">Tạo và chọn khách hàng</button>
      </div>

      <div class="card grid gap-2">
        <div class="flex justify-between"><span>Tổng đơn (xem trước)</span><strong>{{ money(total) }} ₫</strong></div>
        <div class="flex justify-between"><span>Đã thanh toán</span><strong>{{ money(paid) }} ₫</strong></div>
        <div class="flex justify-between text-lg"><span>Còn nợ</span><strong>{{ money(outstanding) }} ₫</strong></div>
        <p class="text-xs text-slate-500">Giá và tổng chính thức được backend xác nhận khi hoàn tất.</p>
        <p v-if="message" class="error" role="alert">{{ message }}</p>
        <button class="btn-primary mt-2" type="button" :disabled="state === 'completing' || state === 'checking' || state === 'completed'" @click="complete">
          {{ state === 'retryable' ? 'Thử lại đúng thao tác' : state === 'completing' || state === 'checking' ? 'Đang xác nhận…' : 'Hoàn tất bán hàng' }}
        </button>
      </div>
    </section>
  </div>
</template>
