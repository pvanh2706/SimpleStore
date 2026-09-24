<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { apiRequest } from '../api/client'
import PurchaseDraftForm from '../components/PurchaseDraftForm.vue'
import type { Product, ProductListItem, ProductPage, Purchase, PurchaseWriteInput, SupplierPage } from '../api/types'

const route = useRoute()
const router = useRouter()
const id = typeof route.params.id === 'string' ? route.params.id : null
const purchase = ref<Purchase | null>(null)
const preselectedProduct = ref<ProductListItem | null>(null)
const loading = ref(true)
const saving = ref(false)
const error = ref('')

async function searchSuppliers(search: string, page: number) {
  const params = new URLSearchParams({ isActive: 'true', page: String(page), pageSize: '20' })
  if (search) params.set('search', search)
  return apiRequest<SupplierPage>(`/api/suppliers?${params}`)
}

async function searchProducts(search: string, page: number) {
  const params = new URLSearchParams({ isActive: 'true', page: String(page), pageSize: '20' })
  if (search) params.set('search', search)
  return apiRequest<ProductPage>(`/api/products?${params}`)
}

async function load() {
  try {
    if (id) purchase.value = await apiRequest<Purchase>(`/api/purchases/${id}`)
    else if (typeof route.query.productId === 'string') {
      try {
        const product = await apiRequest<Product>(`/api/products/${route.query.productId}`)
        if (product.isActive) {
          preselectedProduct.value = {
            id: product.id,
            sku: product.sku,
            barcode: product.barcode,
            name: product.name,
            unit: product.unit,
            salePrice: product.salePrice,
            isActive: product.isActive,
            quantityOnHand: product.quantityOnHand,
          }
        }
      } catch {
        preselectedProduct.value = null
      }
    }
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể tải dữ liệu.'
  } finally { loading.value = false }
}

async function save(value: PurchaseWriteInput) {
  saving.value = true
  error.value = ''
  try {
    const result = await apiRequest<Purchase>(id ? `/api/purchases/${id}` : '/api/purchases', {
      method: id ? 'PUT' : 'POST', body: JSON.stringify(value),
    })
    await router.push(`/purchases/${result.id}`)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể lưu phiếu nhập.'
  } finally { saving.value = false }
}

onMounted(load)
</script>

<template>
  <section>
    <RouterLink class="text-sm font-semibold text-emerald-800" to="/purchases">← Danh sách phiếu nhập</RouterLink>
    <h1 class="mt-3 text-3xl font-black">{{ id ? 'Sửa phiếu nhập' : 'Tạo phiếu nhập' }}</h1>
    <p v-if="loading" class="mt-6 text-slate-500">Đang tải…</p>
    <p v-if="error" class="error mt-4">{{ error }}</p>
    <PurchaseDraftForm
      v-if="!loading"
      class="mt-6"
      :search-suppliers="searchSuppliers"
      :search-products="searchProducts"
      :initial="purchase"
      :preselected-product="preselectedProduct"
      :disabled="purchase?.status === 'Completed'"
      :saving="saving"
      @save="save"
    />
  </section>
</template>
