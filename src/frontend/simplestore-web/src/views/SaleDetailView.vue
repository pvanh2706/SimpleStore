<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import type { OperationStatus, ReturnContext, Sale } from '../api/types'
import SaleReceipt from '../components/SaleReceipt.vue'
import VoidActionPanel, { type VoidAttemptSnapshot } from '../components/VoidActionPanel.vue'
import { useAuthStore } from '../stores/auth'

const sale = ref<Sale | null>(null)
const returnContext = ref<ReturnContext | null>(null)
const error = ref('')
const showVoid = ref(false)
const id = String(useRoute().params.id)
const auth = useAuthStore()
const isOwner = computed(() => auth.session.roles.includes('Owner'))
const canReturn = computed(() => isOwner.value && !sale.value?.isVoided
  && returnContext.value?.lines.some(line => line.returnableQuantity > 0))
const canVoid = computed(() => isOwner.value && sale.value && !sale.value.isVoided
  && (sale.value.returns?.length ?? 0) === 0)
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)

async function loadSale() {
  const result = await apiRequest<Sale>(`/api/sales/${id}`)
  sale.value = result
  if (isOwner.value) returnContext.value = await apiRequest<ReturnContext>(`/api/sales/${id}/return-context`)
  return result
}
async function checkOperation(operationId: string) {
  try { return await apiRequest<OperationStatus>(`/api/operations/${operationId}`) }
  catch (reason) { if (reason instanceof ApiError && reason.status === 404) return null; throw reason }
}
async function voidSale(attempt: VoidAttemptSnapshot) {
  return apiRequest(`/api/sales/${id}/void`, {
    method: 'POST', body: JSON.stringify({ operationId: attempt.operationId, reason: attempt.reason }),
  })
}
function voidCompleted() { showVoid.value = false }

onMounted(async () => {
  try { await loadSale() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải đơn bán.' }
})

const voidErrors: Record<string, string> = {
  'sale-already-voided': 'Đơn bán đã bị hủy trước đó.',
  'sale-has-returns': 'Không thể hủy trực tiếp vì đơn đã có giao dịch trả hàng.',
  'void-reason-required': 'Vui lòng nhập lý do hủy.',
  'void-reason-too-long': 'Lý do hủy không được vượt quá 500 ký tự.',
  'idempotency-key-reused': 'Mã thao tác đã được dùng cho một yêu cầu khác.',
}
</script>

<template>
  <section>
    <RouterLink class="no-print text-sm font-semibold text-emerald-800" to="/sales">← Lịch sử bán hàng</RouterLink>
    <p v-if="error" class="error mt-4">{{ error }}</p>
    <template v-if="sale">
      <div class="no-print mt-4 flex flex-wrap items-start justify-between gap-4">
        <div><h1 class="text-3xl font-black">Chi tiết đơn bán</h1><p class="mt-1 text-slate-500">Giao dịch gốc luôn được giữ nguyên để đối chiếu.</p></div>
        <div v-if="isOwner" class="flex flex-wrap gap-2">
          <RouterLink v-if="canReturn" class="btn-primary" :to="`/sales/${sale.id}/return`">Trả hàng</RouterLink>
          <button v-if="canVoid" class="rounded-lg border border-red-300 px-4 py-2 font-bold text-red-800" type="button" @click="showVoid = !showVoid">Hủy giao dịch</button>
        </div>
      </div>

      <div v-if="sale.isVoided" class="mt-5 rounded-xl border border-red-300 bg-red-50 p-4 text-red-900"><strong class="text-lg">Đã hủy</strong><p>{{ sale.void?.reason }}</p><p class="text-sm">{{ sale.void ? new Date(sale.void.voidedAt).toLocaleString('vi-VN') : '' }} · {{ sale.void?.voidedByUserId }}</p></div>
      <p v-else-if="isOwner && sale.returns.length > 0" class="no-print mt-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-900">Không thể hủy trực tiếp vì đơn đã có trả hàng.</p>

      <VoidActionPanel v-if="showVoid && canVoid" class="no-print mt-5" :target-id="sale.id" operation-type="VoidSale" title="Xác nhận hủy giao dịch" submit-label="Hủy giao dịch" :execute="voidSale" :check-operation="checkOperation" :reload-target="loadSale" :error-messages="voidErrors" @completed="voidCompleted">Hủy giao dịch sẽ đảo tồn kho nhưng không có nghĩa hệ thống vừa hoàn tiền mặt cho khách. Dữ liệu đơn và thanh toán gốc vẫn được giữ lại.</VoidActionPanel>

      <div class="no-print mt-6 grid gap-5 lg:grid-cols-2">
        <section class="card"><h2 class="text-xl font-black">Giao dịch gốc</h2><div class="mt-4 grid gap-2"><p class="flex justify-between"><span>Tổng ban đầu</span><strong>{{ money(sale.originalTotalAmount) }} ₫</strong></p><p class="flex justify-between"><span>Đã thu ban đầu</span><strong>{{ money(sale.originalCollectedAmount) }} ₫</strong></p></div></section>
        <section class="card"><h2 class="text-xl font-black">Trạng thái hiện tại</h2><div class="mt-4 grid gap-2"><p class="flex justify-between"><span>Đã trả hàng</span><strong>{{ money(sale.totalReturnedAmount) }} ₫</strong></p><p class="flex justify-between"><span>Doanh số thuần</span><strong>{{ money(sale.netSaleAmount) }} ₫</strong></p><p class="flex justify-between"><span>Đã hoàn</span><strong>{{ money(sale.totalRefundedAmount) }} ₫</strong></p><p class="flex justify-between"><span>Tiền thu thuần</span><strong>{{ money(sale.netCollectedAmount) }} ₫</strong></p><p class="flex justify-between border-t pt-2 text-lg"><span>Còn nợ</span><strong>{{ money(sale.outstandingAmount) }} ₫</strong></p></div></section>
      </div>

      <section class="no-print card mt-6"><h2 class="text-xl font-black">Lịch sử trả hàng</h2><p v-if="sale.returns.length === 0" class="mt-3 text-slate-500">Chưa có giao dịch trả hàng.</p><div v-for="item in sale.returns" :key="item.id" class="mt-4 grid gap-2 border-t pt-4 sm:grid-cols-[1fr_auto_auto]"><div><RouterLink class="font-semibold text-emerald-800" :to="`/returns/${item.id}`">{{ item.id }}</RouterLink><p class="text-sm text-slate-500">{{ new Date(item.completedAt).toLocaleString('vi-VN') }}</p></div><p>Giá trị trả <strong>{{ money(item.totalReturnAmount) }} ₫</strong></p><p>Đã hoàn <strong>{{ money(item.refundAmount) }} ₫</strong></p></div></section>

      <SaleReceipt class="mt-6" :sale="sale" />
    </template>
  </section>
</template>
