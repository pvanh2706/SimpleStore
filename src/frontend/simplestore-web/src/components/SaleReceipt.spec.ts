import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import SaleReceipt from './SaleReceipt.vue'
import type { Sale } from '../api/types'

const sale: Sale = {
  id: 'sale-1', status: 'Completed', storeName: 'Simple Store', warehouseId: 'warehouse-1',
  customer: null, cashierDisplayName: 'cashier@test', totalAmount: 12000, paidAmount: 12000,
  outstandingAmount: 0, createdAt: '2026-09-21T12:00:00Z', completedAt: '2026-09-21T12:00:00Z', wasAlreadyCompleted: false,
  originalTotalAmount: 12000, totalReturnedAmount: 0, netSaleAmount: 12000,
  originalCollectedAmount: 12000, totalRefundedAmount: 0, netCollectedAmount: 12000,
  isVoided: false, void: null, returns: [],
  lines: [{ id: 'line-1', productId: 'product-1', productName: 'Coffee', productSku: 'CF', productUnit: 'pack', quantity: 1, unitSalePrice: 12000, lineAmount: 12000, unitCostAtSale: 8000, costReliability: 'Reliable' }],
  payments: [{ id: 'payment-1', amount: 12000, method: 'Cash', occurredAt: '2026-09-21T12:00:00Z' }],
}

describe('SaleReceipt', () => {
  afterEach(() => vi.restoreAllMocks())

  it('renders authoritative receipt without cost and calls the browser print boundary', async () => {
    const print = vi.spyOn(window, 'print').mockImplementation(() => {})
    const wrapper = mount(SaleReceipt, { props: { sale } })
    expect(wrapper.text()).toContain('Simple Store')
    expect(wrapper.text()).toContain('Coffee')
    expect(wrapper.text()).toContain('SKU: CF · ĐVT: pack')
    expect(wrapper.text()).toContain('1 pack × 12.000 ₫')
    expect(wrapper.text()).toContain('Tiền mặt')
    expect(wrapper.text()).toContain('Tổng cộng')
    expect(wrapper.text()).not.toContain('8.000')
    await wrapper.get('button').trigger('click')
    expect(print).toHaveBeenCalledOnce()
  })

  it('keeps the completed receipt visible when printing fails so it can be retried', async () => {
    const print = vi.spyOn(window, 'print').mockImplementationOnce(() => { throw new Error('printer unavailable') }).mockImplementation(() => {})
    const before = structuredClone(sale)
    const wrapper = mount(SaleReceipt, { props: { sale } })
    await wrapper.get('button').trigger('click')
    expect(wrapper.text()).toContain('Đơn bán đã hoàn tất')
    expect(wrapper.text()).toContain('In hóa đơn')
    expect(wrapper.text()).toContain('Coffee')
    await wrapper.get('button').trigger('click')
    expect(print).toHaveBeenCalledTimes(2)
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
    expect(sale).toEqual(before)
    expect(wrapper.emitted('completed')).toBeUndefined()
  })

  it('keeps a canceled browser print retryable without changing the completed Sale', async () => {
    const print = vi.spyOn(window, 'print').mockImplementation(() => {})
    const before = structuredClone(sale)
    const wrapper = mount(SaleReceipt, { props: { sale } })
    await wrapper.get('button').trigger('click')
    await wrapper.get('button').trigger('click')
    expect(print).toHaveBeenCalledTimes(2)
    expect(wrapper.get('button').text()).toBe('In hóa đơn')
    expect(sale).toEqual(before)
    expect(wrapper.emitted('completed')).toBeUndefined()
  })

  it('lets a host render the print action while printReceipt keeps the same print boundary', async () => {
    const print = vi.spyOn(window, 'print').mockImplementationOnce(() => { throw new Error('printer unavailable') })
    const wrapper = mount(SaleReceipt, { props: { sale, hidePrintAction: true } })
    expect(wrapper.findAll('button')).toHaveLength(0)
    expect(wrapper.text()).toContain('Coffee')

    const exposed = wrapper.vm as unknown as { printReceipt: () => void; printError: string }
    exposed.printReceipt()
    await wrapper.vm.$nextTick()
    expect(print).toHaveBeenCalledOnce()
    expect(exposed.printError).toContain('thử in lại')
    // The host shows the error next to its own button, so the receipt itself stays clean.
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
  })

  it('renders long Vietnamese names, decimal quantity, transfer and customer facts', () => {
    const detailed: Sale = {
      ...sale, totalAmount: 30000, paidAmount: 20000,
      customer: { id: 'customer-1', name: 'Nguyễn Thị Mai', phone: '0900000000', createdAt: sale.createdAt, updatedAt: sale.createdAt },
      lines: [{ ...sale.lines[0], productName: 'Tỏi khô bóc vỏ thơm ngon đặc sản miền Trung', productSku: 'TOI-KHO-DAI-001', productUnit: 'kg', quantity: 1.5, unitSalePrice: 20000, lineAmount: 30000 }],
      payments: [{ ...sale.payments[0], method: 'Transfer', amount: 20000 }],
    }
    const wrapper = mount(SaleReceipt, { props: { sale: detailed } })
    expect(wrapper.text()).toContain('Tỏi khô bóc vỏ thơm ngon đặc sản miền Trung')
    expect(wrapper.text()).toContain('SKU: TOI-KHO-DAI-001 · ĐVT: kg')
    expect(wrapper.text()).toContain('1.5 kg × 20.000 ₫')
    expect(wrapper.text()).toContain('30.000 ₫')
    expect(wrapper.text()).toContain('Chuyển khoản')
    expect(wrapper.text()).toContain('Nguyễn Thị Mai')
    expect(wrapper.text()).toContain('Còn nợ gốc')
    expect(wrapper.text()).toContain('10.000 ₫')
  })

  it('makes a void and later Return correction visible while retaining original facts', () => {
    const voided: Sale = { ...sale, isVoided: true, void: { id: 'void-1', reason: 'Nhập nhầm', voidedByUserId: 'owner-1', voidedAt: sale.completedAt } }
    const returned: Sale = { ...sale, totalReturnedAmount: 6000, totalRefundedAmount: 6000,
      returns: [{ id: 'return-1', totalReturnAmount: 6000, refundAmount: 6000, completedByUserId: 'owner-1', completedAt: sale.completedAt }] }
    const voidWrapper = mount(SaleReceipt, { props: { sale: voided } })
    expect(voidWrapper.text()).toContain('ĐÃ HỦY')
    expect(voidWrapper.text()).toContain('Coffee')
    expect(voidWrapper.text()).toContain('Tiền mặt')
    const returnWrapper = mount(SaleReceipt, { props: { sale: returned } })
    expect(returnWrapper.text()).toContain('Cập nhật sau giao dịch gốc')
    expect(returnWrapper.text()).toContain('Hàng trả (hiện tại)')
    expect(returnWrapper.text()).toContain('Đã hoàn tiền')
    expect(returnWrapper.text()).toContain('Coffee')
  })

  it('keeps original payment facts on the receipt after corrections change current outstanding', () => {
    const corrected = { ...sale, totalAmount: 12000, paidAmount: 4000, outstandingAmount: 0, totalReturnedAmount: 12000, netSaleAmount: 0, netCollectedAmount: 0 }
    const wrapper = mount(SaleReceipt, { props: { sale: corrected } })
    expect(wrapper.text()).toContain('Còn nợ gốc')
    expect(wrapper.text()).toContain('8.000 ₫')
  })
})
