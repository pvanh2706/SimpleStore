<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiRequest } from '../api/client'
import type { EndOfDayReport, StoreInfo } from '../api/types'

const date = ref('')
const report = ref<EndOfDayReport | null>(null)
const loading = ref(false)
const message = ref('')
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)

function localDate(timeZone: string) {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone, year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(new Date())
  const value = Object.fromEntries(parts.map(part => [part.type, part.value]))
  return `${value.year}-${value.month}-${value.day}`
}

async function load() {
  if (!date.value) return
  loading.value = true
  message.value = ''
  try {
    report.value = await apiRequest<EndOfDayReport>(
      `/api/reports/end-of-day?date=${encodeURIComponent(date.value)}`)
  } catch (reason) {
    message.value = reason instanceof Error ? reason.message : 'Không thể tải báo cáo cuối ngày.'
  } finally { loading.value = false }
}

onMounted(async () => {
  try {
    const store = await apiRequest<StoreInfo>('/api/store/current')
    date.value = localDate(store.timeZoneId)
    await load()
  } catch (reason) { message.value = reason instanceof Error ? reason.message : 'Không thể tải báo cáo.' }
})
</script>

<template>
  <section>
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div><h1 class="text-3xl font-black">Báo cáo cuối ngày</h1><p class="mt-1 text-slate-500">Doanh thu theo ngày sự kiện; công nợ là số dư tại cuối ngày.</p></div>
      <form class="flex items-end gap-2" @submit.prevent="load"><label class="field"><span>Ngày kinh doanh</span><input v-model="date" class="input" type="date" required /></label><button class="btn-primary" :disabled="loading">{{ loading ? 'Đang tải…' : 'Xem báo cáo' }}</button></form>
    </div>
    <p v-if="message" class="error mt-4" role="alert">{{ message }}</p>
    <template v-if="report">
      <p class="mt-5 text-sm text-slate-600">Múi giờ <strong>{{ report.timeZoneId }}</strong> · {{ new Date(report.startUtc).toLocaleString('vi-VN') }} đến trước {{ new Date(report.endUtc).toLocaleString('vi-VN') }}</p>
      <div class="mt-5 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <section class="card"><h2 class="font-bold text-slate-500">Doanh thu</h2><p class="mt-2 text-3xl font-black">{{ money(report.salesRevenue) }} ₫</p><p class="mt-3 text-sm">Theo ngày phát sinh bán, trả hàng và hủy giao dịch.</p></section>
        <section class="card"><h2 class="font-bold text-slate-500">Tiền thu thuần</h2><p class="mt-2 text-3xl font-black">{{ money(report.collected.netAmount) }} ₫</p><p class="mt-3 text-sm">Thanh toán bán hàng + thu nợ − hoàn tiền.</p></section>
        <section class="card"><h2 class="font-bold text-slate-500">Lãi gộp ước tính</h2><p class="mt-2 text-3xl font-black">{{ money(report.estimatedGrossProfit.amount) }} ₫</p><p class="mt-3 text-sm">Giá vốn lịch sử {{ money(report.estimatedGrossProfit.historicalCogs) }} · Độ tin cậy: <strong>{{ report.estimatedGrossProfit.costReliability }}</strong></p><p v-if="report.estimatedGrossProfit.costReliability === 'Unavailable'" class="mt-2 text-sm font-semibold text-amber-800">Dữ liệu giá vốn chưa đủ tin cậy; đây không phải lợi nhuận kế toán.</p></section>
        <section class="card"><h2 class="font-bold text-slate-500">Thanh toán nhà cung cấp</h2><p class="mt-2 text-3xl font-black">{{ money(report.supplierPayments.totalAmount) }} ₫</p><p class="mt-3 text-sm">Phiếu nhập {{ money(report.supplierPayments.purchasePayments) }} · Trả nợ cũ {{ money(report.supplierPayments.supplierDebtPayments) }}</p></section>
      </div>
      <div class="mt-5 grid gap-5 lg:grid-cols-2">
        <section class="card"><h2 class="text-xl font-black">Chi tiết tiền thu</h2><div class="mt-4 grid gap-2"><p class="flex justify-between"><span>Thanh toán đơn bán</span><strong>{{ money(report.collected.salePayments) }} ₫</strong></p><p class="flex justify-between"><span>Thu nợ khách hàng</span><strong>{{ money(report.collected.customerDebtPayments) }} ₫</strong></p><p class="flex justify-between"><span>Hoàn tiền khách hàng</span><strong>-{{ money(report.collected.customerRefunds) }} ₫</strong></p><p class="flex justify-between border-t pt-2"><span>Tiền thu thuần</span><strong>{{ money(report.collected.netAmount) }} ₫</strong></p></div></section>
        <section class="card"><h2 class="text-xl font-black">Công nợ cuối ngày</h2><div class="mt-4 grid gap-2"><p class="flex justify-between"><span>Khách hàng</span><strong>{{ money(report.customerOutstandingDebtAtEnd) }} ₫</strong></p><p class="flex justify-between"><span>Nhà cung cấp</span><strong>{{ money(report.supplierOutstandingDebtAtEnd) }} ₫</strong></p></div></section>
      </div>
      <section class="card mt-5"><h2 class="text-xl font-black">Lãi gộp ước tính</h2><p class="mt-3">Doanh thu thuần {{ money(report.estimatedGrossProfit.netSalesRevenue) }} ₫ − giá vốn lịch sử {{ money(report.estimatedGrossProfit.historicalCogs) }} ₫ = <strong>{{ money(report.estimatedGrossProfit.amount) }} ₫</strong>.</p></section>
    </template>
  </section>
</template>
