<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const submitting = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  if (!currentPassword.value || !newPassword.value || newPassword.value !== confirmPassword.value) {
    error.value = 'Nhập đúng mật khẩu tạm thời và xác nhận mật khẩu mới.'
    return
  }
  submitting.value = true
  try {
    await auth.changePassword(currentPassword.value, newPassword.value)
    await router.push(auth.session.roles.includes('Owner') ? '/today' : '/products')
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể đổi mật khẩu.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="mx-auto max-w-md">
    <div class="card">
      <h1 class="text-3xl font-black">Đổi mật khẩu tạm thời</h1>
      <p class="mt-2 text-slate-500">Tạo mật khẩu riêng trước khi tiếp tục vào các chức năng bán hàng.</p>
      <form class="mt-6 grid gap-4" @submit.prevent="submit">
        <div class="field"><label for="current-password">Mật khẩu tạm thời</label><input id="current-password" v-model="currentPassword" class="input" type="password" autocomplete="current-password" /></div>
        <div class="field"><label for="new-password">Mật khẩu mới</label><input id="new-password" v-model="newPassword" class="input" type="password" autocomplete="new-password" /></div>
        <div class="field"><label for="confirm-password">Nhập lại mật khẩu mới</label><input id="confirm-password" v-model="confirmPassword" class="input" type="password" autocomplete="new-password" /></div>
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <button class="btn-primary" type="submit" :disabled="submitting">{{ submitting ? 'Đang đổi…' : 'Đổi mật khẩu' }}</button>
      </form>
    </div>
  </section>
</template>
