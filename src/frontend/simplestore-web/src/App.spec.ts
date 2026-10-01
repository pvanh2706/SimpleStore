import { flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App.vue'
import type { Session } from './api/types'
import { useAuthStore } from './stores/auth'
import { liveOrderBook } from './sales/orders'

async function mountForSession(session: Session, initialized = true) {
  const pinia = createPinia()
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  })
  const auth = useAuthStore(pinia)
  auth.session = session
  auth.initialized = initialized
  await router.push('/'); await router.isReady()
  return mount(App, { global: { plugins: [pinia, router] } })
}

const storeSession = (role: 'Owner' | 'Cashier'): Session => ({
  isAuthenticated: true, email: 'user@test', storeId: 'store', roles: [role], hasStore: true,
  mustChangePassword: false, isEnabled: true,
})

describe('App navigation', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      id: 'store', name: 'Tạp hóa Việt Anh', timeZoneId: 'Asia/Ho_Chi_Minh',
    }), { status: 200 })))
  })

  it('shows purchasing navigation only to Owner', async () => {
    const owner = await mountForSession(storeSession('Owner'))
    const cashier = await mountForSession(storeSession('Cashier'))

    expect(owner.text()).toContain('Nhà cung cấp')
    expect(owner.text()).toContain('Nhập hàng')
    expect(owner.text()).toContain('Tổng quan')
    expect(cashier.text()).not.toContain('Nhà cung cấp')
    expect(cashier.text()).not.toContain('Nhập hàng')
    expect(cashier.text()).not.toContain('Tổng quan')
  })

  it('keeps the business navigation hidden while the session is unresolved', async () => {
    const wrapper = await mountForSession(storeSession('Owner'), false)
    expect(wrapper.find('[role="status"]').attributes('aria-label')).toBe('Đang tải ứng dụng')
    expect(wrapper.find('nav').exists()).toBe(false)
  })

  it('does not expose Store navigation during setup or forced password change', async () => {
    const setup = await mountForSession({ ...storeSession('Owner'), hasStore: false, storeId: null })
    const password = await mountForSession({ ...storeSession('Cashier'), mustChangePassword: true })

    expect(setup.find('.app-sidebar').exists()).toBe(false)
    expect(password.find('.app-sidebar').exists()).toBe(false)
    expect(password.text()).toContain('Bạn phải đổi mật khẩu tạm thời')
  })
})

describe('live Sales working order across the authenticated session (D-107)', () => {
  const product = { id: 'p1', sku: 'SKU-1', barcode: null, name: 'Coffee', unit: 'gói', salePrice: 12000, isActive: true, quantityOnHand: 5 }
  const customer = { id: 'c1', name: 'An', phone: null, createdAt: '', updatedAt: '' }
  let logoutStatus = 204

  beforeEach(() => {
    logoutStatus = 204
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)
      if (url === '/api/security/antiforgery') return new Response(JSON.stringify({ requestToken: 'token' }), { status: 200 })
      if (url === '/api/auth/logout') return new Response(logoutStatus === 204 ? null : '{}', { status: logoutStatus })
      return new Response(JSON.stringify({ id: 'store', name: 'Tạp hóa Việt Anh', timeZoneId: 'Asia/Ho_Chi_Minh' }), { status: 200 })
    }))
  })
  afterEach(() => liveOrderBook.reset())

  async function mountSignedIn() {
    const pinia = createPinia()
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div />' } },
        { path: '/products', component: { template: '<div />' } },
        { path: '/login', name: 'login', component: { template: '<div />' } },
      ],
    })
    const auth = useAuthStore(pinia)
    auth.session = storeSession('Cashier')
    auth.initialized = true
    await router.push('/'); await router.isReady()
    const wrapper = mount(App, { global: { plugins: [pinia, router] } })
    const order = liveOrderBook.active.value
    order.cart.push({ product, quantity: 2 })
    order.customer = customer
    order.payments.push({ amount: 5000, method: 'Cash' })
    order.paymentIntent = 'explicit-payments'
    return { wrapper, router, auth }
  }

  it('keeps the order on route changes and clears it after a successful logout', async () => {
    const { wrapper, router, auth } = await mountSignedIn()
    await router.push('/products')
    await flushPromises()
    expect(liveOrderBook.active.value.cart).toHaveLength(1)
    expect(liveOrderBook.active.value.customer).toEqual(customer)

    await wrapper.get('.app-sidebar .app-account-area button').trigger('click')
    await flushPromises()
    expect(auth.session.isAuthenticated).toBe(false)
    expect(router.currentRoute.value.name).toBe('login')
    const order = liveOrderBook.active.value
    expect(order.cart).toEqual([])
    expect(order.customer).toBeNull()
    expect(order.payments).toEqual([])
    expect(order.paymentIntent).toBe('full-payment')
    expect(order.payMode).toBe('Cash')
  })

  it('keeps both the session and the order when the logout request fails', async () => {
    const { auth } = await mountSignedIn()
    logoutStatus = 500
    await expect(auth.logout()).rejects.toThrow()
    await flushPromises()
    expect(auth.session.isAuthenticated).toBe(true)
    expect(liveOrderBook.active.value.cart).toHaveLength(1)
  })

  it('clears the order when the session expires or another user signs in', async () => {
    const { auth } = await mountSignedIn()
    auth.session = { ...storeSession('Cashier'), email: 'other@test' }
    await flushPromises()
    expect(liveOrderBook.active.value.cart).toEqual([])

    liveOrderBook.active.value.cart.push({ product, quantity: 1 })
    auth.$reset()
    await flushPromises()
    expect(liveOrderBook.active.value.cart).toEqual([])
  })
})
