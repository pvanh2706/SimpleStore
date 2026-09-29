import { expect, test, type Page, type Route } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

// This smoke suite uses browser-level mocked API contracts; it does not use a real API or database.
const productName = 'Cà phê rang xay nguyên chất hương vị truyền thống đặc biệt cho gia đình Việt Nam'
const product = {
  id: 'product-pos-1', sku: 'CF-001', barcode: '8930000000012', name: productName,
  unit: 'gói', salePrice: 15000, isActive: true, quantityOnHand: 12,
}

function fulfillJson(route: Route, body: unknown, status = 200) {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) })
}

async function captureVisual(page: Page, name: string) {
  const directory = process.env.UI_B_CAPTURE_DIR
  if (!directory) return
  await mkdir(directory, { recursive: true })
  await page.screenshot({ path: join(directory, name), fullPage: true })
}

async function mockCashierCheckout(page: Page) {
  const searches: URLSearchParams[] = []
  const attempts: unknown[] = []

  await page.route('**/api/auth/session', route => fulfillJson(route, {
    isAuthenticated: true, email: 'cashier@example.test', storeId: 'store-1',
    roles: ['Cashier'], hasStore: true, mustChangePassword: false, isEnabled: true,
  }))
  await page.route('**/api/store/current', route => fulfillJson(route, {
    id: 'store-1', name: 'Tạp hóa Việt Anh', mainWarehouseId: 'warehouse-1',
    mainWarehouseName: 'Kho chính', timeZoneId: 'Asia/Ho_Chi_Minh',
  }))
  await page.route('**/api/store/operational-settings', route => fulfillJson(route, { allowNegativeStock: false }))
  await page.route('**/api/security/antiforgery', route => fulfillJson(route, { requestToken: 'test-token' }))
  await page.route('**/api/products?**', route => {
    const query = new URL(route.request().url()).searchParams
    searches.push(query)
    const search = query.get('search')?.toLocaleLowerCase('vi-VN') ?? ''
    const matches = !search || product.name.toLocaleLowerCase('vi-VN').includes(search)
      || product.sku.toLowerCase().includes(search) || product.barcode.includes(search)
    return fulfillJson(route, {
      items: matches ? [product] : [], page: Number(query.get('page') ?? 1),
      pageSize: 20, totalCount: matches ? 1 : 0, totalPages: matches ? 1 : 0,
    })
  })
  await page.route('**/api/sales/complete', route => {
    const attempt = route.request().postDataJSON()
    attempts.push(attempt)
    return fulfillJson(route, {
      id: 'sale-pos-1', status: 'Completed', storeName: 'Tạp hóa Việt Anh',
      warehouseId: 'warehouse-1', customer: null, cashierDisplayName: 'cashier@example.test',
      lines: [{
        id: 'line-1', productId: product.id, productName: product.name,
        productSku: product.sku, productUnit: product.unit, quantity: 2,
        unitSalePrice: product.salePrice, lineAmount: 30000, unitCostAtSale: 10000,
        costReliability: 'Reliable',
      }],
      payments: [{ id: 'payment-1', amount: 30000, method: 'Cash', occurredAt: '2026-09-30T10:00:00Z' }],
      totalAmount: 30000, paidAmount: 30000, outstandingAmount: 0,
      createdAt: '2026-09-30T10:00:00Z', completedAt: '2026-09-30T10:00:00Z',
      wasAlreadyCompleted: false, originalTotalAmount: 30000, totalReturnedAmount: 0,
      netSaleAmount: 30000, originalCollectedAmount: 30000, totalRefundedAmount: 0,
      netCollectedAmount: 30000, isVoided: false, void: null, returns: [],
    })
  })
  return { searches, attempts }
}

test('Cashier scans, edits and completes one sale, then starts a new one', async ({ page }) => {
  const { searches, attempts } = await mockCashierCheckout(page)
  await page.goto('/products')
  await expect(page.locator('.app-sidebar')).toContainText('cashier@example.test')
  await page.getByRole('navigation', { name: 'Điều hướng chính', exact: true })
    .getByRole('link', { name: 'Bán hàng' }).click()
  await expect(page).toHaveURL(/\/sales\/new$/)
  await expect(page.getByRole('heading', { name: 'Bán hàng', exact: true })).toBeVisible()
  const desktopPanels = await page.evaluate(() => {
    const productArea = document.querySelector('.sales-pos__products')?.getBoundingClientRect()
    const checkout = document.querySelector('.sales-pos__checkout')?.getBoundingClientRect()
    return productArea && checkout ? {
      productWidth: productArea.width, productRight: productArea.right,
      checkoutWidth: checkout.width, checkoutLeft: checkout.left, checkoutRight: checkout.right,
      topDifference: Math.abs(productArea.top - checkout.top), viewportWidth: innerWidth,
    } : null
  })
  expect(desktopPanels).not.toBeNull()
  expect(desktopPanels!.productWidth).toBeGreaterThan(350)
  expect(desktopPanels!.checkoutWidth).toBeGreaterThan(300)
  expect(desktopPanels!.checkoutLeft).toBeGreaterThanOrEqual(desktopPanels!.productRight - 2)
  expect(desktopPanels!.checkoutRight).toBeLessThanOrEqual(desktopPanels!.viewportWidth + 1)
  expect(desktopPanels!.topDifference).toBeLessThan(40)

  await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).fill(product.barcode)
  await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).press('Enter')
  await expect(page.getByRole('button', { name: `Thêm sản phẩm ${productName}` })).toBeVisible()
  await captureVisual(page, 'sales-desktop-product.png')
  expect(searches.some(query => query.get('search') === product.barcode && query.get('isActive') === 'true'
    && query.get('page') === '1' && query.get('pageSize') === '20')).toBe(true)

  await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
  await page.getByRole('spinbutton', { name: `Số lượng ${productName}` }).fill('2')
  await page.getByRole('spinbutton', { name: 'Số tiền thanh toán' }).fill('30000')
  await page.getByRole('button', { name: 'Thêm thanh toán' }).click()
  await expect(page.getByText('30.000 ₫').first()).toBeVisible()
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()

  await expect(page.getByRole('heading', { name: 'Đơn bán đã hoàn tất' })).toBeVisible()
  const receipt = page.getByRole('region', { name: 'Hóa đơn bán hàng' })
  await expect(receipt).toContainText('sale-pos-1')
  await expect(receipt).toContainText(productName)
  await expect(receipt).toContainText('30.000 ₫')
  await expect(receipt.getByRole('button', { name: 'In hóa đơn' })).toBeVisible()
  await page.evaluate(() => window.scrollTo(0, 0))
  const shellPosition = await page.evaluate(() => ({
    scrollY: window.scrollY,
    skipLinkBottom: document.querySelector('.app-skip-link')!.getBoundingClientRect().bottom,
    sidebarTop: document.querySelector('.app-sidebar')!.getBoundingClientRect().top,
  }))
  expect(shellPosition.scrollY).toBe(0)
  expect(shellPosition.skipLinkBottom).toBeLessThanOrEqual(0)
  expect(Math.abs(shellPosition.sidebarTop)).toBeLessThanOrEqual(1)
  if (process.env.UI_B_CAPTURE_DIR) {
    await page.screenshot({ path: join(process.env.UI_B_CAPTURE_DIR, 'sales-completed-viewport.png') })
  }
  await captureVisual(page, 'sales-completed.png')
  expect(attempts).toHaveLength(1)
  expect(attempts[0]).toMatchObject({
    customerId: null,
    lines: [{ productId: product.id, quantity: 2 }],
    payments: [{ method: 'Cash', amount: 30000 }],
  })
  expect((attempts[0] as { operationId: string }).operationId).toBeTruthy()

  await page.emulateMedia({ media: 'print' })
  await expect(receipt).toBeVisible()
  await expect(page.locator('.app-sidebar')).toBeHidden()
  await expect(page.locator('.app-mobile-header')).toBeHidden()
  await expect(page.locator('.sales-page__header')).toBeHidden()
  await expect(page.locator('.sales-complete__summary')).toBeHidden()
  await expect(receipt.getByRole('button', { name: 'In hóa đơn', includeHidden: true })).toBeHidden()
  await expect(page.getByRole('button', { name: 'Đơn bán mới', includeHidden: true })).toBeHidden()
  const printWidth = await page.locator('.app-main').evaluate(element => element.getBoundingClientRect().width)
  expect(printWidth).toBeGreaterThan(250)
  expect(printWidth).toBeLessThan(300)
  const productWrap = await receipt.locator('.receipt-product').evaluate(element => ({
    wordBreak: getComputedStyle(element).wordBreak,
    hyphens: getComputedStyle(element).hyphens,
    width: element.getBoundingClientRect().width,
    receiptWidth: element.closest('.receipt')!.getBoundingClientRect().width,
  }))
  expect(productWrap.wordBreak).toBe('normal')
  expect(productWrap.hyphens).toBe('none')
  expect(productWrap.width).toBeLessThanOrEqual(productWrap.receiptWidth + 1)
  await captureVisual(page, 'sales-print.png')
  await page.emulateMedia({ media: 'screen' })

  await page.getByRole('button', { name: 'Đơn bán mới' }).click()
  await expect(page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' })).toBeVisible()
  await expect(receipt).toHaveCount(0)
})

test('Sales workspace remains usable without horizontal clipping on tablet and mobile', async ({ page }) => {
  await mockCashierCheckout(page)
  for (const viewport of [{ width: 820, height: 900 }, { width: 390, height: 844 }]) {
    await page.setViewportSize(viewport)
    await page.goto('/sales/new')
    await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).fill(product.sku)
    await page.getByRole('button', { name: 'Tìm sản phẩm' }).click()
    await expect(page.getByText(productName)).toBeVisible()
    await expect(page.getByRole('button', { name: `Thêm sản phẩm ${productName}` })).toBeVisible()
    await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
    await expect(page.getByRole('spinbutton', { name: `Số lượng ${productName}` })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Hoàn tất bán hàng' })).toBeVisible()
    const panelBounds = await page.evaluate(() => ['.sales-pos__products', '.sales-pos__checkout'].map(selector => {
      const rect = document.querySelector(selector)?.getBoundingClientRect()
      return rect ? { left: rect.left, right: rect.right, width: rect.width } : null
    }))
    for (const bounds of panelBounds) {
      expect(bounds).not.toBeNull()
      expect(bounds!.left).toBeGreaterThanOrEqual(-1)
      expect(bounds!.right).toBeLessThanOrEqual(viewport.width + 1)
      expect(bounds!.width).toBeGreaterThan(0)
    }
    if (viewport.width < 600) {
      const mobilePanels = await page.evaluate(() => {
        const productArea = document.querySelector('.sales-pos__products')?.getBoundingClientRect()
        const checkout = document.querySelector('.sales-pos__checkout')?.getBoundingClientRect()
        return productArea && checkout ? { productBottom: productArea.bottom, checkoutTop: checkout.top } : null
      })
      expect(mobilePanels).not.toBeNull()
      expect(mobilePanels!.checkoutTop).toBeGreaterThanOrEqual(mobilePanels!.productBottom - 2)
    }
    await captureVisual(page, `sales-${viewport.width === 820 ? 'tablet' : 'mobile'}.png`)
    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth)
    expect(scrollWidth).toBeLessThanOrEqual(viewport.width)
  }
})
