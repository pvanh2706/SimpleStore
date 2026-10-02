import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { h } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createMemoryHistory, createRouter, isNavigationFailure, NavigationFailureType, RouterView } from 'vue-router'
import { ApiError } from '../api/client'
import type { OperationStatus, Sale } from '../api/types'
import SaleCheckoutForm from '../components/SaleCheckoutForm.vue'
import { blockedSaleLeaves, keepPendingSale, saleOutcomePending } from '../sales/checkoutGuard'
import { liveOrderBook } from '../sales/orders'
import SaleCheckoutView from './SaleCheckoutView.vue'

const apiRequest = vi.hoisted(() => vi.fn())
vi.mock('../api/client', async original => ({ ...await original<typeof import('../api/client')>(), apiRequest }))

const product = { id: 'product-1', sku: 'CF-1', barcode: '893001', name: 'Coffee', unit: 'gói', salePrice: 12000, isActive: true, quantityOnHand: 9 }
const sale: Sale = {
  id: 'sale-1', status: 'Completed', storeName: 'Tạp hóa', warehouseId: 'warehouse-1', customer: null,
  cashierDisplayName: 'cashier@test',
  lines: [{ id: 'line-1', productId: product.id, productName: product.name, productSku: product.sku, productUnit: product.unit, quantity: 1, unitSalePrice: 12000, lineAmount: 12000, unitCostAtSale: 8000, costReliability: 'Reliable' }],
  payments: [{ id: 'payment-1', amount: 12000, method: 'Cash', occurredAt: '2026-10-02T03:00:00Z' }],
  totalAmount: 12000, paidAmount: 12000, outstandingAmount: 0,
  createdAt: '2026-10-02T03:00:00Z', completedAt: '2026-10-02T03:00:00Z', wasAlreadyCompleted: false,
  originalTotalAmount: 12000, totalReturnedAmount: 0, netSaleAmount: 12000,
  originalCollectedAmount: 12000, totalRefundedAmount: 0, netCollectedAmount: 12000,
  isVoided: false, void: null, returns: [],
}

/** Every CompleteSale request body, in order. */
let sent: Array<Record<string, unknown>>
/** Scripted CompleteSale and operation-status answers, consumed in order. */
let completeAnswers: Array<() => Promise<Sale>>
let statusAnswers: Array<() => Promise<OperationStatus>>
let authenticated: boolean

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((done, fail) => { resolve = done; reject = fail })
  return { promise, resolve, reject }
}

async function openCheckout() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/sales/new', name: 'sale-checkout', component: SaleCheckoutView },
      { path: '/sales', name: 'sales', component: { render: () => h('p', 'Lịch sử bán hàng') } },
      { path: '/products', name: 'products', component: { render: () => h('p', 'Sản phẩm') } },
    ],
  })
  router.beforeEach(keepPendingSale(() => authenticated))
  await router.push('/sales/new')
  await router.isReady()
  const wrapper = mount({ render: () => h(RouterView) }, { global: { plugins: [router] } })
  await flushPromises()
  await wrapper.get('[aria-label="Thêm sản phẩm Coffee"]').trigger('click')
  const form = () => wrapper.findComponent(SaleCheckoutForm)
  const vm = () => form().vm as unknown as { state: string; attempt: { operationId: string } | null }
  const complete = async (label: string) => {
    await wrapper.findAll('button').find(button => button.text() === label)!.trigger('click')
    await flushPromises()
  }
  const leaveTo = async (path: string) => {
    const failure = await router.push(path)
    await flushPromises()
    return failure
  }
  return { wrapper, router, form, vm, complete, leaveTo }
}

/** A cancelable beforeunload, as a reload or tab close would dispatch it. */
function unloadIsGuarded() {
  const event = new Event('beforeunload', { cancelable: true })
  window.dispatchEvent(event)
  return event.defaultPrevented
}

const aborted = (failure: unknown) => isNavigationFailure(failure, NavigationFailureType.aborted)

describe('Sales checkout navigation while a CompleteSale outcome is pending', () => {
  enableAutoUnmount(afterEach)

  beforeEach(() => {
    sent = []
    completeAnswers = []
    statusAnswers = []
    authenticated = true
    let next = 0
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => `operation-${++next}`) })
    apiRequest.mockImplementation(async (path: string, init?: RequestInit) => {
      if (path === '/api/store/operational-settings') return { allowNegativeStock: false }
      if (path.startsWith('/api/products?')) return { items: [product], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }
      if (path === '/api/sales/complete') {
        sent.push(JSON.parse(String(init!.body)))
        return completeAnswers.shift()!()
      }
      if (path.startsWith('/api/operations/')) return statusAnswers.shift()!()
      if (path === '/api/sales/sale-1') return sale
      throw new Error(`Unexpected request ${path}`)
    })
  })
  afterEach(() => {
    liveOrderBook.reset()
    saleOutcomePending.value = false
    vi.unstubAllGlobals()
  })

  it('keeps a retryable attempt mounted with the same OperationId, then retries it exactly', async () => {
    completeAnswers.push(() => Promise.reject(new TypeError('network lost')), () => Promise.resolve(sale))
    statusAnswers.push(() => Promise.reject(new ApiError(404, { title: 'Not found' })))
    const { wrapper, router, form, vm, complete, leaveTo } = await openCheckout()
    await complete('Hoàn tất bán hàng')

    expect(vm().state).toBe('retryable')
    const operationId = vm().attempt!.operationId
    expect(sent[0]!.operationId).toBe(operationId)
    expect(saleOutcomePending.value).toBe(true)
    expect(unloadIsGuarded()).toBe(true)

    const refusedBefore = blockedSaleLeaves.value
    expect(aborted(await leaveTo('/products'))).toBe(true)
    expect(aborted(await leaveTo('/sales'))).toBe(true)
    expect(blockedSaleLeaves.value).toBe(refusedBefore + 2)
    expect(router.currentRoute.value.path).toBe('/sales/new')
    expect(form().exists()).toBe(true)
    expect(vm().state).toBe('retryable')
    expect(vm().attempt!.operationId).toBe(operationId)
    const notice = wrapper.get('.sales-pos__leave-blocked')
    expect(notice.attributes('role')).toBe('alert')
    expect(notice.text()).toContain('Đơn bán đang chờ xác định kết quả.')
    expect(notice.text()).toContain('Hãy kiểm tra kết quả hoặc thử lại đúng thao tác trước khi rời màn bán hàng để tránh tạo đơn trùng.')
    expect(wrapper.text()).toContain('Chưa xác định được kết quả')

    // The header shortcut explains instead of leaving.
    await wrapper.get('a.sales-pos__history-link').trigger('click', { button: 0 })
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/sales/new')
    expect(blockedSaleLeaves.value).toBe(refusedBefore + 3)

    await complete('Thử lại đúng thao tác')
    expect(sent).toHaveLength(2)
    expect(sent[1]).toEqual(sent[0])
    expect(sent[1]!.operationId).toBe(operationId)
    expect(crypto.randomUUID).toHaveBeenCalledTimes(1)
    expect(wrapper.get('h1').text()).toBe('Đơn bán đã hoàn tất')

    // Confirmed: no stale guard, and leaving works again.
    expect(saleOutcomePending.value).toBe(false)
    expect(unloadIsGuarded()).toBe(false)
    expect(aborted(await leaveTo('/products'))).toBe(false)
    expect(router.currentRoute.value.path).toBe('/products')
  })

  it('refuses to leave while the attempt is completing or checking its outcome', async () => {
    const response = deferred<Sale>()
    const status = deferred<OperationStatus>()
    completeAnswers.push(() => response.promise)
    statusAnswers.push(() => status.promise)
    const { router, form, vm, complete, leaveTo } = await openCheckout()
    await complete('Hoàn tất bán hàng')

    expect(vm().state).toBe('completing')
    const operationId = vm().attempt!.operationId
    expect(aborted(await leaveTo('/products'))).toBe(true)
    expect(form().exists()).toBe(true)
    expect(unloadIsGuarded()).toBe(true)

    response.reject(new ApiError(503, { title: 'Unavailable' }))
    await flushPromises()
    expect(vm().state).toBe('checking')
    expect(aborted(await leaveTo('/products'))).toBe(true)
    expect(router.currentRoute.value.path).toBe('/sales/new')
    expect(vm().attempt!.operationId).toBe(operationId)

    // The lookup finds the Sale: the authoritative Sale is loaded and navigation is released.
    status.resolve({ operationId, status: 'Completed', operationType: 'CompleteSale', resultReference: 'sale-1' } as OperationStatus)
    await flushPromises()
    expect(sent).toHaveLength(1)
    expect(apiRequest).toHaveBeenCalledWith('/api/sales/sale-1')
    expect(unloadIsGuarded()).toBe(false)
    expect(aborted(await leaveTo('/products'))).toBe(false)
  })

  it('releases navigation after a non-ambiguous rejection returns the order to editing', async () => {
    completeAnswers.push(() => Promise.reject(new ApiError(422, { title: 'Không đủ tồn kho.' })))
    const { wrapper, router, vm, complete, leaveTo } = await openCheckout()
    await complete('Hoàn tất bán hàng')

    expect(vm().state).toBe('idle')
    expect(vm().attempt).toBeNull()
    expect(wrapper.text()).toContain('Không đủ tồn kho.')
    expect(saleOutcomePending.value).toBe(false)
    expect(unloadIsGuarded()).toBe(false)
    expect(aborted(await leaveTo('/products'))).toBe(false)
    expect(router.currentRoute.value.path).toBe('/products')
  })

  it('allows normal navigation from the confirmed Sale, including Lịch sử bán hàng', async () => {
    completeAnswers.push(() => Promise.resolve(sale))
    const { wrapper, router, complete } = await openCheckout()
    expect(unloadIsGuarded()).toBe(false)
    await complete('Hoàn tất bán hàng')

    expect(wrapper.get('h1').text()).toBe('Đơn bán đã hoàn tất')
    await wrapper.get('a.sales-complete__history').trigger('click')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/sales')
  })

  it('does not hold the checkout once the session has ended', async () => {
    completeAnswers.push(() => Promise.reject(new TypeError('network lost')))
    statusAnswers.push(() => Promise.reject(new TypeError('network lost')))
    const { vm, complete, leaveTo, router } = await openCheckout()
    await complete('Hoàn tất bán hàng')
    expect(vm().state).toBe('retryable')

    authenticated = false
    expect(aborted(await leaveTo('/products'))).toBe(false)
    expect(router.currentRoute.value.path).toBe('/products')
    // Unmounting the checkout clears its guards.
    expect(saleOutcomePending.value).toBe(false)
    expect(unloadIsGuarded()).toBe(false)
  })
})
