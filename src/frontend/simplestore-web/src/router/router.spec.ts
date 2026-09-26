import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAuthStore } from '../stores/auth'
import router from './index'

describe('router authentication guard', () => {
  beforeEach(async () => {
    setActivePinia(createPinia())
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async () => new Response(JSON.stringify({
      isAuthenticated: false, email: null, storeId: null, roles: [], hasStore: false,
    }), { status: 200 })))
    await router.push('/login')
    useAuthStore().initialized = false
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

  it.each(['/today', '/suppliers/debts', '/reports/end-of-day', '/settings/operations'])(
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

  it('uses Today as the Owner default landing', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      isAuthenticated: true, email: 'owner@test', storeId: 'store-1', roles: ['Owner'], hasStore: true,
    }), { status: 200 })))
    await router.push('/')
    expect(router.currentRoute.value.name).toBe('today')
  })

  it('keeps Products as the Cashier operational landing', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      isAuthenticated: true, email: 'cashier@test', storeId: 'store-1', roles: ['Cashier'], hasStore: true,
    }), { status: 200 })))
    await router.push('/')
    expect(router.currentRoute.value.name).toBe('products')
  })

  it('forces a temporary-password Cashier to change password before any business route', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      isAuthenticated: true, email: 'cashier@test', storeId: 'store-1', roles: ['Cashier'], hasStore: true,
      mustChangePassword: true, isEnabled: true,
    }), { status: 200 })))
    await router.push('/products')
    expect(router.currentRoute.value.name).toBe('change-password')
  })

  it('does not leave a changed-password user on the password-change route', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      isAuthenticated: true, email: 'cashier@test', storeId: 'store-1', roles: ['Cashier'], hasStore: true,
      mustChangePassword: false, isEnabled: true,
    }), { status: 200 })))
    await router.push('/change-password')
    expect(router.currentRoute.value.name).toBe('products')
  })

  it('honors an explicit safe authorized redirect instead of forcing Today', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      isAuthenticated: true, email: 'owner@test', storeId: 'store-1', roles: ['Owner'], hasStore: true,
    }), { status: 200 })))
    await router.push({ name: 'login', query: { redirect: '/reports/end-of-day' } })
    expect(router.currentRoute.value.name).toBe('end-of-day')
  })
})
