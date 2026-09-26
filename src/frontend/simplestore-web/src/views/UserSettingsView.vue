<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiRequest } from '../api/client'
import type { CashierAccount, CashierCredential } from '../api/types'

const cashiers = ref<CashierAccount[]>([])
const email = ref('')
const oneTimeCredential = ref<CashierCredential | null>(null)
const loading = ref(true)
const submitting = ref(false)
const error = ref('')

async function load() {
  loading.value = true
  error.value = ''
  try { cashiers.value = await apiRequest<CashierAccount[]>('/api/users/cashiers') }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tải danh sách nhân viên.' }
  finally { loading.value = false }
}

async function createCashier() {
  if (!email.value) return
  submitting.value = true; error.value = ''; oneTimeCredential.value = null
  try {
    oneTimeCredential.value = await apiRequest<CashierCredential>('/api/users/cashiers', {
      method: 'POST', body: JSON.stringify({ email: email.value }),
    })
    email.value = ''
    await load()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể tạo Cashier.' }
  finally { submitting.value = false }
}

async function disableCashier(cashier: CashierAccount) {
  if (!confirm(`Vô hiệu hóa ${cashier.email}? Phiên đang đăng nhập sẽ mất hiệu lực.`)) return
  error.value = ''
  try { await apiRequest(`/api/users/cashiers/${cashier.id}/disable`, { method: 'POST', body: '{}' }); await load() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể vô hiệu hóa Cashier.' }
}

async function resetCredential(cashier: CashierAccount) {
  if (!confirm(`Đặt lại mật khẩu cho ${cashier.email}? Mật khẩu và phiên cũ sẽ mất hiệu lực.`)) return
  error.value = ''; oneTimeCredential.value = null
  try {
    oneTimeCredential.value = await apiRequest<CashierCredential>(`/api/users/cashiers/${cashier.id}/credentials/reset`, { method: 'POST', body: '{}' })
    await load()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : 'Không thể đặt lại mật khẩu.' }
}

onMounted(load)
</script>

<template>
  <section>
    <h1 class="text-3xl font-black">Nhân viên thu ngân</h1>
    <p class="mt-2 text-slate-500">Tạo, vô hiệu hóa và cấp lại mật khẩu tạm thời cho Cashier trong cửa hàng này.</p>
    <form class="card mt-6 flex flex-wrap items-end gap-3" @submit.prevent="createCashier">
      <div class="field min-w-72 flex-1"><label for="cashier-email">Email Cashier</label><input id="cashier-email" v-model.trim="email" class="input" type="email" required /></div>
      <button class="btn-primary" type="submit" :disabled="submitting">Tạo Cashier</button>
    </form>
    <div v-if="oneTimeCredential" class="mt-5 rounded-xl border border-amber-300 bg-amber-50 p-5" role="status">
      <p class="font-bold">Mật khẩu tạm thời — chỉ hiển thị lần này</p>
      <p class="mt-2 font-mono text-lg" data-testid="temporary-password">{{ oneTimeCredential.temporaryPassword }}</p>
      <p class="mt-2 text-sm text-amber-800">Chuyển trực tiếp cho {{ oneTimeCredential.email }}. Cashier bắt buộc đổi mật khẩu ở lần đăng nhập kế tiếp.</p>
      <button class="btn-secondary mt-3" type="button" @click="oneTimeCredential = null">Đã ghi nhận, ẩn mật khẩu</button>
    </div>
    <p v-if="error" class="error mt-5" role="alert">{{ error }}</p>
    <p v-if="loading" class="mt-6 text-slate-500">Đang tải…</p>
    <div v-else class="mt-6 grid gap-3">
      <article v-for="cashier in cashiers" :key="cashier.id" class="card flex flex-wrap items-center justify-between gap-4">
        <div><p class="font-bold">{{ cashier.email }}</p><p class="text-sm" :class="cashier.isEnabled ? 'text-emerald-700' : 'text-red-700'">{{ cashier.isEnabled ? (cashier.mustChangePassword ? 'Đang chờ đổi mật khẩu' : 'Đang hoạt động') : 'Đã vô hiệu hóa' }}</p></div>
        <div v-if="cashier.isEnabled" class="flex gap-2"><button class="btn-secondary" type="button" @click="resetCredential(cashier)">Cấp lại mật khẩu</button><button class="btn-secondary" type="button" @click="disableCashier(cashier)">Vô hiệu hóa</button></div>
      </article>
    </div>
  </section>
</template>
