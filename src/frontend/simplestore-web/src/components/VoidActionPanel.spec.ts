import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import VoidActionPanel from './VoidActionPanel.vue'

const errorMessages = {
  'purchase-void-reversal-basis-unavailable': 'Phiếu nhập cũ không thể hủy an toàn.',
  'purchase-void-downstream-inventory-dependency': 'Tồn kho đã thay đổi sau phiếu nhập.',
  'purchase-void-reference-cost-dependency': 'Giá nhập tham chiếu đã thay đổi.',
  'idempotency-key-reused': 'Mã thao tác đã được dùng.',
}

function mountPanel(overrides: Record<string, unknown> = {}) {
  return mount(VoidActionPanel, { props: {
    targetId: 'purchase-1', operationType: 'VoidPurchase' as const,
    title: 'Xác nhận hủy', submitLabel: 'Hủy phiếu nhập',
    execute: vi.fn().mockResolvedValue({}),
    checkOperation: vi.fn().mockResolvedValue(null),
    reloadTarget: vi.fn().mockResolvedValue({}),
    errorMessages,
    ...overrides,
  }, slots: { default: 'Lịch sử gốc vẫn được giữ lại.' } })
}

afterEach(() => vi.unstubAllGlobals())

describe('VoidActionPanel', () => {
  it('requires and normalizes a reason, submits once, then reloads authoritative detail', async () => {
    vi.stubGlobal('crypto', { randomUUID: vi.fn(() => 'void-operation') })
    const execute = vi.fn().mockResolvedValue({})
    const reloadTarget = vi.fn().mockResolvedValue({})
    const wrapper = mountPanel({ execute, reloadTarget })
    await wrapper.get('button').trigger('click')
    expect(wrapper.text()).toContain('Vui lòng nhập lý do')
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('  Sai giao dịch  ')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(execute).toHaveBeenCalledWith({ operationId: 'void-operation', targetId: 'purchase-1', reason: 'Sai giao dịch' })
    expect(execute).toHaveBeenCalledTimes(1)
    expect(reloadTarget).toHaveBeenCalledOnce()
    expect(wrapper.emitted('completed')).toHaveLength(1)
  })

  it.each([
    ['network', new TypeError('lost')],
    ['HTTP 408', new ApiError(408, { title: 'Timeout' })],
    ['operation-lock-timeout', new ApiError(409, { code: 'operation-lock-timeout' })],
  ])('keeps the same OperationId and normalized reason after ambiguous %s', async (_, failure) => {
    const randomUUID = vi.fn(() => 'void-operation')
    vi.stubGlobal('crypto', { randomUUID })
    const attempts: unknown[] = []
    const execute = vi.fn(async attempt => {
      attempts.push(structuredClone(attempt))
      if (attempts.length === 1) throw failure
      return {}
    })
    const wrapper = mountPanel({ execute })
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('  Lý do cố định  ')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.get('[aria-label="Lý do hủy"]').attributes('disabled')).toBeDefined()
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(attempts[1]).toEqual(attempts[0])
    expect(randomUUID).toHaveBeenCalledTimes(1)
  })

  it('recovers a completed VoidPurchase operation by reloading the Purchase', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'void-operation' })
    const reloadTarget = vi.fn().mockResolvedValue({ isVoided: true })
    const wrapper = mountPanel({
      execute: vi.fn().mockRejectedValue(new TypeError('response lost')),
      checkOperation: vi.fn().mockResolvedValue({ operationId: 'void-operation', status: 'Completed', operationType: 'VoidPurchase', resultReference: 'purchase-void-1' }),
      reloadTarget,
    })
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('Sai phiếu')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(reloadTarget).toHaveBeenCalledOnce()
    expect(wrapper.emitted('completed')).toHaveLength(1)
  })

  it('recovers a completed VoidSale operation by reloading the original Sale', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'sale-void-operation' })
    const reloadTarget = vi.fn().mockResolvedValue({ isVoided: true })
    const wrapper = mountPanel({
      targetId: 'sale-1', operationType: 'VoidSale', submitLabel: 'Hủy giao dịch',
      execute: vi.fn().mockRejectedValue(new TypeError('response lost')),
      checkOperation: vi.fn().mockResolvedValue({ operationId: 'sale-void-operation', status: 'Completed', operationType: 'VoidSale', resultReference: 'sale-void-1' }),
      reloadTarget,
    })
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('Sai giao dịch')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(reloadTarget).toHaveBeenCalledOnce()
    expect(wrapper.emitted('completed')).toHaveLength(1)
  })

  it('keeps the operation immutable when mutation succeeds but authoritative reload is ambiguous', async () => {
    const randomUUID = vi.fn(() => 'void-operation')
    vi.stubGlobal('crypto', { randomUUID })
    const execute = vi.fn().mockResolvedValue({})
    const reloadTarget = vi.fn().mockRejectedValue(new TypeError('reload failed'))
    const wrapper = mountPanel({ execute, reloadTarget, checkOperation: vi.fn().mockResolvedValue(null) })
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('Sai phiếu')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect((wrapper.vm as unknown as { state: string }).state).toBe('retryable')
    expect((wrapper.vm as unknown as { attempt: { operationId: string } }).attempt.operationId).toBe('void-operation')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(randomUUID).toHaveBeenCalledTimes(1)
    expect(execute.mock.calls[1][0]).toEqual(execute.mock.calls[0][0])
  })

  it.each([
    ['purchase-void-reversal-basis-unavailable', 'Phiếu nhập cũ không thể hủy an toàn.'],
    ['purchase-void-downstream-inventory-dependency', 'Tồn kho đã thay đổi sau phiếu nhập.'],
    ['purchase-void-reference-cost-dependency', 'Giá nhập tham chiếu đã thay đổi.'],
  ])('shows the typed deterministic error %s without querying operation status', async (code, expected) => {
    vi.stubGlobal('crypto', { randomUUID: () => 'void-operation' })
    const checkOperation = vi.fn()
    const wrapper = mountPanel({ execute: vi.fn().mockRejectedValue(new ApiError(409, { code })), checkOperation })
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('Sai phiếu')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain(expected)
    expect(checkOperation).not.toHaveBeenCalled()
  })

  it('does not infer success for idempotency-key-reused', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'void-operation' })
    const checkOperation = vi.fn().mockResolvedValue({ operationId: 'void-operation', status: 'Completed', operationType: 'VoidPurchase', resultReference: 'void-1' })
    const wrapper = mountPanel({ execute: vi.fn().mockRejectedValue(new ApiError(409, { code: 'idempotency-key-reused' })), checkOperation })
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('Sai phiếu')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(checkOperation).not.toHaveBeenCalled()
    expect(wrapper.emitted('completed')).toBeUndefined()
  })

  it('blocks a double click while a Void request is active', async () => {
    vi.stubGlobal('crypto', { randomUUID: () => 'void-operation' })
    let resolve!: (value: unknown) => void
    const execute = vi.fn(() => new Promise(done => { resolve = done }))
    const wrapper = mountPanel({ execute })
    await wrapper.get('[aria-label="Lý do hủy"]').setValue('Sai phiếu')
    const button = wrapper.get('button')
    await Promise.all([button.trigger('click'), button.trigger('click')])
    expect(execute).toHaveBeenCalledTimes(1)
    resolve({})
    await flushPromises()
  })
})
