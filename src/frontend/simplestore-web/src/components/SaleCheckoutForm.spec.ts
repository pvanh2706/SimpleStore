import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import SaleCheckoutForm from './SaleCheckoutForm.vue'
import type { CustomerPage, ProductListItem, ProductPage, Sale } from '../api/types'

const product: ProductListItem = {
  id: 'product-101', sku: 'SKU-101', barcode: '893000101', name: 'Coffee', unit: 'pack',
  salePrice: 12000, isActive: true, quantityOnHand: 10,
}
const productPage = (items = [product], page = 1, totalPages = 1): ProductPage => ({
  items, page, pageSize: 20, totalCount: totalPages * 20, totalPages,
})
const customerPage: CustomerPage = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }
const sale: Sale = {
  id: 'sale-1', status: 'Completed', storeName: 'Store', warehouseId: 'warehouse-1', customer: null,
  cashierDisplayName: 'cashier@test', lines: [], payments: [], totalAmount: 12000, paidAmount: 12000,
  outstandingAmount: 0, createdAt: '2026-09-21T12:00:00Z', completedAt: '2026-09-21T12:00:00Z', wasAlreadyCompleted: false,
  originalTotalAmount: 12000, totalReturnedAmount: 0, netSaleAmount: 12000,
  originalCollectedAmount: 12000, totalRefundedAmount: 0, netCollectedAmount: 12000,
  isVoided: false, void: null, returns: [],
}

function mountForm(overrides: Record<string, unknown> = {}) {
  return mount(SaleCheckoutForm, {
    props: {
      allowNegativeStock: false,
      searchProducts: vi.fn().mockResolvedValue(productPage()),
      searchCustomers: vi.fn().mockResolvedValue(customerPage),
      createCustomer: vi.fn(),
      completeSale: vi.fn().mockResolvedValue(sale),
      checkOperation: vi.fn().mockResolvedValue(null),
      loadSale: vi.fn().mockResolvedValue(sale),
      ...overrides,
    },
  })
}

async function addProduct(wrapper: ReturnType<typeof mountForm>, search = 'Coffee') {
  await wrapper.get('[aria-label="Tìm hoặc quét sản phẩm"]').setValue(search)
  await wrapper.get('form').trigger('submit')
  await flushPromises()
  await wrapper.get('[aria-label="Thêm sản phẩm Coffee"]').trigger('click')
}

async function fullyPay(wrapper: ReturnType<typeof mountForm>) {
  await wrapper.get('[aria-label="Số tiền thanh toán"]').setValue('12000')
  const add = wrapper.findAll('button').find(button => button.text() === 'Thêm thanh toán')!
  await add.trigger('click')
}

describe('SaleCheckoutForm', () => {
  beforeEach(() => {
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => 'operation-1') })
  })

  it('uses server-side paginated search for a product outside the first page and barcode search', async () => {
    const searchProducts = vi.fn().mockResolvedValue(productPage([product], 6, 6))
    const wrapper = mountForm({ searchProducts })

    await addProduct(wrapper, '893000101')

    expect(searchProducts).toHaveBeenCalledWith('893000101', 1)
    expect(wrapper.text()).toContain('Coffee')
    expect((wrapper.vm as unknown as { cart: unknown[] }).cart).toHaveLength(1)
  })

  it('prevents duplicate products and calculates cart, payment and outstanding previews', async () => {
    const wrapper = mountForm()
    await addProduct(wrapper)
    await wrapper.get('[aria-label="Thêm sản phẩm Coffee"]').trigger('click')
    expect(wrapper.text()).toContain('Sản phẩm đã có trong giỏ hàng.')

    await wrapper.get('[aria-label="Số lượng Coffee"]').setValue('2')
    await wrapper.get('[aria-label="Số tiền thanh toán"]').setValue('10000')
    await wrapper.findAll('button').find(button => button.text() === 'Thêm thanh toán')!.trigger('click')

    const vm = wrapper.vm as unknown as { total: number; paid: number; outstanding: number }
    expect(vm.total).toBe(24000)
    expect(vm.paid).toBe(10000)
    expect(vm.outstanding).toBe(14000)
    await wrapper.get('[aria-label="Xóa Coffee"]').trigger('click')
    expect((wrapper.vm as unknown as { cart: unknown[] }).cart).toHaveLength(0)
  })

  it('requires a customer for credit and allows searching and selecting one', async () => {
    const customer = { id: 'customer-1', name: 'An', phone: '0909', createdAt: '', updatedAt: '' }
    const searchCustomers = vi.fn().mockResolvedValue({ ...customerPage, items: [customer], totalCount: 1, totalPages: 1 })
    const wrapper = mountForm({ searchCustomers })
    await addProduct(wrapper)

    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    expect(wrapper.text()).toContain('Chọn khách hàng khi đơn còn công nợ.')
    await wrapper.get('[aria-label="Tìm khách hàng"]').setValue('0909')
    await wrapper.findAll('form')[1].trigger('submit')
    await flushPromises()
    await wrapper.get('[aria-label="Chọn khách hàng An"]').trigger('click')
    expect(searchCustomers).toHaveBeenCalledWith('0909', 1)
  })

  it('keeps the exact operation and payment snapshot after ambiguous failure and on retry', async () => {
    const attempts: unknown[] = []
    const completeSale = vi.fn(async (attempt) => {
      attempts.push(structuredClone(attempt))
      if (attempts.length === 1) throw new TypeError('network lost')
      return sale
    })
    const checkOperation = vi.fn().mockResolvedValue(null)
    const wrapper = mountForm({ completeSale, checkOperation })
    await addProduct(wrapper)
    await fullyPay(wrapper)

    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()
    expect(checkOperation).toHaveBeenCalledWith('operation-1')
    expect(wrapper.text()).toContain('đã được khóa')
    expect(wrapper.get('[aria-label="Số tiền thanh toán"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-label="Số lượng Coffee"]').attributes('disabled')).toBeDefined()
    await wrapper.findAll('button').find(button => button.text() === 'Thử lại đúng thao tác')!.trigger('click')
    await flushPromises()

    expect(completeSale).toHaveBeenCalledTimes(2)
    expect(attempts[1]).toEqual(attempts[0])
    expect(crypto.randomUUID).toHaveBeenCalledTimes(1)
  })

  it('treats operation-lock-timeout as ambiguous and preserves the attempt', async () => {
    const completeSale = vi.fn()
      .mockRejectedValueOnce(new ApiError(409, { code: 'operation-lock-timeout', title: 'Busy' }))
      .mockResolvedValueOnce(sale)
    const checkOperation = vi.fn().mockResolvedValue(null)
    const wrapper = mountForm({ completeSale, checkOperation })
    await addProduct(wrapper)
    await fullyPay(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()

    expect(checkOperation).toHaveBeenCalledWith('operation-1')
    expect((wrapper.vm as unknown as { attempt: { operationId: string } }).attempt.operationId).toBe('operation-1')
    await wrapper.findAll('button').find(button => button.text() === 'Thử lại đúng thao tác')!.trigger('click')
    await flushPromises()
    expect(crypto.randomUUID).toHaveBeenCalledTimes(1)
    expect(completeSale.mock.calls[1][0]).toEqual(completeSale.mock.calls[0][0])
  })

  it('treats HTTP 408 as ambiguous and checks operation status', async () => {
    const completeSale = vi.fn().mockRejectedValue(new ApiError(408, { title: 'Timed out' }))
    const checkOperation = vi.fn().mockResolvedValue(null)
    const wrapper = mountForm({ completeSale, checkOperation })
    await addProduct(wrapper)
    await fullyPay(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()
    expect(checkOperation).toHaveBeenCalledWith('operation-1')
    expect((wrapper.vm as unknown as { state: string }).state).toBe('retryable')
  })

  it('does not infer success for idempotency-key-reused', async () => {
    const completeSale = vi.fn().mockRejectedValue(
      new ApiError(409, { code: 'idempotency-key-reused', title: 'Operation was reused.' }),
    )
    const checkOperation = vi.fn().mockResolvedValue({
      operationId: 'operation-1', status: 'Completed', operationType: 'CompleteSale', resultReference: 'sale-1',
    })
    const wrapper = mountForm({ completeSale, checkOperation })
    await addProduct(wrapper)
    await fullyPay(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Operation was reused.')
    expect(checkOperation).not.toHaveBeenCalled()
    expect(wrapper.emitted('completed')).toBeUndefined()
  })

  it('loads the authoritative sale when a lost response operation is completed', async () => {
    const completeSale = vi.fn().mockRejectedValue(new TypeError('network lost'))
    const checkOperation = vi.fn().mockResolvedValue({
      operationId: 'operation-1', status: 'Completed', operationType: 'CompleteSale', resultReference: 'sale-1',
    })
    const loadSale = vi.fn().mockResolvedValue(sale)
    const wrapper = mountForm({ completeSale, checkOperation, loadSale })
    await addProduct(wrapper)
    await fullyPay(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()

    expect(loadSale).toHaveBeenCalledWith('sale-1')
    expect(wrapper.emitted('completed')?.[0]).toEqual([sale])
  })

  it('blocks double submit while one attempt is active', async () => {
    let resolve!: (value: Sale) => void
    const pending = new Promise<Sale>((done) => { resolve = done })
    const completeSale = vi.fn(() => pending)
    const wrapper = mountForm({ completeSale })
    await addProduct(wrapper)
    await fullyPay(wrapper)
    const button = wrapper.findAll('button').find(item => item.text() === 'Hoàn tất bán hàng')!
    await Promise.all([button.trigger('click'), button.trigger('click')])
    expect(completeSale).toHaveBeenCalledTimes(1)
    resolve(sale)
    await flushPromises()
  })
})
