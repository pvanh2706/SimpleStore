import { enableAutoUnmount, flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { h } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter, RouterView } from 'vue-router'
import { ApiError } from '../api/client'
import type { Sale } from '../api/types'
import SaleCheckoutForm from '../components/SaleCheckoutForm.vue'
import { blockedSaleLeaves, keepPendingSale, saleOutcomePending } from '../sales/checkoutGuard'
import { salesDemoEnabled } from '../sales/demo'
import { liveOrderBook } from '../sales/orders'
import SaleCheckoutView from './SaleCheckoutView.vue'

const apiRequest = vi.hoisted(() => vi.fn())
vi.mock('../api/client', async original => ({ ...await original<typeof import('../api/client')>(), apiRequest }))

const coffee = { id: 'product-coffee', sku: 'CF-1', barcode: '893001', name: 'Coffee', unit: 'gói', salePrice: 12000, isActive: true, quantityOnHand: 9 }
const tea = { id: 'product-tea', sku: 'TEA-1', barcode: '893002', name: 'Tea', unit: 'hộp', salePrice: 8000, isActive: true, quantityOnHand: 4 }
const customer = { id: 'customer-1', name: 'An', phone: '0909', createdAt: '', updatedAt: '' }
const sale = (body: Record<string, unknown>): Sale => ({
  id: 'sale-1', status: 'Completed', storeName: 'Tạp hóa', warehouseId: 'warehouse-1', customer: null, cashierDisplayName: 'cashier',
  lines: [], payments: [], totalAmount: 20000, paidAmount: 0, outstandingAmount: 20000,
  createdAt: '2026-10-02T03:00:00Z', completedAt: '2026-10-02T03:00:00Z', wasAlreadyCompleted: false,
  originalTotalAmount: 20000, totalReturnedAmount: 0, netSaleAmount: 20000, originalCollectedAmount: 0,
  totalRefundedAmount: 0, netCollectedAmount: 0, isVoided: false, void: null, returns: [], ...body,
})

/** Every API call the page makes, with its method; production writes are anything but GET. */
let calls: Array<{ path: string; method: string; body: Record<string, unknown> | null }>
let completeAnswers: Array<() => Promise<Sale>>
const writes = () => calls.filter(call => call.method !== 'GET')

async function openSales() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/sales/new', name: 'sale-checkout', component: SaleCheckoutView },
      { path: '/sales', name: 'sales', component: { render: () => h('p', 'Lịch sử bán hàng') } },
    ],
  })
  router.beforeEach(keepPendingSale(() => true))
  await router.push('/sales/new')
  await router.isReady()
  // Attached, so visibility (v-show) is computed like in a browser.
  const wrapper = mount({ render: () => h(RouterView) }, { global: { plugins: [router] }, attachTo: document.body })
  await flushPromises()
  const forms = () => wrapper.findAllComponents(SaleCheckoutForm)
  const live = () => forms().find(form => !form.props('previewOnly'))!
  const demo = () => forms().find(form => form.props('previewOnly'))
  return { wrapper, live, demo }
}

const button = (form: VueWrapper, text: string) => form.findAll('button').find(item => item.text() === text)!
const setDemo = async (enabled: boolean) => {
  salesDemoEnabled.value = enabled
  await flushPromises()
}

async function chooseCustomer(form: VueWrapper) {
  await form.get('[aria-label="Tìm khách hàng"]').setValue('An')
  await form.findAll('form').find(item => item.find('[aria-label="Tìm khách hàng"]').exists())!.trigger('submit')
  await flushPromises()
  await form.get('[aria-label="Chọn khách hàng An"]').trigger('click')
}

async function pay(form: VueWrapper, method: 'Tiền mặt' | 'Chuyển khoản', amount: string) {
  await button(form, method).trigger('click')
  await form.get('[aria-label="Số tiền thanh toán"]').setValue(amount)
  await button(form, 'Thêm thanh toán').trigger('click')
}

/** The live CompleteSale payload carries exactly the approved fields (D-105/D-107). */
function expectApprovedPayload(body: Record<string, unknown>) {
  expect(Object.keys(body).sort()).toEqual(['customerId', 'lines', 'operationId', 'payments'])
  for (const line of body.lines as Array<Record<string, unknown>>) expect(Object.keys(line).sort()).toEqual(['productId', 'quantity'])
  for (const payment of body.payments as Array<Record<string, unknown>>) {
    expect(Object.keys(payment).sort()).toEqual(['amount', 'method'])
    expect(['Cash', 'Transfer']).toContain(payment.method)
  }
}

describe('Sales Demo / Live isolation (D-107 G)', () => {
  enableAutoUnmount(afterEach)

  beforeEach(() => {
    calls = []
    completeAnswers = []
    let next = 0
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => `operation-${++next}`) })
    apiRequest.mockImplementation(async (path: string, init?: RequestInit) => {
      const body = init?.body ? JSON.parse(String(init.body)) as Record<string, unknown> : null
      calls.push({ path, method: init?.method ?? 'GET', body })
      if (path === '/api/store/operational-settings') return { allowNegativeStock: false }
      if (path.startsWith('/api/products?')) return { items: [coffee, tea], page: 1, pageSize: 20, totalCount: 2, totalPages: 1 }
      if (path.startsWith('/api/customers?')) return { items: [customer], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }
      if (path === '/api/sales/complete') return (completeAnswers.shift() ?? (() => Promise.resolve(sale(body!))))()
      if (path.startsWith('/api/operations/')) throw new ApiError(404, { title: 'Not found' })
      throw new Error(`Unexpected request ${init?.method ?? 'GET'} ${path}`)
    })
  })
  afterEach(() => {
    salesDemoEnabled.value = false
    saleOutcomePending.value = false
    liveOrderBook.reset()
    vi.unstubAllGlobals()
  })

  it('never lets the Demo checkout reach the sales API, even through its callbacks', async () => {
    salesDemoEnabled.value = true
    const { demo } = await openSales()
    const preview = demo()!
    const before = calls.length

    await button(preview, 'Hoàn tất bán hàng').trigger('click')
    await flushPromises()
    expect(preview.get('.sales-pos__message').text()).toContain('dữ liệu mẫu')

    // Demo Customer creation stays in the tab.
    await preview.get('[aria-label="Tên khách hàng mới"]').setValue('Khách mẫu')
    await button(preview, 'Tạo và chọn khách hàng').trigger('click')
    await flushPromises()
    expect(preview.get('.sales-pos__customer-picker summary').text()).toContain('Khách mẫu')

    // Even if a future UI regression invoked them, the Demo callbacks refuse locally and never call the API.
    const attempt = { operationId: 'demo', customerId: null, lines: [{ productId: 'demo-coke', quantity: 1 }], payments: [] }
    await expect(preview.props('completeSale')(attempt)).rejects.toMatchObject({ status: 400, problem: { code: 'sales-demo-only' } })
    await expect(preview.props('loadSale')('sale-1')).rejects.toBeInstanceOf(ApiError)
    expect(await preview.props('checkOperation')('demo')).toBeNull()
    expect((await preview.props('createCustomer')('Mẫu', null)).id).toMatch(/^demo-customer-/)
    expect((await preview.props('searchProducts')('', 1)).items.every(item => item.id.startsWith('demo-'))).toBe(true)

    expect(calls.slice(before)).toEqual([])
    expect(writes()).toEqual([])
  })

  it('keeps the live order intact across Live → Demo → Live and sends only approved fields', async () => {
    const { live, demo } = await openSales()
    await live().get('[aria-label="Thêm sản phẩm Coffee"]').trigger('click')
    await live().get('[aria-label="Thêm sản phẩm Tea"]').trigger('click')
    await chooseCustomer(live())
    await button(live(), 'Nhập số tiền').trigger('click')
    await pay(live(), 'Tiền mặt', '10000')
    await pay(live(), 'Chuyển khoản', '5000')
    await live().get('[aria-label="Tìm hoặc quét sản phẩm"]').setValue('Coff')
    const snapshot = () => JSON.stringify({
      order: liveOrderBook.active.value,
      search: (live().get('[aria-label="Tìm hoặc quét sản phẩm"]').element as HTMLInputElement).value,
      cards: live().findAll('.sales-pos__product-card h3').map(card => card.text()),
      state: (live().vm as unknown as { state: string }).state,
    })
    const before = snapshot()
    const callsBefore = calls.length

    await setDemo(true)
    const preview = demo()!
    expect(live().isVisible()).toBe(false)
    await button(preview, 'Bán nợ').trigger('click')
    await preview.get('#sales-order-note-preview').setValue('ghi chú mẫu')
    await preview.get('[aria-label="Thêm sản phẩm Pepsi 330ml"]').trigger('click')
    await button(preview, 'Giữ đơn').trigger('click')
    await setDemo(false)

    expect(demo()).toBeUndefined()
    expect(live().isVisible()).toBe(true)
    expect(snapshot()).toBe(before)
    expect(calls.slice(callsBefore)).toEqual([])

    // A new Demo session starts from the D-106 sample orders, untouched by live or the previous Demo session.
    await setDemo(true)
    expect(demo()!.findAll('button.sales-pos__order-pill')).toHaveLength(3)
    expect(demo()!.find('[aria-label^="Đơn 1: 3 sản phẩm"]').exists()).toBe(true)
    expect(demo()!.find('#sales-order-note-preview').element).toHaveProperty('value', '')
    await setDemo(false)

    await button(live(), 'Hoàn tất bán hàng').trigger('click')
    await flushPromises()
    const [request] = writes()
    expect(writes()).toHaveLength(1)
    expect(request!.path).toBe('/api/sales/complete')
    expectApprovedPayload(request!.body!)
    expect(request!.body).toEqual({
      operationId: 'operation-1',
      customerId: customer.id,
      lines: [{ productId: coffee.id, quantity: 1 }, { productId: tea.id, quantity: 1 }],
      payments: [{ amount: 10000, method: 'Cash' }, { amount: 5000, method: 'Transfer' }],
    })
  })

  it('sends a deliberate full debt as payments: [] with no unsupported field', async () => {
    const { live } = await openSales()
    await live().get('[aria-label="Thêm sản phẩm Tea"]').trigger('click')
    await button(live(), 'Ghi nợ toàn bộ').trigger('click')
    await chooseCustomer(live())
    await button(live(), 'Hoàn tất bán hàng').trigger('click')
    await flushPromises()

    const [request] = writes()
    expectApprovedPayload(request!.body!)
    expect(request!.body).toEqual({ operationId: 'operation-1', customerId: customer.id, lines: [{ productId: tea.id, quantity: 1 }], payments: [] })
  })

  it('keeps an unresolved live Sale visible and guarded when Demo is switched on, then allows it once resolved', async () => {
    completeAnswers.push(() => Promise.reject(new TypeError('network lost')))
    const { wrapper, live, demo } = await openSales()
    await live().get('[aria-label="Thêm sản phẩm Coffee"]').trigger('click')
    await button(live(), 'Hoàn tất bán hàng').trigger('click')
    await flushPromises()
    const vm = () => live().vm as unknown as { state: string; attempt: { operationId: string } | null }
    expect(vm().state).toBe('retryable')
    const operationId = vm().attempt!.operationId
    const refused = blockedSaleLeaves.value

    await setDemo(true)
    expect(salesDemoEnabled.value).toBe(false)
    expect(demo()).toBeUndefined()
    expect(live().isVisible()).toBe(true)
    expect(blockedSaleLeaves.value).toBe(refused + 1)
    expect(live().get('.sales-pos__leave-blocked').text()).toContain('Đơn bán đang chờ xác định kết quả.')
    expect(live().text()).toContain('Chưa xác định được kết quả')
    expect(vm().attempt!.operationId).toBe(operationId)

    await button(live(), 'Thử lại đúng thao tác').trigger('click')
    await flushPromises()
    const sent = writes().map(call => call.body)
    expect(sent).toHaveLength(2)
    expect(sent[1]).toEqual(sent[0])
    expect(wrapper.get('h1').text()).toBe('Đơn bán đã hoàn tất')

    // Resolved: switching modes is ordinary again.
    await setDemo(true)
    expect(salesDemoEnabled.value).toBe(true)
  })

  it('gives the mounted Live and Demo checkouts no conflicting element ids', async () => {
    const { wrapper, live } = await openSales()
    await live().get('[aria-label="Thêm sản phẩm Coffee"]').trigger('click')
    await setDemo(true)
    await wrapper.findAllComponents(SaleCheckoutForm).find(form => form.props('previewOnly'))!
      .get('[aria-label="Thêm sản phẩm Coca Cola 330ml"]').trigger('click')

    const ids = wrapper.findAll('[id]').map(element => element.attributes('id'))
    expect(ids.length).toBeGreaterThan(10)
    expect(new Set(ids).size).toBe(ids.length)
    expect(ids).toContain(`sales-quantity-${coffee.id}`)
    expect(ids).toContain('sales-quantity-demo-coke-preview')
  })
})
