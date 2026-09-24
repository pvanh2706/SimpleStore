import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import { recordC14EventBestEffort } from '../api/c14Telemetry'
import TodayView from './TodayView.vue'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
vi.mock('../api/c14Telemetry', () => ({ recordC14EventBestEffort: vi.fn().mockResolvedValue('event-id') }))
const request = vi.mocked(apiRequest)
const recordEvent = vi.mocked(recordC14EventBestEffort)

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

const emptyAttention = {
  businessDate: '2026-09-23', timeZoneId: 'Asia/Ho_Chi_Minh',
  velocityStartUtc: '2026-09-15T17:00:00Z', velocityEndUtc: '2026-09-22T17:00:00Z',
  completedBusinessDays: [], evaluationCoverage: 'FullSevenCompletedDays',
  totalAttentionCount: 0, page: 1, pageSize: 3, totalPages: 0, items: [],
}

function baseRequest(path: string) {
  if (path === '/api/today') return summary
  if (path.startsWith('/api/today/attention')) return emptyAttention
  throw new Error(`Unexpected path: ${path}`)
}

function mountView() {
  return mount(TodayView, {
    global: {
      stubs: { RouterLink: RouterLinkStub },
    },
  })
}

describe('TodayView', () => {
  beforeEach(() => {
    request.mockReset()
    recordEvent.mockClear()
  })

  it('renders the Owner summary without a date picker regression', async () => {
    request.mockImplementation(async path => baseRequest(path) as never)
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
    expect(wrapper.text()).toContain('Cần chú ý')
    expect(wrapper.text()).toContain('Hiện chưa có mặt hàng nào thỏa điều kiện tín hiệu C14.')
    expect(recordEvent).toHaveBeenCalledWith('TodayOpened')
  })

  it('opens typed debt evidence and displays transaction-local calculation inputs', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/today') return summary as never
      if (path.startsWith('/api/today/attention')) return emptyAttention as never
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
        if (path.startsWith('/api/today/attention')) return emptyAttention as never
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
      if (path.startsWith('/api/today/attention')) return emptyAttention as never
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
    request.mockImplementation(async path => {
      if (path === '/api/today') throw new Error('Không tải được Today')
      return emptyAttention as never
    })
    const failed = mountView()
    await flushPromises()
    expect(failed.get('[role="alert"]').text()).toContain('Không tải được Today')

    request.mockImplementation(async path => path === '/api/today' ? {
      ...summary,
      estimatedGrossProfit: { ...summary.estimatedGrossProfit, costReliability: 'Unavailable' },
    } as never : emptyAttention as never)
    const unavailable = mountView()
    await flushPromises()
    expect(unavailable.text()).toContain('đây không phải lợi nhuận kế toán')
  })

  it('renders the bounded C14 preview, factual/risk wording and total link', async () => {
    const items = [
      { productId: 'p1', productName: 'Âm kho', sku: 'A', unit: 'cái', attentionKind: 'NegativeStock', currentStock: -1, netSoldQuantity: 2, averageDailySales: 1, daysOfCover: null, historyCoverage: 'FullSevenCompletedDays', recentSalesEvidence: 'PositiveNetSold', riskEvaluation: 'Eligible' },
      { productId: 'p2', productName: 'Hết hàng', sku: 'B', unit: 'cái', attentionKind: 'OutOfStock', currentStock: 0, netSoldQuantity: 1, averageDailySales: null, daysOfCover: null, historyCoverage: 'PartialObservation', recentSalesEvidence: 'PositiveNetSold', riskEvaluation: 'InsufficientFullHistory' },
      { productId: 'p3', productName: 'Sắp hết', sku: 'C', unit: 'cái', attentionKind: 'LowStockRisk', currentStock: 2, netSoldQuantity: 7, averageDailySales: 1, daysOfCover: 2, historyCoverage: 'FullSevenCompletedDays', recentSalesEvidence: 'PositiveNetSold', riskEvaluation: 'Eligible' },
    ] as const
    request.mockImplementation(async path => path === '/api/today'
      ? summary as never
      : { ...emptyAttention, totalAttentionCount: 4, totalPages: 2, items } as never)

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.findAll('[data-testid="attention-preview-item"]')).toHaveLength(3)
    expect(wrapper.text()).toContain('Tồn kho đang âm')
    expect(wrapper.text()).toContain('Đã hết hàng')
    expect(wrapper.text()).toContain('Có nguy cơ sắp hết hàng')
    expect(wrapper.text()).not.toContain('Sẽ hết hàng sau')
    expect(wrapper.text()).toContain('Xem tất cả 4 mặt hàng')
    expect(recordEvent.mock.calls.filter(call => call[0] === 'SignalShown')).toHaveLength(3)

    wrapper.vm.$forceUpdate()
    await flushPromises()
    expect(recordEvent.mock.calls.filter(call => call[0] === 'SignalShown')).toHaveLength(3)

    await wrapper.findAllComponents(RouterLinkStub)
      .find(link => link.text() === 'Xem vì sao')!.trigger('click')
    expect(recordEvent).toHaveBeenCalledWith('WhyOpened', 'p1', 'NegativeStock')

    const remounted = mountView()
    await flushPromises()
    expect(recordEvent.mock.calls.filter(call => call[0] === 'TodayOpened')).toHaveLength(2)
    remounted.unmount()
  })

  it('uses neutral insufficient-history wording without a strong risk statement', async () => {
    request.mockImplementation(async path => path === '/api/today'
      ? summary as never
      : { ...emptyAttention, evaluationCoverage: 'PartialObservation' } as never)
    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Chưa đủ 7 ngày lịch sử')
    expect(wrapper.text()).not.toContain('Mọi thứ đều ổn')
    expect(wrapper.text()).not.toContain('Có nguy cơ sắp hết hàng')
  })
})
