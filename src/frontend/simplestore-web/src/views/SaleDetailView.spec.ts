import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import SaleDetailView from './SaleDetailView.vue'
import type { Sale } from '../api/types'

vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'sale-1' } }),
  RouterLink: { template: '<a><slot /></a>' },
}))

const sale: Sale = {
  id: 'sale-1', status: 'Completed', storeName: 'Store', warehouseId: 'warehouse-1', customer: null,
  cashierDisplayName: 'cashier@test', lines: [], payments: [], totalAmount: 0, paidAmount: 0,
  outstandingAmount: 0, createdAt: '2026-09-21T12:00:00Z', completedAt: '2026-09-21T12:00:00Z', wasAlreadyCompleted: false,
}

describe('SaleDetailView', () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset().mockResolvedValue(sale)
    vi.spyOn(window, 'print').mockImplementation(() => {})
  })

  it('reprints an existing sale without calling CompleteSale', async () => {
    const wrapper = mount(SaleDetailView)
    await flushPromises()
    await wrapper.get('button').trigger('click')

    expect(apiRequest).toHaveBeenCalledTimes(1)
    expect(apiRequest).toHaveBeenCalledWith('/api/sales/sale-1')
    expect(window.print).toHaveBeenCalledOnce()
  })
})
