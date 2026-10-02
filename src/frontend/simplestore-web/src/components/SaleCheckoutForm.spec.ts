import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { routerKey } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import SaleCheckoutForm from './SaleCheckoutForm.vue'
import type { CustomerPage, ProductListItem, ProductPage, Sale } from '../api/types'
import { createOrderBook } from '../sales/orders'

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

type Attempt = {
  operationId: string
  customerId: string | null
  lines: Array<{ productId: string; quantity: number }>
  payments: Array<{ amount: number; method: string }>
}
/** Every CompleteSale payload a test sends; afterEach asserts none carries a Debt method (D-107). */
const submitted: Attempt[] = []

function mountForm(overrides: Record<string, unknown> = {}, provide: Record<symbol, unknown> = {}) {
  const completeSale = (overrides.completeSale ?? vi.fn().mockResolvedValue(sale)) as (attempt: Attempt) => Promise<Sale>
  return mount(SaleCheckoutForm, {
    global: { provide },
    props: {
      allowNegativeStock: false,
      searchProducts: vi.fn().mockResolvedValue(productPage()),
      searchCustomers: vi.fn().mockResolvedValue(customerPage),
      createCustomer: vi.fn(),
      checkOperation: vi.fn().mockResolvedValue(null),
      loadSale: vi.fn().mockResolvedValue(sale),
      ...overrides,
      completeSale: (attempt: Attempt) => {
        submitted.push(structuredClone(attempt))
        return completeSale(attempt)
      },
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

const button = (wrapper: ReturnType<typeof mountForm>, text: string) =>
  wrapper.findAll('button').find(item => item.text() === text)!

async function fullyPay(wrapper: ReturnType<typeof mountForm>) {
  await wrapper.get('[aria-label="Số tiền thanh toán"]').setValue('12000')
  const add = wrapper.findAll('button').find(button => button.text() === 'Thêm thanh toán')!
  await add.trigger('click')
}

describe('SaleCheckoutForm', () => {
  // Unmounting releases window listeners (keydown, beforeunload) and the pending-outcome guard.
  enableAutoUnmount(afterEach)
  beforeEach(() => {
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => 'operation-1') })
  })
  afterEach(() => {
    for (const call of submitted.splice(0)) {
      expect(call.payments.every(payment => payment.method === 'Cash' || payment.method === 'Transfer')).toBe(true)
    }
  })

  it('shows the sample catalog with D-106 visual-only elements and never submits a preview sale', async () => {
    const completeSale = vi.fn()
    const wrapper = mountForm({
      previewOnly: true,
      searchProducts: async () => ({
        items: (await import('../sales/demo')).demoProducts,
        page: 1, pageSize: 20, totalCount: 12, totalPages: 1,
      }),
      completeSale,
    })
    await flushPromises()
    expect(wrapper.findAll('.sales-pos__product-card')).toHaveLength(12)
    expect(wrapper.findAll('.sales-pos__product-card img')).toHaveLength(12)
    expect(wrapper.findAll('.sales-pos__categories .sales-pos__chip')).toHaveLength(7)
    expect(wrapper.text()).toContain('Rất ít hàng')
    expect(wrapper.text()).toContain('Sắp hết hàng')
    expect(wrapper.text()).toContain('37.000 đ')
    expect(wrapper.findAll('button.sales-pos__order-pill')).toHaveLength(3)
    for (const visualOnly of ['Đơn mới', 'Danh sách đơn đang chờ', 'Giữ đơn', 'Ghi chú đơn hàng', 'Thêm giảm giá hóa đơn', 'Giảm giá sản phẩm']) {
      expect(wrapper.text()).toContain(visualOnly)
    }
    expect(wrapper.find('.sales-pos__line-discount').exists()).toBe(true)
    expect(wrapper.findAll('.sales-pos__pay-btn').map(item => item.text())).toEqual(['Tiền mặt', 'Chuyển khoản', 'Bán nợ'])
    await wrapper.get('.sales-pos__complete').trigger('click')
    expect(completeSale).not.toHaveBeenCalled()
    expect(wrapper.get('[role="alert"]').text()).toContain('dữ liệu mẫu')
    // The live Lịch sử bán hàng shortcut stays out of the Demo, which keeps its D-106 kebab and waiting list.
    expect(wrapper.find('.sales-pos__history-link').exists()).toBe(false)
    expect(wrapper.find('.sales-pos__muted-row').exists()).toBe(true)
    expect(wrapper.get('.sales-pos__queue-history').text()).toBe('Lịch sử bán hàng')
  })

  it('shows only supported Product browser capability in live mode', async () => {
    const lookalike = { ...product, id: 'live-coke', sku: 'SP0001', name: 'Coca Cola 330ml', quantityOnHand: 2 }
    const few = { ...product, id: 'live-few', name: 'Few', quantityOnHand: 5 }
    const out = { ...product, id: 'live-out', name: 'Out', quantityOnHand: 0 }
    const negative = { ...product, id: 'live-negative', name: 'Negative', quantityOnHand: -3 }
    const wrapper = mountForm({ searchProducts: vi.fn().mockResolvedValue(productPage([lookalike, few, out, negative])) })
    await flushPromises()

    expect(wrapper.find('.sales-pos__categories').exists()).toBe(false)
    expect(wrapper.findAll('.sales-pos__product-card img')).toHaveLength(0)
    expect(wrapper.findAll('.sales-pos__placeholder')).toHaveLength(4)
    expect(wrapper.findAll('.sales-pos__product-footer').map(footer => footer.find('span').text())).toEqual([
      'Còn 2 pack', 'Còn 5 pack', 'Hết hàng · 0 pack', 'Tồn âm · -3 pack',
    ])
    expect(wrapper.text()).not.toContain('Rất ít hàng')
    expect(wrapper.text()).not.toContain('Sắp hết hàng')
    await wrapper.get('[aria-label="Thêm sản phẩm Coca Cola 330ml"]').trigger('click')
    expect(wrapper.find('.sales-pos__line-item img').exists()).toBe(false)
    expect(wrapper.find('.sales-pos__line-image--empty').exists()).toBe(true)
  })

  it('keeps exactly one working order and no visual-only controls in live mode', async () => {
    const orderBook = createOrderBook()
    const wrapper = mountForm({ orderBook })
    await addProduct(wrapper)

    expect(wrapper.find('.sales-pos__order-rail').exists()).toBe(false)
    for (const visualOnly of ['Đơn mới', 'Danh sách đơn đang chờ', 'Giữ đơn', 'Ghi chú đơn hàng', 'Thêm giảm giá hóa đơn', 'Giảm giá sản phẩm', 'Giảm giá hóa đơn', 'Bán nợ']) {
      expect(wrapper.text()).not.toContain(visualOnly)
    }
    expect(wrapper.find('.sales-pos__note').exists()).toBe(false)
    expect(wrapper.find('.sales-pos__muted-row').exists()).toBe(false)
    expect(wrapper.findAll('.sales-pos__pay-btn').map(item => item.text())).toEqual(['Tiền mặt', 'Chuyển khoản'])
    expect(wrapper.get('#sales-checkout-heading').text()).toBe('Đơn 1')
    expect(orderBook.orders.value).toHaveLength(1)
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

  it('requires a customer for a deliberate full debt and explains why', async () => {
    const customer = { id: 'customer-1', name: 'An', phone: '0909', createdAt: '', updatedAt: '' }
    const searchCustomers = vi.fn().mockResolvedValue({ ...customerPage, items: [customer], totalCount: 1, totalPages: 1 })
    const wrapper = mountForm({ searchCustomers })
    await addProduct(wrapper)
    expect(wrapper.find('.sales-pos__required-tag').exists()).toBe(false)
    expect(wrapper.find('[role="note"]').exists()).toBe(false)

    await button(wrapper, 'Ghi nợ toàn bộ').trigger('click')
    const vm = wrapper.vm as unknown as { paid: number; outstanding: number }
    expect(vm.paid).toBe(0)
    expect(vm.outstanding).toBe(12000)
    expect(wrapper.get('.sales-pos__full-debt-state').text()).toBe('Chưa thu tiềnCòn nợ 12.000 đ')
    expect(wrapper.get('.sales-pos__required-tag').text()).toBe('Bắt buộc khi còn nợ')
    expect(wrapper.get('.sales-pos__select').classes()).toContain('is-required')
    expect(wrapper.get('[role="note"]').text()).toBe('Còn nợ 12.000 đ. Chọn khách hàng để ghi nhận công nợ 12.000 đ.')
    await button(wrapper, 'Hoàn tất bán hàng').trigger('click')
    expect(wrapper.text()).toContain('Chọn khách hàng khi đơn còn công nợ.')

    await wrapper.get('[aria-label="Tìm khách hàng"]').setValue('0909')
    await wrapper.findAll('form')[1].trigger('submit')
    await flushPromises()
    await wrapper.get('[aria-label="Chọn khách hàng An"]').trigger('click')
    expect(searchCustomers).toHaveBeenCalledWith('0909', 1)
    expect(wrapper.get('[role="note"]').text()).toBe('Còn nợ 12.000 đ. Ghi nhận công nợ 12.000 đ cho An.')
    expect(wrapper.find('.sales-pos__message').exists()).toBe(false)
    expect(wrapper.get('.sales-pos__select').classes()).not.toContain('is-required')

    await button(wrapper, 'Chuyển về Khách lẻ').trigger('click')
    expect((wrapper.vm as unknown as { customer: unknown }).customer).toBeNull()
    expect(wrapper.get('.sales-pos__select-value').text()).toBe('Khách lẻ')
  })

  it('does not treat an open Nhập số tiền with no amount as a debt', async () => {
    const completeSale = vi.fn()
    const wrapper = mountForm({ completeSale })
    await addProduct(wrapper)
    await button(wrapper, 'Nhập số tiền').trigger('click')

    expect(wrapper.find('[role="note"]').exists()).toBe(false)
    expect(wrapper.find('.sales-pos__required-tag').exists()).toBe(false)
    expect(wrapper.text()).toContain('Nhập số tiền khách đã trả. Không thu tiền? Chọn Ghi nợ toàn bộ.')
    await button(wrapper, 'Hoàn tất bán hàng').trigger('click')

    expect(wrapper.get('.sales-pos__message').text()).toBe('Nhập số tiền đã thu hoặc chọn Ghi nợ toàn bộ.')
    expect(completeSale).not.toHaveBeenCalled()
    expect(crypto.randomUUID).not.toHaveBeenCalled()
  })

  it('keeps Ghi nợ toàn bộ outside the payment methods and switches back predictably', async () => {
    const wrapper = mountForm()
    await addProduct(wrapper)
    const vm = wrapper.vm as unknown as { paid: number; outstanding: number }
    const fullDebt = () => button(wrapper, 'Ghi nợ toàn bộ')
    expect(wrapper.findAll('.sales-pos__payment-grid button').map(item => item.text())).toEqual(['Tiền mặt', 'Chuyển khoản'])
    expect(wrapper.find('.sales-pos__payment-grid .sales-pos__full-debt-btn').exists()).toBe(false)

    await fullDebt().trigger('click')
    expect(fullDebt().attributes('aria-pressed')).toBe('true')
    expect(wrapper.find('[aria-pressed="true"].sales-pos__pay-btn').exists()).toBe(false)
    expect(vm.outstanding).toBe(12000)

    await button(wrapper, 'Chuyển khoản').trigger('click')
    expect(fullDebt().attributes('aria-pressed')).toBe('false')
    expect(wrapper.get('[aria-pressed="true"].sales-pos__pay-btn').text()).toBe('Chuyển khoản')
    expect(vm.paid).toBe(12000)
    expect(vm.outstanding).toBe(0)

    await fullDebt().trigger('click')
    await fullDebt().trigger('click')
    expect(vm.outstanding).toBe(0)
    await fullDebt().trigger('click')
    await button(wrapper, 'Nhập số tiền').trigger('click')
    expect(fullDebt().attributes('aria-pressed')).toBe('false')
    expect(wrapper.find('.sales-pos__full-debt-state').exists()).toBe(false)
  })

  it('does not let Ghi nợ toàn bộ silently drop entered payments', async () => {
    const wrapper = mountForm()
    await addProduct(wrapper)
    await button(wrapper, 'Nhập số tiền').trigger('click')
    await wrapper.get('[aria-label="Số tiền thanh toán"]').setValue('5000')
    await button(wrapper, 'Thêm thanh toán').trigger('click')

    expect(button(wrapper, 'Ghi nợ toàn bộ').attributes('disabled')).toBeDefined()
    await button(wrapper, 'Ghi nợ toàn bộ').trigger('click')
    expect((wrapper.vm as unknown as { payments: unknown[] }).payments).toEqual([{ amount: 5000, method: 'Cash' }])
    await wrapper.get('[aria-label="Xóa khoản thanh toán 1"]').trigger('click')
    expect(button(wrapper, 'Ghi nợ toàn bộ').attributes('disabled')).toBeUndefined()
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

  it('creates and selects a customer for a full-debt Sale and sends no payment', async () => {
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
    await button(wrapper, 'Ghi nợ toàn bộ').trigger('click')
    expect(wrapper.get('[role="note"]').text()).toBe('Còn nợ 12.000 đ. Ghi nhận công nợ 12.000 đ cho Nguyễn Thị An.')

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
    expect(completeSale).toHaveBeenCalledWith(expect.objectContaining({
      payments: [{ amount: 5000, method: 'Cash' }, { amount: 7000, method: 'Transfer' }],
    }))
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
    await button(wrapper, 'Ghi nợ toàn bộ').trigger('click')
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
    // Payment intent decides actual payments, so every payment control stays locked with the attempt.
    for (const name of ['Ghi nợ toàn bộ', 'Nhập số tiền', 'Tiền mặt', 'Chuyển khoản']) {
      expect(button(wrapper, name).attributes('disabled')).toBeDefined()
    }
    expect((wrapper.vm as unknown as { attempt: { payments: unknown[] } }).attempt.payments).toEqual([])
    expect((wrapper.vm as unknown as { outstanding: number }).outstanding).toBe(12000)
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

  it('pays the whole order with the selected method when no amount is entered', async () => {
    const completeSale = vi.fn().mockResolvedValue(sale)
    const wrapper = mountForm({ completeSale })
    await addProduct(wrapper)
    await button(wrapper, 'Chuyển khoản').trigger('click')
    expect(wrapper.find('.sales-pos__required-tag').exists()).toBe(false)
    await button(wrapper, 'Hoàn tất bán hàng').trigger('click')
    await flushPromises()

    expect(completeSale).toHaveBeenCalledWith(expect.objectContaining({
      customerId: null, payments: [{ amount: 12000, method: 'Transfer' }],
    }))
    expect(wrapper.emitted('completed')?.[0]).toEqual([sale])
  })

  it('keeps entered amounts authoritative and shows the remaining debt', async () => {
    const wrapper = mountForm()
    await addProduct(wrapper)
    await button(wrapper, 'Nhập số tiền').trigger('click')
    expect(wrapper.find('[role="note"]').exists()).toBe(false)
    await wrapper.get('[aria-label="Số tiền thanh toán"]').setValue('5000')
    await button(wrapper, 'Thêm thanh toán').trigger('click')

    const vm = wrapper.vm as unknown as { payments: unknown[]; paid: number; outstanding: number }
    expect(vm.payments).toEqual([{ amount: 5000, method: 'Cash' }])
    expect(vm.outstanding).toBe(7000)
    expect(wrapper.get('[role="note"]').text()).toContain('Còn nợ 7.000 đ')
    expect(wrapper.get('.sales-pos__split-summary').text()).toBe('Khách trả 5.000 đ · Còn nợ 7.000 đ')
  })

  it('pays the whole order with Tiền mặt by default', async () => {
    const completeSale = vi.fn().mockResolvedValue(sale)
    const wrapper = mountForm({ completeSale })
    await addProduct(wrapper)
    expect(wrapper.get('[aria-pressed="true"].sales-pos__pay-btn').text()).toBe('Tiền mặt')
    await button(wrapper, 'Hoàn tất bán hàng').trigger('click')
    await flushPromises()
    expect(completeSale).toHaveBeenCalledWith(expect.objectContaining({
      customerId: null, payments: [{ amount: 12000, method: 'Cash' }],
    }))
  })

  it('keeps the live working order across a remount and starts empty after a session reset', async () => {
    const customer = { id: 'customer-1', name: 'An', phone: '0909', createdAt: '', updatedAt: '' }
    const searchCustomers = vi.fn().mockResolvedValue({ ...customerPage, items: [customer], totalCount: 1, totalPages: 1 })
    const orderBook = createOrderBook()
    const first = mountForm({ orderBook, searchCustomers })
    await addProduct(first)
    await first.get('[aria-label="Tìm khách hàng"]').setValue('An')
    await first.findAll('form')[1].trigger('submit')
    await flushPromises()
    await first.get('[aria-label="Chọn khách hàng An"]').trigger('click')
    await button(first, 'Nhập số tiền').trigger('click')
    await first.get('[aria-label="Số tiền thanh toán"]').setValue('5000')
    await button(first, 'Thêm thanh toán').trigger('click')
    first.unmount()

    const returned = mountForm({ orderBook })
    await flushPromises()
    const kept = returned.vm as unknown as { cart: unknown[]; payments: unknown[]; outstanding: number }
    expect(kept.cart).toHaveLength(1)
    expect(kept.payments).toEqual([{ amount: 5000, method: 'Cash' }])
    expect(kept.outstanding).toBe(7000)
    expect(returned.get('.sales-pos__select-value').text()).toBe('An')
    returned.unmount()

    orderBook.reset()
    const fresh = mountForm({ orderBook })
    await flushPromises()
    const reset = fresh.vm as unknown as { cart: unknown[]; payments: unknown[]; customer: unknown }
    expect(reset.cart).toEqual([])
    expect(reset.payments).toEqual([])
    expect(reset.customer).toBeNull()
    expect(fresh.get('.sales-pos__select-value').text()).toBe('Khách lẻ')
    expect(fresh.get('[aria-pressed="true"].sales-pos__pay-btn').text()).toBe('Tiền mặt')
    expect(button(fresh, 'Nhập số tiền').exists()).toBe(true)
    expect(orderBook.active.value.paymentIntent).toBe('full-payment')
  })

  it('returns to paying the whole order when Nhập số tiền is closed without amounts', async () => {
    const wrapper = mountForm()
    await addProduct(wrapper)
    const vm = wrapper.vm as unknown as { paid: number; outstanding: number }
    await button(wrapper, 'Nhập số tiền').trigger('click')
    expect(wrapper.find('[role="note"]').exists()).toBe(false)
    await button(wrapper, 'Thu đủ').trigger('click')
    expect(vm.paid).toBe(12000)
    expect(vm.outstanding).toBe(0)
    expect(wrapper.find('[role="note"]').exists()).toBe(false)
  })

  it('holds and switches sample working orders in Demo mode only', async () => {
    const orderBook = createOrderBook()
    const wrapper = mountForm({ previewOnly: true, orderBook })
    await addProduct(wrapper)
    await button(wrapper, 'Giữ đơn').trigger('click')

    expect(orderBook.orders.value.map(order => order.number)).toEqual([1, 2])
    expect(wrapper.get('#sales-checkout-heading-preview').text()).toBe('Đơn 2')
    expect((wrapper.vm as unknown as { cart: unknown[] }).cart).toHaveLength(0)
    expect(wrapper.text()).toContain('Danh sách đơn đang chờ (2)')

    await wrapper.get('[aria-label="Đơn 1: 1 sản phẩm, 12.000 đồng"]').trigger('click')
    expect(wrapper.get('#sales-checkout-heading-preview').text()).toBe('Đơn 1')
    expect((wrapper.vm as unknown as { cart: unknown[] }).cart).toHaveLength(1)
  })

  it('completes the single live working order and starts the next one empty', async () => {
    const orderBook = createOrderBook()
    const completeSale = vi.fn().mockResolvedValue(sale)
    const wrapper = mountForm({ orderBook, completeSale })
    await addProduct(wrapper)
    await button(wrapper, 'Hoàn tất bán hàng').trigger('click')
    await flushPromises()

    expect(completeSale).toHaveBeenCalledOnce()
    expect(orderBook.orders.value).toHaveLength(1)
    expect(orderBook.active.value.cart).toEqual([])
  })

  it('clears the current order with Xóa đơn', async () => {
    const wrapper = mountForm()
    await addProduct(wrapper)
    await button(wrapper, 'Chuyển khoản').trigger('click')
    await button(wrapper, 'Nhập số tiền').trigger('click')
    await button(wrapper, 'Xóa đơn').trigger('click')

    const vm = wrapper.vm as unknown as { cart: unknown[]; outstanding: number }
    expect(vm.cart).toHaveLength(0)
    expect(wrapper.get('[aria-pressed="true"].sales-pos__pay-btn').text()).toBe('Tiền mặt')
    expect(button(wrapper, 'Nhập số tiền').exists()).toBe(true)
  })

  it('filters the loaded products by the sample category chips in Demo mode', async () => {
    const tea = { ...product, id: 'product-tea', name: 'Trà xanh C2 500ml' }
    const sauce = { ...product, id: 'product-sauce', name: 'Nước mắm Nam Ngư 500ml' }
    const wrapper = mountForm({ previewOnly: true, searchProducts: vi.fn().mockResolvedValue(productPage([tea, sauce])) })
    await flushPromises()
    await button(wrapper, 'Gia vị').trigger('click')

    expect(wrapper.findAll('.sales-pos__product-card').map(card => card.find('h3').text())).toEqual([sauce.name])
    await button(wrapper, 'Tất cả').trigger('click')
    expect(wrapper.findAll('.sales-pos__product-card')).toHaveLength(2)
  })

  it('adds the single match directly with Thêm nhanh', async () => {
    const searchProducts = vi.fn().mockResolvedValue(productPage())
    const wrapper = mountForm({ searchProducts })
    await flushPromises()
    await wrapper.get('[aria-label="Tìm hoặc quét sản phẩm"]').setValue('893000101')
    await button(wrapper, 'Thêm nhanh').trigger('click')
    await flushPromises()

    expect(searchProducts).toHaveBeenLastCalledWith('893000101', 1)
    expect((wrapper.vm as unknown as { cart: unknown[] }).cart).toHaveLength(1)
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
  describe('Pass 2 shell and transaction state', () => {
    /** Every mutable control of the live order that an unresolved CompleteSale must lock. */
    const guardedControls = (wrapper: ReturnType<typeof mountForm>) => [
      wrapper.get('[aria-label="Tìm hoặc quét sản phẩm"]'),
      button(wrapper, 'Thêm nhanh'),
      button(wrapper, 'Quét mã'),
      wrapper.get('[aria-label="Thêm sản phẩm Coffee"]'),
      wrapper.get('[aria-label="Giảm số lượng Coffee"]'),
      wrapper.get('[aria-label="Số lượng Coffee"]'),
      wrapper.get('[aria-label="Tăng số lượng Coffee"]'),
      wrapper.get('[aria-label="Xóa Coffee"]'),
      wrapper.get('[aria-label="Tìm khách hàng"]'),
      wrapper.get('[aria-label="Tên khách hàng mới"]'),
      button(wrapper, 'Tiền mặt'),
      button(wrapper, 'Chuyển khoản'),
      button(wrapper, 'Nhập số tiền'),
      button(wrapper, 'Ghi nợ toàn bộ'),
      wrapper.get('[aria-label="Số tiền thanh toán"]'),
      button(wrapper, 'Thêm thanh toán'),
      button(wrapper, 'Xóa đơn'),
    ]

    it('routes the live Lịch sử bán hàng shortcut to Sales history', async () => {
      const push = vi.fn()
      const wrapper = mountForm({}, { [routerKey]: { push } })
      await addProduct(wrapper)
      const shortcut = wrapper.get('a.sales-pos__history-link')

      expect(shortcut.text()).toBe('Lịch sử bán hàng')
      expect(shortcut.attributes('href')).toBe('/sales')
      expect(shortcut.attributes('aria-disabled')).toBeUndefined()
      // A quiet header utility, never an action beside the Complete CTA.
      expect(wrapper.get('.sales-pos__checkout-tools').element.contains(shortcut.element)).toBe(true)
      expect(wrapper.get('.sales-pos__actions').text()).not.toContain('Lịch sử')
      await shortcut.trigger('click', { button: 0 })
      expect(push).toHaveBeenCalledWith('/sales')
    })

    it('keeps + Thêm khách as one icon-and-label control', async () => {
      const wrapper = mountForm()
      await flushPromises()
      const add = wrapper.get('.sales-pos__create-customer summary')

      expect(add.text()).toBe('Thêm khách')
      expect(add.findAll('.line-icon')).toHaveLength(1)
      expect(add.findAll('span').map(item => item.text())).toEqual(['Thêm khách'])
    })

    it('shows one busy status: Đang xác nhận… then Đang kiểm tra kết quả…', async () => {
      let failComplete!: (reason: unknown) => void
      let answerStatus!: (value: null) => void
      const completeSale = vi.fn(() => new Promise<Sale>((_, reject) => { failComplete = reject }))
      const checkOperation = vi.fn(() => new Promise<null>(resolve => { answerStatus = resolve }))
      const wrapper = mountForm({ completeSale, checkOperation })
      await addProduct(wrapper)
      await button(wrapper, 'Hoàn tất bán hàng').trigger('click')

      const cta = () => wrapper.get('.sales-pos__complete')
      expect(cta().text()).toBe('Đang xác nhận…')
      expect(cta().attributes('aria-busy')).toBe('true')
      expect(cta().attributes('disabled')).toBeDefined()
      expect(wrapper.findAll('.sales-pos__spinner')).toHaveLength(1)
      expect(wrapper.find('.sales-pos__checking').exists()).toBe(false)

      failComplete(new TypeError('network lost'))
      await flushPromises()
      expect(cta().text()).toBe('Đang kiểm tra kết quả…')
      expect(cta().attributes('aria-busy')).toBe('true')
      expect(wrapper.findAll('.sales-pos__spinner')).toHaveLength(1)
      expect(wrapper.get('.sales-pos__checking').text()).toContain('đang kiểm tra đơn đã được ghi nhận hay chưa')
      expect(wrapper.get('.sales-pos').classes()).toContain('sales-pos--guarded')

      answerStatus(null)
      await flushPromises()
      expect(cta().text()).toBe('Thử lại đúng thao tác')
      expect(cta().attributes('aria-busy')).toBeUndefined()
      expect(cta().attributes('disabled')).toBeUndefined()
      expect(wrapper.findAll('.sales-pos__spinner')).toHaveLength(0)
    })

    it('explains an unresolved outcome calmly and visibly locks the whole transaction', async () => {
      const push = vi.fn()
      const completeSale = vi.fn().mockRejectedValue(new ApiError(503, { title: 'Unavailable' }))
      const wrapper = mountForm({ completeSale, checkOperation: vi.fn().mockResolvedValue(null) }, { [routerKey]: { push } })
      await addProduct(wrapper)
      await wrapper.get('.sales-pos__create-customer summary').trigger('click')
      expect((wrapper.get('.sales-pos__create-customer').element as HTMLDetailsElement).open).toBe(true)
      await button(wrapper, 'Hoàn tất bán hàng').trigger('click')
      await flushPromises()

      const block = wrapper.get('.sales-pos__txn-state')
      expect(block.attributes('role')).toBe('alert')
      expect(block.get('.sales-pos__txn-title').text()).toBe('Chưa xác định được kết quả')
      expect(block.text()).toContain('không tạo đơn mới')
      expect(block.text()).toContain('đã được khóa')
      expect(block.text()).toContain('SimpleStore sẽ dùng lại đúng mã thao tác trước đó để tránh tạo đơn trùng.')
      expect(wrapper.text()).not.toMatch(/operation ?id/i)
      // One transaction status: the red correction box is not reused for an unknown outcome.
      expect(wrapper.find('.sales-pos__error').exists()).toBe(false)
      expect(wrapper.get('.sales-pos__complete').text()).toBe('Thử lại đúng thao tác')
      expect(wrapper.get('.sales-pos__status-chip').text()).toBe('Đã khóa')
      expect(wrapper.get('.sales-pos__lock-strip').text()).toContain('Tạm khóa')
      expect(wrapper.get('.sales-pos').classes()).toContain('sales-pos--guarded')

      for (const control of guardedControls(wrapper)) expect(control.attributes('disabled'), control.html()).toBeDefined()
      for (const summary of wrapper.findAll('.sales-pos__field-row summary')) {
        expect(summary.attributes('aria-disabled')).toBe('true')
      }
      // Open pickers close with the lock and cannot be reopened while it holds.
      expect((wrapper.get('.sales-pos__create-customer').element as HTMLDetailsElement).open).toBe(false)
      const toggle = new MouseEvent('click', { bubbles: true, cancelable: true })
      wrapper.get('.sales-pos__customer-picker summary').element.dispatchEvent(toggle)
      expect(toggle.defaultPrevented).toBe(true)
      // Leaving would drop the unresolved attempt, so the history shortcut waits.
      const shortcut = wrapper.get('a.sales-pos__history-link')
      expect(shortcut.attributes('aria-disabled')).toBe('true')
      expect(shortcut.attributes('tabindex')).toBe('-1')
      await shortcut.trigger('click', { button: 0 })
      expect(push).not.toHaveBeenCalled()
    })

    it('returns a rejected sale to an editable correction state without lock residue', async () => {
      const completeSale = vi.fn()
        .mockRejectedValueOnce(new ApiError(422, { title: 'Không đủ tồn kho cho Coffee.' }))
        .mockResolvedValueOnce(sale)
      vi.mocked(crypto.randomUUID)
        .mockReturnValueOnce('00000000-0000-4000-8000-000000000001')
        .mockReturnValueOnce('00000000-0000-4000-8000-000000000002')
      const wrapper = mountForm({ completeSale })
      await addProduct(wrapper)
      await button(wrapper, 'Hoàn tất bán hàng').trigger('click')
      await flushPromises()

      const error = wrapper.get('.sales-pos__error')
      expect(error.attributes('role')).toBe('alert')
      expect(error.get('.sales-pos__error-title').text()).toBe('Chưa hoàn tất đơn bán')
      expect(error.get('.sales-pos__message').text()).toBe('Không đủ tồn kho cho Coffee.')
      expect(error.text()).toContain('sửa đơn rồi bấm Hoàn tất bán hàng')
      // The correction sits right above the transaction action.
      expect(error.element.nextElementSibling?.classList.contains('sales-pos__actions')).toBe(true)
      expect(wrapper.find('.sales-pos__txn-state').exists()).toBe(false)
      expect(wrapper.find('.sales-pos__lock-strip').exists()).toBe(false)
      expect(wrapper.get('.sales-pos').classes()).not.toContain('sales-pos--guarded')
      expect(wrapper.get('.sales-pos__status-chip').text()).toBe('Đang bán')
      expect(wrapper.get('.sales-pos__complete').text()).toBe('Hoàn tất bán hàng')
      for (const control of guardedControls(wrapper)) expect(control.attributes('disabled'), control.html()).toBeUndefined()
      for (const summary of wrapper.findAll('.sales-pos__field-row summary')) {
        expect(summary.attributes('aria-disabled')).toBeUndefined()
      }

      // A corrected order is a new attempt with a new operation, as before Pass 2.
      await wrapper.get('[aria-label="Số lượng Coffee"]').setValue('2')
      await button(wrapper, 'Hoàn tất bán hàng').trigger('click')
      await flushPromises()
      expect(completeSale.mock.calls[0][0]).toMatchObject({ operationId: '00000000-0000-4000-8000-000000000001' })
      expect(completeSale.mock.calls[1][0]).toMatchObject({
        operationId: '00000000-0000-4000-8000-000000000002',
        lines: [{ productId: product.id, quantity: 2 }],
      })
      expect(wrapper.find('.sales-pos__error').exists()).toBe(false)
    })
  })
})
