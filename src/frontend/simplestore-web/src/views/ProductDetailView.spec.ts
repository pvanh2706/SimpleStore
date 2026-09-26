import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest } from '../api/client'
import { useAuthStore } from '../stores/auth'
import ProductDetailView from './ProductDetailView.vue'

vi.mock('vue-router', async importOriginal => ({
  ...(await importOriginal<typeof import('vue-router')>()),
  useRoute: () => ({ params: { id: 'product-1' } }),
}))
vi.mock('../api/client', async importOriginal => ({
  ...(await importOriginal<typeof import('../api/client')>()),
  apiRequest: vi.fn(),
}))

const request = vi.mocked(apiRequest)
const product = {
  id: 'product-1', sku: 'PRA-1', barcode: null, name: 'Pilot product', unit: 'item',
  salePrice: 100, referencePurchaseCost: 20, isActive: true, quantityOnHand: 10,
  inventoryValue: 200, averageCost: 20, hasAverageCost: true,
}

describe('ProductDetailView pilot inventory actions', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    useAuthStore().session = {
      isAuthenticated: true, email: 'owner@example.test', storeId: 'store-1',
      roles: ['Owner'], hasStore: true, mustChangePassword: false, isEnabled: true,
    }
    request.mockReset()
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => 'operation-1') })
  })

  it('keeps an ambiguous Adjustment attempt immutable and retries the same OperationId', async () => {
    const posts: Array<Record<string, unknown>> = []
    request.mockImplementation(async (path, init) => {
      if (String(path) === '/api/products/product-1') return product as never
      if (String(path) === '/api/products/product-1/movements') return [] as never
      if (String(path) === '/api/inventory/adjustments' && init?.method === 'POST') {
        posts.push(JSON.parse(String(init.body)))
        if (posts.length === 1) throw new TypeError('lost response')
        return {
          quantityDelta: 2, costReliability: 'Reliable', wasAlreadyCompleted: true,
        } as never
      }
      throw new Error(`Unexpected request ${String(path)}`)
    })
    const wrapper = mount(ProductDetailView, { global: { stubs: { RouterLink: true } } })
    await flushPromises()
    await wrapper.get('#adjustment-quantity').setValue('2')
    await wrapper.get('#adjustment-reason').setValue('Count correction')
    await wrapper.findAll('form')[0].trigger('submit')
    await flushPromises()

    expect(posts).toHaveLength(1)
    expect(wrapper.get('#adjustment-quantity').attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Giữ nguyên phiếu này')

    await wrapper.findAll('form')[0].trigger('submit')
    await flushPromises()
    expect(posts).toHaveLength(2)
    expect(posts[0]).toEqual(posts[1])
    expect(posts[1]).toMatchObject({ operationId: 'operation-1', quantityDelta: 2, reason: 'Count correction' })
  })

  it('clears a stale Stocktake and requires refresh and recount', async () => {
    request.mockImplementation(async (path, init) => {
      if (String(path) === '/api/products/product-1') return product as never
      if (String(path) === '/api/products/product-1/movements') return [] as never
      if (String(path).includes('/stocktakes/context/')) {
        return {
          productId: 'product-1', productName: 'Pilot product', productSku: 'PRA-1', unit: 'item',
          expectedQuantity: 10, expectedRevision: 'AAAAAAAAAAA=', hasAverageCost: true, averageCost: 20,
        } as never
      }
      if (String(path) === '/api/inventory/stocktakes' && init?.method === 'POST') {
        throw new ApiError(409, { code: 'stocktake-stale', title: 'stale' })
      }
      throw new Error(`Unexpected request ${String(path)}`)
    })
    const wrapper = mount(ProductDetailView, { global: { stubs: { RouterLink: true } } })
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text().includes('Bắt đầu kiểm kho'))!.trigger('click')
    await flushPromises()
    expect(wrapper.get('#counted-quantity').attributes('min')).toBe('0')
    expect(wrapper.get('#counted-quantity').attributes('step')).toBe('0.001')
    await wrapper.get('#counted-quantity').setValue('9')
    await wrapper.findAll('form')[1].trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('Tồn kho đã thay đổi trong lúc đếm')
    expect(wrapper.find('#counted-quantity').exists()).toBe(false)
    expect(wrapper.findAll('button').some(button => button.text().includes('Bắt đầu kiểm kho'))).toBe(true)
  })
})
