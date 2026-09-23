<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError, apiRequest } from '../api/client'
import { isAmbiguousOperationFailure, problemMessage } from '../api/operationRecovery'
import type { DebtBalance, DebtBalancePage, DebtPaymentResult, OperationStatus } from '../api/types'

type Kind = 'customer' | 'supplier'
interface Attempt {
  operationId: string
  partyId: string
  amount: number
  method: 'Cash' | 'Transfer'
  note: string | null
  expectedOutstandingAmount: number
}

const props = defineProps<{ kind: Kind }>()
const list = ref<DebtBalancePage | null>(null)
const selected = ref<DebtBalance | null>(null)
const search = ref('')
const page = ref(1)
const amount = ref(0)
const method = ref<'Cash' | 'Transfer'>('Cash')
const note = ref('')
const busy = ref(false)
const message = ref('')
const success = ref<DebtPaymentResult | null>(null)
const attempt = ref<Attempt | null>(null)
const basePath = computed(() => props.kind === 'customer' ? '/api/customers' : '/api/suppliers')
const title = computed(() => props.kind === 'customer' ? 'Công nợ khách hàng' : 'Công nợ nhà cung cấp')
const action = computed(() => props.kind === 'customer' ? 'thu nợ' : 'trả nợ')
const operationType = computed(() => props.kind === 'customer'
  ? 'RecordCustomerDebtPayment' : 'RecordSupplierDebtPayment')
const money = (value: number) => new Intl.NumberFormat('vi-VN').format(value)

const errors: Record<string, string> = {
  'customer-debt-changed': 'Công nợ khách hàng vừa thay đổi. Dữ liệu mới nhất đã được tải lại; giao dịch chưa được gửi lại.',
  'supplier-debt-changed': 'Công nợ nhà cung cấp vừa thay đổi. Dữ liệu mới nhất đã được tải lại; giao dịch chưa được gửi lại.',
  'debt-payment-exceeds-outstanding': 'Số tiền vượt quá công nợ hiện tại. Dữ liệu mới nhất đã được tải lại.',
  'customer-has-no-outstanding-debt': 'Khách hàng không còn công nợ.',
  'supplier-has-no-outstanding-debt': 'Nhà cung cấp không còn công nợ.',
  'idempotency-key-reused': 'Mã thao tác đã được dùng cho một yêu cầu khác.',
  'invalid-note-length': 'Ghi chú không được vượt quá 250 ký tự.',
}

async function load() {
  const query = new URLSearchParams({ page: String(page.value), pageSize: '20' })
  if (search.value.trim()) query.set('search', search.value.trim())
  list.value = await apiRequest<DebtBalancePage>(`${basePath.value}/debts?${query}`)
  if (selected.value) {
    selected.value = list.value.items.find(item => item.partyId === selected.value?.partyId) ?? null
    if (selected.value) amount.value = selected.value.outstandingAmount
  }
}

async function searchDebts() {
  if (attempt.value) return
  page.value = 1
  await load()
}

async function changePage(nextPage: number) {
  if (attempt.value) return
  page.value = nextPage
  await load()
}

function choose(item: DebtBalance) {
  if (attempt.value) return
  selected.value = item
  amount.value = item.outstandingAmount
  note.value = ''
  message.value = ''
  success.value = null
}

async function checkOperation(operationId: string) {
  try { return await apiRequest<OperationStatus>(`/api/operations/${operationId}`) }
  catch (reason) { if (reason instanceof ApiError && reason.status === 404) return null; throw reason }
}

async function execute(current: Attempt) {
  return apiRequest<DebtPaymentResult>(`${basePath.value}/${current.partyId}/debt-payments`, {
    method: 'POST',
    body: JSON.stringify(current),
  })
}

async function finish(result: DebtPaymentResult) {
  success.value = result
  message.value = `Đã ${action.value} ${money(result.amount)} ₫. Công nợ còn lại: ${money(result.outstandingAfter)} ₫.`
  attempt.value = null
  await load()
  if (result.outstandingAfter === 0) selected.value = null
  else if (selected.value) amount.value = result.outstandingAfter
}

async function submit() {
  if (busy.value || !selected.value) return
  message.value = ''
  success.value = null
  if (!attempt.value) {
    const normalizedNote = note.value.trim() || null
    if (amount.value <= 0 || amount.value > selected.value.outstandingAmount) {
      message.value = 'Số tiền phải lớn hơn 0 và không vượt quá công nợ hiện tại.'
      return
    }
    if (normalizedNote && normalizedNote.length > 250) {
      message.value = errors['invalid-note-length']
      return
    }
    const moneyMeaning = props.kind === 'customer'
      ? 'Đây là tiền thực thu và không phải doanh thu.'
      : 'Đây là tiền thực trả cho nhà cung cấp và không thay đổi giá trị phiếu nhập.'
    if (!confirm(`Xác nhận ${action.value} ${money(amount.value)} ₫ cho ${selected.value.partyName}? ${moneyMeaning}`)) return
    attempt.value = {
      operationId: crypto.randomUUID(),
      partyId: selected.value.partyId,
      amount: amount.value,
      method: method.value,
      note: normalizedNote,
      expectedOutstandingAmount: selected.value.outstandingAmount,
    }
  }

  const current = attempt.value
  busy.value = true
  try {
    await finish(await execute(current))
    return
  } catch (reason) {
    const code = reason instanceof ApiError ? reason.problem.code : undefined
    if (code === 'customer-debt-changed' || code === 'supplier-debt-changed'
      || code === 'debt-payment-exceeds-outstanding'
      || code === 'customer-has-no-outstanding-debt'
      || code === 'supplier-has-no-outstanding-debt') {
      attempt.value = null
      message.value = problemMessage(reason, errors, 'Công nợ đã thay đổi.')
      await load()
      return
    }
    if (!isAmbiguousOperationFailure(reason)) {
      attempt.value = null
      message.value = problemMessage(reason, errors, `Không thể ${action.value}.`)
      return
    }

    try {
      const operation = await checkOperation(current.operationId)
      if (operation?.status === 'Completed' && operation.operationType === operationType.value) {
        await finish(await execute(current))
        return
      }
      if (operation === null) {
        await finish(await execute(current))
        return
      }
    } catch { /* Keep the immutable snapshot until a definitive result. */ }
    message.value = 'Chưa xác định được kết quả. Mã thao tác và toàn bộ dữ liệu đã được khóa để thử lại an toàn.'
  } finally {
    busy.value = false
  }
}

onMounted(async () => {
  try { await load() } catch (reason) { message.value = reason instanceof Error ? reason.message : 'Không thể tải công nợ.' }
})

defineExpose({ attempt })
</script>

<template>
  <section>
    <h1 class="text-3xl font-black">{{ title }}</h1>
    <form class="mt-5 flex gap-2" @submit.prevent="searchDebts">
      <input v-model="search" class="input max-w-md" placeholder="Tìm theo tên hoặc số điện thoại" aria-label="Tìm công nợ" :disabled="busy || !!attempt" />
      <button class="btn-secondary" type="submit" :disabled="busy || !!attempt">Tìm</button>
    </form>
    <p v-if="message" class="mt-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-900" role="status">{{ message }}</p>
    <div v-if="success" class="mt-4 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-900" aria-label="Chi tiết thanh toán công nợ">{{ money(success.amount) }} ₫ · {{ success.method === 'Cash' ? 'Tiền mặt' : 'Chuyển khoản' }} · {{ new Date(success.occurredAt).toLocaleString('vi-VN') }} · còn lại {{ money(success.outstandingAfter) }} ₫</div>
    <div class="mt-6 grid gap-6 lg:grid-cols-[1fr_380px]">
      <div class="card overflow-x-auto p-0">
        <table class="w-full min-w-[560px] text-left text-sm">
          <thead class="border-b bg-stone-50"><tr><th class="p-4">Đối tác</th><th>Điện thoại</th><th>Công nợ</th><th></th></tr></thead>
          <tbody><tr v-for="item in list?.items" :key="item.partyId" class="border-b last:border-0"><td class="p-4 font-semibold">{{ item.partyName }}</td><td>{{ item.phone || '—' }}</td><td class="font-bold">{{ money(item.outstandingAmount) }} ₫</td><td><button class="btn-secondary" type="button" :disabled="!!attempt" @click="choose(item)">{{ action }}</button></td></tr></tbody>
        </table>
        <p v-if="list && list.items.length === 0" class="p-5 text-slate-500">Không có khoản công nợ nào.</p>
        <div v-if="list && list.totalPages > 1" class="flex items-center justify-between p-4"><button class="btn-secondary" type="button" :disabled="busy || !!attempt || page <= 1" @click="changePage(page - 1)">Trước</button><span>Trang {{ page }} / {{ list.totalPages }}</span><button class="btn-secondary" type="button" :disabled="busy || !!attempt || page >= list.totalPages" @click="changePage(page + 1)">Sau</button></div>
        <p v-if="list" class="px-4 pb-4 text-xs text-slate-500">Cập nhật lúc {{ new Date(list.asOf).toLocaleString('vi-VN') }}</p>
      </div>
      <form v-if="selected" class="card h-fit grid gap-4" @submit.prevent="submit">
        <div><h2 class="text-xl font-black">{{ selected.partyName }}</h2><p class="text-sm text-slate-500">{{ selected.phone || 'Không có số điện thoại' }} · Công nợ hiện tại: {{ money(selected.outstandingAmount) }} ₫ · as-of {{ new Date(selected.asOf).toLocaleString('vi-VN') }}</p></div>
        <label class="field"><span>Số tiền</span><input v-model.number="amount" class="input" type="number" min="0.01" step="0.01" :max="selected.outstandingAmount" :disabled="busy || !!attempt" /></label>
        <button class="btn-secondary" type="button" :disabled="busy || !!attempt" @click="amount = selected.outstandingAmount">{{ kind === 'customer' ? 'Thu toàn bộ' : 'Trả toàn bộ' }}</button>
        <label class="field"><span>Phương thức</span><select v-model="method" class="input" :disabled="busy || !!attempt"><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select></label>
        <label class="field"><span>Ghi chú (tối đa 250 ký tự)</span><textarea v-model="note" class="input min-h-20" maxlength="250" :disabled="busy || !!attempt" /></label>
        <p v-if="attempt" class="text-sm font-semibold text-amber-800">Dữ liệu đang được giữ nguyên cho cùng một thao tác.</p>
        <button class="btn-primary" type="submit" :disabled="busy">{{ busy ? 'Đang xác nhận…' : attempt ? 'Thử lại đúng thao tác' : `Xác nhận ${action}` }}</button>
      </form>
    </div>
  </section>
</template>
