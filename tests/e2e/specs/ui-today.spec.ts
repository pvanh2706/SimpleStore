import { expect, test, type Page } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

async function capture(page: Page, name: string) {
  if (!process.env.TODAY_CAPTURE_DIR) return
  await mkdir(process.env.TODAY_CAPTURE_DIR, { recursive: true })
  await page.screenshot({ path: join(process.env.TODAY_CAPTURE_DIR, `${name}.png`) })
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
  await page.route('**/api/today', route => route.fulfill({ json: {
    businessDate: '2026-09-30', timeZoneId: 'Asia/Ho_Chi_Minh', startUtc: '2026-09-29T17:00:00Z', endUtc: '2026-09-30T17:00:00Z',
    salesRevenue: 1250000, netCollected: 1100000,
    estimatedGrossProfit: { netSalesRevenue: 1250000, historicalCogs: 800000, amount: 450000, costReliability: 'Estimated' },
    saleCount: 12, customerDebtCreated: 300000, supplierDebtCreated: 0,
  } }))
  await page.route('**/api/today/attention?**', route => route.fulfill({ json: {
    businessDate: '2026-09-30', timeZoneId: 'Asia/Ho_Chi_Minh', velocityStartUtc: '', velocityEndUtc: '', completedBusinessDays: [],
    evaluationCoverage: 'FullSevenCompletedDays', totalAttentionCount: 4, page: 1, pageSize: 3, totalPages: 2,
    items: [
      { productId: 'p1', productName: 'Trà xanh C2 500ml', sku: 'SP0004', unit: 'Chai', attentionKind: 'NegativeStock', currentStock: -3, netSoldQuantity: 9, averageDailySales: 1.29, daysOfCover: null, historyCoverage: 'FullSevenCompletedDays', recentSalesEvidence: 'PositiveNetSold', riskEvaluation: 'Eligible' },
      { productId: 'p2', productName: 'Pepsi 330ml', sku: 'SP0002', unit: 'Lon', attentionKind: 'OutOfStock', currentStock: 0, netSoldQuantity: 14, averageDailySales: 2, daysOfCover: null, historyCoverage: 'FullSevenCompletedDays', recentSalesEvidence: 'PositiveNetSold', riskEvaluation: 'Eligible' },
      { productId: 'p3', productName: 'Bánh Oreo 133g', sku: 'SP0006', unit: 'Gói', attentionKind: 'LowStockRisk', currentStock: 5, netSoldQuantity: 21, averageDailySales: 3, daysOfCover: 1.67, historyCoverage: 'FullSevenCompletedDays', recentSalesEvidence: 'PositiveNetSold', riskEvaluation: 'Eligible' },
    ],
  } }))
  await page.route('**/api/today/explanations/**', route => route.fulfill({ json: {
    metric: 'revenue', headline: 1250000, historicalCogs: null, costReliability: null, page: 1, pageSize: 20, totalCount: 1, totalPages: 1,
    items: [{ sourceType: 'Sale', sourceId: 'sale-1', relatedSourceId: null, occurredAt: '2026-09-30T03:00:00Z', contributionAmount: 1250000, contributionCount: null, title: 'Đơn bán hoàn tất', debtContribution: null, navigation: { type: 'Sale', id: 'sale-1' } }],
  } }))
  await page.route('**/api/experiments/c14/events', route => route.fulfill({ status: 204, body: '' }))
}

test('today reference data previews the dashboard and its drawers without writing to the API', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  await mockOwner(page)

  await page.goto('/today')
  await expect(page.locator('.app-sidebar a[aria-current="page"]')).toHaveText('Tổng quan')
  await expect(page.getByTestId('attention-preview-item')).toHaveCount(3)
  await expect(page.locator('.today-date-control')).toHaveText('Hôm nay, 30/09/2026')
  await capture(page, 'today-live')
  await page.getByRole('button', { name: /^Doanh thu/ }).click()
  await expect(page.getByText('Dữ liệu nguồn · 1 mục')).toBeVisible()
  await capture(page, 'today-live-drawer')
  await page.getByRole('button', { name: 'Đóng' }).click()
  await expect(page.locator('aside.today-drawer')).toHaveCount(0)

  const writes: string[] = []
  page.on('request', request => { if (request.url().includes('/api/') && request.method() !== 'GET') writes.push(request.url()) })
  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.locator('.today-kpi-value').first()).toHaveText('2.850.000 ₫')
  await expect(page.locator('.today-date-control')).toHaveText('Hôm nay, 16/12/2024')
  await capture(page, 'today-dashboard')

  await page.getByRole('button', { name: /^Doanh thu/ }).click()
  await expect(page.getByRole('dialog', { name: 'Cách tính doanh thu' })).toBeVisible()
  await page.waitForTimeout(300)
  await capture(page, 'today-explainability')
  await page.getByRole('button', { name: 'Xem giao dịch trong ca' }).click()
  await expect(page.getByRole('dialog', { name: 'Giao dịch gần đây' })).toBeVisible()
  await capture(page, 'today-transactions')
  await page.keyboard.press('Escape')

  await page.locator('.today-bar').nth(4).click()
  await page.locator('.today-legend-item').first().click()
  await page.getByLabel('Ca làm việc').selectOption('all')
  await expect(page.locator('.today-kpi-value').first()).toHaveText('5.060.000 ₫')
  await capture(page, 'today-all-shifts')
  await page.getByRole('button', { name: 'Ngày trước' }).click()
  await expect(page.locator('.today-date-control')).toHaveText('15/12/2024')

  await page.setViewportSize({ width: 390, height: 844 })
  await page.getByLabel('Ca làm việc').selectOption('morning')
  await capture(page, 'today-mobile')
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390)

  expect(writes).toEqual([])
})
