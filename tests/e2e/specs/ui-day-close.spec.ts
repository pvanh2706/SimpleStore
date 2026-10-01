import { expect, test, type Page } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

async function capture(page: Page, name: string) {
  if (!process.env.DAY_CLOSE_CAPTURE_DIR) return
  await mkdir(process.env.DAY_CLOSE_CAPTURE_DIR, { recursive: true })
  await page.screenshot({ path: join(process.env.DAY_CLOSE_CAPTURE_DIR, `${name}.png`) })
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
  await page.route('**/api/reports/end-of-day?**', route => route.fulfill({ json: {
    businessDate: new URL(route.request().url()).searchParams.get('date'), timeZoneId: 'Asia/Ho_Chi_Minh',
    startUtc: '2026-09-29T17:00:00Z', endUtc: '2026-09-30T17:00:00Z', salesRevenue: 1500000,
    collected: { salePayments: 1200000, customerDebtPayments: 300000, customerRefunds: 100000, netAmount: 1400000 },
    customerOutstandingDebtAtEnd: 250000,
    supplierPayments: { purchasePayments: 400000, supplierDebtPayments: 100000, totalAmount: 500000 },
    supplierOutstandingDebtAtEnd: 900000,
    estimatedGrossProfit: { netSalesRevenue: 1500000, historicalCogs: 1000000, amount: 500000, costReliability: 'Reliable' },
  } }))
}

test('day-close reference data previews list, report and close flow without writing to the API', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  const writes: string[] = []
  page.on('request', request => { if (request.url().includes('/api/') && request.method() !== 'GET') writes.push(request.url()) })
  await mockOwner(page)

  await page.goto('/day-close')
  await expect(page.locator('.app-sidebar a[aria-current="page"]')).toHaveText('Đóng ngày')
  await expect(page.locator('.dayclose-table tbody tr')).toHaveCount(10)
  await capture(page, 'day-close-live-list')
  await page.getByRole('button', { name: 'Xem báo cáo hôm nay' }).click()
  await expect(page.locator('.dayclose-kpi strong').first()).toHaveText('1.500.000 đ')
  await capture(page, 'day-close-live-report')
  await page.getByRole('button', { name: '← Danh sách ngày' }).click()

  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.locator('.dayclose-table tbody tr')).toHaveCount(6)
  await expect(page.getByText('Hiển thị 1 – 6 / 20 ngày')).toBeVisible()
  await capture(page, 'day-close-list')

  await page.getByRole('button', { name: 'Xem báo cáo ngày 15/12/2024' }).click()
  await expect(page.getByRole('heading', { name: 'Báo cáo đóng ngày' })).toBeVisible()
  await capture(page, 'day-close-report')
  await page.getByRole('button', { name: 'Xem chi tiết Doanh thu bán hàng' }).click()
  await capture(page, 'day-close-detail')
  await page.getByRole('button', { name: 'Giải thích số liệu' }).click()
  await expect(page.getByRole('dialog', { name: 'Giải thích Doanh thu bán hàng' })).toBeVisible()
  await capture(page, 'day-close-explain')
  await page.keyboard.press('Escape')
  await page.getByRole('button', { name: '← Danh sách ngày' }).click()

  await page.getByRole('button', { name: '＋ Đóng ngày hôm nay' }).click()
  const wizard = page.getByRole('dialog', { name: 'Quy trình đóng ngày' })
  await capture(page, 'day-close-step1')
  await wizard.getByRole('button', { name: 'Tiếp theo →' }).click()
  await capture(page, 'day-close-step2')
  await wizard.getByRole('button', { name: 'Tiếp theo →' }).click()
  await capture(page, 'day-close-step3')
  await wizard.getByRole('button', { name: '✓ Xác nhận đóng ngày' }).click()
  await expect(page.locator('.dayclose-table tbody tr').first()).toContainText('Đã đóng')
  await capture(page, 'day-close-closed')

  expect(writes).toEqual([])
})
