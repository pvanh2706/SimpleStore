import { expect, test, type Page } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

async function capture(page: Page, name: string) {
  if (!process.env.INVENTORY_CAPTURE_DIR) return
  await mkdir(process.env.INVENTORY_CAPTURE_DIR, { recursive: true })
  await page.screenshot({ path: join(process.env.INVENTORY_CAPTURE_DIR, `${name}.png`) })
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
  await page.route('**/api/today/attention?**', route => route.fulfill({ json: {
    businessDate: '2026-09-30', timeZoneId: 'Asia/Ho_Chi_Minh', velocityStartUtc: '', velocityEndUtc: '', completedBusinessDays: [],
    evaluationCoverage: 'FullSevenCompletedDays', totalAttentionCount: 0, page: 1, pageSize: 100, totalPages: 0, items: [],
  } }))
}

test('inventory reference data previews every flow without writing to the API', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  const writes: string[] = []
  page.on('request', request => { if (request.url().includes('/api/') && request.method() !== 'GET') writes.push(request.url()) })
  await mockOwner(page)
  await page.route('**/api/products?**', route => route.fulfill({ json: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } }))

  await page.goto('/products?view=inventory')
  await expect(page.getByText('Chưa có sản phẩm trong kho')).toBeVisible()
  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.locator('.inventory-table tbody tr')).toHaveCount(7)
  await expect(page.getByText('Hiển thị 1 – 7 / 156 sản phẩm')).toBeVisible()
  await expect(page.locator('.inventory-table img').first()).toHaveJSProperty('complete', true)
  await capture(page, 'inventory-list')

  await page.getByRole('button', { name: 'Bộ lọc' }).click()
  await capture(page, 'inventory-filter')
  const filter = page.getByRole('dialog', { name: 'Bộ lọc tồn kho' })
  await filter.getByRole('checkbox', { name: 'Hết hàng (tồn = 0)' }).check()
  await filter.getByRole('button', { name: 'Áp dụng' }).click()
  await expect(page.locator('.inventory-table tbody tr')).toHaveCount(1)
  await page.getByRole('button', { name: 'Bộ lọc' }).click()
  await filter.getByRole('button', { name: 'Xóa bộ lọc' }).click()

  await page.getByRole('button', { name: 'Coca Cola 330ml', exact: true }).click()
  await expect(page.getByRole('dialog', { name: 'Chi tiết tồn kho sản phẩm' }).getByText('5.000 đ / Lon')).toBeVisible()
  await capture(page, 'inventory-detail')
  await page.getByRole('dialog', { name: 'Chi tiết tồn kho sản phẩm' }).getByRole('button', { name: 'Đóng' }).click()

  await page.getByRole('button', { name: 'Kiểm kho' }).click()
  const stocktake = page.getByRole('dialog', { name: 'Kiểm kho' })
  await capture(page, 'inventory-stocktake-1')
  await stocktake.getByRole('button', { name: 'Tiếp theo →' }).click()
  await capture(page, 'inventory-stocktake-2')
  await stocktake.getByRole('button', { name: 'Tiếp theo →' }).click()
  await expect(stocktake.locator('.inventory-kpis')).toContainText('140')
  await capture(page, 'inventory-stocktake-3')
  await stocktake.getByRole('button', { name: 'Tiếp theo →' }).click()
  await capture(page, 'inventory-stocktake-4')
  await stocktake.getByRole('button', { name: 'Xác nhận' }).click()

  await page.getByRole('button', { name: 'Điều chỉnh tồn kho' }).first().click()
  const adjust = page.getByRole('dialog', { name: 'Điều chỉnh tồn kho' })
  await capture(page, 'inventory-adjust-1')
  await adjust.getByRole('radio', { name: /Trà xanh C2 500ml/ }).click()
  await adjust.getByRole('button', { name: 'Tiếp theo →' }).click()
  await adjust.getByRole('spinbutton').fill('3')
  await adjust.getByRole('combobox').selectOption('Cân chỉnh sổ sách')
  await capture(page, 'inventory-adjust-2')
  await adjust.getByRole('button', { name: 'Tiếp theo →' }).click()
  await capture(page, 'inventory-adjust-3')
  await adjust.getByRole('button', { name: 'Xác nhận' }).click()
  await expect(page.locator('.inventory-table tbody tr').filter({ hasText: 'Trà xanh C2 500ml' }).locator('.inventory-stock')).toHaveText('3')

  await page.getByRole('button', { name: 'Thao tác Coca Cola 330ml' }).click()
  await page.getByRole('button', { name: 'Lịch sử biến động' }).click()
  await expect(page.getByRole('dialog', { name: 'Lịch sử biến động tồn kho' }).locator('tbody tr')).toHaveCount(5)
  await capture(page, 'inventory-history')
  await page.getByRole('dialog', { name: 'Lịch sử biến động tồn kho' }).getByRole('button', { name: 'Đóng' }).last().click()

  expect(writes).toEqual([])
})

test('live inventory shows supported data and keeps the API as the source of truth', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  await mockOwner(page)
  const item = { id: '11111111-1111-1111-1111-111111111111', sku: 'SP0001', barcode: '8935049500017', name: 'Coca Cola 330ml', unit: 'Lon', salePrice: 10000, isActive: true, quantityOnHand: 24, updatedAt: '2024-12-16T03:23:00Z' }
  await page.route('**/api/products?**', route => route.fulfill({ json: { items: [item], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } }))
  await page.route(`**/api/products/${item.id}`, route => route.fulfill({ json: {
    ...item, referencePurchaseCost: 6000, inventoryValue: 120000, averageCost: 5000, hasAverageCost: true,
    referencePurchaseCostRevision: 1, createdAt: '2024-12-01T00:00:00Z',
  } }))
  await page.route(`**/api/products/${item.id}/movements`, route => route.fulfill({ json: [] }))
  await page.route(`**/api/today/attention/${item.id}`, route => route.fulfill({ status: 404, json: { code: 'c14-attention-not-found' } }))

  await page.goto('/products?view=inventory')
  const row = page.locator('.inventory-table tbody tr')
  await expect(row).toHaveCount(1)
  await expect(row).toContainText('5.000 đ')
  await expect(row).toContainText('120.000 đ')
  await expect(row.locator('.inventory-status')).toHaveText('Còn hàng')
  await capture(page, 'inventory-live')
  await page.getByRole('button', { name: 'Coca Cola 330ml', exact: true }).click()
  await expect(page.getByText('Không có tín hiệu cần chú ý theo C14 hiện tại')).toBeVisible()
})
