<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { isAmbiguousOperationFailure, problemMessage, recoverCompletedOperation } from '../api/operationRecovery'
import type { OperationStatus, ReturnContext, ReturnPreview, ReturnResult } from '../api/types'

type RefundMethod = 'Cash' | 'Transfer'
type State = 'idle' | 'previewing' | 'submitting' | 'checking' | 'retryable' | 'completed'
type ReturnLineInput = { originalSaleLineId: string; quantity: number; restock: boolean }
type ReturnIntentionSnapshot = { originalSaleId: string; lines: ReturnLineInput[] }
type ReturnEntry = { selected: boolean; quantity: number; restock: boolean | null }
export interface ReturnAttemptSnapshot {
  operationId: string
  originalSaleId: string
  lines: ReturnLineInput[]
  refundMethod: RefundMethod | null
}

const props = defineProps<{
  context: ReturnContext
  previewReturn: (lines: ReturnLineInput[]) => Promise<ReturnPreview>
  createReturn: (attempt: ReturnAttemptSnapshot) => Promise<ReturnResult>
  checkOperation: (operationId: string) => Promise<OperationStatus | null>
  loadReturn: (returnId: string) => Promise<ReturnResult>
  refreshContext: () => Promise<ReturnContext>
}>()
const emit = defineEmits<{
  completed: [result: ReturnResult]
  contextReloaded: [context: ReturnContext]
}>()

const entries = reactive<Record<string, ReturnEntry>>({})
const preview = ref<ReturnPreview | null>(null)
const previewSnapshot = ref<ReturnIntentionSnapshot | null>(null)
const refundMethod = ref<RefundMethod | ''>('')
const state = ref<State>('idle')
const message = ref('')
const attempt = ref<ReturnAttemptSnapshot | null>(null)
const previewStale = ref(false)
let previewGeneration = 0
const locked = computed(() => ['submitting', 'checking', 'retryable', 'completed'].includes(state.value))
const intentionLocked = computed(() => locked.value || state.value === 'previewing')

const errorMessages: Record<string, string> = {
  'return-quantity-exceeds-remaining': 'Số lượng có thể trả đã thay đổi. Dữ liệu mới nhất đã được tải lại.',
  'concurrent-update': 'Dữ liệu giao dịch vừa thay đổi. Dữ liệu mới nhất đã được tải lại.',
  'sale-already-voided': 'Đơn bán đã bị hủy nên không thể trả hàng.',
  'return-financial-state-invalid': 'Lịch sử hoàn tiền không nhất quán. Không thể tiếp tục giao dịch trả hàng.',
  'refund-method-required': 'Vui lòng chọn phương thức hoàn tiền.',
  'refund-method-not-applicable': 'Không cần hoàn tiền cho lần trả hàng này.',
  'idempotency-key-reused': 'Mã thao tác đã được dùng cho một yêu cầu khác.',
}

function resetEntries(context: ReturnContext) {
  for (const id of Object.keys(entries)) delete entries[id]
  for (const line of context.lines) {
    entries[line.saleLineId] = { selected: false, quantity: 0, restock: null }
  }
}

function clearPreview(markStale = false) {
  previewGeneration += 1
  preview.value = null
  previewSnapshot.value = null
  previewStale.value = markStale
  refundMethod.value = ''
}

function resetDecisionBoundary(context: ReturnContext) {
  clearPreview()
  resetEntries(context)
}

function invalidatePreview() {
  if (locked.value) return
  const hadAcceptedPreview = preview.value !== null && previewSnapshot.value !== null
  clearPreview(hadAcceptedPreview || previewStale.value)
  message.value = ''
}

function normalizeLines(lines: ReturnLineInput[]) {
  return lines
    .map(line => ({ ...line }))
    .sort((left, right) => left.originalSaleLineId.localeCompare(right.originalSaleLineId))
}

function createIntentionSnapshot(lines: ReturnLineInput[]): ReturnIntentionSnapshot {
  return { originalSaleId: props.context.saleId, lines: normalizeLines(lines) }
}

function sameIntention(left: ReturnIntentionSnapshot, right: ReturnIntentionSnapshot) {
  if (left.originalSaleId !== right.originalSaleId || left.lines.length !== right.lines.length) return false
  return left.lines.every((line, index) => {
    const other = right.lines[index]
    return other !== undefined
      && line.originalSaleLineId === other.originalSaleLineId
      && line.quantity === other.quantity
      && line.restock === other.restock
  })
}

function selectedLines(reportValidation = true): ReturnLineInput[] | null {
  const result: ReturnLineInput[] = []
  for (const line of props.context.lines) {
    const entry = entries[line.saleLineId]
    if (!entry?.selected) continue
    if (entry.quantity <= 0 || entry.quantity > line.returnableQuantity) {
      if (reportValidation) message.value = `Số lượng trả của ${line.productName} phải lớn hơn 0 và không vượt quá ${line.returnableQuantity}.`
      return null
    }
    if (entry.restock === null) {
      if (reportValidation) message.value = `Vui lòng chọn nhập lại kho hoặc không nhập lại kho cho ${line.productName}.`
      return null
    }
    result.push({ originalSaleLineId: line.saleLineId, quantity: entry.quantity, restock: entry.restock })
  }
  if (result.length === 0) {
    if (reportValidation) message.value = 'Chọn ít nhất một sản phẩm cần trả.'
    return null
  }
  return normalizeLines(result)
}

async function requestPreview() {
  if (intentionLocked.value) return
  message.value = ''
  const lines = selectedLines()
  if (!lines) return
  const requestedSnapshot = createIntentionSnapshot(lines)
  clearPreview()
  const requestGeneration = previewGeneration
  state.value = 'previewing'
  try {
    const result = await props.previewReturn(lines.map(line => ({ ...line })))
    const currentLines = selectedLines(false)
    const currentSnapshot = currentLines ? createIntentionSnapshot(currentLines) : null
    if (requestGeneration !== previewGeneration || !currentSnapshot || !sameIntention(requestedSnapshot, currentSnapshot)) {
      previewStale.value = true
      message.value = 'Dữ liệu trả hàng đã thay đổi. Vui lòng cập nhật xem trước lại.'
      return
    }
    preview.value = result
    previewSnapshot.value = requestedSnapshot
    previewStale.value = false
    if (!preview.value.refundMethodRequired) refundMethod.value = ''
  } catch (reason) {
    if (requestGeneration === previewGeneration) {
      message.value = problemMessage(reason, errorMessages, 'Không thể xem trước giao dịch trả hàng.')
    }
  } finally {
    state.value = 'idle'
  }
}

async function completeReturn() {
  if (['submitting', 'checking', 'completed'].includes(state.value)) return
  message.value = ''
  if (!attempt.value) {
    const lines = selectedLines()
    if (!lines) return
    const currentSnapshot = createIntentionSnapshot(lines)
    if (!preview.value || !previewSnapshot.value) {
      message.value = previewStale.value
        ? 'Dữ liệu trả hàng đã thay đổi. Vui lòng cập nhật xem trước lại.'
        : 'Vui lòng cập nhật xem trước từ máy chủ trước khi hoàn tất.'
      return
    }
    if (!sameIntention(currentSnapshot, previewSnapshot.value)) {
      clearPreview(true)
      message.value = 'Dữ liệu trả hàng đã thay đổi. Vui lòng cập nhật xem trước lại.'
      return
    }
    if (preview.value.refundMethodRequired && !refundMethod.value) {
      message.value = 'Vui lòng chọn phương thức hoàn tiền.'
      return
    }
    attempt.value = {
      operationId: crypto.randomUUID(),
      originalSaleId: props.context.saleId,
      lines: lines.map(line => ({ ...line })),
      refundMethod: preview.value.refundMethodRequired ? refundMethod.value as RefundMethod : null,
    }
  }

  const current = attempt.value
  state.value = 'submitting'
  try {
    const result = await props.createReturn({
      ...current,
      lines: current.lines.map(line => ({ ...line })),
    })
    state.value = 'completed'
    emit('completed', result)
  } catch (reason) {
    if (!isAmbiguousOperationFailure(reason)) {
      attempt.value = null
      state.value = 'idle'
      message.value = problemMessage(reason, errorMessages, 'Không thể hoàn tất giao dịch trả hàng.')
      const code = reason && typeof reason === 'object' && 'problem' in reason
        ? (reason as { problem?: { code?: string } }).problem?.code
        : undefined
      if (code === 'return-quantity-exceeds-remaining' || code === 'sale-already-voided' || code === 'concurrent-update') {
        state.value = 'checking'
        resetDecisionBoundary(props.context)
        try {
          const refreshedContext = await props.refreshContext()
          resetDecisionBoundary(refreshedContext)
          emit('contextReloaded', refreshedContext)
        } catch { /* Keep the typed error visible. */ }
        finally { state.value = 'idle' }
      }
      return
    }

    state.value = 'checking'
    try {
      const result = await recoverCompletedOperation(
        current.operationId,
        'CreateReturn',
        props.checkOperation,
        props.loadReturn,
      )
      if (result) {
        state.value = 'completed'
        emit('completed', result)
        return
      }
    } catch { /* The outcome remains ambiguous, so preserve the exact attempt. */ }

    state.value = 'retryable'
    message.value = 'Chưa xác định được kết quả. Dữ liệu trả hàng đã được khóa để thử lại đúng thao tác.'
  }
}

resetEntries(props.context)
watch(() => props.context, context => {
  if (!attempt.value) resetDecisionBoundary(context)
})

defineExpose({ state, attempt, preview, previewSnapshot, entries, refundMethod })
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
</script>

<template>
  <section class="grid gap-6">
    <div class="card overflow-x-auto p-0">
      <table class="w-full min-w-[850px] text-left text-sm">
        <thead class="border-b bg-stone-50"><tr><th class="p-4">Chọn</th><th>Sản phẩm</th><th>Đã bán</th><th>Đã trả</th><th>Có thể trả</th><th>Số lượng trả</th><th>Xử lý tồn kho</th></tr></thead>
        <tbody>
          <tr v-for="line in context.lines" :key="line.saleLineId" class="border-b last:border-0">
            <td class="p-4"><input v-model="entries[line.saleLineId].selected" type="checkbox" :aria-label="`Chọn trả ${line.productName}`" :disabled="intentionLocked || line.returnableQuantity <= 0" @change="invalidatePreview" /></td>
            <td><strong>{{ line.productName }}</strong><p class="text-xs text-slate-500">{{ line.productSku }} · {{ money(line.originalUnitSalePrice) }} ₫/{{ line.productUnit }}</p></td>
            <td>{{ line.soldQuantity }}</td><td>{{ line.previouslyReturnedQuantity }}</td><td>{{ line.returnableQuantity }}</td>
            <td><input v-model.number="entries[line.saleLineId].quantity" class="input w-28" type="number" min="0.001" :max="line.returnableQuantity" step="0.001" :aria-label="`Số lượng trả ${line.productName}`" :disabled="intentionLocked || !entries[line.saleLineId].selected" @input="invalidatePreview" /></td>
            <td><div class="grid gap-2"><label><input v-model="entries[line.saleLineId].restock" type="radio" :name="`restock-${line.saleLineId}`" :value="true" :disabled="intentionLocked || !entries[line.saleLineId].selected" @change="invalidatePreview" /> Nhập lại kho</label><label><input v-model="entries[line.saleLineId].restock" type="radio" :name="`restock-${line.saleLineId}`" :value="false" :disabled="intentionLocked || !entries[line.saleLineId].selected" @change="invalidatePreview" /> Không nhập lại kho</label></div></td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="flex justify-end"><button class="btn-secondary" type="button" :disabled="intentionLocked" @click="requestPreview">{{ state === 'previewing' ? 'Đang xem trước…' : 'Cập nhật xem trước' }}</button></div>

    <section v-if="preview" class="card grid gap-3" aria-label="Xem trước trả hàng">
      <h2 class="text-xl font-black">Kết quả từ máy chủ</h2>
      <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <p>Giá trị trả lần này<br><strong>{{ money(preview.currentReturnValue) }} ₫</strong></p>
        <p>Tổng đã trả hàng<br><strong>{{ money(preview.cumulativeReturnedValue) }} ₫</strong></p>
        <p>Nghĩa vụ sau trả<br><strong>{{ money(preview.netSaleObligation) }} ₫</strong></p>
        <p>Tiền đang giữ<br><strong>{{ money(preview.netCashHeld) }} ₫</strong></p>
        <p>Còn nợ<br><strong>{{ money(preview.outstanding) }} ₫</strong></p>
        <p class="text-emerald-800">Số tiền cần hoàn<br><strong class="text-xl">{{ money(preview.refundDueNow) }} ₫</strong></p>
      </div>
      <p class="text-sm text-slate-600">{{ preview.refundDueNow > 0 ? 'Đây là số tiền thực tế cần hoàn cho khách.' : 'Giá trị trả hàng được giảm công nợ trước; hiện không phát sinh tiền hoàn.' }}</p>
      <label v-if="preview.refundMethodRequired" class="grid max-w-sm gap-1 font-semibold">Phương thức hoàn tiền<select v-model="refundMethod" class="input" aria-label="Phương thức hoàn tiền" :disabled="locked"><option value="">Chọn phương thức</option><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select></label>
    </section>

    <p v-if="state === 'retryable'" class="rounded-lg bg-amber-50 p-3 text-sm font-semibold text-amber-900">OperationId và toàn bộ payload đang được giữ nguyên cho lần thử lại.</p>
    <p v-if="message" class="error" role="alert">{{ message }}</p>
    <button class="btn-primary justify-self-end" type="button" :disabled="state === 'submitting' || state === 'checking' || state === 'completed' || state === 'previewing'" @click="completeReturn">{{ state === 'retryable' ? 'Thử lại đúng thao tác' : state === 'submitting' || state === 'checking' ? 'Đang xác nhận…' : 'Hoàn tất trả hàng' }}</button>
  </section>
</template>
