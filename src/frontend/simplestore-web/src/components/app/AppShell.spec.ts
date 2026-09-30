import { mount, flushPromises } from '@vue/test-utils'
import { nextTick } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AppShell from './AppShell.vue'
import { salesDemoEnabled } from '../../sales/demo'

const mounted: Array<{ unmount: () => void }> = []

beforeEach(() => {
  document.body.style.overflow = ''
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
    id: 'store-1', name: 'Tạp hóa Việt Anh', mainWarehouseId: 'warehouse-1',
    mainWarehouseName: 'Kho chính', timeZoneId: 'Asia/Bangkok',
  }), { status: 200 })))
})

afterEach(() => {
  salesDemoEnabled.value = false
  mounted.forEach(wrapper => wrapper.unmount())
  mounted.length = 0
  document.body.style.overflow = ''
  vi.unstubAllGlobals()
})

async function mountShell(role: 'Owner' | 'Cashier', path = '/products') {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/:pathMatch(.*)*', component: { template: '<div />' } }],
  })
  await router.push(path)
  await router.isReady()
  const wrapper = mount(AppShell, {
    props: { email: `${role.toLowerCase()}@example.test`, roles: [role] },
    slots: { default: '<p>Workspace content</p>' },
    attachTo: document.body,
    global: { plugins: [router] },
  })
  mounted.push(wrapper)
  await flushPromises()
  return { wrapper, router }
}

function sidebarLinks(wrapper: Awaited<ReturnType<typeof mountShell>>['wrapper']) {
  return wrapper.findAll('.app-sidebar-body a').map(link => link.attributes('href'))
}

function activeSidebarLink(wrapper: Awaited<ReturnType<typeof mountShell>>['wrapper']) {
  return wrapper.find('.app-sidebar-body a[aria-current="page"]').attributes('href')
}

describe('AppShell', () => {
  it('toggles browser-only sample data from the sales header', async () => {
    const { wrapper } = await mountShell('Owner', '/sales/new')
    const toggle = wrapper.get('.app-topbar button.app-mock-toggle')
    expect(salesDemoEnabled.value).toBe(false)
    expect(toggle.attributes('aria-pressed')).toBe('false')
    await toggle.trigger('click')
    expect(salesDemoEnabled.value).toBe(true)
    expect(toggle.attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('.app-topbar').text()).toContain('Việt Anh')
    expect(wrapper.get('.app-topbar').text()).toContain('Cửa hàng Tạp Hóa Việt Anh')
    await toggle.trigger('click')
    expect(salesDemoEnabled.value).toBe(false)
    expect(wrapper.get('.app-topbar').text()).toContain('owner')
  })

  it('shows the sample-data button only on the sales workspace', async () => {
    const { wrapper } = await mountShell('Owner', '/products')
    expect(wrapper.find('.app-mock-toggle').exists()).toBe(false)
  })
  it('renders supported Owner links and actual Store/account identity', async () => {
    const { wrapper } = await mountShell('Owner', '/today')

    expect(sidebarLinks(wrapper)).toEqual([
      '/today', '/sales/new', '/purchases', '/products', '/products?view=inventory', '/customers/debts',
      '/reports/end-of-day', '/settings/operations', '/sales', '/import',
      '/suppliers', '/suppliers/debts', '/settings/users',
    ])
    expect(wrapper.find('.app-sidebar .app-store-name').text()).toBe('Tạp hóa Việt Anh')
    expect(wrapper.find('.app-sidebar .app-account-area').text()).toContain('owner@example.test')
    expect(wrapper.find('.app-sidebar .app-account-area').text()).toContain('Chủ cửa hàng')
    expect(activeSidebarLink(wrapper)).toBe('/today')

    await wrapper.find('.app-sidebar .app-account-area button').trigger('click')
    expect(wrapper.emitted('logout')).toHaveLength(1)
  })

  it('omits Owner-only links for Cashier', async () => {
    const { wrapper } = await mountShell('Cashier')

    expect(sidebarLinks(wrapper)).toEqual(['/sales/new', '/products', '/products?view=inventory', '/customers/debts', '/sales'])
    expect(wrapper.find('.app-sidebar .app-account-area').text()).toContain('cashier@example.test')
    expect(wrapper.find('.app-sidebar .app-account-area').text()).toContain('Thu ngân')
  })

  it('marks nested Product, Purchase and Sale flows active in the intended context', async () => {
    const { wrapper, router } = await mountShell('Owner', '/products/sku-1/edit')
    expect(activeSidebarLink(wrapper)).toBe('/products')

    await router.push('/products?view=inventory')
    await nextTick()
    expect(activeSidebarLink(wrapper)).toBe('/products?view=inventory')

    await router.push('/purchases/purchase-1/edit')
    await nextTick()
    expect(activeSidebarLink(wrapper)).toBe('/purchases')

    await router.push('/sales/new')
    await nextTick()
    expect(activeSidebarLink(wrapper)).toBe('/sales/new')

    await router.push('/sales/sale-1/return')
    await nextTick()
    expect(activeSidebarLink(wrapper)).toBe('/sales')

    await router.push('/settings/users')
    await nextTick()
    expect(activeSidebarLink(wrapper)).toBe('/settings/users')
  })

  it('opens a modal mobile drawer, locks background scroll, and closes on Escape', async () => {
    const { wrapper } = await mountShell('Cashier')
    document.body.style.overflow = 'auto'
    const trigger = wrapper.find('button[aria-label="Mở điều hướng"]')
    await trigger.trigger('click')
    await nextTick()

    expect(trigger.attributes('aria-expanded')).toBe('true')
    expect(wrapper.find('#app-mobile-menu[role="dialog"][aria-modal="true"]').exists()).toBe(true)
    expect(wrapper.find('.app-workspace').attributes()).toHaveProperty('inert')
    expect(document.body.style.overflow).toBe('hidden')

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))
    await nextTick()
    expect(wrapper.find('#app-mobile-menu').exists()).toBe(false)
    expect(trigger.attributes('aria-expanded')).toBe('false')
    expect(document.body.style.overflow).toBe('auto')
  })

  it('closes the mobile drawer and restores background on route selection', async () => {
    const { wrapper, router } = await mountShell('Cashier', '/products')
    await wrapper.find('button[aria-label="Mở điều hướng"]').trigger('click')
    await nextTick()
    const drawer = wrapper.find('#app-mobile-menu')
    expect(drawer.findAll('nav a').map(link => link.attributes('href'))).toEqual(
      ['/sales/new', '/products', '/products?view=inventory', '/customers/debts', '/sales'],
    )

    await drawer.find('a[href="/sales/new"]').trigger('click')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/sales/new')
    expect(wrapper.find('#app-mobile-menu').exists()).toBe(false)
    expect(document.body.style.overflow).toBe('')
    expect(activeSidebarLink(wrapper)).toBe('/sales/new')
  })

  it('releases the drawer lock when the viewport expands to desktop width', async () => {
    const { wrapper } = await mountShell('Cashier')
    await wrapper.find('button[aria-label="Mở điều hướng"]').trigger('click')
    await nextTick()
    expect(document.body.style.overflow).toBe('hidden')

    window.dispatchEvent(new Event('resize'))
    await nextTick()
    expect(wrapper.find('#app-mobile-menu').exists()).toBe(false)
    expect(document.body.style.overflow).toBe('')
  })
})
