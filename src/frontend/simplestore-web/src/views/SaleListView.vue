<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiRequest } from '../api/client'
import type { SalePage } from '../api/types'
const result = ref<SalePage | null>(null); const page = ref(1); const error = ref('')
async function load(value = 1) { page.value = value; try { result.value = await apiRequest<SalePage>(`/api/sales?page=${value}&pageSize=20`) } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải đơn bán.' } }
onMounted(() => load())
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
</script>
<template><section><div class="flex items-end justify-between"><div><h1 class="text-3xl font-black">Lịch sử bán hàng</h1><p class="mt-1 text-slate-500">Đơn đã hoàn tất và có thể in lại.</p></div><RouterLink class="btn-primary" to="/sales/new">Bán hàng</RouterLink></div><p v-if="error" class="error mt-4">{{ error }}</p><div class="card mt-6 overflow-x-auto p-0"><table class="w-full min-w-[720px] text-left text-sm"><thead class="border-b bg-stone-50"><tr><th class="p-4">Thời gian</th><th>Khách hàng</th><th>Thu ngân</th><th>Tổng</th><th>Còn nợ</th></tr></thead><tbody><tr v-for="sale in result?.items" :key="sale.id" class="border-b"><td class="p-4"><RouterLink class="font-semibold text-emerald-800" :to="`/sales/${sale.id}`">{{ new Date(sale.completedAt).toLocaleString('vi-VN') }}</RouterLink></td><td>{{ sale.customerName || 'Khách lẻ' }}</td><td>{{ sale.cashierDisplayName }}</td><td>{{ money(sale.totalAmount) }} ₫</td><td>{{ money(sale.outstandingAmount) }} ₫</td></tr></tbody></table></div><div v-if="result && result.totalPages > 1" class="mt-4 flex justify-end gap-3"><button class="btn-secondary" :disabled="page <= 1" @click="load(page - 1)">Trước</button><span>Trang {{ page }}/{{ result.totalPages }}</span><button class="btn-secondary" :disabled="page >= result.totalPages" @click="load(page + 1)">Sau</button></div></section></template>
