<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiRequest } from '../api/client'
import type { StoreOperationalSettings } from '../api/types'
const allow = ref(false); const loading = ref(true); const saving = ref(false); const message = ref('')
onMounted(async () => { try { allow.value = (await apiRequest<StoreOperationalSettings>('/api/store/operational-settings')).allowNegativeStock } finally { loading.value = false } })
async function save() { saving.value = true; message.value = ''; try { const result = await apiRequest<StoreOperationalSettings>('/api/store/operational-settings/negative-stock', { method: 'PUT', body: JSON.stringify({ allowNegativeStock: allow.value }) }); allow.value = result.allowNegativeStock; message.value = 'Đã lưu chính sách tồn kho.' } catch (reason) { message.value = reason instanceof Error ? reason.message : 'Không thể lưu.' } finally { saving.value = false } }
</script>
<template><section><h1 class="text-3xl font-black">Thiết lập vận hành</h1><div v-if="!loading" class="card mt-6 max-w-2xl"><label class="flex items-start gap-3"><input v-model="allow" class="mt-1" type="checkbox" /><span><strong>Cho phép bán âm tồn</strong><span class="mt-1 block text-sm text-slate-500">Khi tắt, toàn bộ đơn thiếu tồn bị từ chối. Khi bật, tồn có thể âm và giá vốn có thể là ước tính.</span></span></label><button class="btn-primary mt-5" :disabled="saving" @click="save">{{ saving ? 'Đang lưu…' : 'Lưu thiết lập' }}</button><p v-if="message" class="mt-3 text-sm" role="status">{{ message }}</p></div></section></template>
