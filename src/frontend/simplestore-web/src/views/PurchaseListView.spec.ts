import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import PurchaseListView from './PurchaseListView.vue'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
const mockedApiRequest = vi.mocked(apiRequest)

describe('PurchaseListView pagination', () => {
  beforeEach(() => {
    mockedApiRequest.mockReset()
    mockedApiRequest.mockImplementation(async (path) => {
      const page = new URL(`https://local${String(path)}`).searchParams.get('page') ?? '1'
      return { items: [], page: Number(page), pageSize: 20, totalCount: 21, totalPages: 2 }
    })
  })

  it('resets filters to page one and preserves them while paging', async () => {
    const wrapper = mount(PurchaseListView, { global: { stubs: { RouterLink: true } } })
    await flushPromises()
    await wrapper.get('select').setValue('Completed')
    await wrapper.get('form').trigger('submit')
    await flushPromises()
    let request = String(mockedApiRequest.mock.calls.at(-1)?.[0])
    expect(request).toContain('page=1&pageSize=20')
    expect(request).toContain('status=Completed')

    await wrapper.findAll('button').find((button) => button.text() === 'Sau')!.trigger('click')
    await flushPromises()
    request = String(mockedApiRequest.mock.calls.at(-1)?.[0])
    expect(request).toContain('page=2&pageSize=20')
    expect(request).toContain('status=Completed')
  })
})
