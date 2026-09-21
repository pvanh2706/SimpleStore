<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiRequest } from '../api/client'
import type { Supplier, SupplierPage } from '../api/types'

const result = ref<SupplierPage | null>(null)
const page = ref(1)
const search = ref('')
const name = ref('')
const phone = ref('')
const note = ref('')
const editingId = ref<string | null>(null)
const error = ref('')
const saving = ref(false)
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)

async function load(requestedPage = page.value) {
  const params = new URLSearchParams({ page: String(requestedPage), pageSize: '20' })
  if (search.value.trim()) params.set('search', search.value.trim())
  try {
    result.value = await apiRequest<SupplierPage>(`/api/suppliers?${params}`)
    page.value = requestedPage
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể tải nhà cung cấp.'
  }
}

function edit(supplier: Supplier) {
  editingId.value = supplier.id
  name.value = supplier.name
  phone.value = supplier.phone ?? ''
  note.value = supplier.note ?? ''
}
function reset() { editingId.value = null; name.value = ''; phone.value = ''; note.value = '' }
async function save() {
  saving.value = true
  error.value = ''
  try {
    await apiRequest(editingId.value ? `/api/suppliers/${editingId.value}` : '/api/suppliers', {
      method: editingId.value ? 'PUT' : 'POST',
      body: JSON.stringify({ name: name.value, phone: phone.value || null, note: note.value || null }),
    })
    reset()
    await load()
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Không thể lưu nhà cung cấp.'
  } finally { saving.value = false }
}
async function deactivate(id: string) {
  if (!confirm('Ngừng sử dụng nhà cung cấp này?')) return
  await apiRequest<void>(`/api/suppliers/${id}/deactivate`, { method: 'POST', body: '{}' })
  await load()
}
onMounted(load)
</script>

<template>
  <section>
    <div><h1 class="text-3xl font-black">Nhà cung cấp</h1><p class="mt-1 text-slate-500">Thông tin tối thiểu và công nợ phát sinh từ phiếu nhập.</p></div>
    <div class="mt-6 grid gap-5 lg:grid-cols-[360px_1fr]">
      <form class="card grid gap-3" @submit.prevent="save">
        <h2 class="text-xl font-black">{{ editingId ? 'Sửa nhà cung cấp' : 'Thêm nhà cung cấp' }}</h2>
        <label class="field"><span>Tên</span><input v-model="name" class="input" required /></label>
        <label class="field"><span>Điện thoại</span><input v-model="phone" class="input" /></label>
        <label class="field"><span>Ghi chú</span><textarea v-model="note" class="input" rows="3"></textarea></label>
        <div class="flex gap-2"><button class="btn-primary" :disabled="saving">{{ saving ? 'Đang lưu…' : 'Lưu' }}</button><button v-if="editingId" class="btn-secondary" type="button" @click="reset">Hủy</button></div>
      </form>
      <div>
        <form class="flex gap-2" @submit.prevent="load(1)"><input v-model="search" class="input" placeholder="Tìm theo tên hoặc điện thoại" aria-label="Tìm nhà cung cấp" /><button class="btn-secondary">Tìm</button></form>
        <p v-if="error" class="error mt-3">{{ error }}</p>
        <div class="card mt-3 overflow-x-auto p-0">
          <p v-if="!result?.items.length" class="p-6 text-slate-500">Chưa có nhà cung cấp.</p>
          <table v-else class="w-full min-w-[600px] text-left text-sm"><thead class="border-b bg-stone-50"><tr><th class="p-4">Tên</th><th>Điện thoại</th><th>Còn nợ</th><th>Trạng thái</th><th></th></tr></thead><tbody><tr v-for="supplier in result.items" :key="supplier.id" class="border-b last:border-0"><td class="p-4 font-semibold">{{ supplier.name }}</td><td>{{ supplier.phone || '—' }}</td><td>{{ money(supplier.outstandingAmount) }} ₫</td><td>{{ supplier.isActive ? 'Đang dùng' : 'Ngừng dùng' }}</td><td class="space-x-3"><button class="text-emerald-800" @click="edit(supplier)">Sửa</button><button v-if="supplier.isActive" class="text-red-700" @click="deactivate(supplier.id)">Ngừng</button></td></tr></tbody></table>
        </div>
        <div v-if="result && result.totalPages > 1" class="mt-4 flex items-center justify-between"><button class="btn-secondary" :disabled="page <= 1" @click="load(page - 1)">Trước</button><span>Trang {{ page }} / {{ result.totalPages }}</span><button class="btn-secondary" :disabled="page >= result.totalPages" @click="load(page + 1)">Sau</button></div>
      </div>
    </div>
  </section>
</template>
