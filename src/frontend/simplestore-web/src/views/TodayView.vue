<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import type { RouteLocationRaw } from 'vue-router'
import { apiRequest } from '../api/client'
import { recordC14EventBestEffort } from '../api/c14Telemetry'
import type { C14AttentionItem, C14AttentionKind, C14AttentionList, TodayExplanation, TodayMetricId, TodaySourceNavigation, TodaySummary } from '../api/types'

const summary = ref<TodaySummary | null>(null)
const attention = ref<C14AttentionList | null>(null)
const explanation = ref<TodayExplanation | null>(null)
const loading = ref(true)
const attentionLoading = ref(true)
const explanationLoading = ref(false)
const message = ref('')
const attentionMessage = ref('')
const explanationMessage = ref('')
const signalAttempts = new Set<string>()
const observedItems = new Map<Element, C14AttentionItem>()
let visibilityObserver: IntersectionObserver | null = null
let todayOpenedAttempted = false
const money = (value: number) => `${new Intl.NumberFormat('vi-VN').format(value)} ₫`

const attentionLabels: Record<C14AttentionKind, string> = {
  NegativeStock: 'Tồn kho đang âm',
  OutOfStock: 'Đã hết hàng',
  LowStockRisk: 'Có nguy cơ sắp hết hàng',
}

const metricLabels: Record<TodayMetricId, string> = {
  revenue: 'Doanh thu hôm nay',
  collected: 'Tiền thu thuần hôm nay',
  'estimated-gross-profit': 'Lãi gộp ước tính',
  'sale-count': 'Số đơn bán',
  'customer-debt-created': 'Công nợ khách mới phát sinh',
  'supplier-debt-created': 'Công nợ nhà cung cấp mới phát sinh',
}

const cards = computed(() => {
  if (!summary.value) return []
  return [
    { metric: 'revenue' as const, value: money(summary.value.salesRevenue) },
    { metric: 'collected' as const, value: money(summary.value.netCollected) },
    { metric: 'estimated-gross-profit' as const, value: money(summary.value.estimatedGrossProfit.amount) },
    { metric: 'sale-count' as const, value: new Intl.NumberFormat('vi-VN').format(summary.value.saleCount) },
    { metric: 'customer-debt-created' as const, value: money(summary.value.customerDebtCreated) },
    { metric: 'supplier-debt-created' as const, value: money(summary.value.supplierDebtCreated) },
  ]
})

function localBusinessDate(value: string) {
  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}

function sourceRoute(navigation: TodaySourceNavigation): RouteLocationRaw {
  const type = navigation.type
  switch (type) {
    case 'Sale':
      return { name: 'sale-detail', params: { id: navigation.id } }
    case 'Return':
      return { name: 'return-detail', params: { id: navigation.id } }
    case 'Purchase':
      return { name: 'purchase-detail', params: { id: navigation.id } }
    default:
      return assertNever(type)
  }
}

function assertNever(value: never): never {
  throw new Error(`Unsupported Today source navigation type: ${String(value)}`)
}

async function loadSummary() {
  loading.value = true
  message.value = ''
  try {
    summary.value = await apiRequest<TodaySummary>('/api/today')
    await nextTick()
    if (!todayOpenedAttempted) {
      todayOpenedAttempted = true
      void recordC14EventBestEffort('TodayOpened')
    }
  } catch (reason) {
    message.value = reason instanceof Error ? reason.message : 'Không thể tải thông tin hôm nay.'
  } finally {
    loading.value = false
  }
}

async function loadAttention() {
  attentionLoading.value = true
  attentionMessage.value = ''
  try {
    attention.value = await apiRequest<C14AttentionList>('/api/today/attention?page=1&pageSize=3')
  } catch (reason) {
    attentionMessage.value = reason instanceof Error ? reason.message : 'Không thể tải các mặt hàng cần chú ý.'
  } finally {
    attentionLoading.value = false
  }
}

function signalKey(item: C14AttentionItem) {
  return `${item.productId}:${item.attentionKind}`
}

function emitSignalShown(item: C14AttentionItem) {
  const key = signalKey(item)
  if (signalAttempts.has(key)) return
  signalAttempts.add(key)
  void recordC14EventBestEffort('SignalShown', item.productId, item.attentionKind)
}

function observeAttention(element: unknown, item: C14AttentionItem) {
  if (!(element instanceof Element)) return
  observedItems.set(element, item)
  if (visibilityObserver) visibilityObserver.observe(element)
  else emitSignalShown(item)
}

function openAttention(item: C14AttentionItem) {
  void recordC14EventBestEffort('WhyOpened', item.productId, item.attentionKind)
}

async function showExplanation(metric: TodayMetricId) {
  explanationLoading.value = true
  explanationMessage.value = ''
  explanation.value = null
  try {
    explanation.value = await apiRequest<TodayExplanation>(
      `/api/today/explanations/${metric}?page=1&pageSize=20`,
    )
  } catch (reason) {
    explanationMessage.value = reason instanceof Error ? reason.message : 'Không thể tải dữ liệu giải thích.'
  } finally {
    explanationLoading.value = false
  }
}

async function changeExplanationPage(page: number) {
  if (!explanation.value || page < 1 || page > explanation.value.totalPages) return
  const metric = explanation.value.metric
  explanationLoading.value = true
  explanationMessage.value = ''
  try {
    explanation.value = await apiRequest<TodayExplanation>(
      `/api/today/explanations/${metric}?page=${page}&pageSize=20`,
    )
  } catch (reason) {
    explanationMessage.value = reason instanceof Error ? reason.message : 'Không thể tải dữ liệu giải thích.'
  } finally {
    explanationLoading.value = false
  }
}

onMounted(() => {
  if (typeof IntersectionObserver !== 'undefined') {
    visibilityObserver = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue
        const item = observedItems.get(entry.target)
        if (item) emitSignalShown(item)
        visibilityObserver?.unobserve(entry.target)
      }
    })
  }
  void loadSummary()
  void loadAttention()
})

onBeforeUnmount(() => visibilityObserver?.disconnect())
</script>

<template>
  <section>
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-sm font-bold uppercase tracking-widest text-emerald-700">Today</p>
        <h1 class="mt-1 text-3xl font-black">Hôm nay cửa hàng thế nào?</h1>
        <p v-if="summary" class="mt-2 text-slate-600">
          Ngày kinh doanh <strong>{{ localBusinessDate(summary.businessDate) }}</strong>
          · Múi giờ <strong>{{ summary.timeZoneId }}</strong>
        </p>
      </div>
      <RouterLink class="btn-secondary" to="/reports/end-of-day">Báo cáo cuối ngày</RouterLink>
    </div>

    <p v-if="loading" class="mt-6 text-slate-500">Đang tải thông tin hôm nay…</p>
    <p v-if="message" class="error mt-6" role="alert">{{ message }}</p>

    <template v-if="summary">
      <div class="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <section v-for="card in cards" :key="card.metric" class="card">
          <h2 class="font-bold text-slate-500">{{ metricLabels[card.metric] }}</h2>
          <p class="mt-2 text-3xl font-black">{{ card.value }}</p>
          <p v-if="card.metric === 'estimated-gross-profit'" class="mt-3 text-sm text-slate-600">
            Giá vốn lịch sử {{ money(summary.estimatedGrossProfit.historicalCogs) }}
            · Độ tin cậy: <strong>{{ summary.estimatedGrossProfit.costReliability }}</strong>
          </p>
          <p
            v-if="card.metric === 'estimated-gross-profit' && summary.estimatedGrossProfit.costReliability === 'Unavailable'"
            class="mt-2 text-sm font-semibold text-amber-800"
          >
            Dữ liệu giá vốn chưa đủ tin cậy; đây không phải lợi nhuận kế toán.
          </p>
          <button class="mt-4 text-sm font-bold text-emerald-700" type="button" @click="showExplanation(card.metric)">
            Vì sao?
          </button>
        </section>
      </div>
    </template>

    <section class="card mt-6" aria-labelledby="attention-heading">
      <div class="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="attention-heading" class="text-2xl font-black">Cần chú ý</h2>
          <p v-if="attention" class="mt-1 text-sm text-slate-500">{{ attention.totalAttentionCount }} mặt hàng</p>
        </div>
        <RouterLink
          v-if="attention && attention.totalAttentionCount > 3"
          class="text-sm font-bold text-emerald-700"
          :to="{ name: 'attention-list' }"
        >
          Xem tất cả {{ attention.totalAttentionCount }} mặt hàng
        </RouterLink>
      </div>
      <p v-if="attentionLoading" class="mt-4 text-slate-500">Đang tải tín hiệu…</p>
      <p v-if="attentionMessage" class="error mt-4" role="alert">{{ attentionMessage }}</p>
      <template v-if="attention && !attentionLoading">
        <p v-if="attention.items.length === 0" class="mt-4 text-slate-600">
          {{ attention.evaluationCoverage === 'PartialObservation'
            ? 'Chưa đủ 7 ngày lịch sử để đưa ra kết luận mạnh về nguy cơ sắp hết hàng.'
            : 'Hiện chưa có mặt hàng nào thỏa điều kiện tín hiệu C14.' }}
        </p>
        <ul v-else class="mt-4 grid gap-3 lg:grid-cols-3">
          <li
            v-for="item in attention.items"
            :key="`${item.productId}-${item.attentionKind}`"
            :ref="element => observeAttention(element, item)"
            class="rounded-xl border border-stone-200 p-4"
            data-testid="attention-preview-item"
          >
            <strong>{{ item.productName }}</strong>
            <p :class="item.attentionKind === 'LowStockRisk' ? 'text-amber-700' : 'text-red-700'">
              {{ attentionLabels[item.attentionKind] }}
            </p>
            <p class="mt-2 text-sm text-slate-600">Tồn hiện tại: {{ item.currentStock }}</p>
            <p v-if="item.averageDailySales !== null" class="text-sm text-slate-600">Bán TB/ngày: {{ item.averageDailySales.toFixed(2) }}</p>
            <p v-if="item.daysOfCover !== null" class="text-sm text-slate-600">Chỉ số ngày tồn: {{ item.daysOfCover.toFixed(2) }}</p>
            <RouterLink
              class="mt-3 inline-block text-sm font-bold text-emerald-700"
              :to="{ name: 'attention-detail', params: { productId: item.productId } }"
              @click="openAttention(item)"
            >Xem vì sao</RouterLink>
          </li>
        </ul>
      </template>
    </section>

    <section v-if="explanationLoading || explanationMessage || explanation" class="card mt-6" aria-live="polite">
      <p v-if="explanationLoading" class="text-slate-500">Đang tải dữ liệu nguồn…</p>
      <p v-if="explanationMessage" class="error" role="alert">{{ explanationMessage }}</p>
      <template v-if="explanation">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-sm font-bold uppercase tracking-widest text-emerald-700">Vì sao?</p>
            <h2 class="mt-1 text-2xl font-black">{{ metricLabels[explanation.metric] }}</h2>
            <p class="mt-1 text-sm text-slate-500">Dữ liệu nguồn · {{ explanation.totalCount }} mục</p>
          </div>
          <button class="btn-secondary" type="button" @click="explanation = null">Đóng</button>
        </div>
        <p v-if="explanation.items.length === 0" class="mt-5 text-slate-500">Chưa có dữ liệu nguồn trong ngày này.</p>
        <ul v-else class="mt-5 grid gap-3">
          <li v-for="item in explanation.items" :key="`${item.sourceType}-${item.sourceId}`" class="rounded-xl border border-stone-200 p-4">
            <div class="flex flex-wrap justify-between gap-2">
              <div><strong>{{ item.title }}</strong><p class="text-xs text-slate-500">{{ item.sourceType }} · {{ item.sourceId }}</p></div>
              <strong v-if="item.contributionAmount !== null">{{ money(item.contributionAmount) }}</strong>
              <strong v-else-if="item.contributionCount !== null">{{ item.contributionCount }}</strong>
            </div>
            <div v-if="item.debtContribution" class="mt-3 grid gap-1 text-sm text-slate-600 sm:grid-cols-2">
              <span>Tổng giao dịch: {{ money(item.debtContribution.originalTotal) }}</span>
              <span>Thanh toán trực tiếp: {{ money(item.debtContribution.directPayments) }}</span>
              <span>Nghĩa vụ cơ sở: {{ money(item.debtContribution.baseDebt) }}</span>
              <span>Return cùng ngày: {{ money(item.debtContribution.sameDayReturnObligationReduction) }}</span>
              <span>Void cùng ngày: {{ item.debtContribution.sameDayVoided ? 'Có' : 'Không' }}</span>
              <span>Đóng góp cuối: {{ money(item.debtContribution.finalContribution) }}</span>
            </div>
            <RouterLink
              v-if="item.navigation"
              class="mt-3 inline-block text-sm font-bold text-emerald-700"
              :to="sourceRoute(item.navigation)"
            >
              Xem giao dịch nguồn
            </RouterLink>
          </li>
        </ul>
        <div v-if="explanation.totalPages > 1" class="mt-5 flex items-center justify-between gap-3">
          <button class="btn-secondary" type="button" :disabled="explanation.page <= 1 || explanationLoading" @click="changeExplanationPage(explanation.page - 1)">Trang trước</button>
          <span class="text-sm text-slate-500">Trang {{ explanation.page }}/{{ explanation.totalPages }}</span>
          <button class="btn-secondary" type="button" :disabled="explanation.page >= explanation.totalPages || explanationLoading" @click="changeExplanationPage(explanation.page + 1)">Trang sau</button>
        </div>
      </template>
    </section>
  </section>
</template>
