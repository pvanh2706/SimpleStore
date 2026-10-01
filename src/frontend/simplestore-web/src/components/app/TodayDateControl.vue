<script setup lang="ts">
import { computed, ref } from 'vue'
import LineIcon from '../ui/LineIcon'
import { displayDate, referenceDate, shiftIsoDate, todayDemoDate, todayDemoEnabled, todayLiveDate } from '../../today/demo'

/** Live Today is current-date only (D-096); only the preview can move between days. */
const liveHint = 'Tổng quan chỉ hiển thị ngày kinh doanh hiện tại; xem ngày trước trong Báo cáo cuối ngày.'
const demo = todayDemoEnabled
const picker = ref<HTMLInputElement | null>(null)
const label = computed(() => demo.value
  ? `${todayDemoDate.value === referenceDate ? 'Hôm nay, ' : ''}${displayDate(todayDemoDate.value)}`
  : todayLiveDate.value ? `Hôm nay, ${displayDate(todayLiveDate.value)}` : 'Hôm nay')

function move(days: number) {
  todayDemoDate.value = shiftIsoDate(todayDemoDate.value, days)
}
function openPicker() {
  const input = picker.value
  if (!input) return
  if (typeof input.showPicker === 'function') input.showPicker()
  else input.click()
}
function pick(event: Event) {
  const value = (event.target as HTMLInputElement).value
  if (value) todayDemoDate.value = value
}
</script>

<template>
  <div class="today-date-control">
    <button class="today-date-square" type="button" aria-label="Ngày trước" :disabled="!demo" :title="demo ? undefined : liveHint" @click="move(-1)"><LineIcon name="chevronLeft" /></button>
    <button v-if="demo" class="today-date-button" type="button" @click="openPicker"><LineIcon name="calendar" /><span>{{ label }}</span></button>
    <span v-else class="today-date-button" :title="liveHint"><LineIcon name="calendar" /><span>{{ label }}</span></span>
    <input v-if="demo" ref="picker" class="today-date-input" type="date" :value="todayDemoDate" aria-label="Chọn ngày" tabindex="-1" @change="pick" />
    <button class="today-date-square" type="button" aria-label="Ngày sau" :disabled="!demo" :title="demo ? undefined : liveHint" @click="move(1)"><LineIcon name="chevronRight" /></button>
  </div>
</template>

<style scoped>
/* TemplateHTML/Today: .date-control, .square-btn, .date-button */
.today-date-control{position:relative;display:flex;align-items:center;gap:5px}
.today-date-square{display:grid;width:35px;height:35px;place-items:center;border:1px solid #dfe9ef;border-radius:8px;background:#fafdff;color:#354779;cursor:pointer}
.today-date-square .line-icon{width:16px;height:16px}
.today-date-square:disabled{opacity:.45;cursor:default}
.today-date-button{display:flex;height:35px;align-items:center;gap:8px;border:1px solid #edf1f5;border-radius:7px;background:#fff;padding:0 10px;color:#2b3c70;font-size:14px;font-weight:400;white-space:nowrap}
button.today-date-button{cursor:pointer}
.today-date-button .line-icon{width:16px;height:16px;color:#465f96}
.today-date-square:not(:disabled):hover,button.today-date-button:hover{border-color:#b5d9cd;background:#f2fbf7}
.today-date-input{position:absolute;top:34px;left:36px;width:1px;height:1px;opacity:0;pointer-events:none}
@media(max-width:430px){.today-date-control{gap:2px}.today-date-button{padding:0 6px}.today-date-square{width:29px}}
</style>
