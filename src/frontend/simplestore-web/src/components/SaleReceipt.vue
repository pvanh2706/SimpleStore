<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Sale } from '../api/types'

const props = defineProps<{ sale: Sale }>()
const printError = ref('')
const originalOutstanding = computed(() => Math.max(0, props.sale.totalAmount - props.sale.paidAmount))
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
    <div v-if="sale.isVoided" class="mb-4 border-2 border-red-700 p-2 text-center font-black text-red-800">ĐÃ HỦY · Giao dịch gốc lưu để đối chiếu</div>
    <div class="text-center"><h1 class="text-2xl font-black">{{ sale.storeName }}</h1><p>HÓA ĐƠN BÁN HÀNG</p></div>
    <div class="mt-5 grid gap-1 text-sm"><p>Mã đơn: {{ sale.id }}</p><p>Thời gian: {{ new Date(sale.completedAt).toLocaleString('vi-VN') }}</p><p>Thu ngân: {{ sale.cashierDisplayName }}</p><p v-if="sale.customer">Khách hàng: {{ sale.customer.name }}{{ sale.customer.phone ? ` · ${sale.customer.phone}` : '' }}</p></div>
    <div class="receipt-lines mt-5 text-sm" role="list" aria-label="Sản phẩm đã bán">
      <div v-for="line in sale.lines" :key="line.id" class="receipt-line border-b py-2" role="listitem">
        <strong class="receipt-product block">{{ line.productName }}</strong>
        <div class="receipt-sku text-xs text-slate-500">SKU: {{ line.productSku }} · ĐVT: {{ line.productUnit }}</div>
        <div class="receipt-calculation mt-1 flex justify-between gap-2">
          <span>{{ line.quantity }} {{ line.productUnit }} × {{ money(line.unitSalePrice) }} ₫</span>
          <strong class="text-right">{{ money(line.lineAmount) }} ₫</strong>
        </div>
      </div>
    </div>
    <div class="mt-4 grid gap-2"><div class="flex justify-between text-lg"><strong>Tổng cộng</strong><strong>{{ money(sale.totalAmount) }} ₫</strong></div><div v-for="payment in sale.payments" :key="payment.id" class="flex justify-between text-sm"><span>{{ payment.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }}</span><span>{{ money(payment.amount) }} ₫</span></div><div v-if="originalOutstanding > 0" class="flex justify-between font-bold text-amber-800"><span>Còn nợ gốc</span><span>{{ money(originalOutstanding) }} ₫</span></div></div>
    <div v-if="sale.totalReturnedAmount > 0" class="receipt-corrections mt-4 border-t pt-2 text-sm">
      <strong>Cập nhật sau giao dịch gốc</strong>
      <div class="flex justify-between gap-2"><span>Hàng trả (hiện tại)</span><span>{{ money(sale.totalReturnedAmount) }} ₫</span></div>
      <div class="flex justify-between gap-2"><span>Đã hoàn tiền</span><span>{{ money(sale.totalRefundedAmount) }} ₫</span></div>
      <p>Chi tiết từng lần trả hàng xem trong đơn bán đã lưu.</p>
    </div>
    <p class="mt-6 text-center text-sm">Cảm ơn quý khách.</p>
    <p v-if="printError" class="error mt-4" role="alert">{{ printError }}</p>
    <button class="btn-primary no-print mt-5 w-full" type="button" @click="printReceipt">In hóa đơn</button>
  </section>
</template>

<style scoped>
@media print {
  .receipt {
    box-sizing: border-box;
    width: 100% !important;
    max-width: none !important;
    min-width: 0;
    margin: 0 !important;
    padding: 0 !important;
    border: 0;
    border-radius: 0;
    box-shadow: none;
    color: #111;
    background: #fff;
    font-size: 9pt;
  }
  .receipt-product { overflow-wrap: normal; word-break: normal; hyphens: none; }
  .receipt-sku { overflow-wrap: anywhere; }
  .receipt-line, .receipt-corrections { break-inside: avoid; }
  .receipt-calculation { align-items: start; }
  .receipt-calculation > :first-child { min-width: 0; }
  .receipt-calculation > :last-child { flex-shrink: 0; }
  .receipt .text-slate-500, .receipt .text-amber-800, .receipt .text-red-800 { color: #111; }
  .receipt .border-red-700 { border-color: #111; }
  .no-print, .error { display: none !important; }
}
</style>
