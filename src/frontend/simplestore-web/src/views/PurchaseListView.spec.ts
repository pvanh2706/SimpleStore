import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import type { Purchase, PurchaseListItem } from '../api/types'
import { purchaseDemoEnabled } from '../purchases/demo'
import { useAuthStore } from '../stores/auth'
import PurchaseListView from './PurchaseListView.vue'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
const push = vi.fn()
vi.mock('vue-router', () => ({
  useRouter: () => ({ push }),
  RouterLink: { props: ['to'], template: '<a :href="to"><slot /></a>' },
}))
const mockedApiRequest = vi.mocked(apiRequest)
const mounted: Array<{ unmount: () => void }> = []

const item = (id: string, patch: Partial<PurchaseListItem>): PurchaseListItem => ({
  id, supplierId: 'supplier-1', supplierName: 'Thiên Long', status: 'Completed', totalAmount: 100000, paidAmount: 40000,
  outstandingAmount: 60000, createdAt: '2026-09-20T02:00:00Z', completedAt: '2026-09-20T03:00:00Z', isVoided: false, ...patch,
})
const liveItems = [
  item('aaaaaaaa-1111', {}),
  item('bbbbbbbb-2222', { status: 'Draft', paidAmount: 0, outstandingAmount: 100000, completedAt: null }),
  item('cccccccc-3333', { isVoided: true, outstandingAmount: 0 }),
]

function mountView(role: 'Owner' | 'Cashier' = 'Owner') {
  const pinia = createPinia()
  setActivePinia(pinia)
  useAuthStore().session = { isAuthenticated: true, email: 'owner@test', storeId: 'store-1', roles: [role], hasStore: true, mustChangePassword: false, isEnabled: true }
  const wrapper = mount(PurchaseListView, { attachTo: document.body, global: { plugins: [pinia] } })
  mounted.push(wrapper)
  return wrapper
}
const buttonWithText = (wrapper: ReturnType<typeof mountView>, text: string) =>
  wrapper.findAll('button').find(button => button.text() === text)!
/** Ignores the one-row requests that only read chip counts. */
const lastListRequest = () => String(mockedApiRequest.mock.calls
  .filter(([path]) => new URL(`https://local${String(path)}`).searchParams.get('pageSize') !== '1').at(-1)?.[0])

beforeEach(() => {
  push.mockReset()
  mockedApiRequest.mockReset()
  mockedApiRequest.mockImplementation(async path => {
    const url = new URL(`https://local${String(path)}`)
    if (url.pathname !== '/api/purchases') throw new Error(`Unexpected ${path}`)
    const page = Number(url.searchParams.get('page') ?? '1')
    return { items: liveItems, page, pageSize: Number(url.searchParams.get('pageSize')), totalCount: 41, totalPages: 3 } as never
  })
})
afterEach(() => {
  purchaseDemoEnabled.value = false
  mounted.forEach(wrapper => wrapper.unmount())
  mounted.length = 0
})

describe('PurchaseListView with live data', () => {
  it('filters by the supported lifecycle status and keeps it while paging', async () => {
    const wrapper = mountView()
    await flushPromises()
    expect(lastListRequest()).toBe('/api/purchases?page=1&pageSize=20')

    await wrapper.findAll('.purchase-chip').find(chip => chip.text().startsWith('Đã nhập'))!.trigger('click')
    await flushPromises()
    expect(lastListRequest()).toBe('/api/purchases?page=1&pageSize=20&status=Completed')

    await wrapper.get('button[aria-label="Trang sau"]').trigger('click')
    await flushPromises()
    expect(lastListRequest()).toBe('/api/purchases?page=2&pageSize=20&status=Completed')
    expect(wrapper.text()).toContain('Hiển thị 21 - 23 / 41 phiếu nhập')
  })

  it('derives payment labels without hiding Draft or Void', async () => {
    const wrapper = mountView()
    await flushPromises()
    const rows = wrapper.findAll('.purchase-table tbody tr')
    expect(rows.map(row => row.get('.purchase-status').text())).toEqual(['Thanh toán một phần', 'Nháp', 'Đã hủy'])
    expect(rows[0]!.text()).toContain('#AAAAAAAA')
    expect(rows[1]!.findAll('.money').map(cell => cell.text())).toEqual(['100.000 đ', '—', '—'])
    expect(wrapper.findAll('.purchase-chip').map(chip => chip.text())).toEqual(['Tất cả (41)', 'Đã nhập (41)', 'Nháp (41)'])
  })

  it('opens the supported create page instead of the sample wizard', async () => {
    const wrapper = mountView()
    await flushPromises()
    await buttonWithText(wrapper, 'Tạo phiếu nhập').trigger('click')
    expect(push).toHaveBeenCalledWith('/purchases/new')
    expect(wrapper.find('.purchase-wizard').exists()).toBe(false)
  })

  it('shows a live detail with per-purchase debt wording and no print action', async () => {
    const purchase: Purchase = {
      id: 'aaaaaaaa-1111', supplierId: 'supplier-1', supplierName: 'Thiên Long', status: 'Completed',
      lines: [{ id: 'line-1', productId: 'product-1', productName: 'Coca Cola 330ml', productUnit: 'Lon', quantity: 10, unitPrice: 10000, lineAmount: 100000 }],
      payments: [{ id: 'payment-1', amount: 40000, method: 'Transfer', paidAt: '2026-09-20T03:00:00Z' }],
      totalAmount: 100000, paidAmount: 40000, outstandingAmount: 60000, createdAt: '2026-09-20T02:00:00Z', updatedAt: '',
      completedAt: '2026-09-20T03:00:00Z', wasAlreadyCompleted: false, isVoided: false, void: null,
    }
    const wrapper = mountView()
    await flushPromises()
    mockedApiRequest.mockResolvedValueOnce(purchase as never)
    await wrapper.get('.purchase-code').trigger('click')
    await flushPromises()
    const drawer = wrapper.get('[aria-label="Chi tiết phiếu nhập"]')
    expect(drawer.text()).toContain('Còn nợ theo phiếu')
    expect(drawer.text()).toContain('60.000 đ')
    expect(drawer.text()).toContain('Coca Cola 330ml')
    expect(drawer.text()).not.toContain('In phiếu')
    await drawer.get('button[aria-label="Thao tác phiếu nhập"]').trigger('click')
    expect(drawer.get('.purchase-action-menu a.danger').attributes('href')).toBe('/purchases/aaaaaaaa-1111')
  })
})

describe('PurchaseListView sample data', () => {
  it('reproduces the reference list without calling the purchase API', async () => {
    purchaseDemoEnabled.value = true
    const wrapper = mountView()
    await flushPromises()
    expect(mockedApiRequest).not.toHaveBeenCalled()
    expect(wrapper.findAll('.purchase-table tbody tr').map(row => row.get('.purchase-code').text()))
      .toEqual(['PN000045', 'PN000044', 'PN000043', 'PN000042', 'PN000041'])
    expect(wrapper.findAll('.purchase-chip').map(chip => chip.text())).toEqual(['Tất cả (56)', 'Đã nhập (45)', 'Đang xử lý (3)', 'Đã hủy (2)'])
    expect(wrapper.text()).toContain('Hiển thị 1 - 5 / 56 phiếu nhập')
    expect(wrapper.findAll('.purchase-pager button').map(button => button.text())).toEqual(['‹', '1', '2', '3', '4', '5', '12', '›'])

    await wrapper.findAll('.purchase-chip').find(chip => chip.text().startsWith('Đang xử lý'))!.trigger('click')
    expect(wrapper.findAll('.purchase-table tbody tr')).toHaveLength(3)
    await wrapper.get('.purchase-search input').setValue('đại phát')
    expect(wrapper.findAll('.purchase-table tbody tr').map(row => row.get('.purchase-code').text())).toEqual(['PN000044'])
  })

  it('creates a sample purchase through the four-step wizard', async () => {
    purchaseDemoEnabled.value = true
    const wrapper = mountView()
    await buttonWithText(wrapper, 'Tạo phiếu nhập').trigger('click')
    const next = () => wrapper.get('.purchase-wizard-foot .purchase-primary').trigger('click')
    expect(wrapper.get('.purchase-supplier-card').text()).toContain('5.200.000 đ')
    await next()
    expect(wrapper.get('.purchase-line-summary').text()).toBe('Tổng số lượng: 126Tổng tiền hàng: 654.000 đ')
    await next()
    expect(wrapper.get('.purchase-summary').text()).toContain('354.000 đ')
    await next()
    expect(wrapper.get('.purchase-wizard-body').text()).toContain('Thanh toán một phần')
    await next()
    await flushPromises()

    expect(wrapper.find('.purchase-wizard').exists()).toBe(false)
    const first = wrapper.get('.purchase-table tbody tr')
    expect(first.text()).toContain('PN000046')
    expect(first.findAll('.money').map(cell => cell.text())).toEqual(['654.000 đ', '300.000 đ', '354.000 đ'])
    expect(wrapper.get('.purchase-chip').text()).toBe('Tất cả (57)')
    expect(wrapper.get('[aria-label="Chi tiết phiếu nhập"]').text()).toContain('Phiếu nhập PN000046')
    expect(mockedApiRequest).not.toHaveBeenCalled()
  })

  it('requires a reason before cancelling a sample purchase', async () => {
    purchaseDemoEnabled.value = true
    const wrapper = mountView()
    await wrapper.findAll('.purchase-code').find(button => button.text() === 'PN000043')!.trigger('click')
    const drawer = wrapper.get('[aria-label="Chi tiết phiếu nhập"]')
    expect(drawer.text()).toContain('2.200.000 đ')
    expect(drawer.text()).toContain('Nhập hàng định kỳ')
    await drawer.get('button[aria-label="Thao tác phiếu nhập"]').trigger('click')
    await buttonWithText(wrapper, 'Hủy phiếu nhập').trigger('click')
    const modal = wrapper.get('[aria-label="Hủy phiếu nhập"]')
    await modal.get('.purchase-danger-outline').trigger('click')
    expect(wrapper.get('.purchase-toast').text()).toBe('Vui lòng nhập lý do hủy')
    await modal.get('input').setValue('Nhập trùng phiếu')
    await modal.get('.purchase-danger-outline').trigger('click')

    const row = wrapper.findAll('.purchase-table tbody tr').find(candidate => candidate.text().includes('PN000043'))!
    expect(row.get('.purchase-status').text()).toBe('Đã hủy')
    expect(wrapper.findAll('.purchase-chip').map(chip => chip.text())).toEqual(['Tất cả (56)', 'Đã nhập (45)', 'Đang xử lý (2)', 'Đã hủy (3)'])
  })

  it('previews the reference empty, loading and error states', async () => {
    purchaseDemoEnabled.value = true
    const wrapper = mountView()
    for (const [option, title] of [['Trạng thái rỗng', 'Chưa có phiếu nhập nào'], ['Đang tải', 'Đang tải dữ liệu...'], ['Lỗi tải dữ liệu', 'Không thể tải dữ liệu']]) {
      await buttonWithText(wrapper, 'Demo trạng thái ▴').trigger('click')
      await buttonWithText(wrapper, option!).trigger('click')
      expect(wrapper.get('.purchase-state h3').text()).toBe(title)
    }
    await buttonWithText(wrapper, 'Thử lại').trigger('click')
    expect(wrapper.findAll('.purchase-table tbody tr')).toHaveLength(5)
  })
})
