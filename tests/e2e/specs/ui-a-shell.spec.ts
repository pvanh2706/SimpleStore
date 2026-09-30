import { expect, test, type Page, type Route } from '@playwright/test'

function fulfillJson(route: Route, value: unknown) {
  return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(value) })
}

async function mockSession(page: Page, role: 'Owner' | 'Cashier') {
  await page.route('**/api/auth/session', route => fulfillJson(route, {
    isAuthenticated: true,
    email: role === 'Owner' ? 'owner@example.test' : 'cashier@example.test',
    storeId: 'store-1',
    roles: [role],
    hasStore: true,
    mustChangePassword: false,
    isEnabled: true,
  }))
  await page.route('**/api/store/current', route => fulfillJson(route, {
    id: 'store-1',
    name: 'Tạp hóa Việt Anh',
    mainWarehouseId: 'warehouse-1',
    mainWarehouseName: 'Kho chính',
    timeZoneId: 'Asia/Ho_Chi_Minh',
  }))
  await page.route('**/api/products?**', route => fulfillJson(route, {
    items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0,
  }))
  await page.route('**/api/sales?**', route => fulfillJson(route, {
    items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0,
  }))
}

test('Owner desktop shell presents authorized navigation, active state and logout', async ({ page }) => {
  await mockSession(page, 'Owner')
  await page.route('**/api/security/antiforgery', route => fulfillJson(route, { requestToken: 'test-token' }))
  await page.route('**/api/auth/logout', route => route.fulfill({ status: 204, body: '' }))

  await page.goto('/products')
  const navigation = page.getByRole('navigation', { name: 'Điều hướng chính', exact: true })
  await expect(navigation).toBeVisible()
  await expect(page.locator('.app-sidebar')).toContainText('Tạp hóa Việt Anh')
  await expect(page.locator('.app-sidebar')).toContainText('owner@example.test')
  await expect(navigation.getByRole('link', { name: 'Tổng quan' })).toBeVisible()
  await expect(navigation.getByRole('link', { name: 'Nhập hàng' })).toBeVisible()
  await expect(navigation.getByRole('link', { name: 'Sản phẩm' })).toHaveAttribute('aria-current', 'page')
  await navigation.getByRole('link', { name: 'Tồn kho' }).click()
  await expect(page.getByRole('heading', { name: 'Tồn kho' })).toBeVisible()
  await expect(navigation.getByRole('link', { name: 'Tồn kho' })).toHaveAttribute('aria-current', 'page')

  await navigation.getByText('Chức năng khác').click()
  await navigation.getByRole('link', { name: 'Đơn bán' }).click()
  await expect(page).toHaveURL(/\/sales$/)
  await expect(page.getByRole('heading', { name: 'Lịch sử bán hàng' })).toBeVisible()
  await expect(navigation.getByRole('link', { name: 'Đơn bán' })).toHaveAttribute('aria-current', 'page')

  await page.locator('.app-topbar-profile summary').click()
  await page.locator('.app-topbar-profile').getByRole('button', { name: 'Đăng xuất' }).click()
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.locator('.app-sidebar')).toHaveCount(0)
})

test('Cashier sees only currently permitted shell destinations', async ({ page }) => {
  await mockSession(page, 'Cashier')
  await page.goto('/products')

  const navigation = page.getByRole('navigation', { name: 'Điều hướng chính', exact: true })
  await expect(navigation).toBeVisible()
  for (const label of ['Bán hàng', 'Sản phẩm', 'Tồn kho', 'Công nợ']) {
    await expect(navigation.getByRole('link', { name: label })).toBeVisible()
  }
  await navigation.getByText('Chức năng khác').click()
  await expect(navigation.getByRole('link', { name: 'Đơn bán' })).toBeVisible()
  for (const label of ['Tổng quan', 'Nhập hàng', 'Nhà cung cấp', 'Công nợ NCC', 'Báo cáo', 'Cài đặt', 'Nhân viên']) {
    await expect(navigation.getByRole('link', { name: label })).toHaveCount(0)
  }
  await expect(page.locator('.app-sidebar')).toContainText('cashier@example.test')

  await page.goto('/today')
  await expect(page).toHaveURL(/\/products$/)
})

test('mobile drawer opens, traps focus, closes with Escape and closes after navigation', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await mockSession(page, 'Cashier')
  await page.goto('/products')

  const trigger = page.getByRole('button', { name: 'Mở điều hướng' })
  const dialog = page.getByRole('dialog', { name: 'Điều hướng ứng dụng' })
  await expect(trigger).toBeVisible()
  await expect(page.locator('.app-sidebar')).toBeHidden()
  await trigger.click()
  await expect(dialog).toBeVisible()
  await expect(trigger).toHaveAttribute('aria-expanded', 'true')
  await expect(dialog.getByRole('button', { name: 'Đóng điều hướng' })).toBeFocused()
  await expect.poll(() => page.evaluate(() => document.body.style.overflow)).toBe('hidden')

  await page.keyboard.press('Escape')
  await expect(dialog).toHaveCount(0)
  await expect(trigger).toBeFocused()
  await expect.poll(() => page.evaluate(() => document.body.style.overflow)).toBe('')

  await trigger.click()
  await dialog.getByRole('link', { name: 'Đơn bán' }).click()
  await expect(page).toHaveURL(/\/sales$/)
  await expect(dialog).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => document.body.style.overflow)).toBe('')
})

test('tablet uses compact navigation without clipping the workspace', async ({ page }) => {
  await page.setViewportSize({ width: 820, height: 900 })
  await mockSession(page, 'Owner')
  await page.goto('/products')

  await expect(page.locator('.app-sidebar')).toBeHidden()
  await expect(page.locator('.app-mobile-header')).toBeVisible()
  await page.getByRole('button', { name: 'Mở điều hướng' }).click()
  await expect(page.getByRole('dialog', { name: 'Điều hướng ứng dụng' })).toBeVisible()
  const documentWidth = await page.evaluate(() => document.documentElement.scrollWidth)
  expect(documentWidth).toBeLessThanOrEqual(820)
})

test('receipt print layout excludes application shell and non-receipt controls', async ({ page }) => {
  await mockSession(page, 'Cashier')
  await page.route('**/api/sales/sale-1', route => fulfillJson(route, {
    id: 'sale-1', status: 'Completed', storeName: 'Tạp hóa Việt Anh', warehouseId: 'warehouse-1',
    customer: null, cashierDisplayName: 'Thu ngân',
    lines: [{
      id: 'line-1', productId: 'product-1', productName: 'Nước suối 500ml',
      productSku: 'SP001', productUnit: 'Chai', quantity: 1, unitSalePrice: 7000,
      lineAmount: 7000, unitCostAtSale: 5000, costReliability: 'Reliable',
    }],
    payments: [{ id: 'payment-1', amount: 7000, method: 'Cash', occurredAt: '2026-09-29T10:00:00Z' }],
    totalAmount: 7000, paidAmount: 7000, outstandingAmount: 0,
    createdAt: '2026-09-29T10:00:00Z', completedAt: '2026-09-29T10:00:00Z',
    wasAlreadyCompleted: false, originalTotalAmount: 7000, totalReturnedAmount: 0,
    netSaleAmount: 7000, originalCollectedAmount: 7000, totalRefundedAmount: 0,
    netCollectedAmount: 7000, isVoided: false, void: null, returns: [],
  }))

  await page.goto('/sales/sale-1')
  const receipt = page.locator('.receipt')
  await expect(receipt).toBeVisible()
  await expect(receipt).toContainText('Nước suối 500ml')

  await page.emulateMedia({ media: 'print' })
  await expect(receipt).toBeVisible()
  await expect(page.locator('.app-sidebar')).toBeHidden()
  await expect(page.locator('.app-mobile-header')).toBeHidden()
  await expect(receipt.getByRole('button', { name: 'In hóa đơn', includeHidden: true })).toBeHidden()
  const workspaceWidth = await page.locator('.app-main').evaluate(element => element.getBoundingClientRect().width)
  expect(workspaceWidth).toBeGreaterThan(250)
  expect(workspaceWidth).toBeLessThan(300)
})
