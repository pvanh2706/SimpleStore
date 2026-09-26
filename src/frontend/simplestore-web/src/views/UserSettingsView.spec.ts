import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import UserSettingsView from './UserSettingsView.vue'

vi.mock('../api/client', async importOriginal => ({
  ...(await importOriginal<typeof import('../api/client')>()),
  apiRequest: vi.fn(),
}))

const request = vi.mocked(apiRequest)
const cashier = {
  id: 'cashier-1', email: 'cashier@example.test', isEnabled: true,
  mustChangePassword: true, disabledAt: null,
  passwordChangeRequiredAt: '2026-09-26T00:00:00Z', passwordChangedAt: null,
}

describe('UserSettingsView', () => {
  beforeEach(() => {
    request.mockReset()
    vi.stubGlobal('confirm', vi.fn(() => true))
  })

  it('creates a Store-scoped Cashier and displays the temporary password once', async () => {
    let loads = 0
    request.mockImplementation(async (path, init) => {
      if (String(path) === '/api/users/cashiers' && !init?.method) {
        loads += 1
        return (loads === 1 ? [] : [cashier]) as never
      }
      if (String(path) === '/api/users/cashiers' && init?.method === 'POST') {
        return { ...cashier, temporaryPassword: 'One-Time!Secret2026', wasAlreadyCompleted: false } as never
      }
      throw new Error(`Unexpected request ${String(path)}`)
    })

    const wrapper = mount(UserSettingsView)
    await flushPromises()
    await wrapper.get('#cashier-email').setValue(cashier.email)
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[data-testid="temporary-password"]').text()).toBe('One-Time!Secret2026')
    expect(wrapper.text()).toContain(cashier.email)
    expect(wrapper.text()).toContain('Đang chờ đổi mật khẩu')
    expect(JSON.parse(String(request.mock.calls.find(call => call[1]?.method === 'POST')![1]?.body)))
      .toEqual({ email: cashier.email })

    await wrapper.findAll('button').find(button => button.text().includes('ẩn mật khẩu'))!.trigger('click')
    expect(wrapper.find('[data-testid="temporary-password"]').exists()).toBe(false)
  })

  it('requires confirmation for reset and disable and refreshes authoritative state', async () => {
    request.mockImplementation(async (path, init) => {
      if (!init?.method) return [cashier] as never
      if (String(path).endsWith('/credentials/reset')) {
        return { ...cashier, temporaryPassword: 'Reset!Secret2026', wasAlreadyCompleted: false } as never
      }
      if (String(path).endsWith('/disable')) return { ...cashier, isEnabled: false } as never
      throw new Error(`Unexpected request ${String(path)}`)
    })
    const wrapper = mount(UserSettingsView)
    await flushPromises()

    await wrapper.findAll('button').find(button => button.text().includes('Cấp lại'))!.trigger('click')
    await flushPromises()
    expect(wrapper.get('[data-testid="temporary-password"]').text()).toBe('Reset!Secret2026')

    await wrapper.findAll('button').find(button => button.text().includes('Vô hiệu hóa'))!.trigger('click')
    await flushPromises()
    expect(confirm).toHaveBeenCalledTimes(2)
    expect(request.mock.calls.some(call => String(call[0]).endsWith('/disable'))).toBe(true)
  })
})
