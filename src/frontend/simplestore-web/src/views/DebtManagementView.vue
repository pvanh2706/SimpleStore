<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { ApiError, apiRequest } from '../api/client'
import { isAmbiguousOperationFailure, problemMessage } from '../api/operationRecovery'
import type { DebtBalance, DebtBalancePage, DebtPaymentResult, OperationStatus, Supplier } from '../api/types'
import LineIcon from '../components/ui/LineIcon'
import type { IconName } from '../components/ui/icons'
import {
  debtDemoEnabled, referenceActor, referenceAsOfTime, referenceHistory, referenceParties, referencePaymentForm, referencePayments,
  referenceRecent, referenceSummary, type DebtKind, type DebtPartyPreview, type DebtTone, type LedgerRow, type PaymentRow,
} from '../debts/demo'
import { useAuthStore } from '../stores/auth'

type Kind = DebtKind
type ListState = 'normal' | 'empty' | 'noresult' | 'loading' | 'error'
type SpecialState = 'empty' | 'noresult' | 'loading' | 'error' | 'missing' | 'forbidden' | 'refresh'
type StatusFilter = 'owing' | 'paid' | 'overdue' | 'due'
type DetailTab = 'overview' | 'transactions' | 'payments' | 'explain'
interface Filter { statuses: StatusFilter[]; from: string; to: string; min: string; max: string }
interface Attempt {
  operationId: string
  partyId: string
  amount: number
  method: 'Cash' | 'Transfer'
  note: string | null
  expectedOutstandingAmount: number
}
interface Row { id: string; name: string; phone: string; debt: number; tone: DebtTone; incurred: number; paid: number; last: string; overdue: number; asOf: string }

const props = defineProps<{ kind: Kind }>()
const router = useRouter()
const auth = useAuthStore()
const isOwner = computed(() => auth.session.roles.includes('Owner'))
const demo = debtDemoEnabled
const customer = computed(() => props.kind === 'customer')
const words = computed(() => customer.value
  ? {
    party: 'khách hàng', Party: 'Khách hàng', action: 'Thu tiền', verb: 'thu', all: 'Thu toàn bộ', modal: 'Thu tiền khách hàng',
    payments: 'Lịch sử thu tiền', info: 'Thông tin thu tiền', amount: 'Số tiền thu', method: 'Phương thức thu tiền', date: 'Ngày thu tiền',
    meaning: 'Đây là tiền thực thu, không phải doanh thu. Khoản thu giảm công nợ chung của khách hàng, không phân bổ vào hóa đơn cụ thể.',
    owe: 'Số tiền khách hàng còn nợ hiện tại', incurred: 'giao dịch bán hàng', paidCount: 'lần thu tiền', operation: 'RecordCustomerDebtPayment',
    search: 'Tìm theo tên khách hàng, số điện thoại...', empty: 'Chưa có khách hàng nào đang nợ',
  }
  : {
    party: 'nhà cung cấp', Party: 'Nhà cung cấp', action: 'Trả tiền', verb: 'trả', all: 'Trả toàn bộ', modal: 'Trả tiền nhà cung cấp',
    payments: 'Lịch sử trả tiền', info: 'Thông tin trả tiền', amount: 'Số tiền trả', method: 'Phương thức trả tiền', date: 'Ngày trả tiền',
    meaning: 'Đây là tiền thực trả cho nhà cung cấp, không làm thay đổi giá trị phiếu nhập. Khoản trả giảm công nợ chung, không phân bổ vào phiếu cụ thể.',
    owe: 'Số tiền cửa hàng còn nợ nhà cung cấp', incurred: 'phiếu nhập hàng', paidCount: 'lần trả tiền', operation: 'RecordSupplierDebtPayment',
    search: 'Tìm theo tên nhà cung cấp, số điện thoại...', empty: 'Chưa có nhà cung cấp nào đang nợ',
  })
const basePath = computed(() => customer.value ? '/api/customers' : '/api/suppliers')

const cloneParties = () => ({ customer: referenceParties.customer.map(item => ({ ...item })), supplier: referenceParties.supplier.map(item => ({ ...item })) })
const defaultFilter = (): Filter => demo.value
  ? { statuses: [], from: '01/12/2024', to: '16/12/2024', min: '', max: '' }
  : { statuses: [], from: '', to: '', min: '', max: '' }
const mock = ref(cloneParties())
const demoPayments = ref<Record<string, PaymentRow[]>>({})
const list = ref<DebtBalancePage | null>(null)
const summary = ref<{ count: number | null; total: number | null; asOf: string }>({ count: null, total: null, asOf: '' })
const search = ref('')
const page = ref(1)
const pageSize = ref(demo.value ? 5 : 20)
const loading = ref(false)
const loadError = ref('')
const filter = ref<Filter>(defaultFilter())
const draft = ref<Filter>(defaultFilter())
const draftKind = ref<Kind>(props.kind)
const drawer = ref<'filter' | 'detail' | null>(null)
const detailId = ref<string | null>(null)
const detailTab = ref<DetailTab>('overview')
const liveDetail = ref<DebtBalance | null>(null)
const liveSupplier = ref<Supplier | null>(null)
const detailLoading = ref(false)
const detailError = ref<'' | 'missing' | 'forbidden' | 'error'>('')
const historyOpen = ref(false)
const historyType = ref('Tất cả')
const historyFrom = ref('01/12/2024')
const historyTo = ref('16/12/2024')
const special = ref<SpecialState | null>(null)
const checkedIds = ref<string[]>([])
const toastText = ref('')
const payOpen = ref(false)
const payStep = ref<'form' | 'confirm' | 'done'>('form')
const payParty = ref<{ id: string; name: string; phone: string; outstanding: number; asOf: string } | null>(null)
const amount = ref(0)
const method = ref<'Cash' | 'Transfer'>('Cash')
const note = ref('')
const payDate = ref('')
const busy = ref(false)
const message = ref('')
const stale = ref(false)
const success = ref<DebtPaymentResult | null>(null)
const demoResult = ref<{ amount: number; method: 'Cash' | 'Transfer'; date: string; before: number; after: number } | null>(null)
const attempt = ref<Attempt | null>(null)
let searchTimer: ReturnType<typeof setTimeout> | undefined
let toastTimer: ReturnType<typeof setTimeout> | undefined
let loadSequence = 0
let detailSequence = 0

const money = (value: number) => `${new Intl.NumberFormat('vi-VN').format(value)} đ`
const pad = (part: number) => String(part).padStart(2, '0')
function formatDateTime(value?: string | null) {
  const date = value ? new Date(value) : null
  return date && !Number.isNaN(date.getTime()) ? `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}` : '—'
}
const isoDate = (value: string) => /^\d{2}\/\d{2}\/\d{4}/.test(value.trim()) ? value.trim().slice(0, 10).split('/').reverse().join('-') : ''
const amountInput = (value: string) => value.replace(/\D/g, '') === '' ? null : Number(value.replace(/\D/g, ''))
const methodLabel = (value: 'Cash' | 'Transfer' | string) => value === 'Cash' ? 'Tiền mặt' : value === 'Transfer' ? 'Chuyển khoản' : value
const statusOptions: Array<[StatusFilter, string]> = [['owing', 'Đang nợ'], ['paid', 'Đã thanh toán'], ['overdue', 'Quá hạn (> 7 ngày)'], ['due', 'Sắp đến hạn (≤ 7 ngày)']]
const errors: Record<string, string> = {
  'customer-debt-changed': 'Công nợ khách hàng vừa thay đổi. Dữ liệu mới nhất đã được tải lại; giao dịch chưa được gửi lại.',
  'supplier-debt-changed': 'Công nợ nhà cung cấp vừa thay đổi. Dữ liệu mới nhất đã được tải lại; giao dịch chưa được gửi lại.',
  'debt-payment-exceeds-outstanding': 'Số tiền vượt quá công nợ hiện tại. Dữ liệu mới nhất đã được tải lại.',
  'customer-has-no-outstanding-debt': 'Khách hàng không còn công nợ.',
  'supplier-has-no-outstanding-debt': 'Nhà cung cấp không còn công nợ.',
  'idempotency-key-reused': 'Mã thao tác đã được dùng cho một yêu cầu khác.',
  'invalid-note-length': 'Ghi chú không được vượt quá 250 ký tự.',
}
const staleCodes = ['customer-debt-changed', 'supplier-debt-changed', 'debt-payment-exceeds-outstanding', 'customer-has-no-outstanding-debt', 'supplier-has-no-outstanding-debt']

/** Live rows only know the current outstanding; due/overdue and lifetime totals are not domain facts (D-100). */
function toneLabel(tone: DebtTone) {
  return { paid: demo.value ? 'Đã thanh toán' : 'Hết nợ', overdue: 'Quá hạn', due: 'Sắp đến hạn', owing: 'Đang nợ' }[tone]
}
const demoRows = computed(() => mock.value[props.kind])
const rows = computed<Row[]>(() => demo.value
  ? demoRows.value.map(item => ({ id: item.id, name: item.name, phone: item.phone, debt: item.debt, tone: item.status, incurred: item.incurred, paid: item.paid, last: item.last, overdue: item.overdue, asOf: '' }))
  : (list.value?.items ?? []).map(item => ({
    id: item.partyId, name: item.partyName, phone: item.phone || '—', debt: item.outstandingAmount,
    tone: item.outstandingAmount > 0 ? 'owing' as const : 'paid' as const, incurred: 0, paid: 0, last: '', overdue: 0, asOf: item.asOf,
  })))
const filteredRows = computed(() => {
  if (!demo.value) return rows.value
  const query = search.value.trim().toLocaleLowerCase('vi-VN')
  const current = filter.value
  const from = isoDate(current.from)
  const to = isoDate(current.to)
  const min = amountInput(current.min)
  const max = amountInput(current.max)
  return rows.value.filter(row => {
    if (query && !`${row.name} ${row.phone}`.toLocaleLowerCase('vi-VN').includes(query)) return false
    if (current.statuses.length && !current.statuses.some(key => key === 'owing' ? row.debt > 0 : key === 'paid' ? row.debt === 0 : row.tone === key)) return false
    const date = isoDate(row.last)
    if ((from && date < from) || (to && date > to)) return false
    return (min === null || row.debt >= min) && (max === null || row.debt <= max)
  })
})
const narrowed = computed(() => search.value.trim() !== '' || JSON.stringify(filter.value) !== JSON.stringify(defaultFilter()))
const visibleRows = computed(() => demo.value ? filteredRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value) : filteredRows.value)
const totalCount = computed(() => demo.value ? narrowed.value ? filteredRows.value.length : referenceSummary[props.kind].parties : list.value?.totalCount ?? 0)
const totalPages = computed(() => demo.value ? Math.ceil(totalCount.value / pageSize.value) : list.value?.totalPages ?? 0)
const firstIndex = computed(() => totalCount.value ? (page.value - 1) * pageSize.value + 1 : 0)
const lastIndex = computed(() => totalCount.value ? Math.min(firstIndex.value + visibleRows.value.length - 1, totalCount.value) : 0)
const pagerItems = computed<Array<number | '…'>>(() => {
  const total = totalPages.value
  const current = page.value
  if (total <= 7) return Array.from({ length: total }, (_, index) => index + 1)
  if (current <= 4) return [1, 2, 3, 4, 5, '…', total]
  if (current >= total - 3) return [1, '…', total - 4, total - 3, total - 2, total - 1, total]
  return [1, '…', current - 1, current, current + 1, '…', total]
})
const allChecked = computed(() => visibleRows.value.length > 0 && visibleRows.value.every(row => checkedIds.value.includes(row.id)))
const listState = computed<ListState>(() => {
  if (!demo.value && loading.value && !list.value) return 'loading'
  if (!demo.value && loadError.value) return 'error'
  if (visibleRows.value.length === 0) return narrowed.value ? 'noresult' : 'empty'
  return 'normal'
})

function bucket(source: DebtPartyPreview[], test: (item: DebtPartyPreview) => boolean) { return source.filter(test).length }
const summaryCards = computed(() => {
  const label = words.value
  if (demo.value) {
    const reference = referenceSummary[props.kind]
    const base = referenceParties[props.kind]
    const now = demoRows.value
    const shift = (test: (item: DebtPartyPreview) => boolean) => bucket(now, test) - bucket(base, test)
    const total = reference.total + now.reduce((sum, item) => sum + item.debt, 0) - base.reduce((sum, item) => sum + item.debt, 0)
    return [
      { key: 'owing' as StatusFilter, label: `Tổng số ${label.party} nợ`, value: String(reference.debtors + shift(item => item.debt > 0)), note: label.Party, tone: '' },
      { key: 'owing' as StatusFilter, label: `Tổng công nợ ${label.party}`, value: money(total), note: '', tone: 'danger' },
      { key: 'overdue' as StatusFilter, label: 'Quá hạn (> 7 ngày)', value: String(reference.overdue + shift(item => item.status === 'overdue')), note: label.Party, tone: 'danger' },
      { key: 'due' as StatusFilter, label: 'Sắp đến hạn (≤ 7 ngày)', value: String(reference.due + shift(item => item.status === 'due')), note: label.Party, tone: 'warning' },
    ]
  }
  const asOf = summary.value.asOf ? formatDateTime(summary.value.asOf) : ''
  return [
    { key: null, label: `Tổng số ${label.party} nợ`, value: summary.value.count === null ? '—' : String(summary.value.count), note: label.Party, tone: '' },
    { key: null, label: `Tổng công nợ ${label.party}`, value: summary.value.total === null ? '—' : money(summary.value.total), note: summary.value.total === null && summary.value.count ? 'Quá 100 đối tác — chưa có API tổng hợp' : asOf ? `Tính đến ${asOf}` : '', tone: 'danger' },
    { key: null, label: 'Số liệu tính đến', value: asOf ? asOf.split(' ')[1]! : '—', note: asOf ? `${asOf.split(' ')[0]} · theo hệ thống` : '', tone: '' },
  ]
})

/* Detail */
const ledgerType = computed(() => customer.value ? 'Thu tiền' : 'Trả tiền')
const sessionLedger = (id: string): LedgerRow[] => (demoPayments.value[id] ?? []).map(item => ({ date: item.date, type: ledgerType.value, document: item.document, incurred: 0, paid: item.amount, balance: -item.amount }))
const detail = computed(() => {
  const label = words.value
  if (demo.value) {
    const party = demoRows.value.find(item => item.id === detailId.value)
    if (!party) return null
    const history = [...sessionLedger(party.id), ...referenceHistory[props.kind]]
    const payments = [...(demoPayments.value[party.id] ?? []), ...referencePayments[props.kind]]
    return {
      id: party.id, name: party.name, phone: party.phone, debt: party.debt, asOf: `${party.last} ${referenceAsOfTime}`,
      info: customer.value
        ? [['Tên khách hàng', party.name], ['Số điện thoại', party.phone], ['Nhóm khách hàng', party.group], ['Ghi chú', party.note]]
        : [['Tên nhà cung cấp', party.name], ['Số điện thoại', party.phone], ['Địa chỉ', party.address], ['Ghi chú', party.note]],
      debtInfo: [
        { label: 'Tổng phát sinh', value: money(party.incurred), tone: '' }, { label: 'Đã thanh toán', value: money(party.paid), tone: 'good' },
        { label: 'Còn nợ', value: money(party.debt), tone: 'danger' }, { label: 'Số ngày quá hạn', value: `${party.overdue} ngày`, tone: party.overdue > 0 ? 'danger' : '' },
        { label: 'Ngày giao dịch cuối', value: party.last, tone: '' },
      ],
      recent: [...sessionLedger(party.id), ...referenceRecent[props.kind]].slice(0, 5),
      history,
      payments,
      breakdown: [
        { label: 'Tổng phát sinh', value: money(party.incurred), hint: `Tổng tiền của ${history.filter(item => item.incurred > 0).length} ${label.incurred}` },
        { label: 'Đã thanh toán', value: money(party.paid), hint: `Tổng tiền của ${payments.length} ${label.paidCount}` },
        { label: 'Còn nợ', value: money(party.debt), hint: label.owe },
      ],
      formula: [`Còn nợ = Tổng phát sinh – Đã thanh toán`, `= ${money(party.incurred)} – ${money(party.paid)}`, `= ${money(party.debt)}`],
    }
  }
  const balance = liveDetail.value
  if (!balance) return null
  return {
    id: balance.partyId, name: balance.partyName, phone: balance.phone || 'Không có số điện thoại', debt: balance.outstandingAmount, asOf: formatDateTime(balance.asOf),
    info: [[`Tên ${label.party}`, balance.partyName], ['Số điện thoại', balance.phone || 'Không có'], ...(liveSupplier.value?.note ? [['Ghi chú', liveSupplier.value.note]] : [])],
    debtInfo: [{ label: 'Còn nợ', value: money(balance.outstandingAmount), tone: 'danger' }, { label: 'Tính đến', value: formatDateTime(balance.asOf), tone: '' }],
    recent: [] as LedgerRow[], history: [] as LedgerRow[], payments: [] as PaymentRow[], breakdown: [] as Array<{ label: string; value: string; hint: string }>, formula: [] as string[],
  }
})
const detailTabs = computed(() => demo.value
  ? [{ key: 'overview' as DetailTab, label: 'Tổng quan' }, { key: 'transactions' as DetailTab, label: `Lịch sử giao dịch (${detail.value?.history.length ?? 0})` }, { key: 'payments' as DetailTab, label: `${words.value.payments} (${detail.value?.payments.length ?? 0})` }, { key: 'explain' as DetailTab, label: 'Giải thích công nợ' }]
  : [{ key: 'overview' as DetailTab, label: 'Tổng quan' }, { key: 'explain' as DetailTab, label: 'Giải thích công nợ' }])
const historyRows = computed(() => {
  const from = isoDate(historyFrom.value)
  const to = isoDate(historyTo.value)
  return (detail.value?.history ?? []).filter(item => {
    const date = isoDate(item.date)
    return (historyType.value === 'Tất cả' || item.type === historyType.value) && (!from || date >= from) && (!to || date <= to)
  })
})
const amountClass = (value: number) => value > 0 ? 'danger-text' : value < 0 ? 'good-text' : ''

/* Loading */
async function load(requestedPage = page.value) {
  if (demo.value) { page.value = requestedPage; return }
  const sequence = ++loadSequence
  loading.value = true
  loadError.value = ''
  const query = new URLSearchParams({ page: String(requestedPage), pageSize: String(pageSize.value) })
  if (search.value.trim()) query.set('search', search.value.trim())
  try {
    const response = await apiRequest<DebtBalancePage>(`${basePath.value}/debts?${query}`)
    if (sequence !== loadSequence) return
    list.value = response
    page.value = requestedPage
  } catch (reason) {
    if (sequence === loadSequence) loadError.value = reason instanceof Error ? reason.message : 'Không thể tải công nợ.'
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}
/** The list API only returns parties that currently owe; one 100-row page gives an exact total with one `asOf`. */
async function loadSummary() {
  const sequence = loadSequence
  try {
    const response = await apiRequest<DebtBalancePage>(`${basePath.value}/debts?page=1&pageSize=100`)
    if (demo.value || sequence !== loadSequence) return
    summary.value = {
      count: response.totalCount,
      total: response.totalPages <= 1 ? response.items.reduce((sum, item) => sum + item.outstandingAmount, 0) : null,
      asOf: response.asOf,
    }
  } catch {
    if (sequence === loadSequence) summary.value = { count: null, total: null, asOf: '' }
  }
}
function reloadLive() { load(page.value); loadSummary() }

function toast(text: string) {
  toastText.value = text
  if (toastTimer) clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toastText.value = '' }, 2200)
}
function switchKind(kind: Kind) {
  if (kind === props.kind || attempt.value || busy.value) return
  if (kind === 'supplier' && !isOwner.value) return
  router.push(kind === 'customer' ? '/customers/debts' : '/suppliers/debts')
}
function goPage(target: number) {
  if (attempt.value || target < 1 || target > totalPages.value || target === page.value) return
  if (demo.value && (target - 1) * pageSize.value >= filteredRows.value.length) { toast('Dữ liệu mẫu chỉ có trang đầu tiên giống bản thiết kế.'); return }
  checkedIds.value = []
  load(target)
}
function toggleAll() { checkedIds.value = allChecked.value ? [] : visibleRows.value.map(row => row.id) }
function toggleChecked(id: string) { checkedIds.value = checkedIds.value.includes(id) ? checkedIds.value.filter(value => value !== id) : [...checkedIds.value, id] }
function openFilter() {
  if (attempt.value) return
  draft.value = { ...filter.value, statuses: [...filter.value.statuses] }
  draftKind.value = props.kind
  drawer.value = 'filter'
}
function toggleDraftStatus(key: StatusFilter | 'all') {
  if (key === 'all') draft.value.statuses = []
  else draft.value.statuses = draft.value.statuses.includes(key) ? draft.value.statuses.filter(value => value !== key) : [...draft.value.statuses, key]
}
function applyFilter() {
  drawer.value = null
  if (draftKind.value !== props.kind) { switchKind(draftKind.value); return }
  if (!demo.value) return
  filter.value = { ...draft.value, statuses: [...draft.value.statuses] }
  page.value = 1
  checkedIds.value = []
  toast('Đã áp dụng bộ lọc')
}
function clearAll() {
  filter.value = defaultFilter()
  draft.value = defaultFilter()
  page.value = 1
  drawer.value = null
  if (search.value) search.value = ''
  toast('Đã xóa bộ lọc')
}
function cardFilter(key: StatusFilter | null) {
  if (!demo.value || !key) return
  filter.value = { ...defaultFilter(), statuses: [key] }
  page.value = 1
}

async function openDetail(row: Row) {
  if (attempt.value) return
  detailId.value = row.id
  detailTab.value = 'overview'
  drawer.value = 'detail'
  if (demo.value) return
  const sequence = ++detailSequence
  liveDetail.value = null
  liveSupplier.value = null
  detailError.value = ''
  detailLoading.value = true
  try {
    const [balance, supplier] = await Promise.all([
      apiRequest<DebtBalance>(`${basePath.value}/${row.id}/debt`),
      customer.value ? Promise.resolve(null) : apiRequest<Supplier>(`/api/suppliers/${row.id}`).catch(() => null),
    ])
    if (sequence !== detailSequence) return
    liveDetail.value = balance
    liveSupplier.value = supplier
  } catch (reason) {
    if (sequence !== detailSequence) return
    detailError.value = reason instanceof ApiError && reason.status === 404 ? 'missing' : reason instanceof ApiError && reason.status === 403 ? 'forbidden' : 'error'
  } finally {
    if (sequence === detailSequence) detailLoading.value = false
  }
}
function openHistory() {
  historyType.value = 'Tất cả'
  historyFrom.value = '01/12/2024'
  historyTo.value = '16/12/2024'
  historyOpen.value = true
}

/* Collection / settlement */
function openPayment(row: { id: string; name: string; phone: string; debt: number; asOf: string }) {
  if (attempt.value || row.debt <= 0) return
  const defaults = referencePaymentForm[props.kind]
  payParty.value = { id: row.id, name: row.name, phone: row.phone, outstanding: row.debt, asOf: row.asOf }
  amount.value = demo.value ? Math.min(defaults.amount, row.debt) : row.debt
  method.value = demo.value ? defaults.method : 'Cash'
  note.value = ''
  payDate.value = defaults.date
  message.value = ''
  stale.value = false
  success.value = null
  demoResult.value = null
  payStep.value = 'form'
  drawer.value = null
  payOpen.value = true
}
function closePayment() {
  if (busy.value || attempt.value) return
  payOpen.value = false
}
function reviewPayment() {
  const party = payParty.value
  if (!party || attempt.value) return
  message.value = ''
  if (!(amount.value > 0) || amount.value > party.outstanding) { message.value = 'Số tiền phải lớn hơn 0 và không vượt quá công nợ hiện tại.'; return }
  if (note.value.trim().length > 250) { message.value = errors['invalid-note-length']!; return }
  if (demo.value && !isoDate(payDate.value)) { message.value = `Nhập ${words.value.date.toLowerCase()} dạng dd/mm/yyyy.`; return }
  payStep.value = 'confirm'
}
async function checkOperation(operationId: string) {
  try { return await apiRequest<OperationStatus>(`/api/operations/${operationId}`) }
  catch (reason) { if (reason instanceof ApiError && reason.status === 404) return null; throw reason }
}
async function execute(current: Attempt) {
  return apiRequest<DebtPaymentResult>(`${basePath.value}/${current.partyId}/debt-payments`, { method: 'POST', body: JSON.stringify(current) })
}
async function finish(result: DebtPaymentResult) {
  success.value = result
  attempt.value = null
  payStep.value = 'done'
  if (payParty.value) payParty.value.outstanding = result.outstandingAfter
  await Promise.all([load(page.value), loadSummary()])
}
async function refreshParty() {
  const party = payParty.value
  if (!party) return
  try {
    const balance = await apiRequest<DebtBalance>(`${basePath.value}/${party.id}/debt`)
    party.outstanding = balance.outstandingAmount
    party.asOf = balance.asOf
    amount.value = Math.min(amount.value, balance.outstandingAmount)
  } catch { /* The list reload below still shows the authoritative state. */ }
  await Promise.all([load(page.value), loadSummary()])
}
function confirmDemoPayment() {
  const party = demoRows.value.find(item => item.id === payParty.value?.id)
  if (!party) return
  const before = party.debt
  const paid = Number(amount.value)
  party.paid += paid
  party.debt -= paid
  party.last = payDate.value.trim()
  if (party.debt === 0) { party.status = 'paid'; party.overdue = 0 }
  const prefix = customer.value ? 'RC' : 'PC'
  const sequence = (customer.value ? 22 : 13) + Object.values(demoPayments.value).reduce((sum, items) => sum + items.length, 0)
  demoPayments.value = { ...demoPayments.value, [party.id]: [{ date: party.last, document: `${prefix}${String(sequence).padStart(6, '0')}`, method: methodLabel(method.value), amount: paid, actor: referenceActor }, ...(demoPayments.value[party.id] ?? [])] }
  demoResult.value = { amount: paid, method: method.value, date: party.last, before, after: party.debt }
  if (payParty.value) payParty.value.outstanding = party.debt
  payStep.value = 'done'
  toast(customer.value ? 'Đã ghi nhận thu tiền trong dữ liệu mẫu' : 'Đã ghi nhận trả tiền trong dữ liệu mẫu')
}
async function confirmPayment() {
  if (demo.value) { confirmDemoPayment(); return }
  const party = payParty.value
  if (busy.value || !party) return
  message.value = ''
  stale.value = false
  if (!attempt.value) {
    attempt.value = {
      operationId: crypto.randomUUID(), partyId: party.id, amount: amount.value, method: method.value,
      note: note.value.trim() || null, expectedOutstandingAmount: party.outstanding,
    }
  }
  const current = attempt.value
  busy.value = true
  try {
    await finish(await execute(current))
    return
  } catch (reason) {
    const code = reason instanceof ApiError ? reason.problem.code : undefined
    if (code && staleCodes.includes(code)) {
      attempt.value = null
      message.value = problemMessage(reason, errors, 'Công nợ đã thay đổi.')
      stale.value = true
      payStep.value = 'form'
      await refreshParty()
      return
    }
    if (!isAmbiguousOperationFailure(reason)) {
      attempt.value = null
      message.value = problemMessage(reason, errors, `Không thể ${words.value.verb} tiền.`)
      payStep.value = 'form'
      return
    }
    try {
      const operation = await checkOperation(current.operationId)
      if ((operation?.status === 'Completed' && operation.operationType === words.value.operation) || operation === null) {
        await finish(await execute(current))
        return
      }
    } catch { /* Keep the immutable snapshot until a definitive result. */ }
    message.value = 'Chưa xác định được kết quả. Mã thao tác và toàn bộ dữ liệu đã được khóa để thử lại an toàn.'
  } finally {
    busy.value = false
  }
}
const payResult = computed(() => success.value
  ? { amount: success.value.amount, method: success.value.method, date: formatDateTime(success.value.occurredAt), before: success.value.outstandingBefore, after: success.value.outstandingAfter }
  : demoResult.value)

function closeOverlays() {
  if (attempt.value || busy.value) return
  drawer.value = null
  historyOpen.value = false
  payOpen.value = false
  special.value = null
}
function onKeydown(event: KeyboardEvent) { if (event.key === 'Escape') closeOverlays() }
function resetWorkspace() {
  ++loadSequence
  ++detailSequence
  if (searchTimer) clearTimeout(searchTimer)
  search.value = ''
  filter.value = defaultFilter()
  page.value = 1
  pageSize.value = demo.value ? 5 : 20
  list.value = null
  summary.value = { count: null, total: null, asOf: '' }
  loading.value = false
  loadError.value = ''
  checkedIds.value = []
  detailId.value = null
  drawer.value = null
  historyOpen.value = false
  payOpen.value = false
  special.value = null
}

watch(search, () => {
  if (demo.value) { page.value = 1; return }
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => load(1), 300)
})
watch(() => props.kind, () => {
  resetWorkspace()
  if (!demo.value) { load(1); loadSummary() }
})
watch(demo, enabled => {
  mock.value = cloneParties()
  demoPayments.value = {}
  attempt.value = null
  resetWorkspace()
  if (!enabled) { load(1); loadSummary() }
})
onMounted(() => {
  window.addEventListener('keydown', onKeydown)
  if (!demo.value) { load(1); loadSummary() }
})
onUnmounted(() => {
  ++loadSequence
  ++detailSequence
  for (const timer of [searchTimer, toastTimer]) if (timer) clearTimeout(timer)
  window.removeEventListener('keydown', onKeydown)
})

const specialCopy: Record<SpecialState, { icon: IconName | null; title: () => string; text: string; action: string; primary?: boolean }> = {
  empty: { icon: 'customer', title: () => `${words.value.empty}`, text: 'Sẽ hiển thị danh sách khi có phát sinh công nợ.', action: 'Quay lại', primary: true },
  noresult: { icon: 'search', title: () => 'Không tìm thấy kết quả', text: 'Thử thay đổi từ khóa hoặc bộ lọc.', action: 'Xóa bộ lọc' },
  loading: { icon: null, title: () => 'Đang tải dữ liệu...', text: 'Vui lòng chờ trong giây lát.', action: 'Đóng' },
  error: { icon: 'warning', title: () => 'Không thể tải dữ liệu', text: 'Vui lòng thử lại sau hoặc liên hệ quản trị viên nếu lỗi vẫn tiếp diễn.', action: 'Thử lại' },
  missing: { icon: 'warning', title: () => 'Không tìm thấy thông tin', text: `${words.value.Party} không tồn tại hoặc đã bị vô hiệu hóa.`, action: 'Quay lại' },
  forbidden: { icon: 'lock', title: () => 'Bạn không có quyền thực hiện thao tác này', text: 'Vui lòng liên hệ quản trị viên.', action: 'Quay lại' },
  refresh: { icon: 'help', title: () => 'Dữ liệu đã thay đổi', text: 'Thông tin công nợ đã được cập nhật bởi người khác. Vui lòng tải lại để xem dữ liệu mới nhất.', action: 'Tải lại', primary: true },
}
const demoStates: Array<[SpecialState, string]> = [['empty', 'Rỗng'], ['noresult', 'Không kết quả'], ['loading', 'Đang tải'], ['error', 'Lỗi tải'], ['missing', 'Không tồn tại'], ['forbidden', 'Không quyền'], ['refresh', 'Xác thực lại']]

defineExpose({ attempt })
</script>

<template>
  <section class="debt-page">
    <div class="debt-panel">
      <div class="debt-head">
        <div><h1>Công nợ</h1><p>{{ isOwner ? 'Quản lý công nợ khách hàng và nhà cung cấp' : 'Quản lý công nợ khách hàng' }}</p></div>
        <div class="debt-segmented" role="group" aria-label="Loại công nợ">
          <button class="debt-seg" :class="{ active: customer }" type="button" :aria-pressed="customer" :disabled="!!attempt" @click="switchKind('customer')"><LineIcon name="customer" />Khách hàng</button>
          <button v-if="isOwner" class="debt-seg" :class="{ active: !customer }" type="button" :aria-pressed="!customer" :disabled="!!attempt" @click="switchKind('supplier')"><LineIcon name="purchase" />Nhà cung cấp</button>
        </div>
      </div>

      <div class="debt-summary" :class="{ three: summaryCards.length === 3 }">
        <template v-for="card in summaryCards" :key="card.label">
          <button v-if="demo" class="debt-summary-card" :class="card.tone" type="button" :aria-label="`Lọc ${card.label}`" @click="cardFilter(card.key)"><span>{{ card.label }}</span><strong>{{ card.value }}</strong><small>{{ card.note }}</small><LineIcon name="chevron" /></button>
          <div v-else class="debt-summary-card" :class="card.tone"><span>{{ card.label }}</span><strong>{{ card.value }}</strong><small>{{ card.note }}</small></div>
        </template>
      </div>

      <div class="debt-toolbar">
        <label class="debt-search"><LineIcon name="search" /><input v-model="search" aria-label="Tìm công nợ" :placeholder="words.search" :disabled="!!attempt" /></label>
        <button class="debt-filter-button" type="button" :disabled="!!attempt" @click="openFilter"><span aria-hidden="true">▽</span> Bộ lọc</button>
      </div>

      <div v-if="listState !== 'normal'" class="debt-state" :role="listState === 'error' ? 'alert' : 'status'">
        <div class="debt-state-inner">
          <div v-if="listState === 'loading'" class="debt-spinner" aria-hidden="true" />
          <span v-else class="debt-state-icon" :class="{ danger: listState === 'error' }"><LineIcon :name="listState === 'error' ? 'warning' : listState === 'noresult' ? 'search' : 'customer'" /></span>
          <h3>{{ listState === 'loading' ? 'Đang tải công nợ...' : listState === 'error' ? 'Không thể tải dữ liệu' : listState === 'noresult' ? 'Không tìm thấy kết quả' : words.empty }}</h3>
          <p>{{ listState === 'loading' ? 'Vui lòng chờ trong giây lát.' : listState === 'error' ? loadError : listState === 'noresult' ? 'Thử thay đổi từ khóa hoặc bộ lọc.' : 'Sẽ hiển thị danh sách khi có phát sinh công nợ.' }}</p>
          <button v-if="listState === 'error'" class="debt-secondary" type="button" @click="reloadLive">Thử lại</button>
          <button v-if="listState === 'noresult'" class="debt-secondary" type="button" @click="clearAll">Xóa bộ lọc</button>
        </div>
      </div>
      <template v-else>
        <div class="debt-table-wrap"><table class="debt-table" :class="{ live: !demo }">
          <colgroup v-if="demo"><col class="col-check" /><col class="col-name" /><col class="col-phone" /><col class="col-money" /><col class="col-money" /><col class="col-money" /><col class="col-date" /><col class="col-overdue" /><col class="col-status" /></colgroup>
          <colgroup v-else><col class="col-check" /><col style="width:32%" /><col style="width:20%" /><col style="width:18%" /><col style="width:16%" /><col style="width:120px" /></colgroup>
          <thead><tr>
            <th><input type="checkbox" :aria-label="`Chọn tất cả ${words.party} trên trang`" :checked="allChecked" @change="toggleAll" /></th>
            <th>{{ words.Party }}</th><th>Số điện thoại</th>
            <template v-if="demo"><th class="money">Tổng phát sinh ↕</th><th class="money">Đã thanh toán ↕</th></template>
            <th class="money">Còn nợ ↕</th>
            <template v-if="demo"><th>Ngày giao dịch cuối ↕</th><th>Số ngày quá hạn ↕</th></template>
            <th>Trạng thái{{ demo ? ' ↕' : '' }}</th>
            <th v-if="!demo"><span class="sr-only">Thao tác</span></th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.id" :class="{ selected: drawer === 'detail' && detailId === row.id }">
              <td><input type="checkbox" :aria-label="`Chọn ${row.name}`" :checked="checkedIds.includes(row.id)" @change="toggleChecked(row.id)" /></td>
              <td><button class="debt-link" type="button" :disabled="!!attempt" @click="openDetail(row)">{{ row.name }}</button></td>
              <td>{{ row.phone }}</td>
              <template v-if="demo"><td class="money">{{ money(row.incurred) }}</td><td class="money">{{ money(row.paid) }}</td></template>
              <td class="money" :class="{ 'danger-text': row.debt > 0 }">{{ money(row.debt) }}</td>
              <template v-if="demo"><td>{{ row.last }}</td><td :class="{ 'danger-text': row.overdue > 0 }">{{ row.overdue || '-' }}</td></template>
              <td><span class="debt-status" :class="row.tone">{{ toneLabel(row.tone) }}</span></td>
              <td v-if="!demo" class="debt-row-action"><button class="debt-row-pay" type="button" :disabled="!!attempt || row.debt <= 0" @click="openPayment(row)"><LineIcon name="plus" />{{ words.action }}</button></td>
            </tr>
          </tbody>
        </table></div>
        <div class="debt-footer">
          <span>Hiển thị {{ firstIndex }} – {{ lastIndex }} / {{ totalCount }} {{ words.party }}<template v-if="!demo && list?.asOf"> · tính đến {{ formatDateTime(list.asOf) }}</template></span>
          <div class="debt-pager">
            <button type="button" aria-label="Trang trước" :disabled="!!attempt || page <= 1" @click="goPage(page - 1)">‹</button>
            <template v-for="(item, index) in pagerItems" :key="`${item}-${index}`">
              <span v-if="item === '…'" class="debt-ellipsis">…</span>
              <button v-else type="button" :class="{ active: page === item }" :aria-current="page === item ? 'page' : undefined" :disabled="!!attempt" @click="goPage(item)">{{ item }}</button>
            </template>
            <button type="button" aria-label="Trang sau" :disabled="!!attempt || page >= totalPages" @click="goPage(page + 1)">›</button>
            <select v-model.number="pageSize" aria-label="Số dòng mỗi trang" :disabled="!!attempt" @change="page = 1; load(1)"><option :value="5">5 / trang</option><option :value="10">10 / trang</option><option :value="20">20 / trang</option><option :value="50">50 / trang</option></select>
          </div>
        </div>
      </template>
    </div>

    <div v-if="drawer" class="debt-overlay" @click="closeOverlays" />

    <!-- Filter -->
    <aside v-if="drawer === 'filter'" class="debt-drawer" role="dialog" aria-modal="true" aria-label="Bộ lọc công nợ">
      <div class="debt-drawer-head"><h2>Bộ lọc</h2><button class="debt-icon-button" type="button" aria-label="Đóng" @click="drawer = null">×</button></div>
      <div class="debt-drawer-body">
        <div class="debt-section"><h3>Loại đối tượng</h3>
          <label class="debt-check-row"><input v-model="draftKind" type="radio" name="debt-kind" value="customer" />Khách hàng</label>
          <label v-if="isOwner" class="debt-check-row"><input v-model="draftKind" type="radio" name="debt-kind" value="supplier" />Nhà cung cấp</label>
        </div>
        <template v-if="demo">
          <div class="debt-section"><h3>Trạng thái công nợ</h3>
            <label class="debt-check-row"><input type="checkbox" :checked="draft.statuses.length === 0" @change="toggleDraftStatus('all')" />Tất cả</label>
            <label v-for="[key, label] in statusOptions" :key="key" class="debt-check-row"><input type="checkbox" :checked="draft.statuses.includes(key)" @change="toggleDraftStatus(key)" />{{ label }}</label>
          </div>
          <div class="debt-section"><h3>Khoảng thời gian giao dịch</h3><div class="debt-grid2"><label class="debt-field">Từ ngày<input v-model="draft.from" class="debt-input" placeholder="dd/mm/yyyy" /></label><label class="debt-field">Đến ngày<input v-model="draft.to" class="debt-input" placeholder="dd/mm/yyyy" /></label></div></div>
          <div class="debt-section"><h3>Khoảng nợ hiện tại</h3><div class="debt-grid2"><label class="debt-field">Từ<input v-model="draft.min" class="debt-input" placeholder="0 đ" /></label><label class="debt-field">Đến<input v-model="draft.max" class="debt-input" placeholder="Không giới hạn" /></label></div></div>
        </template>
        <p v-else class="debt-hint">Lọc theo trạng thái, hạn thanh toán, thời gian giao dịch và khoảng nợ chưa được hệ thống hỗ trợ. Danh sách chỉ gồm các đối tác đang còn nợ.</p>
      </div>
      <div class="debt-drawer-foot"><button class="debt-danger-outline" type="button" @click="clearAll">Xóa bộ lọc</button><button class="debt-primary" type="button" @click="applyFilter">Áp dụng</button></div>
    </aside>

    <!-- Detail -->
    <aside v-if="drawer === 'detail'" class="debt-drawer wide" role="dialog" aria-modal="true" :aria-label="`Chi tiết công nợ ${words.party}`">
      <div class="debt-drawer-head">
        <div class="debt-entity"><button class="debt-icon-button" type="button" aria-label="Quay lại danh sách" @click="drawer = null">‹</button><div><h2>{{ detail?.name ?? rows.find(row => row.id === detailId)?.name }}</h2><p>{{ detail?.phone }}</p></div></div>
        <div v-if="detail" class="debt-action-row">
          <button class="debt-primary" type="button" :disabled="detail.debt <= 0" :title="detail.debt <= 0 ? `${words.Party} không còn công nợ` : undefined" @click="openPayment({ id: detail.id, name: detail.name, phone: detail.phone, debt: detail.debt, asOf: liveDetail?.asOf ?? '' })"><LineIcon name="plus" />{{ words.action }}</button>
          <button v-if="demo" class="debt-secondary debt-more" type="button" aria-label="Thao tác khác" @click="toast('Thao tác khác chỉ minh họa trong dữ liệu mẫu')">•••</button>
        </div>
      </div>
      <div class="debt-drawer-body">
        <p v-if="detailLoading" class="debt-hint">Đang tải công nợ…</p>
        <div v-else-if="detailError" class="debt-state small" role="alert"><div class="debt-state-inner">
          <span class="debt-state-icon danger"><LineIcon :name="detailError === 'forbidden' ? 'lock' : 'warning'" /></span>
          <h3>{{ detailError === 'missing' ? 'Không tìm thấy thông tin' : detailError === 'forbidden' ? 'Bạn không có quyền xem công nợ này' : 'Không thể tải dữ liệu' }}</h3>
          <p>{{ detailError === 'missing' ? `${words.Party} không tồn tại hoặc đã bị vô hiệu hóa.` : detailError === 'forbidden' ? 'Vui lòng liên hệ chủ cửa hàng.' : 'Vui lòng thử lại sau.' }}</p>
          <button class="debt-secondary" type="button" @click="drawer = null">Quay lại</button>
        </div></div>
        <template v-if="detail">
          <div class="debt-tabs" role="tablist"><button v-for="tab in detailTabs" :key="tab.key" class="debt-tab" :class="{ active: detailTab === tab.key }" type="button" role="tab" :aria-selected="detailTab === tab.key" @click="detailTab = tab.key">{{ tab.label }}</button></div>
          <template v-if="detailTab === 'overview'">
            <div class="debt-info-grid">
              <div class="debt-info-card"><h3>Thông tin {{ words.party }}</h3><div v-for="[label, value] in detail.info" :key="label" class="debt-kv"><span>{{ label }}</span><strong>{{ value }}</strong></div></div>
              <div class="debt-info-card"><h3>Công nợ hiện tại</h3><div v-for="item in detail.debtInfo" :key="item.label" class="debt-kv"><span>{{ item.label }}</span><strong :class="{ 'danger-text': item.tone === 'danger', 'good-text': item.tone === 'good' }">{{ item.value }}</strong></div></div>
            </div>
            <template v-if="demo">
              <div class="debt-subhead"><h3>Phát sinh gần đây ({{ detail.recent.length }})</h3><button type="button" @click="openHistory">Xem tất cả lịch sử giao dịch →</button></div>
              <table class="debt-mini-table"><thead><tr><th>#</th><th>Ngày</th><th>Loại giao dịch</th><th>Mã chứng từ</th><th class="num">Phát sinh</th><th class="num">Đã thanh toán</th><th class="num">Còn nợ</th></tr></thead>
                <tbody><tr v-for="(item, index) in detail.recent" :key="index"><td>{{ index + 1 }}</td><td>{{ item.date }}</td><td>{{ item.type }}</td><td>{{ item.document }}</td><td class="num">{{ money(item.incurred) }}</td><td class="num good-text">{{ money(item.paid) }}</td><td class="num" :class="amountClass(item.balance)">{{ money(item.balance) }}</td></tr></tbody>
              </table>
            </template>
            <div v-else class="debt-info-note">ℹ Lịch sử giao dịch theo từng {{ words.party }} chưa được hệ thống hỗ trợ. Xem các {{ customer ? 'đơn bán' : 'phiếu nhập' }} liên quan trong <RouterLink :to="customer ? '/sales' : '/purchases'">{{ customer ? 'Đơn bán' : 'Nhập hàng' }}</RouterLink>.</div>
          </template>
          <template v-else-if="detailTab === 'transactions'">
            <div class="debt-subhead"><h3>Lịch sử giao dịch</h3><button type="button" @click="openHistory">Mở toàn màn hình →</button></div>
            <table class="debt-mini-table"><thead><tr><th>Ngày</th><th>Loại GD</th><th>Mã CT</th><th class="num">Phát sinh</th><th class="num">Thanh toán</th><th class="num">Số dư</th></tr></thead>
              <tbody><tr v-for="(item, index) in detail.history" :key="index"><td>{{ item.date }}</td><td>{{ item.type }}</td><td>{{ item.document }}</td><td class="num">{{ money(item.incurred) }}</td><td class="num good-text">{{ money(item.paid) }}</td><td class="num">{{ money(item.balance) }}</td></tr></tbody>
            </table>
          </template>
          <template v-else-if="detailTab === 'payments'">
            <div class="debt-info-note">Tổng {{ words.verb }} đã ghi nhận: {{ money(detail.payments.reduce((sum, item) => sum + item.amount, 0)) }}.</div>
            <table class="debt-mini-table spaced"><thead><tr><th>Ngày</th><th>Mã phiếu</th><th>Phương thức</th><th class="num">Số tiền</th><th>Người tạo</th></tr></thead>
              <tbody><tr v-for="(item, index) in detail.payments" :key="index"><td>{{ item.date }}</td><td>{{ item.document }}</td><td>{{ item.method }}</td><td class="num good-text">{{ money(item.amount) }}</td><td>{{ item.actor }}</td></tr></tbody>
            </table>
          </template>
          <template v-else>
            <div class="debt-entity padded"><span class="debt-entity-icon"><LineIcon name="customer" /></span><div><h3>{{ detail.name }}</h3><p>{{ detail.phone }}</p></div></div>
            <div class="debt-explain-box highlight"><h3>Công nợ <span class="danger-text">{{ money(detail.debt) }}</span></h3><p class="debt-hint">Tính đến {{ detail.asOf }}</p></div>
            <template v-if="demo">
              <div class="debt-explain-box"><h3>Công thức tính</h3><div class="debt-formula"><span v-for="line in detail.formula" :key="line">{{ line }}</span></div></div>
              <div class="debt-explain-box"><h3>Trong đó</h3><div v-for="item in detail.breakdown" :key="item.label" class="debt-breakdown"><div><strong>{{ item.label }}</strong><span class="debt-hint">{{ item.hint }}</span></div><b>{{ item.value }}</b></div></div>
              <div class="debt-info-note">ℹ Số liệu được tính từ các giao dịch {{ customer ? 'bán hàng và thu tiền' : 'nhập hàng và trả tiền' }} đã hoàn tất. Không bao gồm các giao dịch đang tạm lưu (nếu có).</div>
            </template>
            <template v-else>
              <div class="debt-explain-box"><h3>Công nợ hiện tại được tính thế nào?</h3><div class="debt-formula"><span>Công nợ hiện tại</span><span>= Nghĩa vụ phát sinh hợp lệ</span><span>– Các khoản thanh toán làm giảm nghĩa vụ</span><span>– Các điều chỉnh nghĩa vụ hợp lệ</span></div></div>
              <div class="debt-explain-box"><h3>Trong đó</h3><ul class="debt-explain-list">
                <template v-if="customer"><li>Phần chưa thanh toán của các đơn bán đã hoàn tất.</li><li>Các lần thu nợ riêng làm giảm công nợ chung của khách hàng — không phải doanh thu.</li><li>Trả hàng và hủy đơn bán làm giảm nghĩa vụ; khoản hoàn tiền thực tế được ghi nhận riêng.</li></template>
                <template v-else><li>Phần chưa thanh toán của các phiếu nhập đã hoàn tất.</li><li>Các lần trả nợ riêng làm giảm công nợ chung — không thay đổi giá trị phiếu nhập.</li><li>Hủy phiếu nhập làm giảm nghĩa vụ phải trả.</li></template>
              </ul></div>
              <div class="debt-info-note">ℹ Các khoản {{ words.verb }} tiền không được phân bổ vào hóa đơn hay phiếu cụ thể. Công nợ không bao giờ âm.</div>
            </template>
          </template>
        </template>
      </div>
    </aside>

    <!-- Collection / settlement -->
    <div v-if="payOpen && payParty" class="debt-modal-layer">
      <section class="debt-modal" role="dialog" aria-modal="true" :aria-label="words.modal">
        <div class="debt-modal-head"><h2>{{ words.modal }}</h2><button class="debt-icon-button" type="button" aria-label="Đóng" :disabled="busy || !!attempt" @click="closePayment">×</button></div>
        <form class="debt-modal-body" @submit.prevent="payStep === 'form' ? reviewPayment() : payStep === 'confirm' ? confirmPayment() : closePayment()">
          <div class="debt-entity padded"><span class="debt-entity-icon"><LineIcon name="customer" /></span><div><h3>{{ payParty.name }}</h3><p>{{ payParty.phone }}</p></div></div>
          <h4 class="debt-form-title">Thông tin công nợ hiện tại</h4>
          <div class="debt-amount-box"><span>Còn nợ</span><strong>{{ money(payParty.outstanding) }}</strong></div>
          <p v-if="!demo && payParty.asOf" class="debt-hint">Tính đến {{ formatDateTime(payParty.asOf) }}</p>
          <div v-if="stale" class="debt-info-note warn" role="status"><b>Dữ liệu đã thay đổi</b><br />{{ message }}</div>
          <p v-else-if="message" class="debt-error" role="status">{{ message }}</p>

          <template v-if="payStep === 'form'">
            <h4 class="debt-form-title">{{ words.info }}</h4>
            <label class="debt-field"><span>{{ words.amount }} <span class="required">*</span></span>
              <div class="debt-amount-input"><input v-model.number="amount" class="debt-input" type="number" min="0.01" step="0.01" :max="payParty.outstanding" :disabled="busy || !!attempt" /><span>đ</span></div>
            </label>
            <button class="debt-link-button" type="button" :disabled="busy || !!attempt" @click="amount = payParty.outstanding">{{ words.all }} ({{ money(payParty.outstanding) }})</button>
            <label class="debt-field"><span>{{ words.method }} <span class="required">*</span></span><select v-model="method" class="debt-input" :disabled="busy || !!attempt"><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select></label>
            <label v-if="demo" class="debt-field"><span>{{ words.date }} <span class="required">*</span></span><input v-model="payDate" class="debt-input" placeholder="dd/mm/yyyy" /></label>
            <label class="debt-field"><span>Ghi chú</span><textarea v-model="note" class="debt-input debt-textarea" maxlength="250" placeholder="Nhập ghi chú (nếu có)..." :disabled="busy || !!attempt" /><small class="debt-hint">{{ note.trim().length }}/250 ký tự</small></label>
          </template>
          <div v-else-if="payStep === 'confirm'" class="debt-confirm">
            <p>Xác nhận <b>{{ words.verb }} {{ money(amount) }}</b> {{ customer ? 'từ' : 'cho' }} <b>{{ payParty.name }}</b> bằng {{ methodLabel(method).toLowerCase() }}<template v-if="demo"> ngày {{ payDate }}</template>.</p>
            <div class="debt-kv"><span>Công nợ trước</span><strong>{{ money(payParty.outstanding) }}</strong></div>
            <div class="debt-kv"><span>Công nợ sau</span><strong class="danger-text">{{ money(payParty.outstanding - amount) }}</strong></div>
            <div v-if="note.trim()" class="debt-kv"><span>Ghi chú</span><strong>{{ note.trim() }}</strong></div>
            <div class="debt-info-note">ℹ {{ words.meaning }}</div>
            <p v-if="attempt" class="debt-lock-note">Dữ liệu đang được giữ nguyên cho cùng một thao tác.</p>
          </div>
          <div v-else-if="payResult" class="debt-result" aria-label="Chi tiết thanh toán công nợ">
            <div class="debt-result-head"><LineIcon name="check" />Đã {{ words.verb }} {{ money(payResult.amount) }}</div>
            <div class="debt-kv"><span>Phương thức</span><strong>{{ methodLabel(payResult.method) }}</strong></div>
            <div class="debt-kv"><span>Thời điểm</span><strong>{{ payResult.date }}</strong></div>
            <div class="debt-kv"><span>Công nợ</span><strong>{{ money(payResult.before) }} → {{ money(payResult.after) }}</strong></div>
            <p class="debt-hint">{{ payResult.after === 0 ? `${words.Party} không còn công nợ hiện tại.` : `Công nợ còn lại: ${money(payResult.after)}.` }} Khoản {{ words.verb }} không gắn với hóa đơn cụ thể.</p>
          </div>
        </form>
        <div class="debt-modal-foot">
          <template v-if="payStep === 'form'"><button class="debt-secondary" type="button" :disabled="busy || !!attempt" @click="closePayment">Hủy</button><button class="debt-primary" type="button" :disabled="busy" @click="reviewPayment">{{ words.action }}</button></template>
          <template v-else-if="payStep === 'confirm'"><button class="debt-secondary" type="button" :disabled="busy || !!attempt" @click="payStep = 'form'">← Quay lại</button><button class="debt-primary" type="button" :disabled="busy" @click="confirmPayment">{{ busy ? 'Đang xác nhận…' : attempt ? 'Thử lại đúng thao tác' : `Xác nhận ${words.action.toLowerCase()}` }}</button></template>
          <button v-else class="debt-primary" type="button" @click="closePayment">Xong</button>
        </div>
      </section>
    </div>

    <!-- Full history (sample data) -->
    <div v-if="historyOpen && detail" class="debt-modal-layer" @click.self="historyOpen = false">
      <section class="debt-modal wide" role="dialog" aria-modal="true" aria-label="Lịch sử giao dịch">
        <div class="debt-modal-head"><h2>Lịch sử giao dịch</h2><button class="debt-icon-button" type="button" aria-label="Đóng" @click="historyOpen = false">×</button></div>
        <div class="debt-modal-body">
          <div class="debt-history-filters">
            <label class="debt-field">Loại giao dịch<select v-model="historyType" class="debt-input"><option>Tất cả</option><option v-for="label in customer ? ['Bán hàng', 'Thu tiền'] : ['Nhập hàng', 'Trả tiền']" :key="label">{{ label }}</option></select></label>
            <label class="debt-field">Từ ngày<input v-model="historyFrom" class="debt-input" placeholder="dd/mm/yyyy" /></label>
            <label class="debt-field">Đến ngày<input v-model="historyTo" class="debt-input" placeholder="dd/mm/yyyy" /></label>
          </div>
          <table class="debt-mini-table"><thead><tr><th>#</th><th>Ngày</th><th>Loại giao dịch</th><th>Mã chứng từ</th><th class="num">Phát sinh</th><th class="num">Thanh toán</th><th class="num">Số dư</th></tr></thead>
            <tbody>
              <tr v-for="(item, index) in historyRows" :key="index"><td>{{ index + 1 }}</td><td>{{ item.date }}</td><td>{{ item.type }}</td><td>{{ item.document }}</td><td class="num">{{ money(item.incurred) }}</td><td class="num good-text">{{ money(item.paid) }}</td><td class="num">{{ money(item.balance) }}</td></tr>
              <tr v-if="historyRows.length === 0"><td colspan="7" class="debt-hint">Không có giao dịch phù hợp.</td></tr>
            </tbody>
          </table>
        </div>
        <div class="debt-modal-foot"><button class="debt-secondary" type="button" @click="historyOpen = false">Đóng</button></div>
      </section>
    </div>

    <!-- Reference special states (sample data) -->
    <div v-if="special" class="debt-special" role="status">
      <div class="debt-special-card">
        <div v-if="special === 'loading'" class="debt-spinner" aria-hidden="true" />
        <span v-else-if="specialCopy[special].icon" class="debt-state-icon large" :class="{ danger: special === 'error' || special === 'missing', info: special === 'refresh' }"><LineIcon :name="specialCopy[special].icon!" /></span>
        <h2>{{ specialCopy[special].title() }}</h2>
        <p>{{ specialCopy[special].text }}</p>
        <button :class="specialCopy[special].primary ? 'debt-primary' : 'debt-secondary'" type="button" @click="special = null">{{ specialCopy[special].action }}</button>
      </div>
    </div>
    <div v-if="demo" class="debt-demo-strip" aria-label="Demo trạng thái">
      <button v-for="[state, label] in demoStates" :key="state" type="button" @click="special = state">{{ label }}</button>
    </div>
    <div v-if="toastText" class="debt-toast" role="status">{{ toastText }}</div>
  </section>
</template>

<style scoped>
.debt-page{color:#172033}
.debt-panel{min-height:calc(100vh - 74px);padding:12px;background:#fff;border:1px solid #dfe7ee;border-radius:14px;box-shadow:0 10px 30px #1f304714}
.debt-head{display:flex;align-items:flex-start;justify-content:space-between;gap:18px;padding:2px 2px 12px}
.debt-head h1{margin:0 0 3px;font-size:22px;font-weight:800;line-height:1.2;letter-spacing:-.03em}
.debt-head p{margin:0;color:#66758d;font-size:12px}
.debt-segmented{display:flex;gap:8px}
.debt-seg{display:inline-flex;height:36px;align-items:center;gap:8px;border:1px solid #cbd8e2;border-radius:8px;background:#fff;padding:0 16px;color:#43536a;font-size:12px;font-weight:750;cursor:pointer}
.debt-seg .line-icon{width:16px;height:16px}
.debt-seg.active{border-color:#bfe5d5;background:#eaf8f3;color:#0b805a}
.debt-seg:disabled{cursor:not-allowed;opacity:.6}
.debt-primary,.debt-secondary,.debt-danger-outline{display:inline-flex;height:36px;align-items:center;justify-content:center;gap:7px;border-radius:8px;padding:0 16px;font-size:12px;font-weight:800;white-space:nowrap;cursor:pointer}
.debt-primary{border:0;background:linear-gradient(180deg,#0ba46e,#078c5f);color:#fff;box-shadow:0 5px 12px #08976530}
.debt-primary:hover{background:#087b55}
.debt-primary .line-icon{width:15px;height:15px}
.debt-secondary{border:1px solid #cbd8e2;background:#fff;color:#314158}
.debt-danger-outline{border:1px solid #f5b4b7;background:#fff;color:#d93f45}
.debt-primary:disabled,.debt-secondary:disabled{cursor:not-allowed;opacity:.55}
.debt-summary{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;margin-bottom:12px}
.debt-summary.three{grid-template-columns:repeat(3,minmax(0,1fr))}
.debt-summary-card{position:relative;display:block;min-height:80px;border:1px solid #dfe7ee;border-radius:10px;background:#fff;padding:12px 14px;color:#172033;text-align:left}
button.debt-summary-card{cursor:pointer}
button.debt-summary-card:hover{border-color:#b9d8c9;background:#fbfefd}
.debt-summary-card span{display:block;margin-bottom:8px;color:#65758b;font-size:10px}
.debt-summary-card strong{display:block;font-size:22px;font-weight:850;line-height:1;white-space:nowrap}
.debt-summary-card small{display:block;margin-top:7px;color:#8795a8;font-size:10px}
.debt-summary-card .line-icon{position:absolute;top:50%;right:12px;width:14px;height:14px;color:#9aa7b8;transform:translateY(-50%) rotate(-90deg)}
.debt-summary-card.danger strong{color:#ef2d2d}
.debt-summary-card.warning strong{color:#e58a00}
.debt-toolbar{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:10px;margin-bottom:12px}
.debt-search{display:flex;height:36px;align-items:center;gap:9px;border:1.5px solid #b8cef8;border-radius:8px;background:#fff;padding:0 11px;color:#53617b}
.debt-search>.line-icon{width:13px;height:13px}
.debt-search input{flex:1;min-width:0;border:0;outline:0;background:transparent;color:#44536a;font-size:12px}
.debt-filter-button{height:36px;border:1px solid #cbd8e2;border-radius:8px;background:#fff;color:#263852;font-size:12px;font-weight:700;cursor:pointer}
.debt-filter-button:disabled{cursor:not-allowed;opacity:.6}
/* Positioned so the absolutely placed sr-only header label stays inside the horizontal scroller. */
.debt-table-wrap{position:relative;overflow-x:auto;border:1px solid #dfe7ee;border-radius:9px;background:#fff}
.debt-table{width:100%;min-width:900px;border-collapse:collapse;table-layout:fixed}
.debt-table.live{min-width:640px}
.col-check{width:34px}.col-name{width:15%}.col-phone{width:11%}.col-money{width:12%}.col-date{width:13%}.col-overdue{width:11%}.col-status{width:13%}
.debt-table th{height:34px;border-bottom:1px solid #dfe7ee;background:#f7fafc;padding:0 9px;color:#56657b;font-size:10.5px;font-weight:750;text-align:left;white-space:nowrap}
.debt-table td{height:44px;border-bottom:1px solid #edf1f4;padding:0 9px;color:#2b3a51;font-size:11px;vertical-align:middle}
.debt-table tbody tr:last-child td{border-bottom:0}
.debt-table tbody tr:hover{background:#fbfdfd}
.debt-table tbody tr.selected{background:#f2fbf7}
.debt-table input[type=checkbox]{width:14px;height:14px;margin:0;accent-color:#0a9c67;cursor:pointer}
.debt-table .money{font-weight:750;text-align:right}
.debt-link{overflow:hidden;max-width:100%;padding:0;border:0;background:none;color:#1c2e47;font-weight:800;text-align:left;text-overflow:ellipsis;white-space:nowrap;cursor:pointer}
.debt-link:hover{color:#087b55;text-decoration:underline}
.danger-text{color:#ef3038!important}
.good-text{color:#079b67!important}
.debt-status{display:inline-flex;align-items:center;gap:5px;border-radius:999px;padding:4px 9px;font-size:10px;font-weight:750;white-space:nowrap}
.debt-status::before{width:6px;height:6px;border-radius:50%;background:currentColor;content:""}
.debt-status.paid{background:#e7f8ef;color:#087b57}
.debt-status.overdue{background:#ffe9e9;color:#ef3d46}
.debt-status.due,.debt-status.owing{background:#fff1d9;color:#d77800}
.debt-row-action{text-align:right}
.debt-row-pay{display:inline-flex;height:30px;align-items:center;gap:5px;border:1px solid #bfe5d5;border-radius:7px;background:#eaf8f3;padding:0 10px;color:#0b805a;font-size:11px;font-weight:800;cursor:pointer}
.debt-row-pay .line-icon{width:13px;height:13px}
.debt-row-pay:disabled{cursor:not-allowed;opacity:.5}
.debt-footer{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:12px 4px 2px;color:#637087;font-size:11px}
.debt-pager{display:flex;align-items:center;gap:5px}
.debt-pager button,.debt-pager select,.debt-ellipsis{display:grid;min-width:30px;height:30px;place-items:center;border:1px solid #dfe7ee;border-radius:7px;background:#fff;padding:0 7px;color:#43536a;font-size:11px;cursor:pointer}
.debt-pager button.active{border-color:#ccebdd;background:#e9f7f2;color:#087d59;font-weight:800}
.debt-pager button:disabled{cursor:default;opacity:.45}
.debt-pager select{display:block;margin-left:5px;padding:0 9px}
.debt-ellipsis{cursor:default}
.debt-state{display:grid;min-height:360px;place-items:center;color:#718096;text-align:center}
.debt-state.small{min-height:240px}
.debt-state-inner{max-width:340px}
.debt-state-inner h3{margin:0 0 6px;color:#26364d;font-size:15px;font-weight:800}
.debt-state-inner p{margin:0 0 12px;font-size:11px;line-height:1.5}
.debt-state-icon{display:grid;width:52px;height:52px;margin:0 auto 10px;place-items:center;border-radius:50%;background:#f1f6fb;color:#5c6d86}
.debt-state-icon .line-icon{width:26px;height:26px}
.debt-state-icon.danger{background:#fff0f1;color:#e5484d}
.debt-state-icon.info{background:#e8f1ff;color:#2f6fd6}
.debt-state-icon.large{width:64px;height:64px}
.debt-state-icon.large .line-icon{width:32px;height:32px}
.debt-spinner{width:38px;height:38px;margin:0 auto 12px;border:4px solid #d8e8f8;border-top-color:#3b82f6;border-radius:50%;animation:debt-spin 1s linear infinite}
@keyframes debt-spin{to{transform:rotate(360deg)}}

.debt-overlay{position:fixed;z-index:55;inset:0;background:transparent}
.debt-drawer{position:fixed;z-index:70;top:var(--app-topbar-height,0px);right:0;bottom:0;display:flex;width:min(420px,100vw);flex-direction:column;border-left:1px solid #dfe7ee;background:#fff;box-shadow:-12px 0 28px #1e364d14}
.debt-drawer.wide{width:min(650px,100vw)}
.debt-drawer-head,.debt-modal-head{display:flex;min-height:54px;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #dfe7ee;padding:8px 16px}
.debt-drawer-head h2,.debt-modal-head h2{margin:0;font-size:17px;font-weight:800}
.debt-icon-button{display:grid;width:32px;height:32px;flex:none;place-items:center;border:0;border-radius:8px;background:transparent;color:#42536b;font-size:21px;cursor:pointer}
.debt-icon-button:hover{background:#f4f6f8}
.debt-icon-button:disabled{cursor:not-allowed;opacity:.4}
.debt-drawer-body{flex:1;overflow:auto;padding:14px 16px}
.debt-drawer-foot{display:grid;grid-template-columns:1fr 1fr;gap:8px;border-top:1px solid #dfe7ee;padding:12px 16px}
.debt-section{margin-bottom:14px;border-bottom:1px solid #edf1f4;padding-bottom:14px}
.debt-section:last-child{border-bottom:0}
.debt-section h3{margin:0 0 9px;font-size:12px;font-weight:800}
.debt-check-row{display:flex;align-items:center;gap:8px;margin:8px 0;color:#43536a;font-size:11px;cursor:pointer}
.debt-check-row input{width:14px;height:14px;margin:0;accent-color:#0b9567}
.debt-field{display:grid;gap:5px;margin-bottom:10px;color:#3e4e66;font-size:11px;font-weight:750}
.debt-input{width:100%;min-height:34px;border:1px solid #cbd8e2;border-radius:7px;background:#fff;padding:7px 9px;color:#34445a;font-size:11px;font-weight:400}
.debt-input:disabled{background:#f4f7fa}
.debt-textarea{min-height:70px;resize:vertical}
.debt-grid2{display:grid;grid-template-columns:1fr 1fr;gap:9px}
.debt-hint{display:block;margin:0;color:#7c8a9d;font-size:10px;font-weight:400;line-height:1.45}
.debt-error{margin:0 0 10px;border-radius:8px;background:#fff6e6;padding:8px 10px;color:#8a5a00;font-size:11px}
.required{color:#d63e45}
.debt-entity{display:flex;min-width:0;align-items:center;gap:10px}
.debt-entity h2,.debt-entity h3{margin:0;font-size:15px;font-weight:800}
.debt-entity p{margin:2px 0 0;color:#64748b;font-size:10.5px}
.debt-entity.padded{margin-bottom:12px}
.debt-entity-icon{display:grid;width:36px;height:36px;flex:none;place-items:center;border-radius:8px;background:#edf6ff;color:#3478d2}
.debt-entity-icon .line-icon{width:18px;height:18px}
.debt-action-row{display:flex;gap:7px}
.debt-more{width:36px;padding:0}
.debt-tabs{display:flex;gap:16px;margin:0 -16px 12px;overflow-x:auto;border-bottom:1px solid #dfe7ee;padding:0 16px}
.debt-tab{height:36px;border:0;border-bottom:2px solid transparent;background:transparent;color:#66758b;font-size:11px;font-weight:750;white-space:nowrap;cursor:pointer}
.debt-tab.active{border-bottom-color:#0a9d69;color:#087f59}
.debt-info-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-bottom:12px}
.debt-info-card{border:1px solid #dfe7ee;border-radius:8px;background:#fbfdfe;padding:11px}
.debt-info-card h3{margin:0 0 8px;font-size:12px;font-weight:800}
.debt-kv{display:grid;grid-template-columns:115px 1fr;gap:6px;padding:5px 0;font-size:11px}
.debt-kv span{color:#65758a}
.debt-kv strong{font-weight:700;text-align:right;overflow-wrap:anywhere}
.debt-subhead{display:flex;align-items:center;justify-content:space-between;margin:10px 0 7px}
.debt-subhead h3{margin:0;font-size:12px;font-weight:800}
.debt-subhead button,.debt-link-button{border:0;background:transparent;padding:0;color:#1772c8;font-size:10.5px;font-weight:700;cursor:pointer}
.debt-link-button{justify-self:start;margin:-4px 0 10px}
.debt-link-button:disabled{cursor:not-allowed;opacity:.5}
.debt-mini-table{width:100%;border-collapse:collapse;font-size:10.5px}
.debt-mini-table.spaced{margin-top:10px}
.debt-mini-table th{background:#f7fafc;padding:7px;border-bottom:1px solid #dfe7ee;color:#617089;text-align:left;white-space:nowrap}
.debt-mini-table td{padding:7px;border-bottom:1px solid #edf1f4}
.debt-mini-table .num{text-align:right;font-weight:700}
.debt-info-note{border:1px solid #d4e8fb;border-radius:8px;background:#eef7ff;padding:10px;color:#356385;font-size:10.5px;line-height:1.5}
.debt-info-note.warn{margin-bottom:10px;border-color:#f6e0b3;background:#fff8e8;color:#8a5a00}
.debt-info-note a{color:#087b55;font-weight:700}
.debt-explain-box{margin-bottom:9px;border:1px solid #dce6ee;border-radius:8px;background:#fff;padding:11px}
.debt-explain-box.highlight{border-color:#d4e8fb;background:#f4f9ff}
.debt-explain-box h3{margin:0 0 7px;font-size:12px;font-weight:800}
.debt-formula{display:grid;border:1px solid #f6e0b3;border-radius:7px;background:#fff8e8;padding:10px;font-size:11px;line-height:1.6}
.debt-formula span:last-child{font-weight:800}
.debt-breakdown{display:flex;align-items:flex-start;justify-content:space-between;gap:10px;padding:6px 0;font-size:11px}
.debt-breakdown strong{display:block;font-weight:700}
.debt-explain-list{margin:0;padding-left:18px;color:#3e4e66;font-size:11px;line-height:1.6}

.debt-modal-layer{position:fixed;z-index:90;inset:0;display:grid;place-items:center;background:#12202d42;padding:18px}
.debt-modal{display:flex;width:min(440px,95vw);max-height:92vh;flex-direction:column;overflow:hidden;border-radius:12px;background:#fff;box-shadow:0 20px 55px #152d433d}
.debt-modal.wide{width:min(780px,96vw)}
.debt-modal-body{overflow:auto;padding:14px 16px}
.debt-modal-foot{display:flex;justify-content:flex-end;gap:8px;border-top:1px solid #dfe7ee;padding:12px 16px}
.debt-modal-foot button{min-width:96px}
.debt-form-title{margin:12px 0 8px;font-size:12px;font-weight:800}
.debt-amount-box{display:flex;align-items:center;justify-content:space-between;margin-bottom:6px;border:1px solid #dfe7ee;border-radius:8px;background:#fbfdfe;padding:10px;font-size:11px}
.debt-amount-box span{color:#65758a}
.debt-amount-box strong{color:#ef323b;font-size:15px;font-weight:850}
.debt-amount-input{position:relative}
.debt-amount-input input{padding-right:28px}
.debt-amount-input span{position:absolute;top:9px;right:10px;color:#52627a;font-size:11px;font-weight:400}
.debt-confirm{display:grid;gap:4px;margin-top:10px}
.debt-confirm p{margin:0 0 6px;font-size:12px;line-height:1.55}
.debt-confirm .debt-info-note{margin-top:6px}
.debt-lock-note{color:#8a5a00!important;font-size:11px!important;font-weight:700}
.debt-result{margin-top:10px;border:1px solid #cfeadd;border-radius:9px;background:#f3fbf7;padding:11px}
.debt-result-head{display:flex;align-items:center;gap:7px;margin-bottom:6px;color:#087b57;font-size:13px;font-weight:850}
.debt-result-head .line-icon{width:16px;height:16px}
.debt-history-filters{display:grid;grid-template-columns:repeat(3,1fr);gap:9px;margin-bottom:4px}

.debt-special{position:fixed;z-index:60;top:var(--app-topbar-height,0px);right:0;bottom:0;left:var(--app-sidebar-width,0px);display:grid;place-items:center;background:#f7fafc}
.debt-special-card{width:min(430px,90vw);padding:30px;text-align:center}
.debt-special-card h2{margin:0 0 6px;font-size:18px;font-weight:800}
.debt-special-card p{margin:0 0 14px;color:#718096;font-size:11px;line-height:1.6}
/* Above the special-state overlay so states can be switched, below drawers and modals so it never covers their actions. */
.debt-demo-strip{position:fixed;z-index:65;right:14px;bottom:14px;display:flex;max-width:520px;flex-wrap:wrap;gap:5px;border:1px solid #dfe7ee;border-radius:9px;background:#fff;padding:6px;box-shadow:0 10px 28px #1e364d14}
.debt-demo-strip button{height:28px;border:1px solid #dfe7ee;border-radius:6px;background:#fff;padding:0 8px;color:#45536a;font-size:10px;cursor:pointer}
.debt-demo-strip button:hover{background:#f4f8fa}
.debt-toast{position:fixed;z-index:200;bottom:22px;left:50%;border-radius:8px;background:#172033;padding:9px 14px;color:#fff;font-size:11px;box-shadow:0 10px 28px #1e364d14;transform:translateX(-50%)}

@media(max-width:980px){.debt-summary,.debt-summary.three{grid-template-columns:repeat(2,minmax(0,1fr))}}
@media(max-width:760px){
  .debt-panel{min-height:auto}
  .debt-head{flex-direction:column;align-items:stretch}
  .debt-head h1{font-size:19px}
  .debt-summary-card strong{font-size:17px}
  .debt-toolbar{grid-template-columns:1fr}
  .debt-footer{align-items:flex-start;flex-direction:column}
  .debt-pager{flex-wrap:wrap}
  .debt-drawer,.debt-drawer.wide{width:100%}
  .debt-info-grid,.debt-grid2,.debt-history-filters{grid-template-columns:1fr}
  .debt-drawer-head{flex-wrap:wrap}
  .debt-demo-strip{right:8px;left:8px;justify-content:center}
}
</style>
