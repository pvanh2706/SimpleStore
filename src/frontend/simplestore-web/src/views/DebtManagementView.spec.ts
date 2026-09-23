import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest } from '../api/client'
import DebtManagementView from './DebtManagementView.vue'

vi.mock('../api/client', async importOriginal => ({
  ...(await importOriginal<typeof import('../api/client')>()),
  apiRequest: vi.fn(),
}))
const request = vi.mocked(apiRequest)
const page = (outstandingAmount = 100) => ({
  items: [{ partyId: 'customer-1', partyName: 'An', phone: '0901', outstandingAmount, asOf: '' }],
  page: 1, pageSize: 20, totalCount: 1, totalPages: 1, asOf: '',
})

describe('DebtManagementView', () => {
  beforeEach(() => {
    request.mockReset()
    vi.stubGlobal('confirm', vi.fn(() => true))
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => 'operation-1') })
  })

  it('preserves and retries the exact immutable attempt after an ambiguous result', async () => {
    const payment = { id: 'payment-1', partyId: 'customer-1', direction: 'MoneyIn', purpose: 'CustomerDebtCollection', amount: 30, method: 'Transfer', note: 'note', occurredAt: '', performedByUserId: '', outstandingBefore: 100, outstandingAfter: 70, wasAlreadyRecorded: true }
    let paymentCalls = 0
    request.mockImplementation(async (path, init) => {
      const value = String(path)
      if (value.includes('/debts?')) return page(paymentCalls > 2 ? 70 : 100) as never
      if (value.includes('/api/operations/')) return null as never
      if (init?.method === 'POST') {
        paymentCalls += 1
        if (paymentCalls <= 2) throw new TypeError('network')
        return payment as never
      }
      throw new Error(`Unexpected ${value}`)
    })
    const wrapper = mount(DebtManagementView, { props: { kind: 'customer' } })
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'thu nợ')!.trigger('click')
    await wrapper.get('input[type="number"]').setValue('30')
    await wrapper.get('select').setValue('Transfer')
    await wrapper.get('textarea').setValue(' note ')
    await wrapper.get('form.card').trigger('submit')
    await flushPromises()

    const exposed = wrapper.vm as unknown as { attempt: { operationId: string; amount: number; method: string; note: string } }
    expect(exposed.attempt).toMatchObject({ operationId: 'operation-1', amount: 30, method: 'Transfer', note: 'note' })
    expect(wrapper.get('input[type="number"]').attributes('disabled')).toBeDefined()

    await wrapper.get('form.card').trigger('submit')
    await flushPromises()
    const postBodies = request.mock.calls
      .filter(call => call[1]?.method === 'POST')
      .map(call => JSON.parse(String(call[1]?.body)))
    expect(postBodies).toHaveLength(3)
    expect(postBodies.every(body => body.operationId === 'operation-1'
      && body.amount === 30 && body.method === 'Transfer' && body.note === 'note')).toBe(true)
    expect(wrapper.text()).toContain('Công nợ còn lại: 70 ₫')
  })

  it('reloads stale debt and never automatically resubmits it', async () => {
    let loads = 0
    let posts = 0
    request.mockImplementation(async (path, init) => {
      if (String(path).includes('/debts?')) { loads += 1; return page(loads === 1 ? 100 : 80) as never }
      if (init?.method === 'POST') {
        posts += 1
        throw new ApiError(409, { code: 'customer-debt-changed', title: 'changed' })
      }
      throw new Error('Unexpected request')
    })
    const wrapper = mount(DebtManagementView, { props: { kind: 'customer' } })
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'thu nợ')!.trigger('click')
    await wrapper.get('input[type="number"]').setValue('30')
    await wrapper.get('form.card').trigger('submit')
    await flushPromises()

    expect(posts).toBe(1)
    expect(loads).toBe(2)
    expect((wrapper.vm as unknown as { attempt: unknown }).attempt).toBeNull()
    expect(wrapper.text()).toContain('giao dịch chưa được gửi lại')
  })

  it('recovers a committed operation by exact retry with the same snapshot', async () => {
    const posts: Array<Record<string, unknown>> = []
    request.mockImplementation(async (path, init) => {
      const value = String(path)
      if (value.includes('/debts?')) return page(posts.length > 1 ? 70 : 100) as never
      if (value.includes('/api/operations/')) return {
        operationId: 'operation-1', status: 'Completed',
        operationType: 'RecordCustomerDebtPayment', resultReference: 'payment-1',
      } as never
      if (init?.method === 'POST') {
        posts.push(JSON.parse(String(init.body)))
        if (posts.length === 1) throw new TypeError('lost response')
        return { id: 'payment-1', partyId: 'customer-1', direction: 'MoneyIn', purpose: 'CustomerDebtCollection', amount: 30, method: 'Cash', note: null, occurredAt: '2026-09-23T00:00:00Z', performedByUserId: '', outstandingBefore: 100, outstandingAfter: 70, wasAlreadyRecorded: true } as never
      }
      throw new Error('Unexpected request')
    })
    const wrapper = mount(DebtManagementView, { props: { kind: 'customer' } })
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'thu nợ')!.trigger('click')
    await wrapper.get('input[type="number"]').setValue('30')
    await wrapper.get('form.card').trigger('submit')
    await flushPromises()

    expect(posts).toHaveLength(2)
    expect(posts[0]).toEqual(posts[1])
    expect(wrapper.text()).toContain('Công nợ còn lại: 70 ₫')
    expect(wrapper.get('[aria-label="Chi tiết thanh toán công nợ"]').text()).toContain('Tiền mặt')
  })

  it('prevents double-click from sending a second request', async () => {
    let posts = 0
    let resolvePayment!: (value: unknown) => void
    const pending = new Promise(resolve => { resolvePayment = resolve })
    request.mockImplementation(async (path, init) => {
      if (String(path).includes('/debts?')) return page() as never
      if (init?.method === 'POST') { posts += 1; return await pending as never }
      throw new Error('Unexpected request')
    })
    const wrapper = mount(DebtManagementView, { props: { kind: 'customer' } })
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'thu nợ')!.trigger('click')
    await wrapper.get('form.card').trigger('submit')
    await wrapper.get('form.card').trigger('submit')
    expect(posts).toBe(1)
    resolvePayment({ id: 'payment-1', partyId: 'customer-1', direction: 'MoneyIn', purpose: 'CustomerDebtCollection', amount: 100, method: 'Cash', note: null, occurredAt: '', performedByUserId: '', outstandingBefore: 100, outstandingAfter: 0, wasAlreadyRecorded: false })
    await flushPromises()
  })
})
