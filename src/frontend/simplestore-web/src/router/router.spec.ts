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
})
