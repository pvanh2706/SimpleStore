<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import PurchaseCompletionPanel from '../components/PurchaseCompletionPanel.vue'
import VoidActionPanel, { type VoidAttemptSnapshot } from '../components/VoidActionPanel.vue'
import type { OperationStatus, Purchase, PurchasePayment } from '../api/types'
import { useAuthStore } from '../stores/auth'

const id = String(useRoute().params.id)
const purchase = ref<Purchase | null>(null)
const error = ref('')
const showVoid = ref(false)
const auth = useAuthStore()
const isOwner = computed(() => auth.session.roles.includes('Owner'))
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)

async function load() {
  try {
    const result = await apiRequest<Purchase>(`/api/purchases/${id}`)
    purchase.value = result
    return result
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể tải phiếu nhập.'
    throw reason
  }
}
async function complete(operationId: string, payments: Array<Pick<PurchasePayment, 'amount' | 'method'>>) {
  return apiRequest<Purchase>(`/api/purchases/${id}/complete`, { method: 'POST', body: JSON.stringify({ operationId, payments }) })
}
async function checkOperation(operationId: string) {
  try { return await apiRequest<OperationStatus>(`/api/operations/${operationId}`) }
  catch (reason) { if (reason instanceof ApiError && reason.status === 404) return null; throw reason }
}
async function voidPurchase(attempt: VoidAttemptSnapshot) {
  return apiRequest(`/api/purchases/${id}/void`, {
    method: 'POST', body: JSON.stringify({ operationId: attempt.operationId, reason: attempt.reason }),
  })
}
function completed(result: Purchase) { purchase.value = result }
function voidCompleted() { showVoid.value = false }

const voidErrors: Record<string, string> = {
  'supplier-debt-would-become-negative': 'Không thể hủy vì khoản trả nợ đã làm nghĩa vụ nhà cung cấp thấp hơn giá trị phiếu cần đảo. Supplier refund/recovery chưa được hỗ trợ trong Slice 5; hệ thống không tự tạo Supplier Advance/ứng trước ngầm.',
  'purchase-void-reversal-basis-unavailable': 'Phiếu nhập cũ này không có đủ bằng chứng để hủy trực tiếp một cách an toàn.',
  'purchase-void-reversal-basis-invalid': 'Bằng chứng đảo phiếu nhập không còn hợp lệ. Phiếu không được hủy.',
  'purchase-void-downstream-inventory-dependency': 'Tồn kho đã thay đổi sau phiếu nhập này nên hủy trực tiếp không còn an toàn.',
  'purchase-void-reference-cost-dependency': 'Giá nhập tham chiếu đã thay đổi sau phiếu nhập nên không thể đảo trực tiếp.',
  'purchase-already-voided': 'Phiếu nhập đã bị hủy trước đó.',
  'idempotency-key-reused': 'Mã thao tác đã được dùng cho một yêu cầu khác.',
}

onMounted(async () => { try { await load() } catch { /* load already exposes the error */ } })
</script>

<template>
  <section>
    <RouterLink class="text-sm font-semibold text-emerald-800" to="/purchases">← Danh sách phiếu nhập</RouterLink>
    <p v-if="error" class="error mt-4">{{ error }}</p>
    <template v-if="purchase">
      <div class="mt-3 flex flex-wrap justify-between gap-4"><div><h1 class="text-3xl font-black">Phiếu nhập · {{ purchase.supplierName }}</h1><p class="mt-1 text-slate-500">{{ purchase.status === 'Completed' ? 'Đã hoàn tất' : 'Nháp' }}</p></div><div class="flex gap-2"><RouterLink v-if="purchase.status === 'Draft'" class="btn-secondary" :to="`/purchases/${id}/edit`">Sửa nháp</RouterLink><button v-if="isOwner && purchase.status === 'Completed' && !purchase.isVoided" class="rounded-lg border border-red-300 px-4 py-2 font-bold text-red-800" type="button" @click="showVoid = !showVoid">Hủy phiếu nhập</button></div></div>

      <div v-if="purchase.isVoided" class="mt-5 rounded-xl border border-red-300 bg-red-50 p-4 text-red-900"><strong class="text-lg">Đã hủy</strong><p>{{ purchase.void?.reason }}</p><p class="text-sm">{{ purchase.void ? new Date(purchase.void.voidedAt).toLocaleString('vi-VN') : '' }} · {{ purchase.void?.voidedByUserId }}</p></div>
      <VoidActionPanel v-if="showVoid && purchase.status === 'Completed' && !purchase.isVoided" class="mt-5" :target-id="purchase.id" operation-type="VoidPurchase" title="Xác nhận hủy phiếu nhập" submit-label="Hủy phiếu nhập" :execute="voidPurchase" :check-operation="checkOperation" :reload-target="load" :error-messages="voidErrors" @completed="voidCompleted">Hệ thống chỉ hủy khi còn đủ bằng chứng và chưa có biến động tồn kho hoặc giá tham chiếu phụ thuộc phía sau.</VoidActionPanel>

      <div class="card mt-6 overflow-x-auto p-0"><table class="w-full min-w-[650px] text-left text-sm"><thead class="border-b bg-stone-50"><tr><th class="p-4">Sản phẩm</th><th>Số lượng</th><th>Giá nhập</th><th>Thành tiền</th></tr></thead><tbody><tr v-for="line in purchase.lines" :key="line.id" class="border-b last:border-0"><td class="p-4 font-semibold"><RouterLink class="text-emerald-800 hover:underline" :to="`/products/${line.productId}`">{{ line.productName }}</RouterLink></td><td>{{ line.quantity }} {{ line.productUnit }}</td><td>{{ money(line.unitPrice) }} ₫</td><td>{{ money(line.lineAmount) }} ₫</td></tr></tbody></table></div>
      <div class="mt-5 grid gap-4 sm:grid-cols-3"><div class="card"><span class="text-sm text-slate-500">Tổng lịch sử</span><p class="text-2xl font-black">{{ money(purchase.totalAmount) }} ₫</p></div><div class="card"><span class="text-sm text-slate-500">Đã trả lịch sử</span><p class="text-2xl font-black">{{ money(purchase.paidAmount) }} ₫</p></div><div class="card"><span class="text-sm text-slate-500">Công nợ hiện tại</span><p class="text-2xl font-black">{{ money(purchase.outstandingAmount) }} ₫</p></div></div>

      <PurchaseCompletionPanel v-if="purchase.status === 'Draft'" class="mt-6" :purchase="purchase" :complete-purchase="complete" :check-operation="checkOperation" :load-purchase="purchaseId => apiRequest<Purchase>(`/api/purchases/${purchaseId}`)" @completed="completed" />
      <div v-else class="card mt-6"><h2 class="text-xl font-black">Thanh toán gốc đã ghi nhận</h2><p v-if="purchase.payments.length === 0" class="mt-3 text-slate-500">Chưa trả · toàn bộ là công nợ nhà cung cấp trước điều chỉnh.</p><div v-for="payment in purchase.payments" :key="payment.id" class="mt-3 flex justify-between border-t pt-3"><span>{{ payment.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }}</span><strong>{{ money(payment.amount) }} ₫</strong></div></div>
    </template>
  </section>
</template>
