<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import { isNavigationActive, navigationGroups, type NavigationItem } from '../../navigation'

const props = defineProps<{ items: readonly NavigationItem[]; path: string }>()
const emit = defineEmits<{ navigate: [] }>()

const groups = computed(() => navigationGroups
  .map(group => ({ ...group, items: props.items.filter(item => item.group === group.id) }))
  .filter(group => group.items.length > 0))
</script>

<template>
  <div v-for="group in groups" :key="group.id" class="app-nav-group">
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
