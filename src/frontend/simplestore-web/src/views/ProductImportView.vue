<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import ImportPreview from '../components/ImportPreview.vue'
import { apiRequest } from '../api/client'
import type { ImportConfirmResult, ImportValidationResult } from '../api/types'

const file = ref<File | null>(null); const result = ref<ImportValidationResult | null>(null); const success = ref<ImportConfirmResult | null>(null)
const loading = ref(false); const error = ref('')
function choose(event: Event) { file.value = (event.target as HTMLInputElement).files?.[0] ?? null; result.value = null; success.value = null }
async function validate() {
  if (!file.value) { error.value = 'Chọn tệp CSV trước.'; return }
  loading.value = true; error.value = ''; const form = new FormData(); form.append('file', file.value)
  try { result.value = await apiRequest<ImportValidationResult>('/api/product-imports/validate', { method: 'POST', body: form }) }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể kiểm tra tệp.' }
  finally { loading.value = false }
}
async function confirmImport() {
  if (!result.value?.isValid || !result.value.importId) return
  loading.value = true; error.value = ''
  try { success.value = await apiRequest<ImportConfirmResult>(`/api/product-imports/${result.value.importId}/confirm`, { method: 'POST', body: '{}' }) }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể xác nhận import.' }
  finally { loading.value = false }
}
</script>

<template>
  <section>
    <div class="flex flex-wrap items-start justify-between gap-4"><div><h1 class="text-3xl font-black">Nhập sản phẩm từ CSV</h1><p class="mt-1 text-slate-500">Kiểm tra và xem trước; dữ liệu chỉ được ghi khi bạn xác nhận.</p></div><a class="btn-secondary" href="/api/product-imports/template">Tải tệp mẫu</a></div>
    <div class="card mt-6"><div class="field"><label for="csv-file">Tệp CSV (tối đa 5 MB)</label><input id="csv-file" class="input" type="file" accept=".csv,text/csv" @change="choose" /></div><button class="btn-primary mt-4" :disabled="loading || !file" @click="validate">{{ loading ? 'Đang xử lý…' : 'Kiểm tra và xem trước' }}</button></div>
    <p v-if="error" class="error mt-4" role="alert">{{ error }}</p>
    <div v-if="success" class="mt-5 rounded-xl border border-emerald-200 bg-emerald-50 p-5"><h2 class="font-bold text-emerald-900">Đã nhập {{ success.importedProductCount }} sản phẩm</h2><RouterLink class="mt-2 inline-block font-semibold text-emerald-800 underline" to="/products">Xem danh sách sản phẩm</RouterLink></div>
    <div v-else-if="result" class="mt-5"><ImportPreview :result="result" /><button v-if="result.isValid" class="btn-primary mt-5" :disabled="loading" @click="confirmImport">Xác nhận nhập {{ result.rows.length }} sản phẩm</button><p v-else class="mt-4 text-sm text-slate-500">Sửa các lỗi trong tệp rồi tải lên lại. Chưa có dữ liệu nào được nhập.</p></div>
  </section>
</template>
