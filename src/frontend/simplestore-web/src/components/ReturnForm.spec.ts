import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import type { ReturnContext, ReturnPreview, ReturnResult } from '../api/types'
import ReturnForm from './ReturnForm.vue'

const context: ReturnContext = {
  saleId: 'sale-1', isVoided: false, hasReturns: true,
  originalTotalAmount: 36000, totalReturnedAmount: 12000, netSaleAmount: 24000,
  originalCollectedAmount: 36000, totalRefundedAmount: 12000, netCollectedAmount: 24000,
  outstandingAmount: 0,
  lines: [{
    saleLineId: 'line-1', productId: 'product-1', productName: 'Cà phê', productSku: 'CF', productUnit: 'gói',
    soldQuantity: 3, previouslyReturnedQuantity: 1, returnableQuantity: 2,
    originalUnitSalePrice: 12000, originalLineAmount: 36000,
  }],
}
const preview = (refundDueNow = 0): ReturnPreview => ({
  originalSaleId: 'sale-1',
  lines: [{ originalSaleLineId: 'line-1', productId: 'product-1', requestedQuantity: 1, restock: true, previouslyReturnedQuantity: 1, remainingQuantityBefore: 2, returnLineAmount: 12000, restockedInventoryValue: 8000 }],
  currentReturnValue: 12000, previousReturnedValue: 12000, cumulativeReturnedValue: 24000,
  netSaleObligation: 12000, netCashHeld: refundDueNow > 0 ? 12000 : 0,
  outstanding: 12000, refundDueNow, refundMethodRequired: refundDueNow > 0,
  returnObligationReduction: 12000,
  currentAggregateCustomerDebt: null,
  debtReduction: null,
  requiredActualRefund: refundDueNow,
})
const result: ReturnResult = {
  id: 'return-2', originalSaleId: 'sale-1', status: 'Completed',
  lines: [], refundPayments: [], totalReturnAmount: 12000, refundAmount: 0,
  completedByUserId: 'owner-1', createdAt: '', completedAt: '', wasAlreadyCompleted: false,
}

function mountForm(overrides: Record<string, unknown> = {}) {
  return mount(ReturnForm, { props: {
    context,
    previewReturn: vi.fn().mockResolvedValue(preview()),
    createReturn: vi.fn().mockResolvedValue(result),
    checkOperation: vi.fn().mockResolvedValue(null),
    loadReturn: vi.fn().mockResolvedValue(result),
    refreshContext: vi.fn().mockResolvedValue(context),
    ...overrides,
  } })
}

async function chooseLine(wrapper: ReturnType<typeof mountForm>, restock = true) {
  await wrapper.get('[aria-label="Chọn trả Cà phê"]').setValue(true)
  await wrapper.get('[aria-label="Số lượng trả Cà phê"]').setValue('1')
  const radio = wrapper.findAll('input[type="radio"]')[restock ? 0 : 1]
  await radio.setValue(true)
}

async function requestPreview(wrapper: ReturnType<typeof mountForm>) {
  await wrapper.findAll('button').find(button => button.text() === 'Cập nhật xem trước')!.trigger('click')
  await flushPromises()
}

afterEach(() => vi.unstubAllGlobals())

describe('ReturnForm', () => {
  it('renders sold/returned/returnable quantities and requires valid quantity plus explicit restock choice', async () => {
    const wrapper = mountForm()
    expect(wrapper.text()).toContain('12.000 ₫/gói')
    expect(wrapper.findAll('tbody td').slice(2, 5).map(cell => cell.text())).toEqual(['3', '1', '2'])
    await wrapper.get('[aria-label="Chọn trả Cà phê"]').setValue(true)
    await wrapper.get('[aria-label="Số lượng trả Cà phê"]').setValue('3')
    await requestPreview(wrapper)
    expect(wrapper.text()).toContain('không vượt quá 2')
    await wrapper.get('[aria-label="Số lượng trả Cà phê"]').setValue('1')
    await requestPreview(wrapper)
    expect(wrapper.text()).toContain('Vui lòng chọn nhập lại kho')
  })

  it('uses selected lines for server preview and never sends frontend price/cost/amount authority', async () => {
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => 'return-operation') })
    const previewReturn = vi.fn().mockResolvedValue(preview())
    const createReturn = vi.fn().mockResolvedValue(result)
    const wrapper = mountForm({ previewReturn, createReturn })
    await chooseLine(wrapper, false)
    await requestPreview(wrapper)
    expect(previewReturn).toHaveBeenCalledWith([{ originalSaleLineId: 'line-1', quantity: 1, restock: false }])
    expect(wrapper.find('[aria-label="Phương thức hoàn tiền"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('giảm công nợ trước')
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    await flushPromises()
    const payload = createReturn.mock.calls[0][0]
    expect(payload).toEqual({
      operationId: 'return-operation', originalSaleId: 'sale-1',
      lines: [{ originalSaleLineId: 'line-1', quantity: 1, restock: false }], refundMethod: null,
      expectedAggregateCustomerDebt: null,
      expectedRequiredActualRefund: 0,
    })
    expect(payload).not.toHaveProperty('refundAmount')
    expect(JSON.stringify(payload)).not.toMatch(/price|cost/i)
  })

  it('requires only a method for positive authoritative refund and never exposes an editable refund amount', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'return-operation' })
    const createReturn = vi.fn()
    const wrapper = mountForm({ previewReturn: vi.fn().mockResolvedValue(preview(12000)), createReturn })
    await chooseLine(wrapper)
    await requestPreview(wrapper)
    expect(wrapper.text()).toContain('Số tiền cần hoàn12.000 ₫')
    expect(wrapper.find('[aria-label="Phương thức hoàn tiền"]').exists()).toBe(true)
    expect(wrapper.find('input[aria-label*="hoàn"]').exists()).toBe(false)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    expect(wrapper.text()).toContain('Vui lòng chọn phương thức hoàn tiền')
    expect(createReturn).not.toHaveBeenCalled()
    await wrapper.get('[aria-label="Phương thức hoàn tiền"]').setValue('Transfer')
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    await flushPromises()
    expect(createReturn.mock.calls[0][0].refundMethod).toBe('Transfer')
  })

  it('locks every return-intention input while the server preview is pending', async () => {
    let resolvePreview!: (value: ReturnPreview) => void
    const pendingPreview = new Promise<ReturnPreview>(resolve => { resolvePreview = resolve })
    const wrapper = mountForm({ previewReturn: vi.fn(() => pendingPreview) })
    await chooseLine(wrapper)

    const previewButton = wrapper.findAll('button').find(button => button.text() === 'Cập nhật xem trước')!
    await previewButton.trigger('click')

    expect(wrapper.get('[aria-label="Chọn trả Cà phê"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-label="Số lượng trả Cà phê"]').attributes('disabled')).toBeDefined()
    for (const radio of wrapper.findAll('input[type="radio"]')) {
      expect(radio.attributes('disabled')).toBeDefined()
    }
    expect(wrapper.findAll('button').find(button => button.text() === 'Đang xem trước…')!.attributes('disabled')).toBeDefined()

    resolvePreview(preview())
    await flushPromises()
    expect(wrapper.get('[aria-label="Chọn trả Cà phê"]').attributes('disabled')).toBeUndefined()
    expect(wrapper.get('[aria-label="Số lượng trả Cà phê"]').attributes('disabled')).toBeUndefined()
    for (const radio of wrapper.findAll('input[type="radio"]')) {
      expect(radio.attributes('disabled')).toBeUndefined()
    }
  })

  it('invalidates a preview when the local intention changes and requires a new server preview', async () => {
    const randomUUID = vi.fn(() => 'must-not-be-created')
    vi.stubGlobal('crypto', { randomUUID })
    const createReturn = vi.fn()
    const wrapper = mountForm({ createReturn })
    await chooseLine(wrapper)
    await requestPreview(wrapper)
    expect(wrapper.find('[aria-label="Xem trước trả hàng"]').exists()).toBe(true)

    await wrapper.get('[aria-label="Số lượng trả Cà phê"]').setValue('2')
    expect(wrapper.find('[aria-label="Xem trước trả hàng"]').exists()).toBe(false)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')

    expect(wrapper.text()).toContain('Dữ liệu trả hàng đã thay đổi. Vui lòng cập nhật xem trước lại.')
    expect(createReturn).not.toHaveBeenCalled()
    expect(randomUUID).not.toHaveBeenCalled()
  })

  it('cannot submit a different intention while its exact preview request is in flight', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'return-operation' })
    let resolvePreview!: (value: ReturnPreview) => void
    const pendingPreview = new Promise<ReturnPreview>(resolve => { resolvePreview = resolve })
    const createReturn = vi.fn().mockResolvedValue(result)
    const wrapper = mountForm({ previewReturn: vi.fn(() => pendingPreview), createReturn })
    await chooseLine(wrapper)

    await wrapper.findAll('button').find(button => button.text() === 'Cập nhật xem trước')!.trigger('click')
    const quantity = wrapper.get('[aria-label="Số lượng trả Cà phê"]')
    expect(quantity.attributes('disabled')).toBeDefined()
    expect((quantity.element as HTMLInputElement).value).toBe('1')

    resolvePreview(preview())
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    await flushPromises()
    expect(createReturn.mock.calls[0][0].lines).toEqual([
      { originalSaleLineId: 'line-1', quantity: 1, restock: true },
    ])
  })

  it.each([
    ['network', new TypeError('lost')],
    ['HTTP 408', new ApiError(408, { title: 'Timeout' })],
    ['operation-lock-timeout', new ApiError(409, { code: 'operation-lock-timeout', title: 'Busy' })],
  ])('preserves the exact immutable attempt after ambiguous %s and retries it', async (_, failure) => {
    const randomUUID = vi.fn(() => 'same-operation')
    vi.stubGlobal('crypto', { randomUUID })
    const attempts: unknown[] = []
    const createReturn = vi.fn(async (attempt) => {
      attempts.push(structuredClone(attempt))
      if (attempts.length === 1) throw failure
      return result
    })
    const checkOperation = vi.fn().mockResolvedValue(null)
    const wrapper = mountForm({ createReturn, checkOperation })
    await chooseLine(wrapper)
    await requestPreview(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    await flushPromises()
    expect(checkOperation).toHaveBeenCalledWith('same-operation')
    expect(wrapper.get('[aria-label="Số lượng trả Cà phê"]').attributes('disabled')).toBeDefined()
    await wrapper.findAll('button').find(button => button.text() === 'Thử lại đúng thao tác')!.trigger('click')
    await flushPromises()
    expect(attempts[1]).toEqual(attempts[0])
    expect(randomUUID).toHaveBeenCalledTimes(1)
  })

  it('loads the Return result when a genuinely ambiguous operation is completed', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'same-operation' })
    const loadReturn = vi.fn().mockResolvedValue(result)
    const wrapper = mountForm({
      createReturn: vi.fn().mockRejectedValue(new TypeError('response lost')),
      checkOperation: vi.fn().mockResolvedValue({ operationId: 'same-operation', status: 'Completed', operationType: 'CreateReturn', resultReference: 'return-2' }),
      loadReturn,
    })
    await chooseLine(wrapper); await requestPreview(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    await flushPromises()
    expect(loadReturn).toHaveBeenCalledWith('return-2')
    expect(wrapper.emitted('completed')?.[0]).toEqual([result])
  })

  it('does not reinterpret idempotency reuse as success and refreshes context after over-return', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'same-operation' })
    const checkOperation = vi.fn()
    const wrapper = mountForm({
      createReturn: vi.fn().mockRejectedValue(new ApiError(409, { code: 'idempotency-key-reused', title: 'Reused' })),
      checkOperation,
    })
    await chooseLine(wrapper); await requestPreview(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    await flushPromises()
    expect(checkOperation).not.toHaveBeenCalled()
    expect(wrapper.emitted('completed')).toBeUndefined()
    expect(wrapper.text()).toContain('Mã thao tác đã được dùng')

    const refreshContext = vi.fn().mockResolvedValue(context)
    const concurrent = mountForm({
      createReturn: vi.fn().mockRejectedValue(new ApiError(409, { code: 'return-quantity-exceeds-remaining' })),
      refreshContext,
    })
    await chooseLine(concurrent); await requestPreview(concurrent)
    await concurrent.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    await flushPromises()
    expect(refreshContext).toHaveBeenCalledOnce()
    expect(concurrent.emitted('contextReloaded')?.[0]).toEqual([context])
  })

  it('clears preview, preview snapshot, refund method, and inputs when context is refreshed', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'stale-operation' })
    const refreshedContext: ReturnContext = {
      ...context,
      totalReturnedAmount: 24000,
      lines: [{ ...context.lines[0], previouslyReturnedQuantity: 2, returnableQuantity: 1 }],
    }
    const wrapper = mountForm({
      previewReturn: vi.fn().mockResolvedValue(preview(12000)),
      createReturn: vi.fn().mockRejectedValue(new ApiError(409, { code: 'return-quantity-exceeds-remaining' })),
      refreshContext: vi.fn().mockResolvedValue(refreshedContext),
    })
    await chooseLine(wrapper)
    await requestPreview(wrapper)
    await wrapper.get('[aria-label="Phương thức hoàn tiền"]').setValue('Cash')
    await wrapper.findAll('button').find(button => button.text() === 'Hoàn tất trả hàng')!.trigger('click')
    await flushPromises()

    const exposed = wrapper.vm as unknown as {
      preview: ReturnPreview | null
      previewSnapshot: unknown
      refundMethod: string
      entries: Record<string, { selected: boolean; quantity: number; restock: boolean | null }>
    }
    expect(exposed.preview).toBeNull()
    expect(exposed.previewSnapshot).toBeNull()
    expect(exposed.refundMethod).toBe('')
    expect(exposed.entries['line-1']).toEqual({ selected: false, quantity: 0, restock: null })
    expect(wrapper.emitted('contextReloaded')?.[0]).toEqual([refreshedContext])
    expect(wrapper.text()).toContain('Số lượng có thể trả đã thay đổi')
  })

  it('blocks double-submit while the create request is active', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'same-operation' })
    let resolve!: (value: ReturnResult) => void
    const pending = new Promise<ReturnResult>(done => { resolve = done })
    const createReturn = vi.fn(() => pending)
    const wrapper = mountForm({ createReturn })
    await chooseLine(wrapper); await requestPreview(wrapper)
    const button = wrapper.findAll('button').find(item => item.text() === 'Hoàn tất trả hàng')!
    await Promise.all([button.trigger('click'), button.trigger('click')])
    expect(createReturn).toHaveBeenCalledTimes(1)
    resolve(result)
    await flushPromises()
  })
})
