import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import PurchaseCompletionPanel from './PurchaseCompletionPanel.vue'
import type { Purchase } from '../api/types'

const purchase: Purchase = {
  id: 'purchase-1', supplierId: 'supplier-1', supplierName: 'NCC', status: 'Draft',
  lines: [], payments: [], totalAmount: 100, paidAmount: 0, outstandingAmount: 100,
  createdAt: '', updatedAt: '', completedAt: null, wasAlreadyCompleted: false,
}
const completedPurchase: Purchase = {
  ...purchase, status: 'Completed', paidAmount: 30, outstandingAmount: 70,
  completedAt: '2026-09-21T00:00:00Z',
}

afterEach(() => vi.unstubAllGlobals())

function mountPanel(overrides: Record<string, unknown> = {}) {
  return mount(PurchaseCompletionPanel, {
    props: {
      purchase,
      completePurchase: vi.fn(),
      checkOperation: vi.fn(),
      loadPurchase: vi.fn(),
      ...overrides,
    },
  })
}

async function addPayment(wrapper: ReturnType<typeof mountPanel>, amount = '30') {
  await wrapper.get('input[aria-label="Số tiền thanh toán"]').setValue(amount)
  await wrapper.get('button.btn-secondary').trigger('click')
}

describe('PurchaseCompletionPanel', () => {
  it('previews paid and outstanding amounts', async () => {
    const wrapper = mountPanel()
    await addPayment(wrapper)
    expect(wrapper.text()).toContain('Đã trả30 ₫')
    expect(wrapper.text()).toContain('Còn nợ70 ₫')
  })

  it('retries an ambiguous request with the same OperationId and exact payment snapshot', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'operation-fixed' })
    const completePurchase = vi.fn().mockRejectedValue(new TypeError('network'))
    const wrapper = mountPanel({ completePurchase, checkOperation: vi.fn().mockResolvedValue(null) })
    await addPayment(wrapper)

    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()
    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()

    expect(completePurchase).toHaveBeenCalledTimes(2)
    expect(completePurchase.mock.calls[0]).toEqual(['operation-fixed', [{ amount: 30, method: 'Cash' }]])
    expect(completePurchase.mock.calls[1]).toEqual(['operation-fixed', [{ amount: 30, method: 'Cash' }]])
  })

  it('locks payment mutation while an operation result is ambiguous', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'operation-fixed' })
    const completePurchase = vi.fn().mockRejectedValue(new TypeError('network'))
    const wrapper = mountPanel({ completePurchase, checkOperation: vi.fn().mockResolvedValue(null) })
    await addPayment(wrapper)
    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()

    expect(wrapper.get('input[aria-label="Số tiền thanh toán"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('select[aria-label="Phương thức"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('button[aria-label="Xóa thanh toán 1"]').attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Dữ liệu thanh toán đang được khóa')
    await wrapper.get('button[aria-label="Xóa thanh toán 1"]').trigger('click')
    expect(wrapper.text()).toContain('30 ₫')
  })

  it('treats operation-lock-timeout as ambiguous and retries the exact attempt', async () => {
    const randomUUID = vi.fn().mockReturnValue('operation-locked')
    vi.stubGlobal('crypto', { randomUUID })
    const completePurchase = vi.fn().mockRejectedValue(new ApiError(409, {
      code: 'operation-lock-timeout', title: 'Thao tác đang được xử lý.',
    }))
    const checkOperation = vi.fn().mockResolvedValue(null)
    const wrapper = mountPanel({ completePurchase, checkOperation })
    await addPayment(wrapper, '40')

    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()

    expect(checkOperation).toHaveBeenCalledWith('operation-locked')
    expect(wrapper.get('input[aria-label="Số tiền thanh toán"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('button[aria-label="Xóa thanh toán 1"]').attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Dữ liệu thanh toán đang được khóa')

    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()

    expect(randomUUID).toHaveBeenCalledTimes(1)
    expect(completePurchase).toHaveBeenCalledTimes(2)
    expect(checkOperation).toHaveBeenCalledTimes(2)
    expect(completePurchase.mock.calls[0]).toEqual(['operation-locked', [{ amount: 40, method: 'Cash' }]])
    expect(completePurchase.mock.calls[1]).toEqual(['operation-locked', [{ amount: 40, method: 'Cash' }]])
  })

  it('does not reinterpret idempotency-key-reused as successful completion', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'operation-fixed' })
    const checkOperation = vi.fn().mockResolvedValue({
      operationId: 'operation-fixed', operationType: 'CompletePurchase',
      status: 'Completed', resultReference: 'purchase-1',
    })
    const wrapper = mountPanel({
      completePurchase: vi.fn().mockRejectedValue(new ApiError(409, {
        code: 'idempotency-key-reused', title: 'Mã thao tác đã được dùng cho yêu cầu khác.',
      })),
      checkOperation,
    })

    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Mã thao tác đã được dùng cho yêu cầu khác.')
    expect(checkOperation).not.toHaveBeenCalled()
    expect(wrapper.emitted('completed')).toBeUndefined()
  })

  it('reloads the committed Purchase when status is Completed after a lost response', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'operation-fixed' })
    const loadPurchase = vi.fn().mockResolvedValue(completedPurchase)
    const wrapper = mountPanel({
      completePurchase: vi.fn().mockRejectedValue(new TypeError('response lost')),
      checkOperation: vi.fn().mockResolvedValue({
        operationId: 'operation-fixed', operationType: 'CompletePurchase',
        status: 'Completed', resultReference: 'purchase-1',
      }),
      loadPurchase,
    })

    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()

    expect(loadPurchase).toHaveBeenCalledWith('purchase-1')
    expect(wrapper.emitted('completed')?.[0]?.[0]).toEqual(completedPurchase)
  })
})
