<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiRequest } from '../api/client'
import type { StoreInfo, StoreOperationalSettings } from '../api/types'

const allow = ref(false)
const timeZoneId = ref('Asia/Ho_Chi_Minh')
const loading = ref(true)
const saving = ref(false)
const message = ref('')

onMounted(async () => {
  try {
    const [settings, store] = await Promise.all([
      apiRequest<StoreOperationalSettings>('/api/store/operational-settings'),
      apiRequest<StoreInfo>('/api/store/current'),
    ])
    allow.value = settings.allowNegativeStock
    timeZoneId.value = store.timeZoneId
  } finally { loading.value = false }
})

async function save() {
  saving.value = true
  message.value = ''
  try {
    const [settings, timezone] = await Promise.all([
      apiRequest<StoreOperationalSettings>('/api/store/operational-settings/negative-stock', {
        method: 'PUT', body: JSON.stringify({ allowNegativeStock: allow.value }),
      }),
      apiRequest<{ timeZoneId: string }>('/api/store/timezone', {
        method: 'PUT', body: JSON.stringify({ timeZoneId: timeZoneId.value }),
      }),
    ])
    allow.value = settings.allowNegativeStock
    timeZoneId.value = timezone.timeZoneId
    message.value = 'Đã lưu thiết lập vận hành.'
  } catch (reason) {
    message.value = reason instanceof Error ? reason.message : 'Không thể lưu.'
  } finally { saving.value = false }
}
</script>

<template>
  <section><h1 class="text-3xl font-black">Thiết lập vận hành</h1>
    <div v-if="!loading" class="card mt-6 grid max-w-2xl gap-6">
      <label class="flex items-start gap-3"><input v-model="allow" class="mt-1" type="checkbox" /><span><strong>Cho phép bán âm tồn</strong><span class="mt-1 block text-sm text-slate-500">Khi tắt, đơn thiếu tồn bị từ chối. Khi bật, tồn có thể âm và giá vốn có thể là ước tính.</span></span></label>
      <label class="field"><span>Múi giờ cửa hàng (IANA)</span><input v-model.trim="timeZoneId" class="input" list="timezones" placeholder="Asia/Ho_Chi_Minh" /><small class="text-slate-500">Múi giờ quyết định ranh giới ngày của báo cáo cuối ngày.</small></label>
      <datalist id="timezones"><option value="Asia/Ho_Chi_Minh" /><option value="Asia/Bangkok" /><option value="Asia/Singapore" /><option value="America/New_York" /><option value="Europe/London" /></datalist>
      <button class="btn-primary justify-self-start" :disabled="saving" @click="save">{{ saving ? 'Đang lưu…' : 'Lưu thiết lập' }}</button>
      <p v-if="message" class="text-sm" role="status">{{ message }}</p>
    </div>
  </section>
</template>
