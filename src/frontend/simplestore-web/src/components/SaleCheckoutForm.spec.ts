import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
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
  await flushPromises()
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

  it('searches Vietnamese product names and requests each page from the server', async () => {
    const first = { ...product, id: 'product-first', name: 'Cà phê rang xay truyền thống' }
    const second = { ...product, id: 'product-second', name: 'Cà phê hạt nguyên chất' }
    const searchProducts = vi.fn()
      .mockResolvedValueOnce(productPage([], 1, 0))
      .mockResolvedValueOnce(productPage([first], 1, 2))
      .mockResolvedValueOnce(productPage([second], 2, 2))
    const wrapper = mountForm({ searchProducts })
    await flushPromises()

    await wrapper.get('[aria-label="Tìm hoặc quét sản phẩm"]').setValue('Cà phê')
    await wrapper.get('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain(first.name)
    await wrapper.findAll('button').find(button => button.text() === 'Sau')!.trigger('click')
    await flushPromises()

    expect(searchProducts.mock.calls).toEqual([['', 1], ['Cà phê', 1], ['Cà phê', 2]])
    expect(wrapper.text()).toContain(second.name)
    expect(wrapper.text()).not.toContain(first.name)
  })

  it('loads the first product page and shows loading and an empty result', async () => {
    let resolveSearch!: (page: ProductPage) => void
    const searchProducts = vi.fn().mockImplementation(() => new Promise<ProductPage>(resolve => { resolveSearch = resolve }))
    const wrapper = mountForm({ searchProducts })
    expect(searchProducts).toHaveBeenCalledWith('', 1)
    await nextTick()
    expect(wrapper.get('[role="status"]').text()).toContain('Đang tìm sản phẩm')
    resolveSearch(productPage([], 1, 0))
    await flushPromises()

    expect(wrapper.text()).toContain('Không tìm thấy sản phẩm phù hợp.')
    expect(wrapper.find('[aria-label="Thêm sản phẩm Coffee"]').exists()).toBe(false)
  })

  it('shows a product search error without leaving stale results', async () => {
    const searchProducts = vi.fn()
      .mockResolvedValueOnce(productPage())
      .mockRejectedValueOnce(new Error('network lost'))
    const wrapper = mountForm({ searchProducts })
    await flushPromises()
    expect(wrapper.text()).toContain(product.name)

    await wrapper.get('[aria-label="Tìm hoặc quét sản phẩm"]').setValue('Tea')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain('Không thể tìm sản phẩm.')
    expect(wrapper.find('[aria-label="Thêm sản phẩm Coffee"]').exists()).toBe(false)
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

  it('adjusts quantity through the visual stepper without changing cart semantics', async () => {
    const wrapper = mountForm()
    await addProduct(wrapper)

    await wrapper.get('[aria-label="Tăng số lượng Coffee"]').trigger('click')
    expect((wrapper.vm as unknown as { total: number }).total).toBe(24000)
    await wrapper.get('[aria-label="Giảm số lượng Coffee"]').trigger('click')
    expect((wrapper.vm as unknown as { total: number }).total).toBe(12000)
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

  it('requests later customer pages without losing the search term', async () => {
    const first = { id: 'customer-first', name: 'Nguyễn An', phone: '0901', createdAt: '', updatedAt: '' }
    const second = { id: 'customer-second', name: 'Nguyễn Bình', phone: '0902', createdAt: '', updatedAt: '' }
    const searchCustomers = vi.fn()
      .mockResolvedValueOnce({ ...customerPage, items: [first], totalCount: 2, totalPages: 2 })
      .mockResolvedValueOnce({ ...customerPage, items: [second], page: 2, totalCount: 2, totalPages: 2 })
    const wrapper = mountForm({ searchCustomers })
    await addProduct(wrapper)

    await wrapper.get('[aria-label="Tìm khách hàng"]').setValue('Nguyễn')
    await wrapper.findAll('form').find(form => form.find('[aria-label="Tìm khách hàng"]').exists())!.trigger('submit')
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'Sau')!.trigger('click')
    await flushPromises()

    expect(searchCustomers.mock.calls).toEqual([['Nguyễn', 1], ['Nguyễn', 2]])
    expect(wrapper.text()).toContain(second.name)
    expect(wrapper.text()).not.toContain(first.name)
  })

  it('creates and selects a customer for an outstanding sale', async () => {
    const customer = { id: 'customer-new', name: 'Nguyễn Thị An', phone: '0909000000', createdAt: '', updatedAt: '' }
    const createCustomer = vi.fn().mockResolvedValue(customer)
    const completeSale = vi.fn().mockResolvedValue({ ...sale, customer, paidAmount: 0, outstandingAmount: 12000 })
    const wrapper = mountForm({ createCustomer, completeSale })
    await addProduct(wrapper)

    await wrapper.get('.sales-pos__create-customer summary').trigger('click')
    expect((wrapper.get('.sales-pos__create-customer').element as HTMLDetailsElement).open).toBe(true)
    await wrapper.get('[aria-label="Tên khách hàng mới"]').setValue('  Nguyễn Thị An  ')
    await wrapper.get('[aria-label="Số điện thoại khách hàng mới"]').setValue('0909000000')
    await wrapper.findAll('button').find(button => button.text() === 'Tạo và chọn khách hàng')!.trigger('click')
    await flushPromises()
    expect(createCustomer).toHaveBeenCalledWith('Nguyễn Thị An', '0909000000')
    expect(wrapper.text()).toContain(customer.name)

    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()
    expect(completeSale).toHaveBeenCalledWith(expect.objectContaining({
      customerId: customer.id, payments: [], lines: [{ productId: product.id, quantity: 1 }],
    }))
  })

  it('validates payment amount and supports Cash plus Transfer without overpaying', async () => {
    const completeSale = vi.fn().mockResolvedValue(sale)
    const wrapper = mountForm({ completeSale })
    await addProduct(wrapper)
    const amount = wrapper.get('[aria-label="Số tiền thanh toán"]')
    const addPayment = () => wrapper.findAll('button').find(button => button.text() === 'Thêm thanh toán')!

    await amount.setValue('0')
    await addPayment().trigger('click')
    expect(wrapper.text()).toContain('Số tiền phải lớn hơn 0.')
    await amount.setValue('12001')
    await addPayment().trigger('click')
    expect(wrapper.text()).toContain('Tổng thanh toán không được vượt tổng đơn.')

    await amount.setValue('5000')
    await addPayment().trigger('click')
    await wrapper.findAll('button').find(button => button.text() === 'Chuyển khoản')!.trigger('click')
    await amount.setValue('7000')
    await addPayment().trigger('click')
    const vm = wrapper.vm as unknown as { payments: Array<{ amount: number; method: string }>; paid: number; outstanding: number }
    expect(vm.payments).toEqual([{ amount: 5000, method: 'Cash' }, { amount: 7000, method: 'Transfer' }])
    expect(vm.paid).toBe(12000)
    expect(vm.outstanding).toBe(0)

    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()
    expect(completeSale).toHaveBeenCalledWith(expect.objectContaining({ payments: vm.payments }))
  })

  it('rejects a blank payment amount without adding a non-number payment', async () => {
    const wrapper = mountForm()
    await addProduct(wrapper)
    await wrapper.get('[aria-label="Số tiền thanh toán"]').setValue('')
    await wrapper.findAll('button').find(button => button.text() === 'Thêm thanh toán')!.trigger('click')

    expect(wrapper.text()).toContain('Số tiền phải lớn hơn 0.')
    expect((wrapper.vm as unknown as { payments: unknown[] }).payments).toEqual([])
  })

  it('does not submit an overpaid snapshot after cart quantity is reduced', async () => {
    const completeSale = vi.fn()
    const wrapper = mountForm({ completeSale })
    await addProduct(wrapper)
    await wrapper.get('[aria-label="Số lượng Coffee"]').setValue('2')
    await wrapper.get('[aria-label="Số tiền thanh toán"]').setValue('20000')
    await wrapper.findAll('button').find(button => button.text() === 'Thêm thanh toán')!.trigger('click')
    await wrapper.get('[aria-label="Số lượng Coffee"]').setValue('1')
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')

    expect(wrapper.text()).toContain('Tổng thanh toán không được vượt tổng đơn.')
    expect(completeSale).not.toHaveBeenCalled()
    expect(crypto.randomUUID).not.toHaveBeenCalled()
  })

  it('shows the negative-stock policy and rejects zero quantity before creating an operation', async () => {
    const completeSale = vi.fn()
    const wrapper = mountForm({ allowNegativeStock: true, completeSale })
    expect(wrapper.text()).toContain('cho phép bán âm tồn')
    await addProduct(wrapper)
    await wrapper.get('[aria-label="Số lượng Coffee"]').setValue('0')
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')

    expect(wrapper.text()).toContain('Số lượng bán phải lớn hơn 0.')
    expect(completeSale).not.toHaveBeenCalled()
    expect(crypto.randomUUID).not.toHaveBeenCalled()
  })

  it('rejects a blank quantity before creating an operation', async () => {
    const completeSale = vi.fn()
    const wrapper = mountForm({ completeSale })
    await addProduct(wrapper)
    await wrapper.get('[aria-label="Số lượng Coffee"]').setValue('')
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')

    expect(wrapper.text()).toContain('Số lượng bán phải lớn hơn 0.')
    expect(completeSale).not.toHaveBeenCalled()
    expect(crypto.randomUUID).not.toHaveBeenCalled()
  })

  it.each([Number.NaN, Number.POSITIVE_INFINITY, Number.NEGATIVE_INFINITY])(
    'rejects a non-finite quantity (%s) before creating an operation', async quantity => {
      const completeSale = vi.fn()
      const wrapper = mountForm({ completeSale })
      await addProduct(wrapper)
      const vm = wrapper.vm as unknown as { cart: Array<{ quantity: number }> }
      vm.cart[0]!.quantity = quantity
      await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')

      expect(wrapper.text()).toContain('Số lượng bán phải lớn hơn 0.')
      expect(completeSale).not.toHaveBeenCalled()
      expect(crypto.randomUUID).not.toHaveBeenCalled()
    },
  )

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

  it('keeps the attempt locked when a 5xx result and status lookup are both unavailable', async () => {
    const completeSale = vi.fn().mockRejectedValue(new ApiError(503, { title: 'Temporarily unavailable' }))
    const checkOperation = vi.fn().mockRejectedValue(new TypeError('status network lost'))
    const wrapper = mountForm({ completeSale, checkOperation })
    await addProduct(wrapper)
    await fullyPay(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()

    expect(checkOperation).toHaveBeenCalledWith('operation-1')
    expect((wrapper.vm as unknown as { state: string }).state).toBe('retryable')
    expect((wrapper.vm as unknown as { attempt: { operationId: string } }).attempt.operationId).toBe('operation-1')
    expect(wrapper.get('[aria-label="Tìm hoặc quét sản phẩm"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-label="Số lượng Coffee"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-label="Số tiền thanh toán"]').attributes('disabled')).toBeDefined()
    expect(wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')).toBeUndefined()
  })

  it('locks customer selection and creation while a debt sale outcome is ambiguous', async () => {
    const customer = { id: 'customer-debt', name: 'Nguyễn An', phone: '0909', createdAt: '', updatedAt: '' }
    const searchCustomers = vi.fn().mockResolvedValue({ ...customerPage, items: [customer], totalCount: 1, totalPages: 1 })
    const completeSale = vi.fn().mockRejectedValue(new ApiError(408, { title: 'Timed out' }))
    const checkOperation = vi.fn().mockResolvedValue(null)
    const wrapper = mountForm({ searchCustomers, completeSale, checkOperation })
    await addProduct(wrapper)
    await wrapper.get('[aria-label="Tìm khách hàng"]').setValue('0909')
    await wrapper.findAll('form').find(form => form.find('[aria-label="Tìm khách hàng"]').exists())!.trigger('submit')
    await flushPromises()
    await wrapper.get('[aria-label="Chọn khách hàng Nguyễn An"]').trigger('click')
    await wrapper.get('.sales-pos__create-customer summary').trigger('click')
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()

    expect((wrapper.vm as unknown as { state: string; attempt: { customerId: string } }).state).toBe('retryable')
    expect((wrapper.vm as unknown as { attempt: { customerId: string } }).attempt.customerId).toBe(customer.id)
    expect(wrapper.get('[aria-label="Tìm khách hàng"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-label="Chọn khách hàng Nguyễn An"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-label="Tên khách hàng mới"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-label="Số điện thoại khách hàng mới"]').attributes('disabled')).toBeDefined()
  })

  it('returns to editable correction after a non-ambiguous business error', async () => {
    const completeSale = vi.fn().mockRejectedValue(new ApiError(422, { title: 'Không đủ tồn kho.' }))
    const checkOperation = vi.fn()
    const wrapper = mountForm({ completeSale, checkOperation })
    await addProduct(wrapper)
    await fullyPay(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất bán hàng')!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Không đủ tồn kho.')
    expect((wrapper.vm as unknown as { state: string; attempt: unknown }).state).toBe('idle')
    expect((wrapper.vm as unknown as { state: string; attempt: unknown }).attempt).toBeNull()
    expect(wrapper.get('[aria-label="Số lượng Coffee"]').attributes('disabled')).toBeUndefined()
    expect(checkOperation).not.toHaveBeenCalled()
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
