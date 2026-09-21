import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import PurchaseCompletionPanel from './PurchaseCompletionPanel.vue'
import type { Purchase } from '../api/types'

const purchase: Purchase = {
  id: 'purchase-1', supplierId: 'supplier-1', supplierName: 'NCC', status: 'Draft',
  lines: [], payments: [], totalAmount: 100, paidAmount: 0, outstandingAmount: 100,
  createdAt: '', updatedAt: '', completedAt: null, wasAlreadyCompleted: false,
}

afterEach(() => vi.unstubAllGlobals())

describe('PurchaseCompletionPanel', () => {
  it('previews paid and outstanding amounts', async () => {
    const wrapper = mount(PurchaseCompletionPanel, {
      props: { purchase, completePurchase: vi.fn(), checkOperation: vi.fn() },
    })
    await wrapper.get('input[aria-label="Số tiền thanh toán"]').setValue('30')
    await wrapper.get('button.btn-secondary').trigger('click')

    expect(wrapper.text()).toContain('Đã trả30 ₫')
    expect(wrapper.text()).toContain('Còn nợ70 ₫')
  })

  it('keeps the same OperationId after an ambiguous network failure', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'operation-fixed' })
    const completePurchase = vi.fn().mockRejectedValue(new TypeError('network'))
    const checkOperation = vi.fn().mockResolvedValue(null)
    const wrapper = mount(PurchaseCompletionPanel, {
      props: { purchase, completePurchase, checkOperation },
    })

    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Thử lại cùng thao tác')
    await wrapper.get('button.btn-primary').trigger('click')
    await flushPromises()

    expect(completePurchase).toHaveBeenCalledTimes(2)
    expect(completePurchase.mock.calls[0][0]).toBe('operation-fixed')
    expect(completePurchase.mock.calls[1][0]).toBe('operation-fixed')
    expect(checkOperation).toHaveBeenCalledWith('operation-fixed')
  })
})
