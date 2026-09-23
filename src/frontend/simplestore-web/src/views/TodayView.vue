<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiRequest } from '../api/client'
import type { TodayExplanation, TodayMetricId, TodaySummary } from '../api/types'

const summary = ref<TodaySummary | null>(null)
const explanation = ref<TodayExplanation | null>(null)
const loading = ref(true)
const explanationLoading = ref(false)
const message = ref('')
const explanationMessage = ref('')
const money = (value: number) => `${new Intl.NumberFormat('vi-VN').format(value)} ₫`

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

async function loadSummary() {
  loading.value = true
  message.value = ''
  try {
    summary.value = await apiRequest<TodaySummary>('/api/today')
  } catch (reason) {
    message.value = reason instanceof Error ? reason.message : 'Không thể tải thông tin hôm nay.'
  } finally {
    loading.value = false
  }
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

onMounted(loadSummary)
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
