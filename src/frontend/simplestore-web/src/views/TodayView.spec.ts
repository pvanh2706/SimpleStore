import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { routerKey } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import { recordC14EventBestEffort } from '../api/c14Telemetry'
import { todayDemoDate, todayDemoEnabled, todayLiveDate } from '../today/demo'
import TodayView from './TodayView.vue'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
vi.mock('../api/c14Telemetry', () => ({ recordC14EventBestEffort: vi.fn().mockResolvedValue('event-id') }))
const request = vi.mocked(apiRequest)
const recordEvent = vi.mocked(recordC14EventBestEffort)
const push = vi.fn()
const mounted: Array<{ unmount: () => void }> = []

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
  const wrapper = mount(TodayView, {
    attachTo: document.body,
    global: {
      stubs: { RouterLink: RouterLinkStub },
      provide: { [routerKey as symbol]: { push } },
    },
  })
  mounted.push(wrapper)
  return wrapper
}
type Wrapper = ReturnType<typeof mountView>
const kpi = (wrapper: Wrapper, label: string) => wrapper.findAll('.today-kpi').find(card => card.get('.today-kpi-label').text() === label)!
const drawer = (wrapper: Wrapper) => wrapper.get('aside.today-drawer')
const sourceLink = (wrapper: Wrapper) => wrapper.findAllComponents(RouterLinkStub).find(link => link.text() === 'Xem giao dịch nguồn')

function explanationResponse(patch: Record<string, unknown>, item: Record<string, unknown>) {
  return {
    metric: 'revenue', headline: 10, historicalCogs: null, costReliability: null,
    page: 1, pageSize: 20, totalCount: 1, totalPages: 1, ...patch,
    items: [{
      sourceType: 'Sale', sourceId: 'source-1', relatedSourceId: null, occurredAt: '2026-09-23T03:00:00Z',
      contributionAmount: 10, contributionCount: null, title: 'Đơn bán hoàn tất', debtContribution: null, navigation: null, ...item,
    }],
  }
}

beforeEach(() => {
  request.mockReset()
  recordEvent.mockClear()
  push.mockClear()
})
afterEach(() => {
  todayDemoEnabled.value = false
  todayDemoDate.value = '2024-12-16'
  mounted.forEach(wrapper => wrapper.unmount())
  mounted.length = 0
})

describe('TodayView with live data', () => {
  it('renders the Owner summary in the reference layout without preview-only content', async () => {
    request.mockImplementation(async path => baseRequest(path) as never)
    const wrapper = mountView()
    expect(wrapper.text()).toContain('Đang tải thông tin hôm nay…')
    await flushPromises()

    expect(request).toHaveBeenCalledWith('/api/today')
    expect(wrapper.get('h1').text()).toBe('Tổng quan hôm nay')
    expect(wrapper.get('.today-subtitle').text()).toContain('Thứ 4, 23/09/2026')
    expect(wrapper.get('.today-subtitle-info').attributes('title')).toBe('Múi giờ cửa hàng: Asia/Ho_Chi_Minh')
    expect(todayLiveDate.value).toBe('2026-09-23')
    expect(wrapper.findAll('.today-kpi').map(card => [card.get('.today-kpi-label').text(), card.get('.today-kpi-value').text()])).toEqual([
      ['Doanh thu', '1.000 ₫'], ['Số hóa đơn', '1'], ['Tiền thu thuần', '800 ₫'], ['Lãi gộp ước tính', '600 ₫'],
    ])
    expect(kpi(wrapper, 'Lãi gộp ước tính').text()).toContain('Độ tin cậy giá vốn: Tin cậy')
    expect(wrapper.text()).toContain('Công nợ nhà cung cấp mới phát sinh200 ₫')
    expect(wrapper.find('input[type="date"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('Cần chú ý')
    expect(wrapper.text()).toContain('Hiện chưa có mặt hàng nào thỏa điều kiện tín hiệu C14.')
    for (const previewOnly of ['so với hôm qua', 'Ca làm việc', 'Đang mở cửa', 'Doanh thu theo giờ', 'Sản phẩm bán chạy', 'Số sản phẩm bán']) {
      expect(wrapper.text()).not.toContain(previewOnly)
    }
    expect(wrapper.findComponent(RouterLinkStub).props('to')).toBe('/sales/new')
    expect(recordEvent).toHaveBeenCalledWith('TodayOpened')
  })

  it('opens typed debt evidence and displays transaction-local calculation inputs', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/today' || path.startsWith('/api/today/attention')) return baseRequest(path) as never
      return explanationResponse({ metric: 'customer-debt-created', headline: 700 }, {
        sourceId: 'sale-1', contributionAmount: 700, title: 'Nghĩa vụ nợ mới từ đơn bán',
        navigation: { type: 'Sale', id: 'sale-1' },
        debtContribution: {
          originalTotal: 1_000, directPayments: 0, baseDebt: 1_000,
          sameDayReturnObligationReduction: 300, sameDayVoided: false, finalContribution: 700,
        },
      }) as never
    })
    const wrapper = mountView()
    await flushPromises()

    await wrapper.findAll('.today-debt-button')[0]!.trigger('click')
    await flushPromises()

    expect(request).toHaveBeenCalledWith('/api/today/explanations/customer-debt-created?page=1&pageSize=20')
    expect(drawer(wrapper).attributes('aria-label')).toBe('Cách tính công nợ khách mới')
    expect(drawer(wrapper).text()).toContain('Không phải tổng dư nợ của khách hàng.')
    expect(drawer(wrapper).text()).toContain('Dữ liệu nguồn · 1 mục')
    expect(drawer(wrapper).text()).toContain('Nghĩa vụ cơ sở: 1.000 ₫')
    expect(drawer(wrapper).text()).toContain('Return cùng ngày: 300 ₫')
    expect(drawer(wrapper).text()).toContain('Đóng góp cuối: 700 ₫')
    expect(sourceLink(wrapper)?.props('to')).toEqual({ name: 'sale-detail', params: { id: 'sale-1' } })

    await wrapper.get('button[aria-label="Đóng"]').trigger('click')
    expect(wrapper.find('aside.today-drawer').exists()).toBe(false)
  })

  it.each([
    ['Sale', 'sale-detail', 'sale-1'],
    ['Return', 'return-detail', 'return-1'],
    ['Purchase', 'purchase-detail', 'purchase-1'],
  ] as const)(
    'maps typed %s navigation to the known named route',
    async (type, routeName, id) => {
      request.mockImplementation(async path => path === '/api/today' || path.startsWith('/api/today/attention')
        ? baseRequest(path) as never
        : explanationResponse({}, { navigation: { type, id } }) as never)
      const wrapper = mountView()
      await flushPromises()
      await kpi(wrapper, 'Doanh thu').trigger('click')
      await flushPromises()

      expect(drawer(wrapper).attributes('aria-label')).toBe('Cách tính doanh thu')
      expect(drawer(wrapper).text()).toContain('múi giờ Asia/Ho_Chi_Minh')
      expect(sourceLink(wrapper)?.props('to')).toEqual({ name: routeName, params: { id } })
    },
  )

  it('does not render a broken drill-down for evidence with null navigation', async () => {
    request.mockImplementation(async path => path === '/api/today' || path.startsWith('/api/today/attention')
      ? baseRequest(path) as never
      : explanationResponse({ metric: 'collected' }, { sourceType: 'CustomerDebtPayment', sourceId: 'payment-1', relatedSourceId: 'customer-1', title: 'Thu nợ khách hàng' }) as never)
    const wrapper = mountView()
    await flushPromises()
    await kpi(wrapper, 'Tiền thu thuần').trigger('click')
    await flushPromises()

    expect(request).toHaveBeenCalledWith('/api/today/explanations/collected?page=1&pageSize=20')
    expect(drawer(wrapper).text()).toContain('Thu nợ khách hàng')
    expect(drawer(wrapper).text()).not.toContain('Xem giao dịch nguồn')
  })

  it('shows summary errors and the unavailable-cost warning with historical cost in the explanation', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/today') throw new Error('Không tải được Today')
      return emptyAttention as never
    })
    const failed = mountView()
    await flushPromises()
    expect(failed.get('[role="alert"]').text()).toContain('Không tải được Today')

    request.mockImplementation(async path => {
      if (path === '/api/today') return { ...summary, estimatedGrossProfit: { ...summary.estimatedGrossProfit, costReliability: 'Unavailable' } } as never
      if (path.startsWith('/api/today/attention')) return emptyAttention as never
      return explanationResponse({ metric: 'estimated-gross-profit' }, {}) as never
    })
    const unavailable = mountView()
    await flushPromises()
    expect(unavailable.text()).toContain('đây không phải lợi nhuận kế toán')
    expect(kpi(unavailable, 'Lãi gộp ước tính').get('.today-kpi-trend').classes()).toContain('warn')
    await kpi(unavailable, 'Lãi gộp ước tính').trigger('click')
    await flushPromises()
    expect(drawer(unavailable).get('.today-detail-grid').text()).toBe('Doanh thu thuần1.000 ₫Giá vốn lịch sử400 ₫Độ tin cậy giá vốnKhông khả dụng')
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
    expect(wrapper.text()).toContain('Chỉ số ngày tồn: 2.00')
    expect(wrapper.text()).not.toContain('Sẽ hết hàng sau')
    expect(wrapper.text()).toContain('Xem tất cả 4 mặt hàng')
    expect(recordEvent.mock.calls.filter(call => call[0] === 'SignalShown')).toHaveLength(3)

    wrapper.vm.$forceUpdate()
    await flushPromises()
    expect(recordEvent.mock.calls.filter(call => call[0] === 'SignalShown')).toHaveLength(3)

    await wrapper.findAllComponents(RouterLinkStub)
      .find(link => link.text() === 'Xem vì sao')!.trigger('click')
    expect(recordEvent).toHaveBeenCalledWith('WhyOpened', 'p1', 'NegativeStock')

    mountView()
    await flushPromises()
    expect(recordEvent.mock.calls.filter(call => call[0] === 'TodayOpened')).toHaveLength(2)
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

  it('opens Bán hàng with F12', async () => {
    request.mockImplementation(async path => baseRequest(path) as never)
    mountView()
    await flushPromises()
    const event = new KeyboardEvent('keydown', { key: 'F12', cancelable: true })
    window.dispatchEvent(event)
    expect(event.defaultPrevented).toBe(true)
    expect(push).toHaveBeenCalledWith('/sales/new')
  })
})

describe('TodayView with reference data', () => {
  async function mountPreview() {
    todayDemoEnabled.value = true
    const wrapper = mountView()
    await flushPromises()
    return wrapper
  }

  it('reproduces the reference dashboard without calling the API', async () => {
    const wrapper = await mountPreview()

    expect(request).not.toHaveBeenCalled()
    expect(recordEvent).not.toHaveBeenCalled()
    expect(wrapper.get('.today-subtitle').text()).toBe('Thứ 2, 16/12/2024  (00:00 – 14:23)')
    expect(wrapper.get('.today-status').text()).toBe('Đang mở cửa')
    expect(wrapper.get('.today-shift select').element).toHaveProperty('value', 'morning')
    expect(wrapper.findAll('.today-kpi').map(card => [card.get('.today-kpi-value').text(), card.get('.today-kpi-trend').text()])).toEqual([
      ['2.850.000 ₫', '↑ 12% so với hôm qua'], ['28', '↑ 17% so với hôm qua'], ['156', '↑ 9% so với hôm qua'], ['820.000 ₫', '↑ 11% so với hôm qua'],
    ])
    expect(wrapper.findAll('.today-y-axis span').map(tick => tick.text())).toEqual(['800K', '600K', '400K', '200K', '0K'])
    expect(wrapper.findAll('.today-x-axis span').map(tick => tick.text())).toEqual(['6h', '7h', '8h', '9h', '10h', '11h', '12h', '13h', '14h'])
    expect(wrapper.findAll('.today-bar')[4]!.attributes('style')).toContain('--bar-height: 81.25%')
    expect(wrapper.findAll('.today-legend-item').map(item => item.text())).toEqual(
      ['Đồ uống35%', 'Bánh kẹo22%', 'Gia vị15%', 'Sữa và TP từ sữa12%', 'Đồ gia dụng8%', 'Khác8%'])
    expect(wrapper.findAll('.today-product-row').map(row => [row.get('.today-product-name').text(), row.get('.today-product-qty').text(), row.get('.today-product-total').text()])).toEqual([
      ['Coca Cola 330ml', '48', '480.000 ₫'], ['Mì Hảo Hảo 75g', '36', '144.000 ₫'], ['Nước suối Aquafina 500ml', '32', '224.000 ₫'],
      ['Sữa Vinamilk 180ml', '28', '224.000 ₫'], ['Bánh Oreo 133g', '24', '528.000 ₫'],
    ])
    expect(wrapper.findAll('.today-txn-row').map(row => row.text())).toEqual([
      '#HD00012314:233 sản phẩm120.000 ₫Tiền mặt', '#HD00012214:155 sản phẩm250.000 ₫Chuyển khoản', '#HD00012113:582 sản phẩm75.000 ₫Tiền mặt',
      '#HD00012013:426 sản phẩm320.000 ₫Công nợ', '#HD00011913:301 sản phẩm90.000 ₫Tiền mặt',
    ])
    expect(wrapper.findAll('.today-stock-row').map(row => row.text())).toEqual(
      ['Sắp hết hàng8 sản phẩm', 'Hết hàng3 sản phẩm', 'Tồn kho thấp12 sản phẩm', 'Tồn kho bình thường342 sản phẩm'])
    expect(wrapper.get('.today-debt-body').text()).toBe('Tổng công nợ1.250.000 ₫Số khách còn nợ5Quá hạn2 khách')
  })

  it('filters by shift and day, and reacts to chart, legend and opening-state controls', async () => {
    const wrapper = await mountPreview()

    await wrapper.get('.today-shift select').setValue('all')
    expect(wrapper.get('.today-subtitle').text()).toContain('(00:00 – 22:59)')
    expect(wrapper.get('.today-kpi-value').text()).toBe('5.060.000 ₫')
    expect(wrapper.findAll('.today-bar')).toHaveLength(17)
    expect(wrapper.findAll('.today-txn-row')[0]!.text()).toContain('#HD000140')
    await wrapper.get('.today-shift select').setValue('morning')

    todayDemoDate.value = '2024-12-15'
    await flushPromises()
    expect(wrapper.get('.today-subtitle').text()).toContain('Chủ nhật, 15/12/2024')
    expect(wrapper.get('.today-kpi-value').text()).not.toBe('2.850.000 ₫')
    todayDemoDate.value = '2024-12-16'
    await flushPromises()

    await wrapper.get('.today-chart-select').setValue('invoices')
    expect(wrapper.findAll('.today-y-axis span').map(tick => tick.text())).toEqual(['10', '8', '5', '3', '0'])
    await wrapper.findAll('.today-bar')[4]!.trigger('click')
    expect(wrapper.get('.today-bar.selected .today-bar-tip').text()).toBe('10:00 · 5')

    await wrapper.findAll('.today-legend-item')[0]!.trigger('click')
    expect(wrapper.get('.today-donut-center').text()).toBe('997.500 ₫Đồ uống')
    await wrapper.findAll('.today-legend-item')[0]!.trigger('click')
    expect(wrapper.get('.today-donut-center').text()).toBe('2.850.000 ₫Tổng doanh thu')

    await wrapper.get('.today-status').trigger('click')
    expect(wrapper.get('.today-status').text()).toBe('Đã đóng cửa')
    expect(wrapper.get('.today-toast').text()).toBe('Đã chuyển trạng thái cửa hàng sang đóng cửa.')
  })

  it('opens the reference drawers and drills from revenue into a transaction', async () => {
    const wrapper = await mountPreview()

    await kpi(wrapper, 'Doanh thu').trigger('click')
    expect(drawer(wrapper).get('h2').text()).toBe('Cách tính doanh thu')
    expect(drawer(wrapper).findAll('.today-explain-head').map(head => head.text())).toEqual(
      ['1. Định nghĩa', '2. Công thức', '3. Phạm vi thời gian', '4. Bao gồm', '5. Không bao gồm', '6. Xem chi tiết'])
    expect(drawer(wrapper).get('.today-formula').text()).toBe('Doanh thu = Tổng giá trị Sale hoàn tất− Giá trị Return− Giá trị Sale bị Void')
    expect(drawer(wrapper).text()).toContain('Từ 00:00 đến 14:23, ngày 16/12/2024 (theo múi giờ của cửa hàng).')

    await drawer(wrapper).findAll('button').find(item => item.text() === 'Xem giao dịch trong ca')!.trigger('click')
    expect(drawer(wrapper).get('h2').text()).toBe('Giao dịch gần đây')
    expect(drawer(wrapper).findAll('.today-detail-item')).toHaveLength(5)
    await drawer(wrapper).findAll('.today-detail-item')[1]!.trigger('click')
    expect(drawer(wrapper).get('.today-detail-grid').text()).toBe('Thời gian16/12/2024 · 14:15Trạng tháiHoàn tấtSố sản phẩm5Thanh toánChuyển khoảnGiá trị250.000 ₫')

    await wrapper.findAll('.today-stock-row')[1]!.trigger('click')
    expect(drawer(wrapper).get('h2').text()).toBe('Hết hàng')
    await wrapper.findAll('.today-product-row')[4]!.trigger('click')
    expect(drawer(wrapper).get('.today-detail-grid').text()).toContain('528.000 ₫')
    await wrapper.get('button[aria-label="Giải thích công nợ khách hàng"]').trigger('click')
    expect(drawer(wrapper).get('h2').text()).toBe('Cách tính công nợ')

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    await flushPromises()
    expect(wrapper.find('aside.today-drawer').exists()).toBe(false)
    expect(request).not.toHaveBeenCalled()
  })

  it('loads live data once the preview is switched off', async () => {
    request.mockImplementation(async path => baseRequest(path) as never)
    const wrapper = await mountPreview()
    todayDemoEnabled.value = false
    await flushPromises()
    expect(request).toHaveBeenCalledWith('/api/today')
    expect(wrapper.find('.today-status').exists()).toBe(false)
    expect(kpi(wrapper, 'Doanh thu').get('.today-kpi-value').text()).toBe('1.000 ₫')
  })
})
