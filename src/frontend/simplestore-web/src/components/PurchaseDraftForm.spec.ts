import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import PurchaseDraftForm from './PurchaseDraftForm.vue'
import type { ProductListItem, ProductPage, Purchase, Supplier, SupplierPage } from '../api/types'

const supplier: Supplier = {
  id: 'supplier-101', name: 'NCC ngoài 100', phone: null, note: null, isActive: true,
  outstandingAmount: 0, createdAt: '', updatedAt: '',
}
const products: ProductListItem[] = [
  { id: 'product-101', sku: 'SKU-101', barcode: '8930000101', name: 'Hàng ngoài 100', unit: 'cái', salePrice: 10, isActive: true, quantityOnHand: 0 },
]
const purchase: Purchase = {
  id: 'purchase-1', supplierId: supplier.id, supplierName: supplier.name, status: 'Draft',
  lines: [{ id: 'line-1', productId: products[0].id, productName: products[0].name, productUnit: 'cái', quantity: 2, unitPrice: 12.5, lineAmount: 25 }],
  payments: [], totalAmount: 25, paidAmount: 0, outstandingAmount: 25,
  createdAt: '', updatedAt: '', completedAt: null, wasAlreadyCompleted: false,
  isVoided: false, void: null,
}

const supplierPage = (items: Supplier[], page = 1, totalPages = 1): SupplierPage => ({
  items, page, pageSize: 20, totalCount: items.length, totalPages,
})
const productPage = (items: ProductListItem[], page = 1, totalPages = 1): ProductPage => ({
  items, page, pageSize: 20, totalCount: items.length, totalPages,
})

function mountForm(initial: Purchase | null = purchase) {
  return mount(PurchaseDraftForm, {
    props: {
      initial,
      searchSuppliers: vi.fn().mockResolvedValue(supplierPage([supplier])),
      searchProducts: vi.fn().mockResolvedValue(productPage(products)),
    },
  })
}

describe('PurchaseDraftForm', () => {
  it('searches and selects records outside the first 100 without loading all data', async () => {
    const searchSuppliers = vi.fn()
      .mockResolvedValueOnce(supplierPage([], 1, 8))
      .mockResolvedValueOnce(supplierPage([supplier], 1, 1))
    const searchProducts = vi.fn()
      .mockResolvedValueOnce(productPage([], 1, 8))
      .mockResolvedValueOnce(productPage(products, 1, 1))
    const wrapper = mount(PurchaseDraftForm, { props: { searchSuppliers, searchProducts } })
    await flushPromises()

    await wrapper.get('input[aria-label="Tìm nhà cung cấp"]').setValue('NCC ngoài 100')
    await wrapper.findAll('button').find((button) => button.text() === 'Tìm nhà cung cấp')!.trigger('click')
    await wrapper.get('input[aria-label="Tìm sản phẩm"]').setValue('8930000101')
    await wrapper.findAll('button').find((button) => button.text() === 'Tìm sản phẩm')!.trigger('click')
    await flushPromises()

    expect(searchSuppliers).toHaveBeenLastCalledWith('NCC ngoài 100', 1)
    expect(searchProducts).toHaveBeenLastCalledWith('8930000101', 1)
    await wrapper.get('button[aria-label="Chọn nhà cung cấp NCC ngoài 100"]').trigger('click')
    await wrapper.get('button[aria-label="Thêm sản phẩm Hàng ngoài 100"]').trigger('click')
    await wrapper.get('button[type="submit"]').trigger('submit')

    expect(wrapper.emitted('save')?.[0]?.[0]).toMatchObject({
      supplierId: 'supplier-101',
      lines: [{ productId: 'product-101' }],
    })
  })

  it('keeps referenced supplier and product visible when edit search pages do not contain them', async () => {
    const wrapper = mount(PurchaseDraftForm, {
      props: {
        initial: purchase,
        searchSuppliers: vi.fn().mockResolvedValue(supplierPage([])),
        searchProducts: vi.fn().mockResolvedValue(productPage([])),
      },
    })
    await flushPromises()

    expect(wrapper.get('[data-testid="selected-supplier"]').text()).toContain('NCC ngoài 100')
    expect(wrapper.text()).toContain('Hàng ngoài 100')
  })

  it('prevents a duplicate product and previews the authoritative rounding rule', async () => {
    const wrapper = mountForm()
    await flushPromises()
    await wrapper.get('button[aria-label="Thêm sản phẩm Hàng ngoài 100"]').trigger('click')
    expect(wrapper.text()).toContain('Sản phẩm đã có trong phiếu nhập.')

    await wrapper.get('input[aria-label="Số lượng"]').setValue('1.005')
    await wrapper.get('input[aria-label="Giá nhập"]').setValue('1')
    expect(wrapper.text()).toContain('Tổng dự kiến: 1,01 ₫')
  })

  it('disables editing after completion', () => {
    const wrapper = mount(PurchaseDraftForm, {
      props: {
        initial: { ...purchase, status: 'Completed' },
        disabled: true,
        searchSuppliers: vi.fn().mockResolvedValue(supplierPage([])),
        searchProducts: vi.fn().mockResolvedValue(productPage([])),
      },
    })
    expect(wrapper.get('fieldset').attributes('disabled')).toBeDefined()
  })
})
