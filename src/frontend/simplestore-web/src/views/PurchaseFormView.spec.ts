import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import PurchaseFormView from './PurchaseFormView.vue'

const push = vi.fn()
let productId: string | undefined = 'product-1'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>()
  return {
    ...actual,
    useRoute: () => ({ params: {}, query: productId ? { productId } : {} }),
    useRouter: () => ({ push }),
  }
})

const request = vi.mocked(apiRequest)
const product = {
  id: 'product-1', sku: 'SKU-1', barcode: null, name: 'C14 product', unit: 'cái',
  salePrice: 10, referencePurchaseCost: null, isActive: true, quantityOnHand: 2,
  inventoryValue: 0, averageCost: 0, hasAverageCost: false,
  referencePurchaseCostRevision: 0, createdAt: '', updatedAt: '',
}
const emptyPage = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }

describe('PurchaseFormView C14 transition', () => {
  beforeEach(() => {
    request.mockReset()
    push.mockReset()
    productId = 'product-1'
  })

  it('loads an active Store-scoped product hint without creating a draft or recommending values', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/products/product-1') return product as never
      if (path.startsWith('/api/products?') || path.startsWith('/api/suppliers?')) return emptyPage as never
      throw new Error(`Unexpected request: ${path}`)
    })

    const wrapper = mount(PurchaseFormView, {
      global: { stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()

    expect(wrapper.text()).toContain('C14 product')
    expect(wrapper.text()).toContain('Chưa chọn nhà cung cấp.')
    expect((wrapper.get('input[aria-label="Số lượng"]').element as HTMLInputElement).value).toBe('')
    expect((wrapper.get('input[aria-label="Giá nhập"]').element as HTMLInputElement).value).toBe('')
    expect(request.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
    expect(push).not.toHaveBeenCalled()
  })

  it('ignores inactive or unavailable product hints without bypassing active search rules', async () => {
    request.mockImplementation(async path => {
      if (path === '/api/products/product-1') return { ...product, isActive: false } as never
      if (path.startsWith('/api/products?') || path.startsWith('/api/suppliers?')) return emptyPage as never
      throw new Error('not found')
    })
    const inactive = mount(PurchaseFormView, {
      global: { stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()
    expect(inactive.text()).not.toContain('C14 product')
    expect(inactive.text()).toContain('Chưa có sản phẩm.')

    request.mockImplementation(async path => {
      if (path === '/api/products/product-1') throw new Error('not found')
      if (path.startsWith('/api/products?') || path.startsWith('/api/suppliers?')) return emptyPage as never
      throw new Error(`Unexpected request: ${path}`)
    })
    const missing = mount(PurchaseFormView, {
      global: { stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()
    expect(missing.text()).toContain('Chưa có sản phẩm.')
  })
})
