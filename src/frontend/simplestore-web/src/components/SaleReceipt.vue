<script setup lang="ts">
import { ref } from 'vue'
import type { Sale } from '../api/types'

defineProps<{ sale: Sale }>()
const printError = ref('')
function printReceipt() {
  printError.value = ''
  try {
    window.print()
  } catch {
    printError.value = 'Đơn bán đã hoàn tất nhưng không thể mở hộp thoại in. Bạn có thể thử in lại.'
  }
}
defineExpose({ printReceipt, printError })
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
</script>

<template>
  <section class="card receipt mx-auto max-w-2xl" aria-label="Hóa đơn bán hàng">
    <div class="text-center"><h1 class="text-2xl font-black">{{ sale.storeName }}</h1><p>HÓA ĐƠN BÁN HÀNG</p></div>
    <div class="mt-5 grid gap-1 text-sm"><p>Mã đơn: {{ sale.id }}</p><p>Thời gian: {{ new Date(sale.completedAt).toLocaleString('vi-VN') }}</p><p>Thu ngân: {{ sale.cashierDisplayName }}</p><p v-if="sale.customer">Khách hàng: {{ sale.customer.name }}{{ sale.customer.phone ? ` · ${sale.customer.phone}` : '' }}</p></div>
    <table class="mt-5 w-full text-left text-sm"><thead class="border-y"><tr><th class="py-2">Sản phẩm</th><th>SL</th><th>Đơn giá</th><th class="text-right">Thành tiền</th></tr></thead><tbody><tr v-for="line in sale.lines" :key="line.id" class="border-b"><td class="py-2"><strong>{{ line.productName }}</strong><div class="text-xs text-slate-500">{{ line.productSku }}</div></td><td>{{ line.quantity }} {{ line.productUnit }}</td><td>{{ money(line.unitSalePrice) }} ₫</td><td class="text-right">{{ money(line.lineAmount) }} ₫</td></tr></tbody></table>
    <div class="mt-4 grid gap-2"><div class="flex justify-between text-lg"><strong>Tổng cộng</strong><strong>{{ money(sale.totalAmount) }} ₫</strong></div><div v-for="payment in sale.payments" :key="payment.id" class="flex justify-between text-sm"><span>{{ payment.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }}</span><span>{{ money(payment.amount) }} ₫</span></div><div v-if="sale.outstandingAmount > 0" class="flex justify-between font-bold text-amber-800"><span>Còn nợ</span><span>{{ money(sale.outstandingAmount) }} ₫</span></div></div>
    <p class="mt-6 text-center text-sm">Cảm ơn quý khách.</p>
    <p v-if="printError" class="error mt-4" role="alert">{{ printError }}</p>
    <button class="btn-primary no-print mt-5 w-full" type="button" @click="printReceipt">In hóa đơn</button>
  </section>
</template>

<style scoped>
@media print {
  :global(body *) { visibility: hidden; }
  .receipt, .receipt * { visibility: visible; }
  .receipt { position: absolute; inset: 0; box-shadow: none; border: 0; }
  .no-print { display: none; }
}
</style>
