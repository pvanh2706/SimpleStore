import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import TodayView from './TodayView.vue'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
const request = vi.mocked(apiRequest)

const summary = {
  businessDate: '2026-09-23',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  startUtc: '2026-09-22T17:00:00Z',
  endUtc: '2026-09-23T17:00:00Z',
  salesRevenue: 1_000,
  netCollected: 800,
  estimatedGrossProfit: {
    netSalesRevenue: 1_000,
    historicalCogs: 400,
    amount: 600,
    costReliability: 'Reliable',
  },
  saleCount: 1,
  customerDebtCreated: 100,
  supplierDebtCreated: 200,
}

function mountView() {
  return mount(TodayView, {
    global: {
      stubs: { RouterLink: RouterLinkStub },
    },
  })
}

describe('TodayView', () => {
  beforeEach(() => request.mockReset())

  it('renders the Owner summary without a date picker or fake C14 section', async () => {
    request.mockResolvedValue(summary as never)
    const wrapper = mountView()
    expect(wrapper.text()).toContain('Đang tải thông tin hôm nay…')
    await flushPromises()

    expect(request).toHaveBeenCalledWith('/api/today')
    expect(wrapper.text()).toContain('Hôm nay cửa hàng thế nào?')
    expect(wrapper.text()).toContain('23/09/2026')
    expect(wrapper.text()).toContain('Asia/Ho_Chi_Minh')
    expect(wrapper.text()).toContain('Doanh thu hôm nay')
    expect(wrapper.text()).toContain('Công nợ nhà cung cấp mới phát sinh')
    expect(wrapper.text()).toContain('Giá vốn lịch sử 400 ₫')
    expect(wrapper.text()).toContain('Báo cáo cuối ngày')
    expect(wrapper.find('input[type="date"]').exists()).toBe(false)
    expect(wrapper.text()).not.toContain('Cần chú ý')
  })

  it('opens typed debt evidence and displays transaction-local calculation inputs', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/today') return summary as never
      return {
        metric: 'customer-debt-created',
        headline: 700,
        historicalCogs: null,
        costReliability: null,
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        items: [{
          sourceType: 'Sale',
          sourceId: 'sale-1',
          relatedSourceId: null,
          occurredAt: '2026-09-23T03:00:00Z',
          contributionAmount: 700,
          contributionCount: null,
          title: 'Nghĩa vụ nợ mới từ đơn bán',
          navigation: { type: 'Sale', id: 'sale-1' },
          debtContribution: {
            originalTotal: 1_000,
            directPayments: 0,
            baseDebt: 1_000,
            sameDayReturnObligationReduction: 300,
            sameDayVoided: false,
            finalContribution: 700,
          },
        }],
      } as never
    })
    const wrapper = mountView()
    await flushPromises()

    await wrapper.findAll('button').at(4)!.trigger('click')
    await flushPromises()

    expect(request).toHaveBeenCalledWith(
      '/api/today/explanations/customer-debt-created?page=1&pageSize=20',
    )
    expect(wrapper.text()).toContain('Dữ liệu nguồn · 1 mục')
    expect(wrapper.text()).toContain('Nghĩa vụ cơ sở: 1.000 ₫')
    expect(wrapper.text()).toContain('Return cùng ngày: 300 ₫')
    expect(wrapper.text()).toContain('Đóng góp cuối: 700 ₫')
    const sourceLink = wrapper.findAllComponents(RouterLinkStub)
      .find(link => link.text() === 'Xem giao dịch nguồn')
    expect(sourceLink?.props('to')).toEqual({ name: 'sale-detail', params: { id: 'sale-1' } })
  })

  it.each([
    ['Sale', 'sale-detail', 'sale-1'],
    ['Return', 'return-detail', 'return-1'],
    ['Purchase', 'purchase-detail', 'purchase-1'],
  ] as const)(
    'maps typed %s navigation to the known named route',
    async (type, routeName, id) => {
      request.mockImplementation(async path => {
        if (path === '/api/today') return summary as never
        return {
          metric: 'revenue', headline: 10, historicalCogs: null, costReliability: null,
          page: 1, pageSize: 20, totalCount: 1, totalPages: 1,
          items: [{
            sourceType: 'Sale', sourceId: 'source-1', relatedSourceId: null,
            occurredAt: '2026-09-23T03:00:00Z', contributionAmount: 10,
            contributionCount: null,
            title: type === 'Sale' ? 'Hoàn nhập giá vốn lịch sử' : 'Đơn bán hoàn tất',
            debtContribution: null,
            navigation: { type, id },
          }],
        } as never
      })
      const wrapper = mountView()
      await flushPromises()
      await wrapper.findAll('button').at(0)!.trigger('click')
      await flushPromises()

      const sourceLink = wrapper.findAllComponents(RouterLinkStub)
        .find(link => link.text() === 'Xem giao dịch nguồn')
      expect(sourceLink?.props('to')).toEqual({ name: routeName, params: { id } })
    },
  )

  it('does not render a broken drill-down for evidence with null navigation', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/today') return summary as never
      return {
        metric: 'collected', headline: 10, historicalCogs: null, costReliability: null,
        page: 1, pageSize: 20, totalCount: 1, totalPages: 1,
        items: [{
          sourceType: 'CustomerDebtPayment', sourceId: 'payment-1', relatedSourceId: 'customer-1',
          occurredAt: '2026-09-23T03:00:00Z', contributionAmount: 10,
          contributionCount: null, title: 'Thu nợ khách hàng', debtContribution: null,
          navigation: null,
        }],
      } as never
    })
    const wrapper = mountView()
    await flushPromises()
    await wrapper.findAll('button').at(1)!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Thu nợ khách hàng')
    expect(wrapper.text()).not.toContain('Xem giao dịch nguồn')
  })

  it('shows summary errors and the unavailable-cost warning', async () => {
    request.mockRejectedValueOnce(new Error('Không tải được Today'))
    const failed = mountView()
    await flushPromises()
    expect(failed.get('[role="alert"]').text()).toContain('Không tải được Today')

    request.mockResolvedValueOnce({
      ...summary,
      estimatedGrossProfit: { ...summary.estimatedGrossProfit, costReliability: 'Unavailable' },
    } as never)
    const unavailable = mountView()
    await flushPromises()
    expect(unavailable.text()).toContain('đây không phải lợi nhuận kế toán')
  })
})
