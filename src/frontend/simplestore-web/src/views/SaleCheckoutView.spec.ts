import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import SaleCheckoutForm from '../components/SaleCheckoutForm.vue'
import SaleCheckoutView from './SaleCheckoutView.vue'
import type { Sale } from '../api/types'

const apiRequest = vi.hoisted(() => vi.fn())
vi.mock('../api/client', async original => ({ ...await original<typeof import('../api/client')>(), apiRequest }))

const sale: Sale = {
  id: 'sale-0001', status: 'Completed', storeName: 'Tạp hóa Việt Anh', warehouseId: 'warehouse-1',
  customer: { id: 'customer-1', name: 'Nguyễn Thị Minh Anh', phone: '0909123456', createdAt: '', updatedAt: '' },
  cashierDisplayName: 'cashier@test',
  lines: [{ id: 'line-1', productId: 'product-1', productName: 'Coffee', productSku: 'CF', productUnit: 'gói', quantity: 2, unitSalePrice: 20500, lineAmount: 41000, unitCostAtSale: 12000, costReliability: 'Reliable' }],
  payments: [
    { id: 'payment-1', amount: 30000, method: 'Cash', occurredAt: '2026-10-02T03:00:00Z' },
    { id: 'payment-2', amount: 1000, method: 'Transfer', occurredAt: '2026-10-02T03:00:00Z' },
  ],
  totalAmount: 41000, paidAmount: 31000, outstandingAmount: 10000,
  createdAt: '2026-10-02T03:00:00Z', completedAt: '2026-10-02T03:00:00Z', wasAlreadyCompleted: false,
  originalTotalAmount: 41000, totalReturnedAmount: 0, netSaleAmount: 41000,
  originalCollectedAmount: 31000, totalRefundedAmount: 0, netCollectedAmount: 31000,
  isVoided: false, void: null, returns: [],
}

async function mountCompleted(completed: Sale = sale) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/sales/new', component: SaleCheckoutView },
      { path: '/sales', component: { template: '<p>Lịch sử bán hàng</p>' } },
    ],
  })
  await router.push('/sales/new')
  const wrapper = mount(SaleCheckoutView, { global: { plugins: [router] } })
  await flushPromises()
  wrapper.findComponent(SaleCheckoutForm).vm.$emit('completed', completed)
  await flushPromises()
  return wrapper
}

describe('SaleCheckoutView completion', () => {
  beforeEach(() => {
    apiRequest.mockImplementation(async (path: string) => {
      if (path === '/api/store/operational-settings') return { allowNegativeStock: false }
      if (path.startsWith('/api/products?')) return { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }
      throw new Error(`Unexpected request ${path}`)
    })
  })
  afterEach(() => vi.restoreAllMocks())

  it('orders the confirmed Sale facts before print and the next workflow actions', async () => {
    const wrapper = await mountCompleted()
    const summary = wrapper.get('.sales-complete__summary')

    expect(summary.get('h1').text()).toBe('Đơn bán đã hoàn tất')
    expect(summary.text()).toContain('Việc in hóa đơn không thay đổi trạng thái giao dịch.')
    const text = summary.text()
    const order = ['Giao dịch thành công', 'Mã đơn', 'Thời gian', 'Tổng đơn', 'Đã thu', 'Còn nợ', 'Khách hàng', 'In hóa đơn', 'Đơn bán mới', 'Lịch sử bán hàng']
      .map(label => text.indexOf(label))
    expect(order.every(index => index >= 0)).toBe(true)
    expect(order).toEqual([...order].sort((a, b) => a - b))
    expect(summary.text()).toContain('sale-0001')
    expect(summary.get('.sales-complete__total').text()).toContain('41.000 ₫')
    expect(summary.text()).toContain('Tiền mặt 30.000 ₫ · Chuyển khoản 1.000 ₫')
    const debt = summary.get('.sales-complete__row.is-debt')
    expect(debt.text()).toContain('10.000 ₫')
    expect(debt.text()).toContain('Ghi công nợ cho Nguyễn Thị Minh Anh')
    expect(summary.get('.sales-complete__customer').text()).toContain('0909123456')
    expect(summary.find('.sales-complete__recovered').exists()).toBe(false)
  })

  it('makes Đơn bán mới the primary action, print secondary and history a link', async () => {
    const wrapper = await mountCompleted()
    const prints = wrapper.findAll('button').filter(button => button.text() === 'In hóa đơn')

    expect(prints).toHaveLength(1)
    expect(prints[0]!.element.closest('.receipt')).toBeNull()
    expect(wrapper.get('.sales-complete__next button').text()).toBe('Đơn bán mới')
    expect(wrapper.get('.sales-complete__next button').classes()).toContain('sales-complete__new')
    const history = wrapper.get('.sales-complete__next a')
    expect(history.text()).toBe('Lịch sử bán hàng')
    expect(history.attributes('href')).toBe('/sales')
    // The receipt preview keeps the authoritative Sale and no transaction controls.
    const receipt = wrapper.get('.receipt')
    expect(receipt.text()).toContain('Coffee')
    expect(receipt.findAll('button')).toHaveLength(0)
  })

  it('keeps the completed Sale and allows reprint when the browser print fails', async () => {
    const print = vi.spyOn(window, 'print')
      .mockImplementationOnce(() => { throw new Error('printer unavailable') })
      .mockImplementation(() => {})
    const before = structuredClone(sale)
    const wrapper = await mountCompleted()
    const printButton = () => wrapper.findAll('button').find(button => button.text() === 'In hóa đơn')!

    await printButton().trigger('click')
    expect(wrapper.get('.sales-complete__print-error').attributes('role')).toBe('alert')
    expect(wrapper.get('.sales-complete__print-error').text()).toContain('Đơn bán đã hoàn tất')
    expect(wrapper.get('h1').text()).toBe('Đơn bán đã hoàn tất')

    await printButton().trigger('click')
    expect(print).toHaveBeenCalledTimes(2)
    expect(wrapper.find('.sales-complete__print-error').exists()).toBe(false)
    expect(sale).toEqual(before)
    expect(apiRequest.mock.calls.some(([path]) => String(path).includes('/api/sales'))).toBe(false)
  })

  it('notes a recovered Sale and starts an empty checkout with Đơn bán mới', async () => {
    const wrapper = await mountCompleted({ ...sale, wasAlreadyCompleted: true, customer: null, outstandingAmount: 0, paidAmount: 41000, payments: [] })

    expect(wrapper.get('.sales-complete__recovered').text()).toContain('không có đơn trùng')
    expect(wrapper.find('.sales-complete__customer').exists()).toBe(false)
    expect(wrapper.find('.sales-complete__row.is-debt').exists()).toBe(false)
    await wrapper.get('.sales-complete__new').trigger('click')
    await flushPromises()
    expect(wrapper.find('.sales-complete').exists()).toBe(false)
    expect(wrapper.findComponent(SaleCheckoutForm).exists()).toBe(true)
  })
})
