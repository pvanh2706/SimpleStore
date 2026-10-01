<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import { isNavigationActive, navigationGroups, type NavigationItem } from '../../navigation'
import LineIcon from '../ui/LineIcon'
import type { IconName } from '../ui/icons'

const props = defineProps<{ items: readonly NavigationItem[]; path: string; salesLayout?: boolean; expandExtras?: boolean }>()
const emit = defineEmits<{ navigate: [] }>()

const groups = computed(() => navigationGroups
  .map(group => ({ ...group, items: props.items.filter(item => item.group === group.id) }))
  .filter(group => group.items.length > 0))
/** Primary entries of the approved sidebar reference, in its order and wording. */
const primary: ReadonlyArray<{ id: string; label?: string; icon: IconName }> = [
  { id: 'today', label: 'Tổng quan', icon: 'home' },
  { id: 'sale-checkout', icon: 'navCart' },
  { id: 'purchases', icon: 'purchase' },
  { id: 'products', icon: 'product' },
  { id: 'inventory', icon: 'inventory' },
  { id: 'customer-debts', label: 'Công nợ', icon: 'customer' },
  { id: 'end-of-day', label: 'Báo cáo', icon: 'report' },
  { id: 'day-close', icon: 'calendarCheck' },
  { id: 'operational-settings', label: 'Cài đặt', icon: 'settings' },
]
const primaryItems = computed(() => primary.flatMap(entry => {
  const item = props.items.find(candidate => candidate.id === entry.id)
  return item ? [{ item, label: entry.label ?? item.label, icon: entry.icon }] : []
}))
const extraItems = computed(() => props.items.filter(item => !primary.some(entry => entry.id === item.id)))
const extrasActive = computed(() => extraItems.value.some(item => isNavigationActive(item, props.path)))
</script>

<template>
  <template v-if="salesLayout">
    <div class="app-nav-sales">
      <RouterLink v-for="{ item, label, icon } in primaryItems" :key="item.id" :to="item.to" class="app-nav-link"
        :class="{ 'is-active': isNavigationActive(item, path) }" :title="label"
        :aria-current="isNavigationActive(item, path) ? 'page' : undefined" @click="emit('navigate')">
        <LineIcon :name="icon" />
        <span>{{ label }}</span>
      </RouterLink>
      <details v-if="extraItems.length" class="app-nav-extra" :open="expandExtras || extrasActive">
        <summary>Chức năng khác</summary>
        <RouterLink v-for="item in extraItems" :key="item.id" :to="item.to" class="app-nav-link"
          :class="{ 'is-active': isNavigationActive(item, path) }"
          :aria-current="isNavigationActive(item, path) ? 'page' : undefined" @click="emit('navigate')">{{ item.label }}</RouterLink>
      </details>
    </div>
  </template>
  <div v-for="group in salesLayout ? [] : groups" :key="group.id" class="app-nav-group">
    <p class="app-nav-group-label">{{ group.label }}</p>
    <RouterLink
      v-for="item in group.items"
      :key="item.id"
      :to="item.to"
      class="app-nav-link"
      :class="{ 'is-active': isNavigationActive(item, path) }"
      :aria-current="isNavigationActive(item, path) ? 'page' : undefined"
      @click="emit('navigate')"
    >{{ item.label }}</RouterLink>
  </div>
</template>
