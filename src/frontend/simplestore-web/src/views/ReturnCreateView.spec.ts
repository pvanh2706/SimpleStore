import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { apiRequest } from '../api/client'
import type { ReturnContext } from '../api/types'
import ReturnCreateView from './ReturnCreateView.vue'

const push = vi.fn()
vi.mock('../api/client', () => ({ apiRequest: vi.fn() }))
vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'sale-1' } }),
  useRouter: () => ({ push }),
  RouterLink: { props: ['to'], template: '<a><slot /></a>' },
}))

const context: ReturnContext = {
  saleId: 'sale-1', isVoided: false, hasReturns: false,
  originalTotalAmount: 100, totalReturnedAmount: 0, netSaleAmount: 100,
  originalCollectedAmount: 100, totalRefundedAmount: 0, netCollectedAmount: 100, outstandingAmount: 0,
  lines: [],
}

describe('ReturnCreateView', () => {
  it('navigates back to Sale detail after Return completion so authoritative state is reloaded', async () => {
    vi.mocked(apiRequest).mockResolvedValue(context)
    push.mockReset()
    const wrapper = mount(ReturnCreateView, { global: { stubs: {
      ReturnForm: { template: '<button aria-label="complete-return" @click="$emit(\'completed\', {})">Complete</button>' },
    } } })
    await flushPromises()
    await wrapper.get('[aria-label="complete-return"]').trigger('click')
    await flushPromises()
    expect(push).toHaveBeenCalledWith('/sales/sale-1')
  })
})
