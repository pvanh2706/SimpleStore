<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { apiRequest } from '../api/client'
import type { InventoryMovement, Product } from '../api/types'
import { useAuthStore } from '../stores/auth'

const route = useRoute(); const id = String(route.params.id)
const product = ref<Product | null>(null); const movements = ref<InventoryMovement[]>([]); const loading = ref(true); const error = ref(''); const deactivating = ref(false)
const auth = useAuthStore()
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)
const movementLabel = (type: string) => type === 'Purchase' ? 'Nhập hàng' : type === 'Sale' ? 'Bán hàng' : 'Tồn đầu'

async function load() {
  loading.value = true
  try { [product.value, movements.value] = await Promise.all([apiRequest<Product>(`/api/products/${id}`), apiRequest<InventoryMovement[]>(`/api/products/${id}/movements`)]) }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải sản phẩm.' }
  finally { loading.value = false }
}
async function deactivate() {
  if (!confirm('Ngừng bán sản phẩm này?')) return
  deactivating.value = true
  try { await apiRequest<void>(`/api/products/${id}/deactivate`, { method: 'POST', body: '{}' }); await load() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể ngừng bán.' }
  finally { deactivating.value = false }
}
onMounted(load)
</script>

<template>
  <section>
    <RouterLink class="text-sm font-semibold text-emerald-800" to="/products">← Danh sách sản phẩm</RouterLink>
    <p v-if="loading" class="mt-6 text-slate-500">Đang tải…</p><p v-if="error" class="error mt-5">{{ error }}</p>
    <template v-if="product">
      <div class="mt-3 flex flex-wrap items-start justify-between gap-4"><div><h1 class="text-3xl font-black">{{ product.name }}</h1><p class="mt-1 text-slate-500">{{ product.sku }} · {{ product.barcode || 'Không có barcode' }}</p></div><div v-if="auth.session.roles.includes('Owner')" class="flex gap-2"><RouterLink class="btn-secondary" :to="`/products/${id}/edit`">Sửa</RouterLink><button v-if="product.isActive" class="btn-secondary" :disabled="deactivating" @click="deactivate">Ngừng bán</button></div></div>
      <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4"><div class="card"><p class="text-sm text-slate-500">Tồn hiện tại</p><p class="mt-1 text-2xl font-black">{{ product.quantityOnHand }} {{ product.unit }}</p></div><div class="card"><p class="text-sm text-slate-500">Giá trị tồn</p><p class="mt-1 text-2xl font-black">{{ money(product.inventoryValue) }} ₫</p></div><div class="card"><p class="text-sm text-slate-500">Giá vốn bình quân</p><p class="mt-1 text-2xl font-black">{{ product.hasAverageCost ? `${money(product.averageCost)} ₫` : 'Chưa đủ tin cậy' }}</p></div><div class="card"><p class="text-sm text-slate-500">Giá bán</p><p class="mt-1 text-2xl font-black">{{ money(product.salePrice) }} ₫</p></div></div>
      <div class="card mt-6"><h2 class="text-xl font-black">Nguồn tồn kho</h2><p v-if="movements.length === 0" class="mt-4 text-slate-500">Sản phẩm chưa có biến động tồn.</p><div v-for="movement in movements" :key="movement.id" class="mt-4 flex flex-wrap justify-between gap-3 border-t pt-4"><div><p class="font-semibold">{{ movementLabel(movement.type) }}</p><p class="text-sm text-slate-500">{{ new Date(movement.occurredAt).toLocaleString('vi-VN') }}</p></div><div class="text-right"><p class="font-semibold">{{ movement.quantityDelta > 0 ? '+' : '' }}{{ movement.quantityDelta }} {{ product.unit }}</p><p class="text-sm text-slate-500">{{ money(movement.inventoryValueDelta) }} ₫</p></div></div></div>
    </template>
  </section>
</template>
