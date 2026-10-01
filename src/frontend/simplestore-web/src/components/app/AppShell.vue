<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { apiRequest } from '../../api/client'
import type { StoreInfo } from '../../api/types'
import { visibleNavigation } from '../../navigation'
import AppButton from '../ui/AppButton.vue'
import AppNavigation from './AppNavigation.vue'
import TodayDateControl from './TodayDateControl.vue'
import LineIcon from '../ui/LineIcon'
import { demoIdentity, salesDemoEnabled } from '../../sales/demo'
import { productDemoEnabled } from '../../products/demo'
import { purchaseDemoEnabled } from '../../purchases/demo'
import { inventoryDemoEnabled } from '../../inventory/demo'
import { debtDemoEnabled } from '../../debts/demo'
import { dayCloseDemoEnabled } from '../../dayclose/demo'
import { todayDemoEnabled } from '../../today/demo'
import { settingsDemoEnabled } from '../../settings/demo'

const props = defineProps<{ email: string | null; roles: readonly string[] }>()
const emit = defineEmits<{ logout: [] }>()
const route = useRoute()
const items = computed(() => visibleNavigation(props.roles))
const storeName = ref('')
const menuOpen = ref(false)
const sidebarCollapsed = ref(false)
const menuTrigger = ref<InstanceType<typeof AppButton> | null>(null)
const drawer = ref<HTMLElement | null>(null)
const closeButton = ref<InstanceType<typeof AppButton> | null>(null)
let previousOverflow: string | null = null

const roleLabel = computed(() => props.roles.includes('Owner') ? 'Chủ cửa hàng' : 'Thu ngân')
const isSalesWorkspace = computed(() => route.path.startsWith('/sales/new'))
const isProductListWorkspace = computed(() => route.path === '/products' && route.query.view !== 'inventory')
const isPurchaseListWorkspace = computed(() => route.path === '/purchases')
const isInventoryWorkspace = computed(() => route.path === '/products' && route.query.view === 'inventory')
const isDebtWorkspace = computed(() => route.path === '/customers/debts' || route.path === '/suppliers/debts')
const isDayCloseWorkspace = computed(() => route.path === '/day-close')
const isTodayWorkspace = computed(() => route.path === '/today')
const isSettingsWorkspace = computed(() => route.path === '/settings/operations')
const now = ref(new Date())
const dateLabel = computed(() => {
  const value = now.value
  const weekday = value.getDay() === 0 ? 'Chủ nhật' : `Thứ ${value.getDay() + 1}`
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${weekday}, ${pad(value.getDate())}/${pad(value.getMonth() + 1)}/${value.getFullYear()}\u00a0\u00a0${pad(value.getHours())}:${pad(value.getMinutes())}`
})
const accountLabel = computed(() => props.email?.split('@')[0] || roleLabel.value)
const demoActive = computed(() => (isSalesWorkspace.value && salesDemoEnabled.value)
  || (isProductListWorkspace.value && productDemoEnabled.value)
  || (isPurchaseListWorkspace.value && purchaseDemoEnabled.value)
  || (isInventoryWorkspace.value && inventoryDemoEnabled.value)
  || (isDebtWorkspace.value && debtDemoEnabled.value)
  || (isDayCloseWorkspace.value && dayCloseDemoEnabled.value)
  || (isTodayWorkspace.value && todayDemoEnabled.value)
  || (isSettingsWorkspace.value && settingsDemoEnabled.value))
const shownStoreName = computed(() => demoActive.value ? demoIdentity.storeName : storeName.value || 'SimpleStore')
const shownDate = computed(() => demoActive.value ? demoIdentity.dateLabel : dateLabel.value)
const userLabel = computed(() => demoActive.value ? demoIdentity.userName : accountLabel.value)
const initials = computed(() => {
  if (demoActive.value) return demoIdentity.initials
  const parts = accountLabel.value.split(/[\s._-]+/).filter(Boolean)
  return (parts.length > 1 ? parts[0]!.charAt(0) + parts[1]!.charAt(0) : accountLabel.value.slice(0, 2)).toUpperCase()
})
let clockTimer: ReturnType<typeof setInterval> | undefined

onMounted(async () => {
  clockTimer = setInterval(() => { now.value = new Date() }, 60_000)
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
  if (clockTimer) clearInterval(clockTimer)
  salesDemoEnabled.value = false
  productDemoEnabled.value = false
  purchaseDemoEnabled.value = false
  inventoryDemoEnabled.value = false
  debtDemoEnabled.value = false
  dayCloseDemoEnabled.value = false
  todayDemoEnabled.value = false
  settingsDemoEnabled.value = false
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
  if (!menuOpen.value && isSalesWorkspace.value && (event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
    event.preventDefault()
    focusProductSearch()
    return
  }
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

function focusProductSearch() {
  // The live and the preview checkout can both be mounted; focus the visible one.
  Array.from(document.querySelectorAll<HTMLInputElement>('.sales-pos__search-box input'))
    .find(input => input.offsetParent !== null)?.focus()
}

function onResize() {
  if (window.innerWidth > 900) closeMenu(false)
}

function logout() {
  closeMenu(false)
  salesDemoEnabled.value = false
  purchaseDemoEnabled.value = false
  inventoryDemoEnabled.value = false
  debtDemoEnabled.value = false
  dayCloseDemoEnabled.value = false
  todayDemoEnabled.value = false
  settingsDemoEnabled.value = false
  // The live Sales working order is reset by App once the session actually ends.
  emit('logout')
}
</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--sales': isSalesWorkspace, 'app-shell--today': isTodayWorkspace, 'app-shell--settings': isSettingsWorkspace, 'app-shell--compact': isProductListWorkspace || isPurchaseListWorkspace || isInventoryWorkspace || isDebtWorkspace || isDayCloseWorkspace, 'app-shell--collapsed': sidebarCollapsed, 'app-shell--demo': demoActive }">
    <a class="app-skip-link no-print" href="#main-content" :inert="menuOpen">Chuyển đến nội dung</a>
    <aside class="app-sidebar no-print" :inert="menuOpen" aria-label="Thanh điều hướng ứng dụng">
      <RouterLink class="app-brand" to="/">
        <span class="app-brand-icon" aria-hidden="true"><LineIcon name="brand" /></span>
        <span class="app-brand-name">SimpleStore</span>
        <span v-if="storeName || demoActive" class="app-store-name">{{ demoActive ? demoIdentity.storeName : storeName }}</span>
      </RouterLink>
      <nav class="app-sidebar-body" aria-label="Điều hướng chính">
        <AppNavigation :items="items" :path="route.fullPath" sales-layout />
      </nav>
      <button class="app-sidebar-collapse" type="button" :aria-label="sidebarCollapsed ? 'Mở rộng menu' : 'Thu gọn menu'" :aria-expanded="!sidebarCollapsed" @click="sidebarCollapsed = !sidebarCollapsed"><LineIcon :name="sidebarCollapsed ? 'expand' : 'collapse'" /><span>{{ sidebarCollapsed ? 'Mở rộng' : 'Thu gọn' }}</span></button>
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
      <label v-if="isSalesWorkspace || isProductListWorkspace || isPurchaseListWorkspace || isInventoryWorkspace || isDebtWorkspace || isDayCloseWorkspace || isTodayWorkspace || isSettingsWorkspace" class="app-demo-toggle" title="Hiển thị dữ liệu mẫu">
        <input v-if="isSalesWorkspace" v-model="salesDemoEnabled" type="checkbox" aria-label="Dữ liệu mẫu Bán hàng trên điện thoại" />
        <input v-else-if="isProductListWorkspace" v-model="productDemoEnabled" type="checkbox" aria-label="Dữ liệu mẫu Sản phẩm trên điện thoại" />
        <input v-else-if="isPurchaseListWorkspace" v-model="purchaseDemoEnabled" type="checkbox" aria-label="Dữ liệu mẫu Nhập hàng trên điện thoại" />
        <input v-else-if="isInventoryWorkspace" v-model="inventoryDemoEnabled" type="checkbox" aria-label="Dữ liệu mẫu Tồn kho trên điện thoại" />
        <input v-else-if="isDebtWorkspace" v-model="debtDemoEnabled" type="checkbox" aria-label="Dữ liệu mẫu Công nợ trên điện thoại" />
        <input v-else-if="isDayCloseWorkspace" v-model="dayCloseDemoEnabled" type="checkbox" aria-label="Dữ liệu mẫu Đóng ngày trên điện thoại" />
        <input v-else-if="isTodayWorkspace" v-model="todayDemoEnabled" type="checkbox" aria-label="Dữ liệu mẫu Tổng quan trên điện thoại" />
        <input v-else v-model="settingsDemoEnabled" type="checkbox" aria-label="Dữ liệu mẫu Cài đặt trên điện thoại" />
        <span>Mẫu</span>
      </label>
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
        <AppNavigation :items="items" :path="route.fullPath" sales-layout expand-extras @navigate="closeMenu(false)" />
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
      <header class="app-topbar no-print">
        <!-- A single Store per account today: the chevron mirrors the reference, there is no Store switcher yet. -->
        <div class="app-topbar-store" :title="shownStoreName"><span>{{ shownStoreName }}</span><LineIcon name="chevron" /></div>
        <div class="app-topbar-actions">
          <button v-if="isSalesWorkspace" class="app-mock-toggle" type="button" :aria-pressed="salesDemoEnabled" title="Hiển thị dữ liệu mẫu giống bản thiết kế; không lưu giao dịch" @click="salesDemoEnabled = !salesDemoEnabled"><LineIcon name="box" />Dữ liệu mẫu</button>
          <button v-if="isProductListWorkspace" class="app-mock-toggle" type="button" :aria-pressed="productDemoEnabled" title="Hiển thị dữ liệu mẫu giống bản thiết kế; không lưu sản phẩm" @click="productDemoEnabled = !productDemoEnabled"><LineIcon name="box" />Dữ liệu mẫu</button>
          <button v-if="isPurchaseListWorkspace" class="app-mock-toggle" type="button" :aria-pressed="purchaseDemoEnabled" title="Hiển thị dữ liệu mẫu giống bản thiết kế; không lưu phiếu nhập" @click="purchaseDemoEnabled = !purchaseDemoEnabled"><LineIcon name="box" />Dữ liệu mẫu</button>
          <button v-if="isInventoryWorkspace" class="app-mock-toggle" type="button" :aria-pressed="inventoryDemoEnabled" title="Hiển thị dữ liệu mẫu giống bản thiết kế; không ghi tồn kho" @click="inventoryDemoEnabled = !inventoryDemoEnabled"><LineIcon name="box" />Dữ liệu mẫu</button>
          <button v-if="isDebtWorkspace" class="app-mock-toggle" type="button" :aria-pressed="debtDemoEnabled" title="Hiển thị dữ liệu mẫu giống bản thiết kế; không ghi thu/trả nợ" @click="debtDemoEnabled = !debtDemoEnabled"><LineIcon name="box" />Dữ liệu mẫu</button>
          <button v-if="isDayCloseWorkspace" class="app-mock-toggle" type="button" :aria-pressed="dayCloseDemoEnabled" title="Hiển thị dữ liệu mẫu giống bản thiết kế; đóng ngày chỉ mô phỏng, không ghi dữ liệu" @click="dayCloseDemoEnabled = !dayCloseDemoEnabled"><LineIcon name="box" />Dữ liệu mẫu</button>
          <button v-if="isTodayWorkspace" class="app-mock-toggle" type="button" :aria-pressed="todayDemoEnabled" title="Hiển thị dữ liệu mẫu giống bản thiết kế; ngày, ca và so sánh chỉ minh họa" @click="todayDemoEnabled = !todayDemoEnabled"><LineIcon name="box" />Dữ liệu mẫu</button>
          <button v-if="isSettingsWorkspace" class="app-mock-toggle" type="button" :aria-pressed="settingsDemoEnabled" title="Hiển thị dữ liệu mẫu giống bản thiết kế; thay đổi giao diện chỉ mô phỏng, không lưu" @click="settingsDemoEnabled = !settingsDemoEnabled"><LineIcon name="box" />Dữ liệu mẫu</button>
          <button v-if="isSalesWorkspace" class="app-search-shortcut" type="button" aria-label="Tìm sản phẩm (Ctrl K)" @click="focusProductSearch">Ctrl + K</button>
          <!-- Today carries its own date control, as in its reference. -->
          <TodayDateControl v-if="isTodayWorkspace" />
          <template v-else>
            <!-- Help and calendar are visual placeholders until those destinations exist. -->
            <span class="app-top-icon" aria-hidden="true"><LineIcon name="help" /></span>
            <span class="app-top-icon" aria-hidden="true"><LineIcon name="calendar" /></span>
            <time class="app-topbar-date" :datetime="demoActive ? undefined : now.toISOString()">{{ shownDate }}</time>
          </template>
          <details class="app-topbar-profile">
            <summary :aria-label="`Tài khoản ${email ?? ''}`"><span class="app-topbar-avatar" aria-hidden="true">{{ initials }}</span><span class="app-topbar-user">{{ userLabel }}</span><LineIcon name="chevron" /></summary>
            <div class="app-topbar-menu">
              <p class="app-topbar-identity"><strong>{{ email }}</strong><span>{{ roleLabel }}</span></p>
              <button type="button" @click="logout">Đăng xuất</button>
            </div>
          </details>
        </div>
      </header>
      <main id="main-content" class="app-main" tabindex="-1">
        <slot />
      </main>
    </div>
  </div>
</template>
