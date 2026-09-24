import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import AttentionDetailView from './AttentionDetailView.vue'

const push = vi.fn()
vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>()
  return {
    ...actual,
    useRoute: () => ({ params: { productId: 'product-1' } }),
    useRouter: () => ({ push }),
  }
})

const request = vi.mocked(apiRequest)
const detail = {
  productId: 'product-1', productName: 'C14 product', sku: 'SKU-1', unit: 'cái',
  attentionKind: 'LowStockRisk', currentStock: 3, netSoldQuantity: 7,
  averageDailySales: 1, daysOfCover: 3,
  historyCoverage: 'FullSevenCompletedDays', recentSalesEvidence: 'PositiveNetSold',
  riskEvaluation: 'Eligible', businessDate: '2026-09-23', timeZoneId: 'Asia/Ho_Chi_Minh',
  velocityStartUtc: '2026-09-15T17:00:00Z', velocityEndUtc: '2026-09-22T17:00:00Z',
  completedBusinessDays: [{ businessDate: '2026-09-16', startUtc: '2026-09-15T17:00:00Z', endUtc: '2026-09-16T17:00:00Z' }],
  formulaInputs: { saleQuantity: 10, returnQuantity: 2, saleVoidQuantity: 1, denominator: 7 },
  evidence: [{ sourceType: 'Sale', sourceId: 'sale-1', relatedAggregateId: null,
    occurredAt: '2026-09-16T03:00:00Z', businessDate: '2026-09-16', productId: 'product-1',
    quantityContribution: 10, navigation: { type: 'Sale', id: 'sale-1' } }],
}

describe('AttentionDetailView', () => {
  beforeEach(() => {
    request.mockReset()
    push.mockReset()
  })

  it('shows formula, completed dates, evidence and product/purchase actions', async () => {
    request.mockImplementation(async (path, init) => {
      if (path === '/api/today/attention/product-1') return detail as never
      if (path === '/api/experiments/c14/events' && init?.method === 'POST') return {} as never
      throw new Error(`Unexpected request: ${path}`)
    })
    const wrapper = mount(AttentionDetailView, {
      global: { stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()

    expect(wrapper.text()).toContain('10 bán − 2 trả hàng − 1 hủy = 7 lượng bán thuần.')
    expect(wrapper.text()).toContain('2026-09-16')
    expect(wrapper.text()).toContain('Sale · +10')
    expect(wrapper.findAllComponents(RouterLinkStub)
      .find(link => link.text() === 'Xem sản phẩm')?.props('to'))
      .toEqual({ name: 'product-detail', params: { id: 'product-1' } })

    await wrapper.get('button').trigger('click')
    await flushPromises()
    const eventCall = request.mock.calls.find(([path]) => path === '/api/experiments/c14/events')
    expect(JSON.parse(String(eventCall?.[1]?.body))).toMatchObject({
      eventType: 'PurchaseDraftStarted', productId: 'product-1', attentionKind: 'LowStockRisk',
    })
    expect(push).toHaveBeenCalledWith({
      name: 'purchase-create', query: { productId: 'product-1' },
    })
  })

  it('still opens the Purchase form when best-effort telemetry fails', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/today/attention/product-1') return detail as never
      throw new Error('telemetry unavailable')
    })
    const debug = vi.spyOn(console, 'debug').mockImplementation(() => undefined)
    const wrapper = mount(AttentionDetailView, {
      global: { stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()
    await wrapper.get('button').trigger('click')
    await flushPromises()

    expect(push).toHaveBeenCalledWith({
      name: 'purchase-create', query: { productId: 'product-1' },
    })
    expect(debug).toHaveBeenCalled()
    debug.mockRestore()
  })
})
