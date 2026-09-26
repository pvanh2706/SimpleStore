<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const email = ref('')
const password = ref('')
const submitting = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  if (!email.value || !password.value) {
    error.value = 'Nhập email và mật khẩu.'
    return
  }
  submitting.value = true
  try {
    await auth.login(email.value, password.value)
    const candidate = typeof route.query.redirect === 'string' ? route.query.redirect : null
    const redirect = candidate?.startsWith('/') && !candidate.startsWith('//') && !candidate.startsWith('/login')
      ? candidate
      : null
    const defaultPath = auth.session.mustChangePassword
      ? '/change-password'
      : auth.session.hasStore
      ? (auth.session.roles.includes('Owner') ? '/today' : '/products')
      : '/setup'
    await router.push(auth.session.mustChangePassword ? defaultPath : (redirect ?? defaultPath))
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể đăng nhập.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="mx-auto mt-12 max-w-md">
    <div class="card p-7">
      <p class="text-sm font-bold uppercase tracking-widest text-emerald-700">SimpleStore</p>
      <h1 class="mt-2 text-3xl font-black">Đăng nhập cửa hàng</h1>
      <p class="mt-2 text-slate-500">Dùng tài khoản Owner đã được cấu hình cho môi trường này.</p>
      <form class="mt-7 grid gap-5" @submit.prevent="submit">
        <div class="field"><label for="email">Email</label><input id="email" v-model.trim="email" class="input" type="email" autocomplete="username" /></div>
        <div class="field"><label for="password">Mật khẩu</label><input id="password" v-model="password" class="input" type="password" autocomplete="current-password" /></div>
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <button class="btn-primary" :disabled="submitting" type="submit">{{ submitting ? 'Đang đăng nhập…' : 'Đăng nhập' }}</button>
      </form>
    </div>
  </section>
</template>
