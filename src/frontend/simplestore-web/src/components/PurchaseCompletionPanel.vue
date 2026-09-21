<script setup lang="ts">
import { computed, ref } from 'vue'
import type { OperationStatus, Purchase, PurchasePayment } from '../api/types'

type PaymentInput = Pick<PurchasePayment, 'amount' | 'method'>
type CompletionState = 'idle' | 'completing' | 'checking' | 'completed' | 'validation-failed' | 'retryable'

const props = defineProps<{
  purchase: Purchase
  completePurchase: (operationId: string, payments: PaymentInput[]) => Promise<Purchase>
  checkOperation: (operationId: string) => Promise<OperationStatus | null>
}>()
const emit = defineEmits<{ completed: [purchase: Purchase] }>()

const payments = ref<PaymentInput[]>([])
const amount = ref(0)
const method = ref<'Cash' | 'Transfer'>('Cash')
const state = ref<CompletionState>('idle')
const message = ref('')
const operationId = ref<string | null>(null)
const paid = computed(() => payments.value.reduce((sum, payment) => sum + payment.amount, 0))
const outstanding = computed(() => props.purchase.totalAmount - paid.value)

function addPayment() {
  message.value = ''
  if (amount.value <= 0) { message.value = 'Số tiền phải lớn hơn 0.'; return }
  if (paid.value + amount.value > props.purchase.totalAmount) { message.value = 'Tổng thanh toán không được vượt tổng phiếu.'; return }
  payments.value.push({ amount: amount.value, method: method.value })
  amount.value = 0
  if (state.value === 'validation-failed') operationId.value = null
  state.value = 'idle'
}

async function complete() {
  operationId.value ??= crypto.randomUUID()
  state.value = 'completing'; message.value = ''
  try {
    const result = await props.completePurchase(operationId.value, payments.value)
    state.value = 'completed'; emit('completed', result)
  } catch (reason) {
    state.value = 'checking'
    try {
      const operation = await props.checkOperation(operationId.value)
      if (operation?.status === 'Completed') {
        state.value = 'completed'
        message.value = 'Phiếu đã hoàn tất. Đang tải kết quả…'
        emit('completed', props.purchase)
        return
      }
    } catch { /* unknown remains retryable with the same OperationId */ }
    const status = typeof reason === 'object' && reason !== null && 'status' in reason
      ? Number((reason as { status: number }).status) : 0
    state.value = status >= 400 && status < 500 ? 'validation-failed' : 'retryable'
    message.value = state.value === 'retryable'
      ? 'Chưa xác định được kết quả. Hãy thử lại với cùng mã thao tác.'
      : reason instanceof Error ? reason.message : 'Không thể hoàn tất phiếu.'
  }
}
</script>

<template>
  <section class="card grid gap-4">
    <h2 class="text-xl font-black">Thanh toán và hoàn tất</h2>
    <div class="grid gap-3 sm:grid-cols-[1fr_1fr_auto]"><select v-model="method" class="input" aria-label="Phương thức"><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select><input v-model.number="amount" class="input" type="number" min="0.01" step="0.01" aria-label="Số tiền thanh toán" /><button class="btn-secondary" type="button" @click="addPayment">Thêm thanh toán</button></div>
    <ul v-if="payments.length" class="grid gap-2 text-sm"><li v-for="(payment, index) in payments" :key="index" class="flex justify-between"><span>{{ payment.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }}</span><span>{{ new Intl.NumberFormat('vi-VN').format(payment.amount) }} ₫</span></li></ul>
    <div class="grid gap-1 border-t pt-4 text-sm"><div class="flex justify-between"><span>Tổng phiếu</span><strong>{{ new Intl.NumberFormat('vi-VN').format(purchase.totalAmount) }} ₫</strong></div><div class="flex justify-between"><span>Đã trả</span><strong>{{ new Intl.NumberFormat('vi-VN').format(paid) }} ₫</strong></div><div class="flex justify-between text-lg"><span>Còn nợ</span><strong>{{ new Intl.NumberFormat('vi-VN').format(outstanding) }} ₫</strong></div></div>
    <p v-if="message" class="error" role="alert">{{ message }}</p>
    <p v-if="state === 'checking'" class="text-sm text-slate-500">Đang kiểm tra kết quả thao tác…</p>
    <button class="btn-primary" type="button" :disabled="['completing', 'checking', 'completed'].includes(state)" @click="complete">{{ state === 'completing' ? 'Đang hoàn tất…' : state === 'retryable' ? 'Thử lại cùng thao tác' : 'Hoàn tất phiếu nhập' }}</button>
  </section>
</template>
