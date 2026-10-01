import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest } from '../api/client'
import type { Product, ProductListItem } from '../api/types'
import { inventoryDemoEnabled } from '../inventory/demo'
import { useAuthStore } from '../stores/auth'
import InventoryView from './InventoryView.vue'

vi.mock('../api/client', async importOriginal => {
  const actual = await importOriginal<typeof import('../api/client')>()
  return { ...actual, apiRequest: vi.fn() }
})
vi.mock('vue-router', () => ({ RouterLink: { props: ['to'], template: '<a :href="to"><slot /></a>' } }))
const mockedApiRequest = vi.mocked(apiRequest)
const mounted: Array<{ unmount: () => void }> = []

const listItem = (id: string, quantityOnHand: number, patch: Partial<ProductListItem> = {}): ProductListItem => ({
  id, sku: `SKU-${id}`, barcode: null, name: `Sản phẩm ${id}`, unit: 'Chai', salePrice: 10000, isActive: true, quantityOnHand,
  updatedAt: '2026-09-20T03:00:00Z', ...patch,
})
const product = (item: ProductListItem, patch: Partial<Product> = {}): Product => ({
  ...item, referencePurchaseCost: 6000, inventoryValue: item.quantityOnHand * 5000, averageCost: 5000, hasAverageCost: true,
  referencePurchaseCostRevision: 1, createdAt: '2026-09-01T00:00:00Z', updatedAt: item.updatedAt ?? '', ...patch,
})
const items = [listItem('a', 24), listItem('b', 0), listItem('c', -3), listItem('d', 4), listItem('e', 9, { isActive: false })]
let posts: Array<{ path: string; body: unknown }> = []

function mountView(role: 'Owner' | 'Cashier' = 'Owner') {
  const pinia = createPinia()
  setActivePinia(pinia)
  useAuthStore().session = { isAuthenticated: true, email: 'owner@test', storeId: 'store-1', roles: [role], hasStore: true }
  const wrapper = mount(InventoryView, { attachTo: document.body, global: { plugins: [pinia] } })
  mounted.push(wrapper)
  return wrapper
}
const button = (wrapper: ReturnType<typeof mountView>, text: string) => wrapper.findAll('button').find(candidate => candidate.text() === text)!
const menuItem = (wrapper: ReturnType<typeof mountView>, text: string) =>
  wrapper.get('.inventory-action-menu').findAll('button').find(candidate => candidate.text() === text)!
const listRequests = () => mockedApiRequest.mock.calls.map(([path]) => String(path)).filter(path => /^\/api\/products\?/.test(path) && !path.includes('pageSize=1&'))

beforeEach(() => {
  posts = []
  mockedApiRequest.mockReset()
  mockedApiRequest.mockImplementation(async (path, init) => {
    const url = new URL(`https://local${String(path)}`)
    if (init?.method === 'POST') { posts.push({ path: url.pathname, body: JSON.parse(String(init.body)) }); return {} as never }
    if (url.pathname === '/api/products' && url.searchParams.get('pageSize') === '1') return { items: [], page: 1, pageSize: 1, totalCount: 4, totalPages: 4 } as never
    if (url.pathname === '/api/products') return { items, page: Number(url.searchParams.get('page')), pageSize: 10, totalCount: 25, totalPages: 3 } as never
    const productMatch = url.pathname.match(/^\/api\/products\/(\w+)$/)
    if (productMatch) return product(items.find(item => item.id === productMatch[1])!) as never
    if (url.pathname.endsWith('/movements')) return [] as never
    if (url.pathname === '/api/today/attention') {
      return { items: [{ productId: 'd', attentionKind: 'LowStockRisk' }, { productId: 'b', attentionKind: 'OutOfStock' }], totalPages: 1 } as never
    }
    if (url.pathname.startsWith('/api/today/attention/')) throw new ApiError(404, { code: 'c14-attention-not-found' })
    if (url.pathname.startsWith('/api/inventory/stocktakes/context/')) {
      return { productId: 'a', productName: 'Sản phẩm a', productSku: 'SKU-a', unit: 'Chai', expectedQuantity: 24, expectedRevision: 'rev-1', hasAverageCost: true, averageCost: 5000 } as never
    }
    throw new Error(`Unexpected ${path}`)
  })
})
afterEach(() => {
  inventoryDemoEnabled.value = false
  mounted.forEach(wrapper => wrapper.unmount())
  mounted.length = 0
})

describe('InventoryView with live data', () => {
  it('shows factual stock, C14 low-stock risk and per-product cost without an invented threshold', async () => {
    const wrapper = mountView()
    await flushPromises()
    const rows = wrapper.findAll('.inventory-table tbody tr')
    expect(rows.map(row => row.get('.inventory-status').text())).toEqual(['Còn hàng', 'Hết hàng', 'Tồn kho âm', 'Sắp hết hàng', 'Ngừng bán'])
    expect(rows[0]!.text()).toContain('5.000 đ')
    expect(rows[0]!.text()).toContain('120.000 đ')
    expect(wrapper.findAll('.inventory-summary-card strong').map(card => card.text())).toEqual(['4', '1', '1', '0'])
    expect(wrapper.findAll('.inventory-chip').map(chip => chip.text())).toEqual(['Tất cả (25)', 'Hết hàng', 'Tồn kho âm', 'Sắp hết hàng'])
  })

  it('searches on the server and filters stock state only within the loaded page', async () => {
    vi.useFakeTimers()
    const wrapper = mountView()
    await flushPromises()
    await wrapper.get('.inventory-search input').setValue('coca')
    await vi.advanceTimersByTimeAsync(300)
    await flushPromises()
    vi.useRealTimers()
    expect(listRequests().at(-1)).toBe('/api/products?page=1&pageSize=10&search=coca')

    const requests = listRequests().length
    await wrapper.findAll('.inventory-chip').find(chip => chip.text() === 'Tồn kho âm')!.trigger('click')
    expect(listRequests()).toHaveLength(requests)
    expect(wrapper.findAll('.inventory-table tbody tr')).toHaveLength(1)
    expect(wrapper.get('.inventory-scope-note').text()).toContain('trang hiện tại')
  })

  it('hides C14 counts and inventory actions from a Cashier', async () => {
    const wrapper = mountView('Cashier')
    await flushPromises()
    expect(mockedApiRequest.mock.calls.some(([path]) => String(path).startsWith('/api/today'))).toBe(false)
    expect(wrapper.findAll('.inventory-summary-card strong').map(card => card.text())).toEqual(['4', '—', '—', '—'])
    expect(wrapper.find('.inventory-head-actions').exists()).toBe(false)
  })

  it('submits one adjustment and keeps the operation id when the outcome is uncertain', async () => {
    const wrapper = mountView()
    await flushPromises()
    await wrapper.get('button[aria-label="Thao tác Sản phẩm a"]').trigger('click')
    await menuItem(wrapper, 'Điều chỉnh tồn kho').trigger('click')
    await flushPromises()
    const next = () => wrapper.get('.inventory-modal-foot .inventory-primary').trigger('click')
    await next()
    await flushPromises()
    const dialog = wrapper.get('[aria-label="Điều chỉnh tồn kho"]')
    await dialog.get('input[type="number"]').setValue(-2)
    await dialog.get('select').setValue('Hàng hỏng')
    await next()
    expect(dialog.text()).toContain('Tồn sau điều chỉnh22')

    mockedApiRequest.mockRejectedValueOnce(new TypeError('network'))
    await next()
    await flushPromises()
    expect(dialog.get('.inventory-error').text()).toContain('Chưa rõ kết quả')
    await next()
    await flushPromises()
    expect(posts).toHaveLength(1)
    const attempts = mockedApiRequest.mock.calls.filter(([path]) => path === '/api/inventory/adjustments').map(([, init]) => JSON.parse(String(init?.body)))
    expect(attempts).toHaveLength(2)
    expect(attempts[0].operationId).toBe(attempts[1].operationId)
    expect(attempts[1]).toMatchObject({ productId: 'a', quantityDelta: -2, reason: 'Hàng hỏng', adjustmentUnitCost: null })
    expect(wrapper.find('[aria-label="Điều chỉnh tồn kho"]').exists()).toBe(false)
  })

  it('runs a one-product stocktake against the loaded expected revision', async () => {
    const wrapper = mountView()
    await flushPromises()
    await wrapper.get('button[aria-label="Thao tác Sản phẩm a"]').trigger('click')
    await menuItem(wrapper, 'Kiểm kho').trigger('click')
    const next = () => wrapper.get('.inventory-modal-foot .inventory-primary').trigger('click')
    await next()
    await flushPromises()
    await wrapper.get('input[aria-label="Số đếm Sản phẩm a"]').setValue(20)
    await next()
    expect(wrapper.get('.inventory-kpis').text()).toContain('-4')
    await next()
    await next()
    await flushPromises()
    expect(posts).toEqual([{ path: '/api/inventory/stocktakes', body: expect.objectContaining({ productId: 'a', expectedQuantity: 24, expectedRevision: 'rev-1', countedQuantity: 20 }) }])
  })
})

describe('InventoryView sample data', () => {
  it('reproduces the reference overview without calling the API', async () => {
    inventoryDemoEnabled.value = true
    const wrapper = mountView()
    await flushPromises()
    expect(mockedApiRequest).not.toHaveBeenCalled()
    expect(wrapper.findAll('.inventory-summary-card strong').map(card => card.text())).toEqual(['156', '8', '12', '3'])
    expect(wrapper.findAll('.inventory-chip').map(chip => chip.text())).toEqual(['Tất cả (156)', 'Hết hàng (8)', 'Tồn kho thấp (12)', 'Tồn kho âm (3)', 'Sắp hết hàng (5)', 'Khác (128)'])
    const rows = wrapper.findAll('.inventory-table tbody tr')
    expect(rows.map(row => row.get('.inventory-name').text())).toEqual(['Coca Cola 330ml', 'Pepsi 330ml', 'Nước suối Aquafina 500ml', 'Trà xanh C2 500ml', 'Sữa Vinamilk 180ml', 'Bánh Oreo 133g', 'Mì Hảo Hảo 75g'])
    expect(rows[3]!.text()).toContain('-13.500 đ')
    expect(wrapper.text()).toContain('Hiển thị 1 – 7 / 156 sản phẩm')
    expect(wrapper.findAll('.inventory-pager button').map(item => item.text())).toEqual(['‹', '1', '2', '3', '4', '5', '16', '›'])

    await wrapper.findAll('.inventory-chip').find(chip => chip.text().startsWith('Sắp hết hàng'))!.trigger('click')
    expect(wrapper.findAll('.inventory-table tbody tr').map(row => row.get('.inventory-name').text())).toEqual(['Bánh Oreo 133g'])
  })

  it('adjusts a sample product and records the movement', async () => {
    inventoryDemoEnabled.value = true
    const wrapper = mountView()
    await wrapper.get('button[aria-label="Thao tác Trà xanh C2 500ml"]').trigger('click')
    await menuItem(wrapper, 'Điều chỉnh tồn kho').trigger('click')
    const next = () => wrapper.get('.inventory-modal-foot .inventory-primary').trigger('click')
    expect(wrapper.get('.inventory-selection-row.selected').text()).toContain('Trà xanh C2 500ml')
    await next()
    const dialog = wrapper.get('[aria-label="Điều chỉnh tồn kho"]')
    await dialog.get('input[type="number"]').setValue(13)
    await dialog.get('select').setValue('Cân chỉnh sổ sách')
    await next()
    await next()
    const row = wrapper.findAll('.inventory-table tbody tr').find(candidate => candidate.text().includes('Trà xanh C2 500ml'))!
    expect(row.get('.inventory-stock').text()).toBe('10')
    expect(row.get('.inventory-status').text()).toBe('Bình thường')
    expect(wrapper.findAll('.inventory-summary-card strong').map(card => card.text())).toEqual(['156', '8', '12', '2'])
    expect(mockedApiRequest).not.toHaveBeenCalled()
  })

  it('walks the four-step reference stocktake with 140 matched and 16 different', async () => {
    inventoryDemoEnabled.value = true
    const wrapper = mountView()
    await button(wrapper, 'Kiểm kho').trigger('click')
    const next = () => wrapper.get('.inventory-modal-foot .inventory-primary').trigger('click')
    expect((wrapper.get('[aria-label="Kiểm kho"] input').element as HTMLInputElement).value).toBe('Kiểm kho cuối tháng 12/2024')
    await next()
    expect(wrapper.findAll('.inventory-count-grid input')).toHaveLength(5)
    await next()
    expect(wrapper.findAll('.inventory-kpis strong').map(item => item.text())).toEqual(['156', '140', '16'])
    expect(wrapper.findAll('.inventory-mini-table tbody tr').map(row => row.findAll('td')[0]!.text()))
      .toEqual(['Pepsi 330ml', 'Nước suối Aquafina 500ml', 'Trà xanh C2 500ml', 'Sữa Vinamilk 180ml', 'Mì Hảo Hảo 75g'])
    await next()
    expect(wrapper.get('.inventory-modal-foot .inventory-primary').text()).toBe('Xác nhận')
    await next()
    const pepsi = wrapper.findAll('.inventory-table tbody tr').find(candidate => candidate.text().includes('Pepsi 330ml'))!
    expect(pepsi.get('.inventory-stock').text()).toBe('2')
    expect(wrapper.get('.inventory-toast').text()).toBe('Đã xác nhận kiểm kho trong dữ liệu mẫu')
  })

  it('opens the detail tabs and the movement history of a sample product', async () => {
    inventoryDemoEnabled.value = true
    const wrapper = mountView()
    await wrapper.findAll('.inventory-name').find(name => name.text() === 'Coca Cola 330ml')!.trigger('click')
    const drawer = wrapper.get('[aria-label="Chi tiết tồn kho sản phẩm"]')
    expect(drawer.text()).toContain('24 Lon')
    expect(drawer.text()).toContain('5.000 đ / Lon')
    expect(drawer.text()).toContain('8935049500017')
    expect(drawer.text()).toContain('Không có cảnh báo')
    await button(wrapper, 'Lịch sử biến động').trigger('click')
    expect(drawer.findAll('.inventory-mini-table tbody tr')).toHaveLength(3)
    await button(wrapper, 'Xem toàn bộ lịch sử biến động →').trigger('click')
    const history = wrapper.get('[aria-label="Lịch sử biến động tồn kho"]')
    expect(history.findAll('tbody tr')).toHaveLength(5)
    await history.get('select').setValue('Bán hàng')
    expect(history.findAll('tbody tr')).toHaveLength(2)
  })
})
