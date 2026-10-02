<script setup lang="ts">
import { computed, watch } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import AppShell from './components/app/AppShell.vue'
import AppButton from './components/ui/AppButton.vue'
import AppSkeleton from './components/ui/AppSkeleton.vue'
import { useAuthStore } from './stores/auth'
import { canLeaveSales } from './sales/checkoutGuard'
import { liveOrderBook } from './sales/orders'

const auth = useAuthStore()
const router = useRouter()
const showShell = computed(() => auth.initialized && auth.session.isAuthenticated
  && auth.session.hasStore && !auth.session.mustChangePassword)

/**
 * The live Sales working order lives in RAM for one authenticated session only (D-107). It survives
 * route changes, and is cleared when the session ends (logout, expiry) or another user signs in.
 * A failed logout keeps the session, so it keeps the order too.
 */
const sessionIdentity = computed(() => auth.session.isAuthenticated
  ? `${auth.session.storeId ?? ''}|${auth.session.email ?? ''}` : null)
watch(sessionIdentity, () => liveOrderBook.reset())

async function logout() {
  // Signing out would abandon a live CompleteSale whose outcome is unknown; the checkout explains why it stays.
  if (!canLeaveSales()) return
  await auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div v-if="!auth.initialized" class="auth-layout min-h-screen px-5 py-8">
    <p class="mx-auto max-w-3xl text-xl font-black text-emerald-800">SimpleStore</p>
    <AppSkeleton class="mx-auto mt-8 h-20 max-w-3xl" aria-label="Đang tải ứng dụng" />
  </div>
  <AppShell v-else-if="showShell" :email="auth.session.email" :roles="auth.session.roles" @logout="logout">
    <RouterView />
  </AppShell>
  <div v-else class="auth-layout min-h-screen">
    <header v-if="auth.session.isAuthenticated" class="no-print border-b border-stone-200 bg-white">
      <div class="mx-auto flex max-w-4xl items-center justify-between gap-4 px-5 py-4">
        <RouterLink class="text-xl font-black text-emerald-800" to="/">SimpleStore</RouterLink>
        <AppButton variant="secondary" type="button" @click="logout">Đăng xuất</AppButton>
      </div>
    </header>
    <main class="mx-auto w-full max-w-4xl px-5 py-8">
      <p v-if="auth.session.mustChangePassword" class="mb-5 rounded-lg bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-800">
        Bạn phải đổi mật khẩu tạm thời trước khi sử dụng cửa hàng.
      </p>
      <RouterView />
    </main>
  </div>
</template>
