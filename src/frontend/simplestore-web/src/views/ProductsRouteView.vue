<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import InventoryView from './InventoryView.vue'
import ProductListView from './ProductListView.vue'

/** `/products?view=inventory` is the Inventory workspace; every other `/products` visit is the Product List. */
const route = useRoute()
const inventory = computed(() => route.query.view === 'inventory')
</script>

<template>
  <InventoryView v-if="inventory" />
  <ProductListView v-else />
</template>
