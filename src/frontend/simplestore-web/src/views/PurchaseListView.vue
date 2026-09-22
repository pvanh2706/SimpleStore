<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiRequest } from '../api/client'
import type { PurchasePage } from '../api/types'

const result = ref<PurchasePage | null>(null)
const status = ref('')
const page = ref(1)
const error = ref('')
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)

async function load(requestedPage = page.value) {
  const params = new URLSearchParams({ page: String(requestedPage), pageSize: '20' })
  if (status.value) params.set('status', status.value)
  try {
    result.value = await apiRequest<PurchasePage>(`/api/purchases?${params}`)
    page.value = requestedPage
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể tải phiếu nhập.'
  }
}
onMounted(load)
</script>

<template>
  <section>
    <div class="flex flex-wrap items-end justify-between gap-4"><div><h1 class="text-3xl font-black">Phiếu nhập</h1><p class="mt-1 text-slate-500">Nháp, hoàn tất, thanh toán và công nợ nhà cung cấp.</p></div><RouterLink class="btn-primary" to="/purchases/new">Tạo phiếu nhập</RouterLink></div>
    <form class="mt-6 flex max-w-md gap-2" @submit.prevent="load(1)"><select v-model="status" class="input"><option value="">Tất cả trạng thái</option><option value="Draft">Nháp</option><option value="Completed">Đã hoàn tất</option></select><button class="btn-secondary">Lọc</button></form>
    <p v-if="error" class="error mt-4">{{ error }}</p>
    <div class="card mt-4 overflow-x-auto p-0"><p v-if="!result?.items.length" class="p-8 text-slate-500">Chưa có phiếu nhập.</p><table v-else class="w-full min-w-[720px] text-left text-sm"><thead class="border-b bg-stone-50"><tr><th class="p-4">Nhà cung cấp</th><th>Trạng thái</th><th>Tổng</th><th>Đã trả</th><th>Còn nợ</th><th>Ngày tạo</th></tr></thead><tbody><tr v-for="purchase in result.items" :key="purchase.id" class="border-b last:border-0"><td class="p-4 font-semibold"><RouterLink class="text-emerald-800 hover:underline" :to="`/purchases/${purchase.id}`">{{ purchase.supplierName }}</RouterLink></td><td :class="purchase.isVoided ? 'font-bold text-red-700' : ''">{{ purchase.isVoided ? 'Đã hủy' : purchase.status === 'Completed' ? 'Đã hoàn tất' : 'Nháp' }}</td><td>{{ money(purchase.totalAmount) }} ₫</td><td>{{ money(purchase.paidAmount) }} ₫</td><td>{{ money(purchase.outstandingAmount) }} ₫</td><td>{{ new Date(purchase.createdAt).toLocaleDateString('vi-VN') }}</td></tr></tbody></table></div>
    <div v-if="result && result.totalPages > 1" class="mt-4 flex items-center justify-between"><button class="btn-secondary" :disabled="page <= 1" @click="load(page - 1)">Trước</button><span>Trang {{ page }} / {{ result.totalPages }}</span><button class="btn-secondary" :disabled="page >= result.totalPages" @click="load(page + 1)">Sau</button></div>
  </section>
</template>
