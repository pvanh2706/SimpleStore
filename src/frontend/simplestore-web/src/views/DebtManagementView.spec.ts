import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest } from '../api/client'
import { debtDemoEnabled } from '../debts/demo'
import { useAuthStore } from '../stores/auth'
import DebtManagementView from './DebtManagementView.vue'

vi.mock('../api/client', async importOriginal => ({
  ...(await importOriginal<typeof import('../api/client')>()),
  apiRequest: vi.fn(),
}))
const push = vi.fn()
vi.mock('vue-router', () => ({
  useRouter: () => ({ push }),
  RouterLink: { props: ['to'], template: '<a :href="to"><slot /></a>' },
}))
const request = vi.mocked(apiRequest)
const mounted: Array<{ unmount: () => void }> = []
const page = (outstandingAmount = 100) => ({
  items: [{ partyId: 'customer-1', partyName: 'An', phone: '0901', outstandingAmount, asOf: '2026-09-23T03:00:00Z' }],
  page: 1, pageSize: 20, totalCount: 1, totalPages: 1, asOf: '2026-09-23T03:00:00Z',
})
const payment = (patch: Record<string, unknown> = {}) => ({
  id: 'payment-1', partyId: 'customer-1', direction: 'MoneyIn', purpose: 'CustomerDebtCollection', amount: 30, method: 'Transfer', note: 'note',
  occurredAt: '2026-09-23T04:00:00Z', performedByUserId: '', outstandingBefore: 100, outstandingAfter: 70, wasAlreadyRecorded: true, ...patch,
})

function mountView(kind: 'customer' | 'supplier' = 'customer', role: 'Owner' | 'Cashier' = 'Owner') {
  const pinia = createPinia()
  setActivePinia(pinia)
  useAuthStore().session = { isAuthenticated: true, email: 'user@test', storeId: 'store-1', roles: [role], hasStore: true }
  const wrapper = mount(DebtManagementView, { props: { kind }, attachTo: document.body, global: { plugins: [pinia] } })
  mounted.push(wrapper)
  return wrapper
}
type Wrapper = ReturnType<typeof mountView>
const button = (wrapper: Wrapper, text: string) => wrapper.findAll('button').find(candidate => candidate.text() === text)!
const listLoads = () => request.mock.calls.filter(([path]) => String(path).includes('/debts?') && !String(path).includes('pageSize=100')).length
const postBodies = () => request.mock.calls.filter(call => call[1]?.method === 'POST').map(call => JSON.parse(String(call[1]?.body)))
async function startPayment(wrapper: Wrapper, amount?: number, method?: 'Cash' | 'Transfer', note?: string) {
  await wrapper.get('.debt-row-pay').trigger('click')
  const dialog = wrapper.get('[aria-label="Thu tiền khách hàng"]')
  if (amount !== undefined) await dialog.get('input[type="number"]').setValue(String(amount))
  if (method) await dialog.get('select').setValue(method)
  if (note !== undefined) await dialog.get('textarea').setValue(note)
  await dialog.get('.debt-modal-foot .debt-primary').trigger('click')
  return dialog
}
const confirmButton = (wrapper: Wrapper) => wrapper.get('[aria-label="Thu tiền khách hàng"] .debt-modal-foot .debt-primary')

beforeEach(() => {
  push.mockReset()
  request.mockReset()
  vi.stubGlobal('crypto', { randomUUID: vi.fn(() => 'operation-1') })
})
afterEach(() => {
  debtDemoEnabled.value = false
  mounted.forEach(wrapper => wrapper.unmount())
  mounted.length = 0
  vi.unstubAllGlobals()
})

describe('DebtManagementView payment safety', () => {
  it('preserves and retries the exact immutable attempt after an ambiguous result', async () => {
    let paymentCalls = 0
    request.mockImplementation(async (path, init) => {
      const value = String(path)
      if (value.includes('/debts?')) return { ...page(paymentCalls > 2 ? 70 : 100), totalCount: 21, totalPages: 2 } as never
      if (value.includes('/api/operations/')) return null as never
      if (init?.method === 'POST') {
        paymentCalls += 1
        if (paymentCalls <= 2) throw new TypeError('network')
        return payment() as never
      }
      throw new Error(`Unexpected ${value}`)
    })
    const wrapper = mountView()
    await flushPromises()
    await startPayment(wrapper, 30, 'Transfer', ' note ')
    expect(wrapper.text()).toContain('Đây là tiền thực thu, không phải doanh thu')
    await confirmButton(wrapper).trigger('click')
    await flushPromises()

    const exposed = wrapper.vm as unknown as { attempt: { operationId: string; amount: number; method: string; note: string; expectedOutstandingAmount: number } }
    expect(exposed.attempt).toMatchObject({ operationId: 'operation-1', amount: 30, method: 'Transfer', note: 'note', expectedOutstandingAmount: 100 })
    expect(wrapper.text()).toContain('Chưa xác định được kết quả')
    expect(confirmButton(wrapper).text()).toBe('Thử lại đúng thao tác')
    const immutableAttempt = JSON.parse(JSON.stringify(exposed.attempt))
    expect(wrapper.get('input[aria-label="Tìm công nợ"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('button[aria-label="Trang sau"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[aria-label="Thu tiền khách hàng"] button[aria-label="Đóng"]').attributes('disabled')).toBeDefined()
    expect(button(wrapper, '← Quay lại').attributes('disabled')).toBeDefined()
    expect(listLoads()).toBe(1)

    await confirmButton(wrapper).trigger('click')
    await flushPromises()
    expect(postBodies()).toHaveLength(3)
    expect(postBodies().every(body => JSON.stringify(body) === JSON.stringify(immutableAttempt))).toBe(true)
    expect(exposed.attempt).toBeNull()
    expect(wrapper.get('[aria-label="Chi tiết thanh toán công nợ"]').text()).toContain('Công nợ còn lại: 70 đ')
  })

  it('reloads stale debt and never automatically resubmits it', async () => {
    let posts = 0
    request.mockImplementation(async (path, init) => {
      const value = String(path)
      if (value.includes('/debts?')) return page(posts ? 80 : 100) as never
      if (value.endsWith('/customer-1/debt')) return page(80).items[0] as never
      if (init?.method === 'POST') {
        posts += 1
        throw new ApiError(409, { code: 'customer-debt-changed', title: 'changed' })
      }
      throw new Error(`Unexpected ${value}`)
    })
    const wrapper = mountView()
    await flushPromises()
    await startPayment(wrapper, 30)
    await confirmButton(wrapper).trigger('click')
    await flushPromises()

    expect(posts).toBe(1)
    expect((wrapper.vm as unknown as { attempt: unknown }).attempt).toBeNull()
    const dialog = wrapper.get('[aria-label="Thu tiền khách hàng"]')
    expect(dialog.text()).toContain('Dữ liệu đã thay đổi')
    expect(dialog.text()).toContain('giao dịch chưa được gửi lại')
    expect(dialog.get('.debt-amount-box').text()).toContain('80 đ')
    expect(listLoads()).toBe(2)
  })

  it('recovers a committed operation by exact retry with the same snapshot', async () => {
    const posts: Array<Record<string, unknown>> = []
    request.mockImplementation(async (path, init) => {
      const value = String(path)
      if (value.includes('/debts?')) return page(posts.length > 1 ? 70 : 100) as never
      if (value.includes('/api/operations/')) return { operationId: 'operation-1', status: 'Completed', operationType: 'RecordCustomerDebtPayment', resultReference: 'payment-1' } as never
      if (init?.method === 'POST') {
        posts.push(JSON.parse(String(init.body)))
        if (posts.length === 1) throw new TypeError('lost response')
        return payment({ method: 'Cash', note: null }) as never
      }
      throw new Error(`Unexpected ${value}`)
    })
    const wrapper = mountView()
    await flushPromises()
    await startPayment(wrapper, 30)
    await confirmButton(wrapper).trigger('click')
    await flushPromises()

    expect(posts).toHaveLength(2)
    expect(posts[0]).toEqual(posts[1])
    const result = wrapper.get('[aria-label="Chi tiết thanh toán công nợ"]')
    expect(result.text()).toContain('Tiền mặt')
    expect(result.text()).toContain('100 đ → 70 đ')
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
    const wrapper = mountView()
    await flushPromises()
    await startPayment(wrapper)
    await confirmButton(wrapper).trigger('click')
    await confirmButton(wrapper).trigger('click')
    expect(posts).toBe(1)
    resolvePayment(payment({ amount: 100, outstandingAfter: 0 }))
    await flushPromises()
    expect(wrapper.text()).toContain('Khách hàng không còn công nợ hiện tại.')
  })

  it('rejects an amount above the authoritative outstanding before any request', async () => {
    request.mockImplementation(async path => { if (String(path).includes('/debts?')) return page() as never; throw new Error('Unexpected') })
    const wrapper = mountView()
    await flushPromises()
    const dialog = await startPayment(wrapper, 150)
    expect(dialog.text()).toContain('Số tiền phải lớn hơn 0 và không vượt quá công nợ hiện tại.')
    expect(postBodies()).toHaveLength(0)
  })
})

describe('DebtManagementView live workspace', () => {
  beforeEach(() => {
    request.mockImplementation(async path => {
      const value = String(path)
      if (value.includes('/debts?')) return { ...page(), items: [...page().items, { ...page().items[0]!, partyId: 'customer-2', partyName: 'Bình', phone: null, outstandingAmount: 250 }], totalCount: 2 } as never
      if (value.endsWith('/customer-1/debt')) return page().items[0] as never
      throw new Error(`Unexpected ${value}`)
    })
  })

  it('shows only current outstanding, never due or overdue figures', async () => {
    const wrapper = mountView()
    await flushPromises()
    expect(wrapper.findAll('.debt-summary-card strong').map(card => card.text())).toEqual(['2', '350 đ', expect.stringMatching(/^\d{2}:\d{2}$/)])
    expect(wrapper.get('.debt-table thead').text()).not.toContain('quá hạn')
    expect(wrapper.get('.debt-table thead').text()).not.toContain('Tổng phát sinh')
    expect(wrapper.findAll('.debt-status').map(status => status.text())).toEqual(['Đang nợ', 'Đang nợ'])
    expect(wrapper.text()).not.toContain('Demo trạng thái')
  })

  it('keeps Supplier mode Owner-only and switches modes through the router', async () => {
    const cashier = mountView('customer', 'Cashier')
    await flushPromises()
    expect(cashier.findAll('.debt-seg').map(item => item.text())).toEqual(['Khách hàng'])
    const owner = mountView()
    await flushPromises()
    await button(owner, 'Nhà cung cấp').trigger('click')
    expect(push).toHaveBeenCalledWith('/suppliers/debts')
  })

  it('opens a live detail with the conceptual explanation instead of unsupported history', async () => {
    const wrapper = mountView()
    await flushPromises()
    await button(wrapper, 'An').trigger('click')
    await flushPromises()
    const drawer = wrapper.get('[aria-label="Chi tiết công nợ khách hàng"]')
    expect(drawer.findAll('.debt-tab').map(tab => tab.text())).toEqual(['Tổng quan', 'Giải thích công nợ'])
    await button(wrapper, 'Giải thích công nợ').trigger('click')
    expect(drawer.text()).toContain('Nghĩa vụ phát sinh hợp lệ')
    expect(drawer.text()).not.toContain('Tổng phát sinh – Đã thanh toán')
  })
})

describe('DebtManagementView sample data', () => {
  it('reproduces the reference customer overview without calling the API', async () => {
    debtDemoEnabled.value = true
    const wrapper = mountView()
    await flushPromises()
    expect(request).not.toHaveBeenCalled()
    expect(wrapper.findAll('.debt-summary-card strong').map(card => card.text())).toEqual(['12', '18.450.000 đ', '5', '3'])
    expect(wrapper.findAll('.debt-link').map(link => link.text())).toEqual(['Nguyễn Văn A', 'Trần Thị B', 'Lê Văn C', 'Phạm Thị D', 'Hoàng Văn E'])
    expect(wrapper.findAll('.debt-status').map(status => status.text())).toEqual(['Quá hạn', 'Đã thanh toán', 'Quá hạn', 'Sắp đến hạn', 'Đã thanh toán'])
    expect(wrapper.text()).toContain('Hiển thị 1 – 5 / 12 khách hàng')
    expect(wrapper.findAll('.debt-pager button').map(item => item.text())).toEqual(['‹', '1', '2', '3', '›'])
    expect(wrapper.findAll('.debt-demo-strip button')).toHaveLength(7)
  })

  it('collects a sample payment and moves the reference totals', async () => {
    debtDemoEnabled.value = true
    const wrapper = mountView()
    await button(wrapper, 'Nguyễn Văn A').trigger('click')
    const drawer = wrapper.get('[aria-label="Chi tiết công nợ khách hàng"]')
    expect(drawer.text()).toContain('Khách quen, lấy hàng thường xuyên')
    expect(drawer.findAll('.debt-mini-table tbody tr')).toHaveLength(5)
    await drawer.get('.debt-action-row .debt-primary').trigger('click')
    const dialog = wrapper.get('[aria-label="Thu tiền khách hàng"]')
    expect((dialog.get('input[type="number"]').element as HTMLInputElement).value).toBe('1000000')
    await dialog.get('.debt-modal-foot .debt-primary').trigger('click')
    await dialog.get('.debt-modal-foot .debt-primary').trigger('click')
    expect(dialog.get('[aria-label="Chi tiết thanh toán công nợ"]').text()).toContain('3.200.000 đ → 2.200.000 đ')
    await button(wrapper, 'Xong').trigger('click')
    expect(wrapper.findAll('.debt-summary-card strong').map(card => card.text())).toEqual(['12', '17.450.000 đ', '5', '3'])
    expect(wrapper.findAll('.debt-table tbody tr')[0]!.text()).toContain('3.000.000 đ')
    expect(request).not.toHaveBeenCalled()
  })

  it('shows the supplier reference with settlement wording', async () => {
    debtDemoEnabled.value = true
    const wrapper = mountView('supplier')
    expect(wrapper.findAll('.debt-summary-card strong').map(card => card.text())).toEqual(['3', '12.200.000 đ', '1', '2'])
    expect(wrapper.text()).toContain('Hiển thị 1 – 5 / 9 nhà cung cấp')
    await button(wrapper, 'Thiên Long').trigger('click')
    const drawer = wrapper.get('[aria-label="Chi tiết công nợ nhà cung cấp"]')
    expect(drawer.text()).toContain('123 Đường ABC, Quận 1, TP.HCM')
    expect(drawer.findAll('.debt-tab').map(tab => tab.text())).toEqual(['Tổng quan', 'Lịch sử giao dịch (6)', 'Lịch sử trả tiền (2)', 'Giải thích công nợ'])
    await button(wrapper, 'Giải thích công nợ').trigger('click')
    expect(drawer.text()).toContain('= 25.600.000 đ – 20.400.000 đ')
    await drawer.get('.debt-action-row .debt-primary').trigger('click')
    expect(wrapper.get('[aria-label="Trả tiền nhà cung cấp"]').text()).toContain('Số tiền trả')
  })

  it('previews the reference special states', async () => {
    debtDemoEnabled.value = true
    const wrapper = mountView()
    for (const [label, title] of [['Không quyền', 'Bạn không có quyền thực hiện thao tác này'], ['Xác thực lại', 'Dữ liệu đã thay đổi'], ['Rỗng', 'Chưa có khách hàng nào đang nợ']]) {
      await button(wrapper, label!).trigger('click')
      expect(wrapper.get('.debt-special h2').text()).toBe(title)
      await wrapper.get('.debt-special button').trigger('click')
      expect(wrapper.find('.debt-special').exists()).toBe(false)
    }
  })
})
