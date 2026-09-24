<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import type { RouteLocationRaw } from 'vue-router'
import { apiRequest } from '../api/client'
import { recordC14EventBestEffort } from '../api/c14Telemetry'
import type { C14AttentionDetail, C14AttentionKind, TodaySourceNavigation } from '../api/types'

const route = useRoute()
const router = useRouter()
const productId = String(route.params.productId)
const detail = ref<C14AttentionDetail | null>(null)
const loading = ref(true)
const message = ref('')

const kindLabel: Record<C14AttentionKind, string> = {
  NegativeStock: 'Tồn kho đang âm',
  OutOfStock: 'Đã hết hàng',
  LowStockRisk: 'Có nguy cơ sắp hết hàng',
}

function sourceRoute(navigation: TodaySourceNavigation): RouteLocationRaw {
  switch (navigation.type) {
    case 'Sale': return { name: 'sale-detail', params: { id: navigation.id } }
    case 'Return': return { name: 'return-detail', params: { id: navigation.id } }
    case 'Purchase': return { name: 'purchase-detail', params: { id: navigation.id } }
  }
}

async function load() {
  try {
    detail.value = await apiRequest<C14AttentionDetail>(`/api/today/attention/${productId}`)
  } catch (reason) {
    message.value = reason instanceof Error ? reason.message : 'Không thể tải chi tiết cần chú ý.'
  } finally {
    loading.value = false
  }
}

function startPurchase() {
  if (!detail.value) return
  void recordC14EventBestEffort(
    'PurchaseDraftStarted',
    detail.value.productId,
    detail.value.attentionKind,
  )
  void router.push({ name: 'purchase-create', query: { productId: detail.value.productId } })
}

onMounted(load)
</script>

<template>
  <section>
    <RouterLink class="text-sm font-semibold text-emerald-800" :to="{ name: 'attention-list' }">← Tất cả cần chú ý</RouterLink>
    <p v-if="loading" class="mt-6 text-slate-500">Đang tải…</p>
    <p v-if="message" class="error mt-6" role="alert">{{ message }}</p>

    <template v-if="detail">
      <div class="mt-3 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-3xl font-black">{{ detail.productName }}</h1>
          <p class="mt-1 text-slate-500">{{ detail.sku }} · {{ detail.unit }}</p>
        </div>
        <strong :class="detail.attentionKind === 'LowStockRisk' ? 'text-amber-700' : 'text-red-700'">
          {{ kindLabel[detail.attentionKind] }}
        </strong>
      </div>

      <section class="card mt-6 grid gap-2 sm:grid-cols-2">
        <p>Tồn hiện tại: <strong>{{ detail.currentStock }}</strong></p>
        <p>Lượng bán thuần: <strong>{{ detail.netSoldQuantity }}</strong></p>
        <p>Bán trung bình/ngày: <strong>{{ detail.averageDailySales?.toFixed(2) ?? 'Không áp dụng' }}</strong></p>
        <p>Chỉ số ngày tồn: <strong>{{ detail.daysOfCover?.toFixed(2) ?? 'Không áp dụng' }}</strong></p>
        <p>Độ phủ lịch sử: <strong>{{ detail.historyCoverage }}</strong></p>
        <p>Evidence bán gần đây: <strong>{{ detail.recentSalesEvidence }}</strong></p>
        <p>Đánh giá rủi ro: <strong>{{ detail.riskEvaluation }}</strong></p>
        <p>Múi giờ: <strong>{{ detail.timeZoneId }}</strong></p>
      </section>

      <section class="card mt-6">
        <h2 class="text-xl font-black">Công thức</h2>
        <p class="mt-2">
          {{ detail.formulaInputs.saleQuantity }} bán − {{ detail.formulaInputs.returnQuantity }} trả hàng
          − {{ detail.formulaInputs.saleVoidQuantity }} hủy = {{ detail.netSoldQuantity }} lượng bán thuần.
        </p>
        <p v-if="detail.averageDailySales !== null" class="mt-1">
          {{ detail.netSoldQuantity }} / {{ detail.formulaInputs.denominator }} ngày = {{ detail.averageDailySales.toFixed(2) }}/ngày.
        </p>
      </section>

      <section class="card mt-6">
        <h2 class="text-xl font-black">7 ngày kinh doanh đã hoàn tất</h2>
        <ul class="mt-3 grid gap-2 text-sm">
          <li v-for="day in detail.completedBusinessDays" :key="day.businessDate">
            <strong>{{ day.businessDate }}</strong> · {{ day.startUtc }} → {{ day.endUtc }}
          </li>
        </ul>
      </section>

      <section class="card mt-6">
        <h2 class="text-xl font-black">Dữ liệu nguồn</h2>
        <ul class="mt-3 grid gap-3">
          <li v-for="item in detail.evidence" :key="`${item.sourceType}-${item.sourceId}`" class="rounded-xl border border-stone-200 p-3">
            <strong>{{ item.sourceType }} · {{ item.quantityContribution > 0 ? '+' : '' }}{{ item.quantityContribution }}</strong>
            <p class="text-xs text-slate-500">{{ item.businessDate }} · {{ item.sourceId }}</p>
            <RouterLink class="mt-2 inline-block text-sm font-bold text-emerald-700" :to="sourceRoute(item.navigation)">
              Xem giao dịch nguồn
            </RouterLink>
          </li>
        </ul>
      </section>

      <div class="mt-6 flex flex-wrap gap-3">
        <RouterLink class="btn-secondary" :to="{ name: 'product-detail', params: { id: detail.productId } }">Xem sản phẩm</RouterLink>
        <button class="btn-primary" type="button" @click="startPurchase">Tạo phiếu nhập</button>
      </div>
    </template>
  </section>
</template>
