<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { apiRequest } from '../api/client'
import type { ReturnResult } from '../api/types'

const id = String(useRoute().params.id)
const result = ref<ReturnResult | null>(null)
const error = ref('')
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
onMounted(async () => {
  try { result.value = await apiRequest<ReturnResult>(`/api/returns/${id}`) }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải giao dịch trả hàng.' }
})
</script>

<template>
  <section>
    <RouterLink v-if="result" class="text-sm font-semibold text-emerald-800" :to="`/sales/${result.originalSaleId}`">← Chi tiết đơn bán</RouterLink>
    <p v-if="error" class="error mt-5">{{ error }}</p>
    <template v-if="result">
      <div class="mt-3"><h1 class="text-3xl font-black">Giao dịch trả hàng</h1><p class="mt-1 text-slate-500">{{ result.id }} · {{ new Date(result.completedAt).toLocaleString('vi-VN') }}</p></div>
      <div class="mt-6 grid gap-4 sm:grid-cols-2"><div class="card"><p class="text-sm text-slate-500">Giá trị trả</p><p class="text-2xl font-black">{{ money(result.totalReturnAmount) }} ₫</p></div><div class="card"><p class="text-sm text-slate-500">Tiền hoàn thực tế</p><p class="text-2xl font-black">{{ money(result.refundAmount) }} ₫</p></div></div>
      <div class="card mt-6 overflow-x-auto p-0"><table class="w-full min-w-[650px] text-left text-sm"><thead class="border-b bg-stone-50"><tr><th class="p-4">Sản phẩm</th><th>Số lượng</th><th>Đơn giá gốc</th><th>Giá trị trả</th><th>Tồn kho</th></tr></thead><tbody><tr v-for="line in result.lines" :key="line.id" class="border-b last:border-0"><td class="p-4">{{ line.productId }}</td><td>{{ line.quantity }}</td><td>{{ money(line.unitSalePriceBasis) }} ₫</td><td>{{ money(line.returnLineAmount) }} ₫</td><td>{{ line.restock ? 'Nhập lại kho' : 'Không nhập lại kho' }}</td></tr></tbody></table></div>
      <div class="card mt-6"><h2 class="text-xl font-black">Hoàn tiền</h2><p v-if="result.refundPayments.length === 0" class="mt-3 text-slate-500">Không phát sinh tiền hoàn.</p><div v-for="payment in result.refundPayments" :key="payment.id" class="mt-3 flex justify-between border-t pt-3"><span>{{ payment.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }}</span><strong>{{ money(payment.amount) }} ₫</strong></div></div>
    </template>
  </section>
</template>
