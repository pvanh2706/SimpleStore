<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { apiRequest } from '../api/client'
import SaleReceipt from '../components/SaleReceipt.vue'
import type { Sale } from '../api/types'
const sale = ref<Sale | null>(null); const error = ref(''); const id = String(useRoute().params.id)
onMounted(async () => { try { sale.value = await apiRequest<Sale>(`/api/sales/${id}`) } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải đơn bán.' } })
</script>
<template><section><RouterLink class="no-print text-sm font-semibold text-emerald-800" to="/sales">← Lịch sử bán hàng</RouterLink><p v-if="error" class="error mt-4">{{ error }}</p><SaleReceipt v-if="sale" class="mt-5" :sale="sale" /></section></template>
