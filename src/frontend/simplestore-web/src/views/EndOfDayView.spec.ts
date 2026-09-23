import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import EndOfDayView from './EndOfDayView.vue'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
const request = vi.mocked(apiRequest)

describe('EndOfDayView', () => {
  beforeEach(() => request.mockReset())

  it('defaults by store timezone and presents report breakdowns', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/store/current') return { timeZoneId: 'Asia/Ho_Chi_Minh' } as never
      return {
        businessDate: '2026-09-23', timeZoneId: 'Asia/Ho_Chi_Minh', startUtc: '2026-09-22T17:00:00Z', endUtc: '2026-09-23T17:00:00Z',
        salesRevenue: 100,
        collected: { salePayments: 150, customerDebtPayments: 30, customerRefunds: 0, netAmount: 180 },
        customerOutstandingDebtAtEnd: 20,
        supplierPayments: { purchasePayments: 20, supplierDebtPayments: 30, totalAmount: 50 },
        supplierOutstandingDebtAtEnd: 50,
        estimatedGrossProfit: { netSalesRevenue: 100, historicalCogs: 10, amount: 90, costReliability: 'Reliable' },
      } as never
    })
    const wrapper = mount(EndOfDayView)
    await flushPromises()

    expect(request.mock.calls.some(call => String(call[0]).startsWith('/api/reports/end-of-day?date='))).toBe(true)
    expect(wrapper.text()).toContain('Doanh thu')
    expect(wrapper.text()).toContain('Công nợ cuối ngày')
    expect(wrapper.text()).toContain('Độ tin cậy: Reliable')
  })
})
