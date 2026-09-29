import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App.vue'
import type { Session } from './api/types'
import { useAuthStore } from './stores/auth'

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
    expect(owner.text()).toContain('Hôm nay')
    expect(cashier.text()).not.toContain('Nhà cung cấp')
    expect(cashier.text()).not.toContain('Nhập hàng')
    expect(cashier.text()).not.toContain('Hôm nay')
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
