<script setup lang="ts">
import { computed, ref } from 'vue'
import { isAmbiguousOperationFailure, problemMessage, recoverCompletedOperation } from '../api/operationRecovery'
import type { OperationStatus } from '../api/types'

type State = 'idle' | 'submitting' | 'checking' | 'retryable' | 'completed'
export interface VoidAttemptSnapshot { operationId: string; targetId: string; reason: string }

const props = defineProps<{
  targetId: string
  operationType: 'VoidSale' | 'VoidPurchase'
  title: string
  submitLabel: string
  execute: (attempt: VoidAttemptSnapshot) => Promise<unknown>
  checkOperation: (operationId: string) => Promise<OperationStatus | null>
  reloadTarget: () => Promise<unknown>
  errorMessages: Record<string, string>
}>()
const emit = defineEmits<{ completed: [] }>()

const reason = ref('')
const state = ref<State>('idle')
const message = ref('')
const attempt = ref<VoidAttemptSnapshot | null>(null)
const locked = computed(() => ['submitting', 'checking', 'retryable', 'completed'].includes(state.value))

async function submit() {
  if (['submitting', 'checking', 'completed'].includes(state.value)) return
  message.value = ''
  if (!attempt.value) {
    const normalizedReason = reason.value.trim()
    if (!normalizedReason) {
      message.value = 'Vui lòng nhập lý do.'
      return
    }
    if (normalizedReason.length > 500) {
      message.value = 'Lý do không được vượt quá 500 ký tự.'
      return
    }
    attempt.value = { operationId: crypto.randomUUID(), targetId: props.targetId, reason: normalizedReason }
  }

  const current = attempt.value
  state.value = 'submitting'
  try {
    await props.execute({ ...current })
  } catch (failure) {
    if (!isAmbiguousOperationFailure(failure)) {
      attempt.value = null
      state.value = 'idle'
      message.value = problemMessage(failure, props.errorMessages, `Không thể ${props.submitLabel.toLocaleLowerCase('vi-VN')}.`)
      return
    }
    return recover(current)
  }

  state.value = 'checking'
  try {
    await props.reloadTarget()
    attempt.value = null
    state.value = 'completed'
    emit('completed')
    return
  } catch {
    await recover(current)
  }
}

async function recover(current: VoidAttemptSnapshot) {
  state.value = 'checking'
  try {
    const recovered = await recoverCompletedOperation(
      current.operationId,
      props.operationType,
      props.checkOperation,
      async () => {
        await props.reloadTarget()
        return true
      },
    )
    if (recovered) {
      attempt.value = null
      state.value = 'completed'
      emit('completed')
      return
    }
  } catch { /* Preserve the exact request while its result remains ambiguous. */ }

  state.value = 'retryable'
  message.value = 'Chưa xác định được kết quả. Lý do và mã thao tác đã được khóa để thử lại an toàn.'
}

defineExpose({ state, attempt, reason })
</script>

<template>
  <section class="rounded-xl border border-red-200 bg-red-50 p-5" :aria-label="title">
    <h2 class="text-xl font-black text-red-900">{{ title }}</h2>
    <p class="mt-2 text-sm text-red-900"><slot /></p>
    <label class="mt-4 grid gap-1 text-sm font-semibold text-red-950">Lý do
      <textarea v-model="reason" class="input min-h-24 bg-white" maxlength="500" aria-label="Lý do hủy" :disabled="locked" />
    </label>
    <p v-if="state === 'retryable'" class="mt-3 text-sm font-semibold text-amber-900">Dữ liệu đang được khóa cho lần thử lại của cùng thao tác.</p>
    <p v-if="message" class="error mt-3" role="alert">{{ message }}</p>
    <button class="mt-4 rounded-lg bg-red-700 px-4 py-2 font-bold text-white disabled:opacity-50" type="button" :disabled="state === 'submitting' || state === 'checking' || state === 'completed'" @click="submit">{{ state === 'retryable' ? 'Thử lại đúng thao tác' : state === 'submitting' || state === 'checking' ? 'Đang xác nhận…' : submitLabel }}</button>
  </section>
</template>
