import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import type { EndOfDayReport } from '../api/types'
import { dayCloseDemoEnabled } from '../dayclose/demo'
import DayCloseView from './DayCloseView.vue'

vi.mock('../api/client', async importOriginal => {
  const actual = await importOriginal<typeof import('../api/client')>()
  return { ...actual, apiRequest: vi.fn() }
})
const mockedApiRequest = vi.mocked(apiRequest)
const mounted: Array<{ unmount: () => void }> = []

const report = (date: string): EndOfDayReport => ({
  businessDate: date, timeZoneId: 'Asia/Ho_Chi_Minh', startUtc: '2026-09-29T17:00:00Z', endUtc: '2026-09-30T17:00:00Z',
  salesRevenue: 1500000,
  collected: { salePayments: 1200000, customerDebtPayments: 300000, customerRefunds: 100000, netAmount: 1400000 },
  customerOutstandingDebtAtEnd: 250000,
  supplierPayments: { purchasePayments: 400000, supplierDebtPayments: 100000, totalAmount: 500000 },
  supplierOutstandingDebtAtEnd: 900000,
  estimatedGrossProfit: { netSalesRevenue: 1500000, historicalCogs: 1000000, amount: 500000, costReliability: 'Estimated' },
})

function mountView() {
  const wrapper = mount(DayCloseView, { attachTo: document.body })
  mounted.push(wrapper)
  return wrapper
}
const button = (wrapper: ReturnType<typeof mountView>, text: string) => wrapper.findAll('button').find(candidate => candidate.text() === text)!
const reportRequests = () => mockedApiRequest.mock.calls.map(([path]) => String(path)).filter(path => path.startsWith('/api/reports/end-of-day'))

beforeEach(() => {
  // 01:30 on 1 Oct in Ho Chi Minh while the UTC calendar still says 30 Sep.
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date('2026-09-30T18:30:00Z'))
  mockedApiRequest.mockReset()
  mockedApiRequest.mockImplementation(async path => {
    const url = new URL(`https://local${String(path)}`)
    if (url.pathname === '/api/store/current') return { id: 'store-1', name: 'Store', mainWarehouseId: 'w', mainWarehouseName: 'Kho', timeZoneId: 'Asia/Ho_Chi_Minh' } as never
    if (url.pathname === '/api/reports/end-of-day') return report(url.searchParams.get('date')!) as never
    throw new Error(`Unexpected ${path}`)
  })
})
afterEach(() => {
  dayCloseDemoEnabled.value = false
  mounted.forEach(wrapper => wrapper.unmount())
  mounted.length = 0
  vi.useRealTimers()
})

describe('DayCloseView with live data', () => {
  it('lists Store-local business dates and offers no close action', async () => {
    const wrapper = mountView()
    await flushPromises()
    const rows = wrapper.findAll('.dayclose-table tbody tr')
    expect(rows).toHaveLength(10)
    expect(rows[0]!.text()).toContain('01/10/2026')
    expect(rows[0]!.text()).toContain('Thứ 5')
    expect(rows[0]!.text()).toContain('Hôm nay')
    expect(rows[1]!.text()).toContain('30/09/2026')
    expect(wrapper.text()).toContain('Hiển thị 1 – 10 / 30 ngày')
    expect(wrapper.text()).toContain('chưa được hệ thống hỗ trợ')
    expect(wrapper.text()).not.toContain('Đã đóng')
    expect(wrapper.text()).not.toContain('Đóng ngày hôm nay')

    await wrapper.findAll('.dayclose-pager button').find(candidate => candidate.text() === '2')!.trigger('click')
    expect(wrapper.find('.dayclose-table tbody tr').text()).toContain('21/09/2026')
  })

  it('shows the report aggregates, cost reliability and its explanation without claiming locked figures', async () => {
    const wrapper = mountView()
    await flushPromises()
    await wrapper.get('[aria-label="Xem báo cáo ngày 30/09/2026"]').trigger('click')
    await flushPromises()
    expect(reportRequests()).toEqual(['/api/reports/end-of-day?date=2026-09-30'])
    expect(wrapper.findAll('.dayclose-kpi strong').map(item => item.text())).toEqual(
      ['1.500.000 đ', '1.400.000 đ', '250.000 đ', '1.000.000 đ', '500.000 đ', '500.000 đ'])
    expect(wrapper.text()).toContain('Độ tin cậy giá vốn: Ước tính')
    expect(wrapper.text()).toContain('Hệ thống chưa phân loại doanh thu theo nhóm hàng.')
    expect(wrapper.get('.dayclose-meta').text()).toContain('00:00 30/09/2026')
    expect(wrapper.get('.dayclose-meta').text()).toContain('00:00 01/10/2026')
    expect(wrapper.find('.dayclose-status').exists()).toBe(false)

    await wrapper.get('[aria-label="Xem chi tiết Tiền thu thuần"]').trigger('click')
    const detail = wrapper.get('[role="dialog"][aria-label="Chi tiết tiền thu thuần"]')
    expect(detail.text()).toContain('Hoàn tiền khách hàng')
    expect(detail.text()).toContain('-100.000 đ')
    await button(wrapper, 'Giải thích số liệu').trigger('click')
    const explain = wrapper.get('[role="dialog"][aria-label="Giải thích Tiền thu thuần"]')
    expect(explain.findAll('.dayclose-formula-box').map(box => box.text())).toEqual(
      ['1.500.000 đThanh toán + thu nợ', '100.000 đHoàn tiền', '1.400.000 đTiền thu thuần'])
    expect(explain.text()).toContain('không phải số liệu đã chốt')
    expect(wrapper.text()).not.toContain('được chốt tại thời điểm đóng ngày')
  })

  it('steps between business dates but not past today', async () => {
    const wrapper = mountView()
    await flushPromises()
    await wrapper.get('[aria-label="Xem báo cáo ngày 30/09/2026"]').trigger('click')
    await flushPromises()
    await wrapper.get('[aria-label="Ngày sau"]').trigger('click')
    await flushPromises()
    expect(reportRequests().at(-1)).toBe('/api/reports/end-of-day?date=2026-10-01')
    expect(wrapper.get('.dayclose-status').text()).toBe('Đang diễn ra')
    expect(wrapper.get('[aria-label="Ngày sau"]').attributes('disabled')).toBeDefined()
    await wrapper.get('[aria-label="Ngày trước"]').trigger('click')
    await wrapper.get('[aria-label="Ngày trước"]').trigger('click')
    await flushPromises()
    expect(reportRequests().at(-1)).toBe('/api/reports/end-of-day?date=2026-09-29')
    await button(wrapper, '← Danh sách ngày').trigger('click')
    expect(wrapper.findAll('.dayclose-table tbody tr')).toHaveLength(10)
  })
})

describe('DayCloseView with reference data', () => {
  it('reproduces the reference list, report, drawers and close wizard without calling the API', async () => {
    const wrapper = mountView()
    await flushPromises()
    dayCloseDemoEnabled.value = true
    await flushPromises()
    mockedApiRequest.mockClear()

    const rows = wrapper.findAll('.dayclose-table tbody tr')
    expect(rows.map(row => row.findAll('td').slice(0, 4).map(cell => cell.text()))).toEqual([
      ['16/12/2024', '-', 'Chưa đóng', '-'],
      ['15/12/2024', '23:52', 'Đã đóng', 'Việt Anh'],
      ['14/12/2024', '23:48', 'Đã đóng', 'Việt Anh'],
      ['13/12/2024', '23:51', 'Đã đóng', 'Thu ngân'],
      ['12/12/2024', '23:55', 'Đã đóng', 'Việt Anh'],
      ['11/12/2024', '23:49', 'Đã đóng', 'Việt Anh'],
    ])
    expect(wrapper.text()).toContain('Hiển thị 1 – 6 / 20 ngày')
    expect(wrapper.findAll('.dayclose-pager button').map(item => item.text())).toEqual(['‹', '1', '2', '3', '4', '5', '…', '20', '›'])

    await wrapper.get('[aria-label="Xem báo cáo ngày 15/12/2024"]').trigger('click')
    expect(wrapper.get('.dayclose-meta').text()).toBe('Giờ đóng: 23:52Người thực hiện: Việt Anh')
    expect(wrapper.findAll('.dayclose-kpi strong').map(item => item.text())).toEqual(
      ['12.450.000 đ', '9.800.000 đ', '2.650.000 đ', '7.320.000 đ', '5.130.000 đ', '48'])
    expect(wrapper.get('.dayclose-donut').attributes('style')).toContain('conic-gradient(#0aa06a 0% 53.1%,#2f6fe4 53.1% 100%)')
    expect(wrapper.findAll('.dayclose-bar-row b').map(item => item.text())).toEqual(
      ['3.850.000 đ (30.9%)', '2.980.000 đ (23.9%)', '1.920.000 đ (15.4%)', '1.560.000 đ (12.5%)', '2.140.000 đ (17.2%)'])
    expect(wrapper.findAll('.dayclose-detail-grid .dayclose-card h3').map(item => item.text())).toEqual(
      ['Chi tiết doanh thu bán hàng', 'Chi tiết thực thu tiền', 'Chi tiết công nợ phát sinh'])

    await wrapper.get('[aria-label="Xem chi tiết Doanh thu bán hàng"]').trigger('click')
    expect(wrapper.get('[role="dialog"][aria-label="Chi tiết doanh thu bán hàng"]').text()).toContain('RT000021')
    await button(wrapper, 'Giải thích số liệu').trigger('click')
    expect(wrapper.findAll('.dayclose-formula-box').map(box => box.text())).toEqual(['12.650.000 đ', '300.000 đ', '12.450.000 đ'])
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    await flushPromises()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)

    await wrapper.get('[aria-label="Ngày sau"]').trigger('click')
    expect(wrapper.get('.dayclose-date-pill').text()).toBe('16/12/2024')
    expect(wrapper.get('.dayclose-status').text()).toBe('Chưa đóng')
    await button(wrapper, '← Danh sách ngày').trigger('click')

    await button(wrapper, '＋ Đóng ngày hôm nay').trigger('click')
    const wizard = wrapper.get('[role="dialog"][aria-label="Quy trình đóng ngày"]')
    expect(wizard.text()).toContain('Số liệu ngày 16/12/2024')
    await button(wrapper, 'Tiếp theo →').trigger('click')
    expect(wizard.text()).toContain('Đối soát thanh toán')
    await button(wrapper, 'Tiếp theo →').trigger('click')
    expect(wizard.get('.dayclose-step.active').text()).toBe('3Xác nhận đóng ngày')
    await wizard.get('textarea').setValue('Đã đếm két')
    await button(wrapper, '✓ Xác nhận đóng ngày').trigger('click')

    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(wrapper.get('.dayclose-toast').text()).toBe('Đã đóng ngày 16/12/2024 trong dữ liệu mẫu')
    expect(wrapper.findAll('.dayclose-table tbody tr')[0]!.findAll('td').slice(0, 4).map(cell => cell.text()))
      .toEqual(['16/12/2024', '14:23', 'Đã đóng', 'Việt Anh'])
    expect(button(wrapper, '＋ Đóng ngày hôm nay').attributes('disabled')).toBeDefined()
    expect(mockedApiRequest).not.toHaveBeenCalled()

    dayCloseDemoEnabled.value = false
    await flushPromises()
    expect(wrapper.findAll('.dayclose-table tbody tr')).toHaveLength(10)
  })
})
