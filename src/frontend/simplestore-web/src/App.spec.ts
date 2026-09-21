import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'
import App from './App.vue'
import { useAuthStore } from './stores/auth'

async function mountForRole(role: 'Owner' | 'Cashier') {
  const pinia = createPinia()
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  })
  const auth = useAuthStore(pinia)
  auth.session = { isAuthenticated: true, email: 'user@test', storeId: 'store', roles: [role], hasStore: true }
  auth.initialized = true
  await router.push('/'); await router.isReady()
  return mount(App, { global: { plugins: [pinia, router] } })
}

describe('App navigation', () => {
  it('shows purchasing navigation only to Owner', async () => {
    const owner = await mountForRole('Owner')
    const cashier = await mountForRole('Cashier')

    expect(owner.text()).toContain('Nhà cung cấp')
    expect(owner.text()).toContain('Nhập hàng')
    expect(cashier.text()).not.toContain('Nhà cung cấp')
    expect(cashier.text()).not.toContain('Nhập hàng')
  })
})
