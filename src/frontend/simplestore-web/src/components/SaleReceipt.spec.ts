import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import SaleReceipt from './SaleReceipt.vue'
import type { Sale } from '../api/types'

const sale: Sale = {
  id: 'sale-1', status: 'Completed', storeName: 'Simple Store', warehouseId: 'warehouse-1',
  customer: null, cashierDisplayName: 'cashier@test', totalAmount: 12000, paidAmount: 12000,
  outstandingAmount: 0, createdAt: '2026-09-21T12:00:00Z', completedAt: '2026-09-21T12:00:00Z', wasAlreadyCompleted: false,
  lines: [{ id: 'line-1', productId: 'product-1', productName: 'Coffee', productSku: 'CF', productUnit: 'pack', quantity: 1, unitSalePrice: 12000, lineAmount: 12000, unitCostAtSale: 8000, costReliability: 'Reliable' }],
  payments: [{ id: 'payment-1', amount: 12000, method: 'Cash', occurredAt: '2026-09-21T12:00:00Z' }],
}

describe('SaleReceipt', () => {
  it('renders authoritative receipt without cost and calls the browser print boundary', async () => {
    const print = vi.spyOn(window, 'print').mockImplementation(() => {})
    const wrapper = mount(SaleReceipt, { props: { sale } })
    expect(wrapper.text()).toContain('Simple Store')
    expect(wrapper.text()).toContain('Coffee')
    expect(wrapper.text()).not.toContain('8.000')
    await wrapper.get('button').trigger('click')
    expect(print).toHaveBeenCalledOnce()
  })

  it('keeps the completed receipt visible when printing fails so it can be retried', async () => {
    vi.spyOn(window, 'print').mockImplementation(() => { throw new Error('printer unavailable') })
    const wrapper = mount(SaleReceipt, { props: { sale } })
    await wrapper.get('button').trigger('click')
    expect(wrapper.text()).toContain('Đơn bán đã hoàn tất')
    expect(wrapper.text()).toContain('In hóa đơn')
    expect(wrapper.text()).toContain('Coffee')
  })
})
