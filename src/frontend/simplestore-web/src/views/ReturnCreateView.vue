<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import ReturnForm, { type ReturnAttemptSnapshot } from '../components/ReturnForm.vue'
import type { OperationStatus, ReturnContext, ReturnPreview, ReturnResult } from '../api/types'

const route = useRoute()
const router = useRouter()
const saleId = String(route.params.id)
const context = ref<ReturnContext | null>(null)
const error = ref('')

async function loadContext() {
  const result = await apiRequest<ReturnContext>(`/api/sales/${saleId}/return-context`)
  context.value = result
  return result
}
async function previewReturn(lines: ReturnAttemptSnapshot['lines']) {
  return apiRequest<ReturnPreview>('/api/returns/preview', {
    method: 'POST', body: JSON.stringify({ originalSaleId: saleId, lines }),
  })
}
async function createReturn(attempt: ReturnAttemptSnapshot) {
  return apiRequest<ReturnResult>('/api/returns', { method: 'POST', body: JSON.stringify(attempt) })
}
async function checkOperation(operationId: string) {
  try { return await apiRequest<OperationStatus>(`/api/operations/${operationId}`) }
  catch (reason) { if (reason instanceof ApiError && reason.status === 404) return null; throw reason }
}
async function completed() { await router.push(`/sales/${saleId}`) }

onMounted(async () => {
  try { await loadContext() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải dữ liệu trả hàng.' }
})
</script>

<template>
  <section>
    <RouterLink class="text-sm font-semibold text-emerald-800" :to="`/sales/${saleId}`">← Chi tiết đơn bán</RouterLink>
    <div class="mt-3"><h1 class="text-3xl font-black">Trả hàng</h1><p class="mt-1 text-slate-500">Chọn số lượng và quyết định xử lý tồn kho cho từng sản phẩm.</p></div>
    <p v-if="error" class="error mt-5" role="alert">{{ error }}</p>
    <div v-if="context?.isVoided" class="mt-5 rounded-xl border border-red-200 bg-red-50 p-4 text-red-900">Đơn bán đã bị hủy, không thể tạo giao dịch trả hàng.</div>
    <ReturnForm v-else-if="context" class="mt-6" :context="context" :preview-return="previewReturn" :create-return="createReturn" :check-operation="checkOperation" :load-return="id => apiRequest<ReturnResult>(`/api/returns/${id}`)" :refresh-context="loadContext" @context-reloaded="context = $event" @completed="completed" />
  </section>
</template>
