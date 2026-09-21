<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { apiRequest } from '../api/client'
import type { StoreInfo } from '../api/types'
import { useAuthStore } from '../stores/auth'

const name = ref('')
const submitting = ref(false)
const error = ref('')
const auth = useAuthStore()
const router = useRouter()

async function submit() {
  error.value = ''
  if (!name.value.trim()) { error.value = 'Nhập tên cửa hàng.'; return }
  submitting.value = true
  try {
    await apiRequest<StoreInfo>('/api/store/initialize', { method: 'POST', body: JSON.stringify({ name: name.value }) })
    await auth.loadSession(true)
    await router.push('/products')
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể tạo cửa hàng.'
  } finally { submitting.value = false }
}
</script>

<template>
  <section class="mx-auto max-w-xl">
    <div class="card p-7">
      <p class="text-sm font-bold text-emerald-700">BƯỚC THIẾT LẬP</p>
      <h1 class="mt-2 text-3xl font-black">Đặt tên cửa hàng</h1>
      <p class="mt-2 text-slate-500">SimpleStore sẽ tự tạo Kho chính. Bạn không cần cấu hình kho thủ công.</p>
      <form class="mt-7 grid gap-5" @submit.prevent="submit">
        <div class="field"><label for="store-name">Tên cửa hàng</label><input id="store-name" v-model.trim="name" class="input" maxlength="120" autofocus /></div>
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <button class="btn-primary" :disabled="submitting" type="submit">{{ submitting ? 'Đang khởi tạo…' : 'Bắt đầu quản lý sản phẩm' }}</button>
      </form>
    </div>
  </section>
</template>
