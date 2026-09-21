<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import ProductForm from '../components/ProductForm.vue'
import { apiRequest } from '../api/client'
import type { Product, ProductInput } from '../api/types'

const route = useRoute(); const router = useRouter()
const product = ref<Product | null>(null); const loading = ref(false); const submitting = ref(false); const error = ref('')
const id = typeof route.params.id === 'string' ? route.params.id : null

onMounted(async () => {
  if (!id) return
  loading.value = true
  try { product.value = await apiRequest<Product>(`/api/products/${id}`) }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải sản phẩm.' }
  finally { loading.value = false }
})

async function save(input: ProductInput) {
  submitting.value = true; error.value = ''
  try {
    const saved = await apiRequest<Product>(id ? `/api/products/${id}` : '/api/products', { method: id ? 'PUT' : 'POST', body: JSON.stringify(input) })
    await router.push(`/products/${saved.id}`)
  } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể lưu sản phẩm.' }
  finally { submitting.value = false }
}
</script>

<template>
  <section class="mx-auto max-w-3xl">
    <RouterLink class="text-sm font-semibold text-emerald-800" to="/products">← Danh sách sản phẩm</RouterLink>
    <h1 class="mb-6 mt-3 text-3xl font-black">{{ id ? 'Sửa sản phẩm' : 'Thêm sản phẩm' }}</h1>
    <p v-if="loading" class="text-slate-500">Đang tải…</p>
    <p v-if="error" class="error mb-4" role="alert">{{ error }}</p>
    <ProductForm v-if="!id || product" :product="product" :submitting="submitting" @submit="save" />
  </section>
</template>
