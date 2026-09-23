import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest } from '../api/client'
import { useAuthStore } from '../stores/auth'
import SaleDetailView from './SaleDetailView.vue'
import type { ReturnContext, Sale } from '../api/types'

vi.mock('../api/client', async importOriginal => {
  const actual = await importOriginal<typeof import('../api/client')>()
  return { ...actual, apiRequest: vi.fn() }
})
vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'sale-1' } }),
  RouterLink: { props: ['to'], template: '<a><slot /></a>' },
}))

const sale: Sale = {
  id: 'sale-1', status: 'Completed', storeName: 'Store', warehouseId: 'warehouse-1', customer: null,
  cashierDisplayName: 'cashier@test',
  lines: [{ id: 'line-1', productId: 'product-1', productName: 'Coffee', productSku: 'CF', productUnit: 'pack', quantity: 2, unitSalePrice: 12000, lineAmount: 24000, unitCostAtSale: 8000, costReliability: 'Reliable' }],
  payments: [{ id: 'payment-1', amount: 24000, method: 'Cash', occurredAt: '2026-09-21T12:00:00Z' }],
  totalAmount: 24000, paidAmount: 24000, outstandingAmount: 0,
  createdAt: '2026-09-21T12:00:00Z', completedAt: '2026-09-21T12:00:00Z', wasAlreadyCompleted: false,
  originalTotalAmount: 24000, totalReturnedAmount: 0, netSaleAmount: 24000,
  originalCollectedAmount: 24000, totalRefundedAmount: 0, netCollectedAmount: 24000,
  isVoided: false, void: null, returns: [],
}
const returnContext: ReturnContext = {
  saleId: 'sale-1', isVoided: false, hasReturns: false,
  originalTotalAmount: 24000, totalReturnedAmount: 0, netSaleAmount: 24000,
  originalCollectedAmount: 24000, totalRefundedAmount: 0, netCollectedAmount: 24000, outstandingAmount: 0,
  lines: [{ saleLineId: 'line-1', productId: 'product-1', productName: 'Coffee', productSku: 'CF', productUnit: 'pack', soldQuantity: 2, previouslyReturnedQuantity: 0, returnableQuantity: 2, originalUnitSalePrice: 12000, originalLineAmount: 24000 }],
}

function mountView(role: 'Owner' | 'Cashier', saleValue: Sale = sale, contextValue: ReturnContext = returnContext) {
  const pinia = createPinia()
  setActivePinia(pinia)
  const auth = useAuthStore()
  auth.session = { isAuthenticated: true, email: 'user@test', storeId: 'store-1', roles: [role], hasStore: true }
  vi.mocked(apiRequest).mockImplementation(async path => {
    if (path === '/api/sales/sale-1') return saleValue as never
    if (path === '/api/sales/sale-1/return-context') return contextValue as never
    throw new Error(`Unexpected ${path}`)
  })
  return mount(SaleDetailView, { global: { plugins: [pinia] } })
}

describe('SaleDetailView', () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset()
    vi.spyOn(window, 'print').mockImplementation(() => {})
  })

  it('lets an Owner see eligible Return and Sale Void actions from authoritative context', async () => {
    const wrapper = mountView('Owner')
    await flushPromises()
    expect(wrapper.text()).toContain('Trả hàng')
    expect(wrapper.text()).toContain('Hủy giao dịch')
    expect(apiRequest).toHaveBeenCalledWith('/api/sales/sale-1/return-context')
    await wrapper.findAll('button').find(button => button.text() === 'Hủy giao dịch')!.trigger('click')
    expect(wrapper.text()).toContain('không có nghĩa hệ thống vừa hoàn tiền mặt')
    expect(wrapper.find('input[aria-label*="hoàn"]').exists()).toBe(false)
  })

  it('hides Return and Void mutation actions from a Cashier', async () => {
    const wrapper = mountView('Cashier')
    await flushPromises()
    expect(wrapper.text()).not.toContain('Trả hàng')
    expect(wrapper.text()).not.toContain('Hủy giao dịch')
    expect(apiRequest).not.toHaveBeenCalledWith('/api/sales/sale-1/return-context')
  })

  it('does not offer direct Void after a Return exists', async () => {
    const withReturn: Sale = { ...sale, totalReturnedAmount: 12000, netSaleAmount: 12000, totalRefundedAmount: 12000, netCollectedAmount: 12000, returns: [{ id: 'return-1', totalReturnAmount: 12000, refundAmount: 12000, completedByUserId: 'owner-1', completedAt: '2026-09-22T00:00:00Z' }] }
    const wrapper = mountView('Owner', withReturn, { ...returnContext, hasReturns: true, totalReturnedAmount: 12000, lines: [{ ...returnContext.lines[0], previouslyReturnedQuantity: 1, returnableQuantity: 1 }] })
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể hủy trực tiếp')
    expect(wrapper.findAll('button').some(button => button.text() === 'Hủy giao dịch')).toBe(false)
    expect(wrapper.text()).toContain('return-1')
  })

  it('guides customer debt conflicts to Return/refund by stable problem code', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'void-operation' })
    const wrapper = mountView('Owner')
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'Hủy giao dịch')!.trigger('click')
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('Sai đơn')
    vi.mocked(apiRequest).mockRejectedValueOnce(new ApiError(409, {
      code: 'customer-debt-would-become-negative',
      title: 'Server wording must not be parsed',
    }))
    await wrapper.get('[aria-label="Xác nhận hủy giao dịch"] button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Hãy dùng luồng Trả hàng/hoàn tiền phù hợp thay vì Hủy giao dịch')
    expect(wrapper.text()).not.toContain('Server wording must not be parsed')
  })

  it('shows void state while retaining original lines, payments and receipt history', async () => {
    const voided: Sale = { ...sale, isVoided: true, netSaleAmount: 0, netCollectedAmount: 0, void: { id: 'void-1', reason: 'Nhập nhầm đơn', voidedByUserId: 'owner-1', voidedAt: '2026-09-22T01:00:00Z' } }
    const wrapper = mountView('Owner', voided, { ...returnContext, isVoided: true, lines: [{ ...returnContext.lines[0], returnableQuantity: 0 }] })
    await flushPromises()
    expect(wrapper.text()).toContain('Đã hủy')
    expect(wrapper.text()).toContain('Nhập nhầm đơn')
    expect(wrapper.text()).toContain('Coffee')
    expect(wrapper.text()).toContain('Tiền mặt')
    expect(wrapper.text()).toContain('ĐÃ HỦY')
    expect(wrapper.text()).not.toContain('Trả hàng')
  })

  it('reprints an existing sale without calling CompleteSale', async () => {
    const wrapper = mountView('Cashier')
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'In hóa đơn')!.trigger('click')
    expect(apiRequest).toHaveBeenCalledTimes(1)
    expect(apiRequest).toHaveBeenCalledWith('/api/sales/sale-1')
    expect(window.print).toHaveBeenCalledOnce()
  })
})
