import { flushPromises, mount } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import { createMemoryHistory, createRouter, RouterView } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest } from '../api/client'
import { settingsDemoEnabled } from '../settings/demo'
import OperationalSettingsView from './OperationalSettingsView.vue'

vi.mock('../api/client', async importOriginal => {
  const actual = await importOriginal<typeof import('../api/client')>()
  return { ...actual, apiRequest: vi.fn() }
})
const request = vi.mocked(apiRequest)
const mounted: Array<{ unmount: () => void }> = []
let puts: Array<{ path: string; body: unknown }> = []
let failTimezone = false

async function mountView(path = '/settings/operations') {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/settings/operations', component: OperationalSettingsView },
      { path: '/today', component: { template: '<p>Today</p>' } },
    ],
  })
  await router.push(path)
  await router.isReady()
  const wrapper = mount(defineComponent({ render: () => h(RouterView) }), { attachTo: document.body, global: { plugins: [router] } })
  mounted.push(wrapper)
  await flushPromises()
  return { wrapper, router }
}
type Wrapper = Awaited<ReturnType<typeof mountView>>['wrapper']
const button = (wrapper: Wrapper, text: string) => wrapper.findAll('button').find(candidate => candidate.text() === text)!
const page = (wrapper: Wrapper) => wrapper.get('.settings-page')

beforeEach(() => {
  puts = []
  failTimezone = false
  request.mockReset()
  request.mockImplementation(async (path, init) => {
    if (init?.method === 'PUT') {
      puts.push({ path: String(path), body: JSON.parse(String(init.body)) })
      if (path === '/api/store/timezone' && failTimezone) throw new ApiError(400, { title: 'Múi giờ không hợp lệ.' })
      return JSON.parse(String(init.body)) as never
    }
    if (path === '/api/store/operational-settings') return { allowNegativeStock: false } as never
    if (path === '/api/store/current') return { id: 'store-1', name: 'Tạp hóa Việt Anh', mainWarehouseId: 'w', mainWarehouseName: 'Kho chính', timeZoneId: 'Asia/Ho_Chi_Minh' } as never
    throw new Error(`Unexpected ${String(path)}`)
  })
})
afterEach(() => {
  settingsDemoEnabled.value = false
  mounted.forEach(wrapper => wrapper.unmount())
  mounted.length = 0
  vi.restoreAllMocks()
})

describe('OperationalSettingsView with live data', () => {
  it('opens Giao diện with the real Store name and no appearance persistence', async () => {
    const { wrapper } = await mountView()

    expect(wrapper.findAll('.settings-tab').map(tab => tab.text())).toEqual(['Giao diện', 'Vận hành'])
    expect(wrapper.get('.settings-tab.active').text()).toBe('Giao diện')
    expect(wrapper.get('h1').text()).toBe('Giao diện')
    const name = wrapper.get<HTMLInputElement>('#settings-store-name')
    expect(name.element.value).toBe('Tạp hóa Việt Anh')
    expect(name.attributes('readonly')).toBeDefined()
    expect(button(wrapper, '✓ Lưu thay đổi').attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Lưu giao diện chưa được hệ thống hỗ trợ.')
    expect(wrapper.find('.settings-logo-box').element.tagName).toBe('DIV')
    expect(wrapper.text()).not.toContain('Thay đổi ảnh')
    expect(wrapper.get('.settings-login-name').text()).toBe('Tạp hóa Việt Anh')
    expect(wrapper.get('.settings-receipt').text()).not.toContain('Nguyễn Văn Cừ')

    await wrapper.get('[aria-label="Ocean"]').trigger('click')
    expect(page(wrapper).attributes('style')).toContain('--accent: #347fd3')
    await button(wrapper, '↶ Khôi phục mặc định').trigger('click')
    expect(page(wrapper).attributes('style')).toContain('--accent: #0b9a68')
    expect(puts).toEqual([])
  })

  it('saves only the changed operational setting with an explicit Save', async () => {
    const { wrapper } = await mountView('/settings/operations?section=operations')
    expect(wrapper.get('h1').text()).toBe('Vận hành')
    const save = () => button(wrapper, '✓ Lưu thay đổi')
    const toggle = wrapper.get('[role="switch"]')
    expect(toggle.text()).toContain('Cho phép bán khi tồn kho không đủ')
    expect(toggle.attributes('aria-checked')).toBe('false')
    expect(save().attributes('disabled')).toBeDefined()

    await toggle.trigger('click')
    expect(toggle.attributes('aria-checked')).toBe('true')
    expect(wrapper.text()).toContain('tồn kho âm và độ tin cậy giá vốn có thể bị ảnh hưởng')
    expect(wrapper.find('.settings-dirty-dot').exists()).toBe(true)
    await save().trigger('click')
    await flushPromises()

    expect(puts).toEqual([{ path: '/api/store/operational-settings/negative-stock', body: { allowNegativeStock: true } }])
    expect(wrapper.get('.settings-toast').text()).toBe('Đã lưu thiết lập vận hành.')
    expect(save().attributes('disabled')).toBeDefined()
    expect(wrapper.find('.settings-dirty-dot').exists()).toBe(false)
  })

  it('explains a timezone change, keeps input after a failed save and restores on cancel', async () => {
    const { wrapper } = await mountView('/settings/operations?section=operations')
    const input = wrapper.get<HTMLInputElement>('#settings-timezone')
    expect(wrapper.text()).toContain('Hiện tại: GMT+7.')
    expect(wrapper.find('.settings-warn-note').exists()).toBe(false)

    await input.setValue('Asia/Tokyo')
    expect(wrapper.text()).toContain('Hiện tại: GMT+9.')
    expect(wrapper.get('.settings-warn-note').text()).toContain('nhóm theo ngày kinh doanh')
    failTimezone = true
    await button(wrapper, '✓ Lưu thay đổi').trigger('click')
    await flushPromises()
    expect(puts).toEqual([{ path: '/api/store/timezone', body: { timeZoneId: 'Asia/Tokyo' } }])
    expect(wrapper.get('[role="alert"]').text()).toBe('Múi giờ không hợp lệ. Thông tin bạn nhập vẫn được giữ để thử lại.')
    expect(input.element.value).toBe('Asia/Tokyo')

    await button(wrapper, 'Hủy thay đổi').trigger('click')
    expect(input.element.value).toBe('Asia/Ho_Chi_Minh')
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
  })

  it('asks before leaving with unsaved operational changes', async () => {
    const { wrapper, router } = await mountView('/settings/operations?section=operations')
    await wrapper.get('[role="switch"]').trigger('click')
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false)

    await router.push('/settings/operations')
    expect(wrapper.get('h1').text()).toBe('Giao diện')
    expect(confirm).not.toHaveBeenCalled()
    await router.push('/today')
    expect(confirm).toHaveBeenCalledOnce()
    expect(router.currentRoute.value.path).toBe('/settings/operations')

    confirm.mockReturnValue(true)
    await router.push('/today')
    expect(router.currentRoute.value.path).toBe('/today')
  })
})

describe('OperationalSettingsView with reference data', () => {
  it('reproduces the reference Giao diện screen and its interactions without calling the API', async () => {
    settingsDemoEnabled.value = true
    const { wrapper } = await mountView('/settings/operations?section=operations')

    expect(request).not.toHaveBeenCalled()
    expect(wrapper.find('.settings-tabs').exists()).toBe(false)
    expect(wrapper.get('h1').text()).toBe('Giao diện')
    expect(wrapper.get<HTMLInputElement>('#settings-store-name').element.value).toBe('Tạp hóa Minh Anh')
    expect(wrapper.get('.settings-swatch.active').attributes('aria-label')).toBe('Emerald')
    expect(wrapper.get('.settings-seg.active').text()).toBe('☀ Sáng')
    expect(wrapper.get('.settings-choice.active').text()).toContain('Gọn')
    expect(wrapper.findAll('[role="switch"]').map(item => item.attributes('aria-checked'))).toEqual(['true', 'true', 'true'])
    expect(page(wrapper).classes()).toContain('tabular')
    expect(page(wrapper).attributes('style')).toContain('--control-h: 36px')
    expect(wrapper.findAll('.settings-mini-product').map(item => item.text())).toEqual([
      'Coca Cola 330ml8.000đ', 'Sting dâu 330ml7.000đ', 'Bia Sài Gòn lon15.000đ', 'Nước suối Lavie 500ml6.000đ', 'Mì Hảo Hảo4.000đ',
      'Dầu ăn Tường An 1L42.000đ', 'Đường cát trắng 1kg22.000đ', 'Nước mắm Nam Ngư28.000đ', 'Bánh Oreo12.000đ',
    ])
    expect(wrapper.get('.settings-cart-total').text()).toBe('Tạm tính (4 món) 62.000đ')
    expect(wrapper.get('.settings-receipt').text()).toContain('123 Nguyễn Văn Cừ, P. An Hòa, Q. Ninh Kiều, Cần Thơ')
    expect(wrapper.get('.settings-receipt').text()).toContain('TỔNG CỘNG:62.000đ')

    await wrapper.get('[aria-label="Terracotta"]').trigger('click')
    expect(page(wrapper).attributes('style')).toContain('--accent: #eb764f')
    await button(wrapper, '☾ Tối').trigger('click')
    expect(page(wrapper).classes()).toContain('dark')
    await wrapper.findAll('.settings-choice')[2]!.trigger('click')
    expect(page(wrapper).attributes('style')).toContain('--control-h: 48px')
    await wrapper.findAll('[role="switch"]')[0]!.trigger('click')
    expect(wrapper.get('.settings-receipt-logo').attributes('style')).toContain('visibility: hidden')
    await wrapper.findAll('[role="switch"]')[1]!.trigger('click')
    expect(page(wrapper).classes()).toContain('square')
    await wrapper.get('#settings-store-name').setValue('Tạp hóa An Bình')
    expect(wrapper.get('.settings-login-name').text()).toBe('Tạp hóa An Bình')
    expect(wrapper.get('.settings-receipt h3').text()).toBe('Tạp hóa An Bình')
    await wrapper.get('.settings-logo-box').trigger('click')
    expect(wrapper.get('.settings-login-logo').text()).toBe('MA')
    await button(wrapper, '↥ Thay đổi ảnh').trigger('click')
    expect(wrapper.get('.settings-login-photo').classes()).toContain('alternate')

    await button(wrapper, '✓ Lưu thay đổi').trigger('click')
    expect(wrapper.get('.settings-toast').text()).toBe('Đã lưu thay đổi trong dữ liệu mẫu')
    await button(wrapper, '↶ Khôi phục mặc định').trigger('click')
    expect(wrapper.get('.settings-toast').text()).toBe('Đã khôi phục mặc định')
    expect(wrapper.get('.settings-swatch.active').attributes('aria-label')).toBe('Emerald')
    expect(page(wrapper).classes()).not.toContain('dark')
    expect(wrapper.get<HTMLInputElement>('#settings-store-name').element.value).toBe('Tạp hóa Minh Anh')
    expect(request).not.toHaveBeenCalled()

    settingsDemoEnabled.value = false
    await flushPromises()
    expect(request).toHaveBeenCalledWith('/api/store/current')
    expect(wrapper.get('.settings-tab.active').text()).toBe('Vận hành')
  })
})
