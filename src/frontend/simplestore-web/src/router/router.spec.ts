import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import router from './index'

describe('router authentication guard', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      isAuthenticated: false, email: null, storeId: null, roles: [], hasStore: false,
    }), { status: 200 })))
  })

  it('redirects unauthenticated users to login', async () => {
    await router.push('/products')
    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query.redirect).toBe('/products')
  })

  it('redirects a Cashier away from Owner-only correction routes', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      isAuthenticated: true, email: 'cashier@test', storeId: 'store-1', roles: ['Cashier'], hasStore: true,
    }), { status: 200 })))
    await router.push('/sales/sale-1/return')
    expect(router.currentRoute.value.name).toBe('products')
  })

  it.each(['/suppliers/debts', '/reports/end-of-day', '/settings/operations'])(
    'redirects a Cashier away from Owner-only Stage 5B route %s',
    async path => {
      vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
        isAuthenticated: true, email: 'cashier@test', storeId: 'store-1', roles: ['Cashier'], hasStore: true,
      }), { status: 200 })))
      await router.push(path)
      expect(router.currentRoute.value.name).toBe('products')
    },
  )

  it('allows a Cashier to open Customer debt', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      isAuthenticated: true, email: 'cashier@test', storeId: 'store-1', roles: ['Cashier'], hasStore: true,
    }), { status: 200 })))
    await router.push('/customers/debts')
    expect(router.currentRoute.value.name).toBe('customer-debts')
  })
})
