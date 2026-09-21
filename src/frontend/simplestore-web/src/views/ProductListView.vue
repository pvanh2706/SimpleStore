<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiRequest } from '../api/client'
import type { ProductPage } from '../api/types'

const result = ref<ProductPage | null>(null)
const search = ref('')
const active = ref('true')
const page = ref(1)
const loading = ref(false)
const error = ref('')

async function load(requestedPage = 1) {
  loading.value = true; error.value = ''; page.value = requestedPage
  const params = new URLSearchParams({ page: String(page.value), pageSize: '20' })
  if (search.value.trim()) params.set('search', search.value.trim())
  if (active.value) params.set('isActive', active.value)
  try { result.value = await apiRequest<ProductPage>(`/api/products?${params}`) }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải sản phẩm.' }
  finally { loading.value = false }
}

onMounted(() => load())
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
</script>

<template>
  <section>
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div><h1 class="text-3xl font-black">Sản phẩm</h1><p class="mt-1 text-slate-500">Tra cứu hàng hóa và tồn tại Kho chính.</p></div>
      <div class="flex gap-2"><RouterLink class="btn-secondary" to="/import">Nhập CSV</RouterLink><RouterLink class="btn-primary" to="/products/new">Thêm sản phẩm</RouterLink></div>
    </div>
    <form class="card mt-6 grid gap-3 md:grid-cols-[1fr_180px_auto]" @submit.prevent="load(1)">
      <input v-model="search" class="input" aria-label="Tìm sản phẩm" placeholder="Tên, SKU hoặc barcode" />
      <select v-model="active" class="input" aria-label="Trạng thái"><option value="true">Đang bán</option><option value="false">Ngừng bán</option><option value="">Tất cả</option></select>
      <button class="btn-secondary" type="submit">Tìm kiếm</button>
    </form>
    <p v-if="error" class="error mt-4" role="alert">{{ error }}</p>
    <div class="card mt-5 overflow-x-auto p-0">
      <p v-if="loading" class="p-8 text-center text-slate-500">Đang tải…</p>
      <p v-else-if="result?.items.length === 0" class="p-8 text-center text-slate-500">Chưa có sản phẩm phù hợp.</p>
      <table v-else class="w-full min-w-[700px] text-left text-sm">
        <thead class="border-b bg-stone-50 text-slate-500"><tr><th class="p-4">Sản phẩm</th><th>SKU / Barcode</th><th>Giá bán</th><th>Tồn hiện tại</th><th>Trạng thái</th></tr></thead>
        <tbody><tr v-for="item in result?.items" :key="item.id" class="border-b last:border-0">
          <td class="p-4 font-semibold"><RouterLink class="text-emerald-800 hover:underline" :to="`/products/${item.id}`">{{ item.name }}</RouterLink></td>
          <td><div>{{ item.sku }}</div><div class="text-slate-400">{{ item.barcode || '—' }}</div></td><td>{{ money(item.salePrice) }} ₫</td><td>{{ item.quantityOnHand }} {{ item.unit }}</td><td>{{ item.isActive ? 'Đang bán' : 'Ngừng bán' }}</td>
        </tr></tbody>
      </table>
    </div>
    <div v-if="result && result.totalPages > 1" class="mt-4 flex items-center justify-end gap-3"><button class="btn-secondary" :disabled="page <= 1" @click="load(page - 1)">Trước</button><span>Trang {{ page }}/{{ result.totalPages }}</span><button class="btn-secondary" :disabled="page >= result.totalPages" @click="load(page + 1)">Sau</button></div>
  </section>
</template>
