import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest } from '../api/client'
import { useAuthStore } from '../stores/auth'
import type { Purchase } from '../api/types'
import PurchaseDetailView from './PurchaseDetailView.vue'

vi.mock('../api/client', async importOriginal => {
  const actual = await importOriginal<typeof import('../api/client')>()
  return { ...actual, apiRequest: vi.fn() }
})
vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'purchase-1' } }),
  RouterLink: { props: ['to'], template: '<a><slot /></a>' },
}))

const draft: Purchase = {
  id: 'purchase-1', supplierId: 'supplier-1', supplierName: 'NCC', status: 'Draft',
  lines: [{ id: 'line-1', productId: 'product-1', productName: 'Coffee', productUnit: 'pack', quantity: 2, unitPrice: 8000, lineAmount: 16000 }],
  payments: [], totalAmount: 16000, paidAmount: 0, outstandingAmount: 16000,
  createdAt: '', updatedAt: '', completedAt: null, wasAlreadyCompleted: false, isVoided: false, void: null,
}
const completed: Purchase = {
  ...draft, status: 'Completed', payments: [{ id: 'payment-1', amount: 10000, method: 'Cash', paidAt: '' }],
  paidAmount: 10000, outstandingAmount: 6000, completedAt: '2026-09-22T00:00:00Z',
}

function mountView(purchaseValue: Purchase, role: 'Owner' | 'Cashier' = 'Owner') {
  const pinia = createPinia()
  setActivePinia(pinia)
  useAuthStore().session = { isAuthenticated: true, email: 'user@test', storeId: 'store-1', roles: [role], hasStore: true }
  vi.mocked(apiRequest).mockResolvedValue(purchaseValue as never)
  return mount(PurchaseDetailView, { global: { plugins: [pinia] } })
}

describe('PurchaseDetailView correction state', () => {
  beforeEach(() => vi.mocked(apiRequest).mockReset())

  it('does not show Void for a Draft Purchase', async () => {
    const wrapper = mountView(draft)
    await flushPromises()
    expect(wrapper.text()).toContain('Sửa nháp')
    expect(wrapper.text()).not.toContain('Hủy phiếu nhập')
  })

  it('shows Void for an Owner with a Completed Purchase but not for a Cashier', async () => {
    const owner = mountView(completed)
    await flushPromises()
    expect(owner.text()).toContain('Hủy phiếu nhập')
    const cashier = mountView(completed, 'Cashier')
    await flushPromises()
    expect(cashier.text()).not.toContain('Hủy phiếu nhập')
  })

  it('shows void evidence and keeps original Purchase lines/payments visible without active Void', async () => {
    const voided: Purchase = {
      ...completed, isVoided: true, outstandingAmount: 0,
      void: { id: 'void-1', reason: 'Nhập sai phiếu', voidedByUserId: 'owner-1', voidedAt: '2026-09-22T01:00:00Z' },
    }
    const wrapper = mountView(voided)
    await flushPromises()
    expect(wrapper.text()).toContain('Đã hủy')
    expect(wrapper.text()).toContain('Nhập sai phiếu')
    expect(wrapper.text()).toContain('Coffee')
    expect(wrapper.text()).toContain('Tiền mặt')
    expect(wrapper.findAll('button').some(button => button.text() === 'Hủy phiếu nhập')).toBe(false)
  })

  it.each([
    ['purchase-void-reversal-basis-unavailable', 'Phiếu nhập cũ này không có đủ bằng chứng'],
    ['purchase-void-downstream-inventory-dependency', 'Tồn kho đã thay đổi sau phiếu nhập này'],
    ['purchase-void-reference-cost-dependency', 'Giá nhập tham chiếu đã thay đổi sau phiếu nhập'],
  ])('surfaces useful feedback for %s', async (code, message) => {
    vi.stubGlobal('crypto', { randomUUID: () => 'void-operation' })
    const wrapper = mountView(completed)
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'Hủy phiếu nhập')!.trigger('click')
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('Sai phiếu')
    vi.mocked(apiRequest).mockRejectedValueOnce(new ApiError(409, { code }))
    await wrapper.get('[aria-label="Xác nhận hủy phiếu nhập"] button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain(message)
  })
})
