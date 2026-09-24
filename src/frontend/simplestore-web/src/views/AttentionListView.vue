<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { apiRequest } from '../api/client'
import { recordC14EventBestEffort } from '../api/c14Telemetry'
import type { C14AttentionItem, C14AttentionKind, C14AttentionList } from '../api/types'

const router = useRouter()
const result = ref<C14AttentionList | null>(null)
const loading = ref(true)
const message = ref('')

const kindLabel: Record<C14AttentionKind, string> = {
  NegativeStock: 'Tồn kho đang âm',
  OutOfStock: 'Đã hết hàng',
  LowStockRisk: 'Có nguy cơ sắp hết hàng',
}

async function load(page = 1) {
  loading.value = true
  message.value = ''
  try {
    result.value = await apiRequest<C14AttentionList>(`/api/today/attention?page=${page}&pageSize=20`)
  } catch (reason) {
    message.value = reason instanceof Error ? reason.message : 'Không thể tải danh sách cần chú ý.'
  } finally {
    loading.value = false
  }
}

function why(item: C14AttentionItem) {
  void recordC14EventBestEffort('WhyOpened', item.productId, item.attentionKind)
  void router.push({ name: 'attention-detail', params: { productId: item.productId } })
}

onMounted(() => load())
</script>

<template>
  <section>
    <RouterLink class="text-sm font-semibold text-emerald-800" :to="{ name: 'today' }">← Hôm nay</RouterLink>
    <h1 class="mt-3 text-3xl font-black">Cần chú ý</h1>
    <p v-if="result" class="mt-2 text-slate-600">
      {{ result.totalAttentionCount }} mặt hàng · {{ result.timeZoneId }}
    </p>
    <p v-if="loading" class="mt-6 text-slate-500">Đang tải…</p>
    <p v-if="message" class="error mt-6" role="alert">{{ message }}</p>

    <template v-if="result && !loading">
      <p v-if="result.items.length === 0" class="card mt-6 text-slate-600">
        {{ result.evaluationCoverage === 'PartialObservation'
          ? 'Chưa đủ 7 ngày lịch sử để đưa ra kết luận mạnh về nguy cơ sắp hết hàng.'
          : 'Hiện chưa có mặt hàng nào thỏa điều kiện tín hiệu C14.' }}
      </p>
      <ul v-else class="mt-6 grid gap-4">
        <li v-for="item in result.items" :key="`${item.productId}-${item.attentionKind}`" class="card">
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 class="text-xl font-black">{{ item.productName }}</h2>
              <p class="text-sm text-slate-500">{{ item.sku }} · {{ item.unit }}</p>
            </div>
            <strong :class="item.attentionKind === 'LowStockRisk' ? 'text-amber-700' : 'text-red-700'">
              {{ kindLabel[item.attentionKind] }}
            </strong>
          </div>
          <div class="mt-3 grid gap-1 text-sm text-slate-600 sm:grid-cols-3">
            <span>Tồn hiện tại: {{ item.currentStock }}</span>
            <span v-if="item.averageDailySales !== null">Bán TB/ngày: {{ item.averageDailySales.toFixed(2) }}</span>
            <span v-if="item.daysOfCover !== null">Chỉ số ngày tồn: {{ item.daysOfCover.toFixed(2) }}</span>
          </div>
          <button class="mt-4 text-sm font-bold text-emerald-700" type="button" @click="why(item)">Xem vì sao</button>
        </li>
      </ul>
      <div v-if="result.totalPages > 1" class="mt-5 flex items-center justify-between">
        <button class="btn-secondary" type="button" :disabled="result.page <= 1" @click="load(result.page - 1)">Trang trước</button>
        <span>Trang {{ result.page }}/{{ result.totalPages }}</span>
        <button class="btn-secondary" type="button" :disabled="result.page >= result.totalPages" @click="load(result.page + 1)">Trang sau</button>
      </div>
    </template>
  </section>
</template>
