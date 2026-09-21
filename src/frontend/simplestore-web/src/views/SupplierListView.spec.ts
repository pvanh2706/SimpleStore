import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import SupplierListView from './SupplierListView.vue'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
const mockedApiRequest = vi.mocked(apiRequest)

describe('SupplierListView pagination', () => {
  beforeEach(() => {
    mockedApiRequest.mockReset()
    mockedApiRequest.mockImplementation(async (path) => {
      const page = new URL(`https://local${String(path)}`).searchParams.get('page') ?? '1'
      return { items: [], page: Number(page), pageSize: 20, totalCount: 21, totalPages: 2 }
    })
  })

  it('pages on the server and resets to page one when search changes', async () => {
    const wrapper = mount(SupplierListView)
    await flushPromises()
    await wrapper.findAll('button').find((button) => button.text() === 'Sau')!.trigger('click')
    await flushPromises()
    expect(String(mockedApiRequest.mock.calls.at(-1)?.[0])).toContain('page=2&pageSize=20')

    await wrapper.get('input[aria-label="Tìm nhà cung cấp"]').setValue('An')
    await wrapper.findAll('form')[1].trigger('submit')
    await flushPromises()
    const request = String(mockedApiRequest.mock.calls.at(-1)?.[0])
    expect(request).toContain('page=1&pageSize=20')
    expect(request).toContain('search=An')
  })
})
