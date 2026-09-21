import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import PurchaseDraftForm from './PurchaseDraftForm.vue'
import type { ProductListItem, Purchase, Supplier } from '../api/types'

const supplier: Supplier = {
  id: 'supplier-1', name: 'NCC A', phone: null, note: null, isActive: true,
  outstandingAmount: 0, createdAt: '', updatedAt: '',
}
const products: ProductListItem[] = [
  { id: 'product-1', sku: 'A', barcode: null, name: 'Hàng A', unit: 'cái', salePrice: 10, isActive: true, quantityOnHand: 0 },
  { id: 'product-2', sku: 'B', barcode: null, name: 'Hàng B', unit: 'chai', salePrice: 20, isActive: true, quantityOnHand: 0 },
]
const purchase: Purchase = {
  id: 'purchase-1', supplierId: supplier.id, supplierName: supplier.name, status: 'Draft',
  lines: [{ id: 'line-1', productId: 'product-1', productName: 'Hàng A', productUnit: 'cái', quantity: 2, unitPrice: 12.5, lineAmount: 25 }],
  payments: [], totalAmount: 25, paidAmount: 0, outstandingAmount: 25,
  createdAt: '', updatedAt: '', completedAt: null, wasAlreadyCompleted: false,
}

describe('PurchaseDraftForm', () => {
  it('prevents a duplicate product and previews the authoritative rounding rule', async () => {
    const wrapper = mount(PurchaseDraftForm, { props: { suppliers: [supplier], products, initial: purchase } })

    await wrapper.get('select[aria-label="Sản phẩm"]').setValue('product-1')
    await wrapper.get('button[type="button"]').trigger('click')
    expect(wrapper.text()).toContain('Sản phẩm đã có trong phiếu nhập.')

    const inputs = wrapper.findAll('input[aria-label="Số lượng"]')
    await inputs[0].setValue('1.005')
    await wrapper.get('input[aria-label="Giá nhập"]').setValue('1')
    expect(wrapper.text()).toContain('Tổng dự kiến: 1,01 ₫')
  })

  it('disables editing after completion', () => {
    const wrapper = mount(PurchaseDraftForm, {
      props: { suppliers: [supplier], products, initial: { ...purchase, status: 'Completed' }, disabled: true },
    })

    expect(wrapper.get('fieldset').attributes('disabled')).toBeDefined()
  })
})
