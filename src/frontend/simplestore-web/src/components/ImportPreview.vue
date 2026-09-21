<script setup lang="ts">
import type { ImportValidationResult } from '../api/types'
defineProps<{ result: ImportValidationResult }>()
</script>

<template>
  <div class="grid gap-5">
    <div v-if="result.errors.length" class="rounded-xl border border-red-200 bg-red-50 p-4">
      <h2 class="font-bold text-red-800">Cần sửa {{ result.errors.length }} lỗi</h2>
      <ul class="mt-3 grid gap-2 text-sm text-red-700"><li v-for="(item, index) in result.errors" :key="`${item.rowNumber}-${item.field}-${index}`"><strong>{{ item.rowNumber ? `Dòng ${item.rowNumber}` : 'Tệp' }} · {{ item.field }}:</strong> {{ item.message }}</li></ul>
    </div>
    <div v-if="result.rows.length" class="card overflow-x-auto p-0">
      <table class="w-full min-w-[760px] text-left text-sm"><thead class="border-b bg-stone-50"><tr><th class="p-3">Dòng</th><th>SKU</th><th>Tên hàng</th><th>Đơn vị</th><th>Giá bán</th><th>Tồn đầu</th><th>Giá vốn đầu</th></tr></thead><tbody><tr v-for="row in result.rows" :key="row.rowNumber" class="border-b last:border-0"><td class="p-3">{{ row.rowNumber }}</td><td>{{ row.sku }}</td><td>{{ row.name }}</td><td>{{ row.unit }}</td><td>{{ row.salePrice }}</td><td>{{ row.openingQuantity }}</td><td>{{ row.openingCost ?? '—' }}</td></tr></tbody></table>
    </div>
  </div>
</template>
