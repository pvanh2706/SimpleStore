import { expect, test, type Page } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

async function capture(page: Page, name: string) {
  if (!process.env.DEBT_CAPTURE_DIR) return
  await mkdir(process.env.DEBT_CAPTURE_DIR, { recursive: true })
  await page.screenshot({ path: join(process.env.DEBT_CAPTURE_DIR, `${name}.png`) })
}

async function mockOwner(page: Page) {
  await page.route('**/api/auth/session', route => route.fulfill({ json: {
    isAuthenticated: true, email: 'owner@example.test', storeId: 'store-1',
    roles: ['Owner'], hasStore: true, mustChangePassword: false, isEnabled: true,
  } }))
  await page.route('**/api/store/current', route => route.fulfill({ json: {
    id: 'store-1', name: 'Tạp hóa Việt Anh', mainWarehouseId: 'warehouse-1',
    mainWarehouseName: 'Kho chính', timeZoneId: 'Asia/Ho_Chi_Minh',
  } }))
}
const empty = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, asOf: '2024-12-16T03:23:00Z' }

test('debt reference data previews both modes without writing to the API', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  const writes: string[] = []
  page.on('request', request => { if (request.url().includes('/api/') && request.method() !== 'GET') writes.push(request.url()) })
  await mockOwner(page)
  await page.route('**/api/customers/debts?**', route => route.fulfill({ json: empty }))
  await page.route('**/api/suppliers/debts?**', route => route.fulfill({ json: empty }))

  await page.goto('/customers/debts')
  await expect(page.getByText('Chưa có khách hàng nào đang nợ')).toBeVisible()
  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.locator('.debt-table tbody tr')).toHaveCount(5)
  await expect(page.getByText('Hiển thị 1 – 5 / 12 khách hàng')).toBeVisible()
  await capture(page, 'debt-customers')

  await page.getByRole('button', { name: 'Bộ lọc' }).click()
  await capture(page, 'debt-filter')
  const filter = page.getByRole('dialog', { name: 'Bộ lọc công nợ' })
  await filter.getByRole('checkbox', { name: 'Quá hạn (> 7 ngày)' }).check()
  await filter.getByRole('button', { name: 'Áp dụng' }).click()
  await expect(page.locator('.debt-table tbody tr')).toHaveCount(2)
  await page.getByRole('button', { name: 'Bộ lọc' }).click()
  await filter.getByRole('button', { name: 'Xóa bộ lọc' }).click()

  await page.getByRole('button', { name: 'Nguyễn Văn A' }).click()
  const detail = page.getByRole('dialog', { name: 'Chi tiết công nợ khách hàng' })
  await expect(detail.getByText('Khách quen, lấy hàng thường xuyên')).toBeVisible()
  await capture(page, 'debt-customer-detail')
  await detail.getByRole('tab', { name: 'Giải thích công nợ' }).click()
  await capture(page, 'debt-explain')
  await detail.getByRole('tab', { name: 'Tổng quan' }).click()
  await detail.getByRole('button', { name: 'Xem tất cả lịch sử giao dịch →' }).click()
  await expect(page.getByRole('dialog', { name: 'Lịch sử giao dịch' }).locator('tbody tr')).toHaveCount(7)
  await capture(page, 'debt-history')
  await page.getByRole('dialog', { name: 'Lịch sử giao dịch' }).getByRole('button', { name: 'Đóng' }).last().click()

  await detail.getByRole('button', { name: 'Thu tiền' }).click()
  const collect = page.getByRole('dialog', { name: 'Thu tiền khách hàng' })
  await capture(page, 'debt-collect')
  await collect.getByRole('button', { name: 'Thu tiền' }).click()
  await capture(page, 'debt-collect-confirm')
  await collect.getByRole('button', { name: 'Xác nhận thu tiền' }).click()
  await expect(collect.getByText('3.200.000 đ → 2.200.000 đ')).toBeVisible()
  await collect.getByRole('button', { name: 'Xong' }).click()

  await page.getByRole('button', { name: 'Nhà cung cấp' }).click()
  await expect(page).toHaveURL(/\/suppliers\/debts$/)
  await expect(page.getByText('Hiển thị 1 – 5 / 9 nhà cung cấp')).toBeVisible()
  await capture(page, 'debt-suppliers')
  await page.getByRole('button', { name: 'Thiên Long' }).click()
  await capture(page, 'debt-supplier-detail')
  await page.getByRole('dialog', { name: 'Chi tiết công nợ nhà cung cấp' }).getByRole('button', { name: 'Trả tiền' }).click()
  await capture(page, 'debt-settle')
  await page.getByRole('dialog', { name: 'Trả tiền nhà cung cấp' }).getByRole('button', { name: 'Hủy' }).click()

  await page.getByRole('button', { name: 'Không quyền' }).click()
  await expect(page.getByText('Bạn không có quyền thực hiện thao tác này')).toBeVisible()
  await capture(page, 'debt-state-forbidden')
  await page.locator('.debt-special button').click()

  expect(writes).toEqual([])
})

test('live customer debt keeps the supported facts and collects through the API', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  await mockOwner(page)
  let outstanding = 3200000
  const item = () => ({ partyId: '11111111-1111-1111-1111-111111111111', partyName: 'Nguyễn Văn A', phone: '0901 234 567', outstandingAmount: outstanding, asOf: '2024-12-16T03:23:00Z' })
  await page.route('**/api/customers/debts?**', route => route.fulfill({ json: { ...empty, items: outstanding ? [item()] : [], totalCount: outstanding ? 1 : 0, totalPages: outstanding ? 1 : 0 } }))
  await page.route('**/api/security/antiforgery', route => route.fulfill({ json: { requestToken: 'token' } }))
  await page.route('**/api/customers/*/debt-payments', async route => {
    const body = route.request().postDataJSON() as { amount: number; expectedOutstandingAmount: number }
    expect(body.expectedOutstandingAmount).toBe(3200000)
    outstanding -= body.amount
    await route.fulfill({ json: {
      id: 'payment-1', partyId: item().partyId, direction: 'MoneyIn', purpose: 'CustomerDebtCollection', amount: body.amount, method: 'Cash',
      note: null, occurredAt: '2024-12-16T07:23:00Z', performedByUserId: 'user-1', outstandingBefore: 3200000, outstandingAfter: outstanding, wasAlreadyRecorded: false,
    } })
  })

  await page.goto('/customers/debts')
  await expect(page.locator('.debt-table tbody tr')).toHaveCount(1)
  await expect(page.locator('.debt-summary-card').nth(1)).toContainText('3.200.000 đ')
  await capture(page, 'debt-live')
  await page.getByRole('button', { name: 'Thu tiền' }).click()
  const collect = page.getByRole('dialog', { name: 'Thu tiền khách hàng' })
  await collect.getByRole('spinbutton').fill('1000000')
  await collect.getByRole('button', { name: 'Thu tiền' }).click()
  await collect.getByRole('button', { name: 'Xác nhận thu tiền' }).click()
  await expect(collect.getByText('3.200.000 đ → 2.200.000 đ')).toBeVisible()
  await collect.getByRole('button', { name: 'Xong' }).click()
  await expect(page.locator('.debt-table tbody tr')).toContainText('2.200.000 đ')
})
