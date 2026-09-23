<script setup lang="ts">
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const router = useRouter()

async function logout() {
  await auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div class="min-h-screen bg-stone-50 text-slate-900">
    <header v-if="auth.session.isAuthenticated" class="border-b border-stone-200 bg-white">
      <div class="mx-auto flex max-w-6xl flex-wrap items-center gap-5 px-5 py-4">
        <RouterLink class="text-xl font-black tracking-tight text-emerald-800" to="/products">SimpleStore</RouterLink>
        <nav v-if="auth.session.hasStore" class="flex flex-1 gap-4 text-sm font-semibold">
          <RouterLink class="nav-link" to="/sales/new">Bán hàng</RouterLink>
          <RouterLink class="nav-link" to="/sales">Đơn bán</RouterLink>
          <RouterLink class="nav-link" to="/products">Sản phẩm</RouterLink>
          <RouterLink class="nav-link" to="/customers/debts">Công nợ khách</RouterLink>
          <template v-if="auth.session.roles.includes('Owner')">
            <RouterLink class="nav-link" to="/suppliers/debts">Công nợ NCC</RouterLink>
            <RouterLink class="nav-link" to="/reports/end-of-day">Cuối ngày</RouterLink>
            <RouterLink class="nav-link" to="/import">Nhập từ CSV</RouterLink>
            <RouterLink class="nav-link" to="/suppliers">Nhà cung cấp</RouterLink>
            <RouterLink class="nav-link" to="/purchases">Nhập hàng</RouterLink>
            <RouterLink class="nav-link" to="/settings/operations">Thiết lập</RouterLink>
          </template>
        </nav>
        <span class="ml-auto hidden text-sm text-slate-500 sm:inline">{{ auth.session.email }}</span>
        <button class="btn-secondary" type="button" @click="logout">Đăng xuất</button>
      </div>
    </header>
    <main class="mx-auto max-w-6xl px-5 py-8">
      <RouterView />
    </main>
  </div>
</template>
