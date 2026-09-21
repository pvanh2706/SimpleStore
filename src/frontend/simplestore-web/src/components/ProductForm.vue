<script setup lang="ts">
import { reactive, ref, watch } from 'vue'
import type { Product, ProductInput } from '../api/types'

const props = defineProps<{ product?: Product | null; submitting?: boolean }>()
const emit = defineEmits<{ submit: [value: ProductInput] }>()
const error = ref('')
const form = reactive({ sku: '', barcode: '', name: '', unit: '', salePrice: '', referencePurchaseCost: '', openingQuantity: '0', openingCost: '' })

watch(() => props.product, (product) => {
  if (!product) return
  Object.assign(form, {
    sku: product.sku, barcode: product.barcode ?? '', name: product.name, unit: product.unit,
    salePrice: String(product.salePrice), referencePurchaseCost: product.referencePurchaseCost == null ? '' : String(product.referencePurchaseCost),
  })
}, { immediate: true })

function submit() {
  error.value = ''
  const price = Number(form.salePrice)
  const quantity = Number(form.openingQuantity)
  const cost = form.openingCost === '' ? null : Number(form.openingCost)
  if (!form.name.trim() || !form.unit.trim()) error.value = 'Tên hàng và đơn vị là bắt buộc.'
  else if (form.salePrice === '' || !Number.isFinite(price) || price < 0) error.value = 'Giá bán phải là số không âm.'
  else if (!props.product && (!Number.isFinite(quantity) || quantity < 0)) error.value = 'Tồn đầu phải là số không âm.'
  else if (!props.product && quantity > 0 && (cost == null || !Number.isFinite(cost) || cost < 0)) error.value = 'Nhập giá vốn đầu khi tồn đầu lớn hơn 0.'
  if (error.value) return
  emit('submit', {
    sku: form.sku.trim() || null, barcode: form.barcode.trim() || null, name: form.name.trim(), unit: form.unit.trim(),
    salePrice: price, referencePurchaseCost: form.referencePurchaseCost === '' ? null : Number(form.referencePurchaseCost),
    ...(!props.product ? { openingQuantity: quantity, openingCost: cost } : {}),
  })
}
</script>

<template>
  <form class="card grid gap-5" @submit.prevent="submit">
    <div class="grid gap-5 md:grid-cols-2">
      <div class="field"><label for="name">Tên hàng *</label><input id="name" v-model.trim="form.name" class="input" maxlength="160" /></div>
      <div class="field"><label for="unit">Đơn vị *</label><input id="unit" v-model.trim="form.unit" class="input" maxlength="32" placeholder="cái, chai, kg…" /></div>
      <div class="field"><label for="sku">SKU</label><input id="sku" v-model.trim="form.sku" class="input" maxlength="64" placeholder="Để trống để tự tạo" /></div>
      <div class="field"><label for="barcode">Barcode</label><input id="barcode" v-model.trim="form.barcode" class="input" maxlength="64" /></div>
      <div class="field"><label for="sale-price">Giá bán *</label><input id="sale-price" v-model="form.salePrice" class="input" min="0" step="0.01" type="number" /></div>
      <div class="field"><label for="reference-cost">Giá vốn tham khảo</label><input id="reference-cost" v-model="form.referencePurchaseCost" class="input" min="0" step="0.01" type="number" /></div>
    </div>
    <fieldset v-if="!product" class="grid gap-5 rounded-xl border border-stone-200 p-4 md:grid-cols-2">
      <legend class="px-2 font-bold">Tồn đầu (không bắt buộc)</legend>
      <div class="field"><label for="opening-quantity">Số lượng tồn đầu</label><input id="opening-quantity" v-model="form.openingQuantity" class="input" min="0" step="0.001" type="number" /></div>
      <div class="field"><label for="opening-cost">Giá vốn đầu / đơn vị</label><input id="opening-cost" v-model="form.openingCost" class="input" min="0" step="0.01" type="number" /></div>
    </fieldset>
    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <div><button class="btn-primary" :disabled="submitting" type="submit">{{ submitting ? 'Đang lưu…' : 'Lưu sản phẩm' }}</button></div>
  </form>
</template>
