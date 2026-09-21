<script setup lang="ts">
import { computed, ref } from 'vue'
import { ApiError } from '../api/client'
import type { OperationStatus, Purchase, PurchasePayment } from '../api/types'

type PaymentInput = Pick<PurchasePayment, 'amount' | 'method'>
type CompletionState = 'idle' | 'completing' | 'checking' | 'completed' | 'validation-failed' | 'retryable'
interface AttemptSnapshot { operationId: string; payments: PaymentInput[] }

const props = defineProps<{
  purchase: Purchase
  completePurchase: (operationId: string, payments: PaymentInput[]) => Promise<Purchase>
  checkOperation: (operationId: string) => Promise<OperationStatus | null>
  loadPurchase: (purchaseId: string) => Promise<Purchase>
}>()
const emit = defineEmits<{ completed: [purchase: Purchase] }>()

const payments = ref<PaymentInput[]>([])
const amount = ref(0)
const method = ref<'Cash' | 'Transfer'>('Cash')
const state = ref<CompletionState>('idle')
const message = ref('')
const attempt = ref<AttemptSnapshot | null>(null)
const paid = computed(() => payments.value.reduce((sum, payment) => sum + payment.amount, 0))
const outstanding = computed(() => props.purchase.totalAmount - paid.value)
const payloadLocked = computed(() => ['completing', 'checking', 'retryable', 'completed'].includes(state.value))

function resetDeterministicAttempt() {
  if (state.value === 'validation-failed') {
    attempt.value = null
    state.value = 'idle'
    message.value = ''
  }
}

function addPayment() {
  if (payloadLocked.value) return
  message.value = ''
  if (amount.value <= 0) { message.value = 'Số tiền phải lớn hơn 0.'; return }
  if (paid.value + amount.value > props.purchase.totalAmount) { message.value = 'Tổng thanh toán không được vượt tổng phiếu.'; return }
  resetDeterministicAttempt()
  payments.value.push({ amount: amount.value, method: method.value })
  amount.value = 0
}

function removePayment(index: number) {
  if (payloadLocked.value) return
  resetDeterministicAttempt()
  payments.value.splice(index, 1)
}

async function complete() {
  if (!attempt.value) {
    attempt.value = {
      operationId: crypto.randomUUID(),
      payments: payments.value.map((payment) => ({ ...payment })),
    }
  }
  const currentAttempt = attempt.value
  state.value = 'completing'
  message.value = ''
  try {
    const result = await props.completePurchase(
      currentAttempt.operationId,
      currentAttempt.payments.map((payment) => ({ ...payment })),
    )
    state.value = 'completed'
    emit('completed', result)
  } catch (reason) {
    const isDeterministicClientError = reason instanceof ApiError
      && reason.status >= 400
      && reason.status < 500
      && reason.status !== 408
    if (isDeterministicClientError) {
      state.value = 'validation-failed'
      message.value = reason.message
      attempt.value = null
      return
    }

    state.value = 'checking'
    try {
      const operation = await props.checkOperation(currentAttempt.operationId)
      if (operation?.status === 'Completed' && operation.resultReference) {
        const committedPurchase = await props.loadPurchase(operation.resultReference)
        state.value = 'completed'
        message.value = 'Phiếu đã hoàn tất. Đã tải kết quả giao dịch.'
        emit('completed', committedPurchase)
        return
      }
    } catch { /* The result remains ambiguous; preserve the exact attempt snapshot. */ }

    state.value = 'retryable'
    message.value = 'Chưa xác định được kết quả. Hãy thử lại với cùng mã thao tác và dữ liệu thanh toán.'
  }
}
</script>

<template>
  <section class="card grid gap-4">
    <h2 class="text-xl font-black">Thanh toán và hoàn tất</h2>
    <div class="grid gap-3 sm:grid-cols-[1fr_1fr_auto]">
      <select v-model="method" class="input" aria-label="Phương thức" :disabled="payloadLocked"><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select>
      <input v-model.number="amount" class="input" type="number" min="0.01" step="0.01" aria-label="Số tiền thanh toán" :disabled="payloadLocked" />
      <button class="btn-secondary" type="button" :disabled="payloadLocked" @click="addPayment">Thêm thanh toán</button>
    </div>
    <ul v-if="payments.length" class="grid gap-2 text-sm">
      <li v-for="(payment, index) in payments" :key="index" class="flex justify-between gap-3">
        <span>{{ payment.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }}</span>
        <span class="flex items-center gap-3">{{ new Intl.NumberFormat('vi-VN').format(payment.amount) }} ₫ <button class="text-red-700" type="button" :disabled="payloadLocked" :aria-label="`Xóa thanh toán ${index + 1}`" @click="removePayment(index)">Xóa</button></span>
      </li>
    </ul>
    <div class="grid gap-1 border-t pt-4 text-sm"><div class="flex justify-between"><span>Tổng phiếu</span><strong>{{ new Intl.NumberFormat('vi-VN').format(purchase.totalAmount) }} ₫</strong></div><div class="flex justify-between"><span>Đã trả</span><strong>{{ new Intl.NumberFormat('vi-VN').format(paid) }} ₫</strong></div><div class="flex justify-between text-lg"><span>Còn nợ</span><strong>{{ new Intl.NumberFormat('vi-VN').format(outstanding) }} ₫</strong></div></div>
    <p v-if="state === 'retryable'" class="text-sm font-semibold text-amber-800">Dữ liệu thanh toán đang được khóa cho lần thử lại của thao tác này.</p>
    <p v-if="message" :class="state === 'completed' ? 'text-sm text-emerald-800' : 'error'" role="alert">{{ message }}</p>
    <p v-if="state === 'checking'" class="text-sm text-slate-500">Đang kiểm tra kết quả thao tác…</p>
    <button class="btn-primary" type="button" :disabled="['completing', 'checking', 'completed'].includes(state)" @click="complete">{{ state === 'completing' ? 'Đang hoàn tất…' : state === 'retryable' ? 'Thử lại cùng thao tác' : 'Hoàn tất phiếu nhập' }}</button>
  </section>
</template>
