<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { apiRequest } from '../../api/client'
import type { StoreInfo } from '../../api/types'
import { visibleNavigation } from '../../navigation'
import AppButton from '../ui/AppButton.vue'
import AppNavigation from './AppNavigation.vue'

const props = defineProps<{ email: string | null; roles: readonly string[] }>()
const emit = defineEmits<{ logout: [] }>()
const route = useRoute()
const items = computed(() => visibleNavigation(props.roles))
const storeName = ref('')
const menuOpen = ref(false)
const menuTrigger = ref<InstanceType<typeof AppButton> | null>(null)
const drawer = ref<HTMLElement | null>(null)
const closeButton = ref<InstanceType<typeof AppButton> | null>(null)
let previousOverflow: string | null = null

const roleLabel = computed(() => props.roles.includes('Owner') ? 'Chủ cửa hàng' : 'Thu ngân')

onMounted(async () => {
  window.addEventListener('keydown', onWindowKeydown)
  window.addEventListener('resize', onResize)
  try {
    const store = await apiRequest<StoreInfo | null>('/api/store/current')
    storeName.value = store?.name ?? ''
  } catch {
    // The product identity remains usable if the optional Store label cannot load.
  }
})

onUnmounted(() => {
  window.removeEventListener('keydown', onWindowKeydown)
  window.removeEventListener('resize', onResize)
  restoreScroll()
})

watch(() => route.fullPath, () => closeMenu(false))
watch(menuOpen, async open => {
  if (open) {
    previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    await nextTick()
    const closeElement = closeButton.value?.$el as HTMLElement | undefined
    closeElement?.focus()
  } else {
    restoreScroll()
  }
})

function restoreScroll() {
  if (previousOverflow !== null) {
    document.body.style.overflow = previousOverflow
    previousOverflow = null
  }
}

function closeMenu(restoreFocus = true) {
  if (!menuOpen.value) return
  menuOpen.value = false
  if (restoreFocus) nextTick(() => (menuTrigger.value?.$el as HTMLElement | undefined)?.focus())
}

function onWindowKeydown(event: KeyboardEvent) {
  if (!menuOpen.value) return
  if (event.key === 'Escape') {
    event.preventDefault()
    closeMenu()
    return
  }
  if (event.key !== 'Tab' || !drawer.value) return
  const focusable = Array.from(drawer.value.querySelectorAll<HTMLElement>(
    'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])',
  ))
  if (!focusable.length) return
  const first = focusable[0]
  const last = focusable[focusable.length - 1]
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault()
    last?.focus()
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault()
    first?.focus()
  }
}

function onResize() {
  if (window.innerWidth > 900) closeMenu(false)
}

function logout() {
  closeMenu(false)
  emit('logout')
}
</script>

<template>
  <div class="app-shell">
    <a class="app-skip-link no-print" href="#main-content" :inert="menuOpen">Chuyển đến nội dung</a>
    <aside class="app-sidebar no-print" :inert="menuOpen" aria-label="Thanh điều hướng ứng dụng">
      <RouterLink class="app-brand" to="/">
        <span class="app-brand-name">SimpleStore</span>
        <span v-if="storeName" class="app-store-name">{{ storeName }}</span>
      </RouterLink>
      <nav class="app-sidebar-body" aria-label="Điều hướng chính">
        <AppNavigation :items="items" :path="route.path" />
      </nav>
      <div class="app-account-area">
        <div class="min-w-0">
          <p class="truncate font-semibold">{{ email }}</p>
          <p class="text-sm">{{ roleLabel }}</p>
        </div>
        <AppButton variant="secondary" type="button" @click="logout">Đăng xuất</AppButton>
      </div>
    </aside>

    <header class="app-mobile-header no-print" :inert="menuOpen">
      <AppButton ref="menuTrigger" variant="secondary" type="button" aria-label="Mở điều hướng" :aria-controls="menuOpen ? 'app-mobile-menu' : undefined" :aria-expanded="menuOpen" @click="menuOpen = true">Menu</AppButton>
      <RouterLink class="app-brand-name" to="/">SimpleStore</RouterLink>
      <span class="app-mobile-store-name">{{ storeName }}</span>
    </header>

    <div v-if="menuOpen" class="app-backdrop no-print" aria-hidden="true" @click="closeMenu()" />
    <aside v-if="menuOpen" id="app-mobile-menu" ref="drawer" class="app-mobile-drawer no-print" role="dialog" aria-modal="true" aria-label="Điều hướng ứng dụng">
      <div class="app-mobile-drawer-header">
        <span class="app-brand-name">SimpleStore</span>
        <AppButton ref="closeButton" variant="secondary" type="button" aria-label="Đóng điều hướng" @click="closeMenu()">Đóng</AppButton>
      </div>
      <p v-if="storeName" class="app-store-name">{{ storeName }}</p>
      <nav aria-label="Điều hướng chính trên điện thoại">
        <AppNavigation :items="items" :path="route.path" @navigate="closeMenu(false)" />
      </nav>
      <div class="app-account-area">
        <div class="min-w-0">
          <p class="truncate font-semibold">{{ email }}</p>
          <p class="text-sm">{{ roleLabel }}</p>
        </div>
        <AppButton variant="secondary" type="button" @click="logout">Đăng xuất</AppButton>
      </div>
    </aside>

    <div class="app-workspace" :inert="menuOpen">
      <main id="main-content" class="app-main" tabindex="-1">
        <slot />
      </main>
    </div>
  </div>
</template>
