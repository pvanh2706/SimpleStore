<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { apiRequest } from '../api/client'
import PurchaseDraftForm from '../components/PurchaseDraftForm.vue'
import type { ProductPage, Purchase, PurchaseWriteInput, SupplierPage } from '../api/types'

const route = useRoute(); const router = useRouter(); const id = typeof route.params.id === 'string' ? route.params.id : null
const suppliers = ref<SupplierPage | null>(null); const products = ref<ProductPage | null>(null); const purchase = ref<Purchase | null>(null); const loading = ref(true); const saving = ref(false); const error = ref('')
async function load() { try { const [supplierPage, productPage] = await Promise.all([apiRequest<SupplierPage>('/api/suppliers?isActive=true&page=1&pageSize=100'), apiRequest<ProductPage>('/api/products?isActive=true&page=1&pageSize=100')] as const); suppliers.value = supplierPage; products.value = productPage; if (id) purchase.value = await apiRequest<Purchase>(`/api/purchases/${id}`) } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải dữ liệu.' } finally { loading.value = false } }
async function save(value: PurchaseWriteInput) { saving.value = true; error.value = ''; try { const result = await apiRequest<Purchase>(id ? `/api/purchases/${id}` : '/api/purchases', { method: id ? 'PUT' : 'POST', body: JSON.stringify(value) }); await router.push(`/purchases/${result.id}`) } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể lưu phiếu nhập.' } finally { saving.value = false } }
onMounted(load)
</script>

<template><section><RouterLink class="text-sm font-semibold text-emerald-800" to="/purchases">← Danh sách phiếu nhập</RouterLink><h1 class="mt-3 text-3xl font-black">{{ id ? 'Sửa phiếu nhập' : 'Tạo phiếu nhập' }}</h1><p v-if="loading" class="mt-6 text-slate-500">Đang tải…</p><p v-if="error" class="error mt-4">{{ error }}</p><div v-if="!loading && suppliers && products" class="mt-6"><p v-if="suppliers.items.length === 0" class="error">Cần tạo nhà cung cấp trước khi lập phiếu nhập.</p><PurchaseDraftForm v-else :suppliers="suppliers.items" :products="products.items" :initial="purchase" :disabled="purchase?.status === 'Completed'" :saving="saving" @save="save" /></div></section></template>
