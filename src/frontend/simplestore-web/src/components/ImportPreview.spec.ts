import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import ImportPreview from './ImportPreview.vue'

describe('ImportPreview', () => {
  it('renders row-level validation errors and preview rows', () => {
    const wrapper = mount(ImportPreview, { props: { result: {
      importId: null, isValid: false,
      rows: [{ rowNumber: 2, sku: 'SKU-1', barcode: null, name: 'Sản phẩm', unit: 'cái', salePrice: 1000, openingCost: null, openingQuantity: 0 }],
      errors: [{ rowNumber: 3, field: 'SalePrice', code: 'invalid-number', message: 'Giá bán không hợp lệ.' }],
    } } })

    expect(wrapper.text()).toContain('Dòng 3 · SalePrice')
    expect(wrapper.text()).toContain('SKU-1')
  })
})
