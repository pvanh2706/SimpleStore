import { expect, test, type Page, type Route } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

// This smoke suite uses browser-level mocked API contracts; it does not use a real API or database.
const products = [
  { id: 'product-pos-1', sku: 'CF-001', barcode: '8930000000012', name: 'Cà phê rang xay nguyên chất hương vị truyền thống đặc biệt cho gia đình Việt Nam', unit: 'gói', salePrice: 15000, isActive: true, quantityOnHand: 12 },
  { id: 'product-pos-2', sku: 'NUOC-001', barcode: '8930000000013', name: 'Nước suối Aquafina 500ml', unit: 'chai', salePrice: 7000, isActive: true, quantityOnHand: 36 },
  { id: 'product-pos-3', sku: 'MI-001', barcode: '8930000000014', name: 'Mì Hảo Hảo tôm chua cay 75g', unit: 'gói', salePrice: 4000, isActive: true, quantityOnHand: 48 },
  { id: 'product-pos-4', sku: 'C2-001', barcode: '8930000000015', name: 'Trà xanh C2 hương chanh 500ml', unit: 'chai', salePrice: 10000, isActive: true, quantityOnHand: 20 },
  { id: 'product-pos-5', sku: 'SUA-001', barcode: '8930000000016', name: 'Sữa tươi Vinamilk có đường 180ml', unit: 'hộp', salePrice: 8000, isActive: true, quantityOnHand: 8 },
  { id: 'product-pos-6', sku: 'OREO-001', barcode: '8930000000017', name: 'Bánh quy Oreo kem vani 133g', unit: 'gói', salePrice: 22000, isActive: true, quantityOnHand: 15 },
  { id: 'product-pos-7', sku: 'DAU-001', barcode: '8930000000018', name: 'Dầu ăn Tường An Cooking Oil 1L', unit: 'chai', salePrice: 45000, isActive: true, quantityOnHand: 3 },
  { id: 'product-pos-8', sku: 'MAM-001', barcode: '8930000000019', name: 'Nước mắm Nam Ngư 500ml', unit: 'chai', salePrice: 28000, isActive: true, quantityOnHand: 10 },
  { id: 'product-pos-9', sku: 'DUONG-001', barcode: '8930000000020', name: 'Đường cát trắng tinh luyện 1kg', unit: 'gói', salePrice: 24000, isActive: true, quantityOnHand: 6 },
  { id: 'product-pos-10', sku: 'KNORR-001', barcode: '8930000000021', name: 'Hạt nêm Knorr thịt thăn xương ống 400g', unit: 'gói', salePrice: 32000, isActive: true, quantityOnHand: 9 },
  { id: 'product-pos-11', sku: 'GIAY-001', barcode: '8930000000022', name: 'Giấy vệ sinh Pulppy hai lớp 10 cuộn', unit: 'lốc', salePrice: 38000, isActive: true, quantityOnHand: 2 },
  { id: 'product-pos-12', sku: 'PEPSI-001', barcode: '8930000000023', name: 'Nước ngọt Pepsi không calo 330ml', unit: 'lon', salePrice: 10000, isActive: true, quantityOnHand: 18 },
]
const product = products[0]
const productName = product.name
const customer = { id: 'customer-1', name: 'Nguyễn Thị Minh Anh', phone: '0909123456', createdAt: '', updatedAt: '' }

function fulfillJson(route: Route, body: unknown, status = 200) {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) })
}

async function captureVisual(page: Page, name: string, fullPage = false) {
  const directory = process.env.UI_B_CAPTURE_DIR
  if (!directory) return
  await mkdir(directory, { recursive: true })
  // A full-page capture starts from the top so the sticky mobile header is not painted mid-page.
  if (fullPage) await page.evaluate(() => { (document.activeElement as HTMLElement | null)?.blur(); window.scrollTo(0, 0) })
  await page.screenshot({ path: join(directory, name), fullPage })
}

/** `+ Thêm khách` stays one line: icon and label share a row and the label does not wrap. */
async function expectAddCustomerOnOneLine(page: Page) {
  const geometry = await page.locator('.sales-pos__create-customer summary').evaluate(summary => {
    const label = summary.querySelector('span')!
    const icon = summary.querySelector('svg')!.getBoundingClientRect()
    const box = summary.getBoundingClientRect()
    const text = label.getBoundingClientRect()
    return {
      lineHeight: parseFloat(getComputedStyle(label).lineHeight) || parseFloat(getComputedStyle(label).fontSize) * 1.3,
      labelHeight: text.height, boxHeight: box.height, scrollWidth: summary.scrollWidth, clientWidth: summary.clientWidth,
      sameRow: Math.abs((icon.top + icon.bottom) / 2 - (text.top + text.bottom) / 2) < 4,
      selectWidth: summary.closest('.sales-pos__field-row')!.querySelector('.sales-pos__select')!.getBoundingClientRect().width,
    }
  })
  expect(geometry.labelHeight).toBeLessThan(geometry.lineHeight * 1.5)
  expect(geometry.sameRow).toBe(true)
  expect(geometry.scrollWidth).toBeLessThanOrEqual(geometry.clientWidth + 1)
  expect(geometry.boxHeight).toBeLessThanOrEqual(48)
  // The Customer selector keeps a usable width beside it.
  expect(geometry.selectWidth).toBeGreaterThan(150)
}

async function expectNoHorizontalOverflow(page: Page, width: number) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width)
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
    const matches = products.filter(item => !search || item.name.toLocaleLowerCase('vi-VN').includes(search)
      || item.sku.toLowerCase().includes(search) || item.barcode.includes(search))
    return fulfillJson(route, {
      items: matches, page: Number(query.get('page') ?? 1),
      pageSize: 20, totalCount: matches.length, totalPages: matches.length ? 1 : 0,
    })
  })
  await page.route('**/api/customers?**', route => fulfillJson(route, {
    items: [customer], page: 1, pageSize: 20, totalCount: 1, totalPages: 1,
  }))
  await page.route('**/api/sales/complete', route => {
    const attempt = route.request().postDataJSON() as {
      customerId: string | null
      lines: Array<{ productId: string; quantity: number }>
      payments: Array<{ amount: number; method: 'Cash' | 'Transfer' }>
    }
    attempts.push(attempt)
    const lines = attempt.lines.map((line, index) => {
      const item = products.find(candidate => candidate.id === line.productId)!
      return {
        id: `line-${index + 1}`, productId: item.id, productName: item.name,
        productSku: item.sku, productUnit: item.unit, quantity: line.quantity,
        unitSalePrice: item.salePrice, lineAmount: item.salePrice * line.quantity, unitCostAtSale: item.salePrice * 0.65,
        costReliability: 'Reliable',
      }
    })
    const totalAmount = lines.reduce((sum, line) => sum + line.lineAmount, 0)
    const paidAmount = attempt.payments.reduce((sum, payment) => sum + payment.amount, 0)
    return fulfillJson(route, {
      id: 'sale-pos-1', status: 'Completed', storeName: 'Tạp hóa Việt Anh',
      warehouseId: 'warehouse-1', customer: attempt.customerId ? customer : null, cashierDisplayName: 'cashier@example.test',
      lines,
      payments: attempt.payments.map((payment, index) => ({ id: `payment-${index + 1}`, ...payment, occurredAt: '2026-09-30T10:00:00Z' })),
      totalAmount, paidAmount, outstandingAmount: totalAmount - paidAmount,
      createdAt: '2026-09-30T10:00:00Z', completedAt: '2026-09-30T10:00:00Z',
      wasAlreadyCompleted: false, originalTotalAmount: totalAmount, totalReturnedAmount: 0,
      netSaleAmount: totalAmount, originalCollectedAmount: paidAmount, totalRefundedAmount: 0,
      netCollectedAmount: paidAmount, isVoided: false, void: null, returns: [],
    })
  })
  return { searches, attempts }
}

test('Cashier scans, edits and completes one sale, then starts a new one', async ({ page }) => {
  const { searches, attempts } = await mockCashierCheckout(page)
  await page.setViewportSize({ width: 1536, height: 1024 })
  await page.goto('/products')
  await expect(page.locator('.app-topbar')).toContainText('cashier')
  await page.getByRole('navigation', { name: 'Điều hướng chính', exact: true })
    .getByRole('link', { name: 'Bán hàng' }).click()
  await expect(page).toHaveURL(/\/sales\/new$/)
  await expect(page.getByRole('heading', { name: 'Đơn 1', exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: `Thêm sản phẩm ${products[11].name}` })).toBeVisible()
  await captureVisual(page, 'sales-desktop-products-1536x1024.png')
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

  // D-107 live Product browser: no keyword Category, neutral placeholders, factual stock only.
  await expect(page.locator('.sales-pos__categories')).toHaveCount(0)
  await expect(page.locator('.sales-pos__product-card img')).toHaveCount(0)
  await expect(page.locator('.sales-pos__product-card .sales-pos__placeholder')).toHaveCount(products.length)
  await expect(page.getByText('Rất ít hàng')).toHaveCount(0)
  await expect(page.getByText('Sắp hết hàng')).toHaveCount(0)
  await expect(page.locator('.sales-pos__product-card').filter({ hasText: products[10].name })).toContainText('Còn 2 lốc')
  // D-107 live cart: one working order and no visual-only controls.
  await expect(page.locator('.sales-pos__order-rail')).toHaveCount(0)
  for (const name of ['Đơn mới', 'Giữ đơn', 'Thêm giảm giá hóa đơn', 'Bán nợ']) {
    await expect(page.getByRole('button', { name })).toHaveCount(0)
  }
  await expect(page.getByText('Danh sách đơn đang chờ')).toHaveCount(0)
  await expect(page.getByLabel('Ghi chú đơn hàng')).toHaveCount(0)
  await expect(page.locator('.sales-pos__pay-btn')).toHaveText(['Tiền mặt', 'Chuyển khoản'])
  // Pass 2: a quiet Sales history shortcut in the checkout header, far from the Complete CTA.
  const historyShortcut = page.locator('.sales-pos__checkout-tools').getByRole('link', { name: 'Lịch sử bán hàng' })
  await expect(historyShortcut).toHaveAttribute('href', '/sales')
  await expect(page.locator('.sales-pos__actions').getByText('Lịch sử bán hàng')).toHaveCount(0)
  await expectAddCustomerOnOneLine(page)

  await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).fill(product.barcode)
  await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).press('Enter')
  await expect(page.getByRole('button', { name: `Thêm sản phẩm ${productName}` })).toBeVisible()
  expect(searches.some(query => query.get('search') === product.barcode && query.get('isActive') === 'true'
    && query.get('page') === '1' && query.get('pageSize') === '20')).toBe(true)

  await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
  await page.getByRole('spinbutton', { name: `Số lượng ${productName}` }).fill('2')
  await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).fill('')
  await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).press('Enter')
  await page.getByRole('button', { name: `Thêm sản phẩm ${products[1].name}` }).click()
  await page.getByRole('button', { name: `Thêm sản phẩm ${products[2].name}` }).click()
  await captureVisual(page, 'sales-desktop-cart-1536x1024.png')
  await page.getByRole('button', { name: 'Nhập số tiền' }).click()
  await page.getByRole('spinbutton', { name: 'Số tiền thanh toán' }).fill('30000')
  await page.getByRole('button', { name: 'Thêm thanh toán' }).click()
  await expect(page.getByRole('list', { name: 'Các khoản đã nhập' })).toContainText('30.000 đ')
  await expect(page.getByRole('note')).toContainText('Còn nợ 11.000 đ')
  await expect(page.getByRole('note')).toContainText('Chọn khách hàng để ghi nhận công nợ 11.000 đ.')
  await expect(page.locator('.sales-pos__required-tag')).toHaveText('Bắt buộc khi còn nợ')
  await captureVisual(page, 'sales-desktop-outstanding-required-1536x1024.png')
  await page.locator('.sales-pos__customer-picker summary').click()
  await page.getByRole('textbox', { name: 'Tìm khách hàng' }).fill('0909')
  await page.getByRole('button', { name: 'Tìm khách hàng' }).click()
  await page.getByRole('button', { name: `Chọn khách hàng ${customer.name}` }).click()
  await expect(page.locator('.sales-pos__customer-picker summary')).toContainText(customer.name)
  await expect(page.getByRole('note')).toContainText(`Ghi nhận công nợ 11.000 đ cho ${customer.name}.`)
  await captureVisual(page, 'sales-desktop-payment-customer-1536x1024.png')
  await page.getByRole('button', { name: 'Chuyển khoản', exact: true }).click()
  await page.getByRole('spinbutton', { name: 'Số tiền thanh toán' }).fill('11000')
  await page.getByRole('button', { name: 'Thêm thanh toán' }).click()
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()

  await expect(page.getByRole('heading', { name: 'Đơn bán đã hoàn tất' })).toBeVisible()
  const receipt = page.getByRole('region', { name: 'Hóa đơn bán hàng' })
  await expect(receipt).toContainText('sale-pos-1')
  await expect(receipt).toContainText(productName)
  await expect(receipt).toContainText('41.000 ₫')
  await expect(page.getByRole('button', { name: 'In hóa đơn' })).toBeVisible()
  // Completion hierarchy: Đơn bán mới is the primary next step; print and history are secondary.
  const summary = page.locator('.sales-complete__summary')
  await expect(summary.getByRole('heading', { level: 1 })).toHaveText('Đơn bán đã hoàn tất')
  await expect(summary).toContainText('Việc in hóa đơn không thay đổi trạng thái giao dịch.')
  await expect(summary.locator('.sales-complete__customer')).toContainText(customer.name)
  await expect(receipt.getByRole('button')).toHaveCount(0)
  const actionBoxes = await page.evaluate(() => ['.sales-complete__new', '.sales-complete__print-btn', '.sales-complete__history']
    .map(selector => document.querySelector(selector)!.getBoundingClientRect()))
  expect(actionBoxes[0]!.width * actionBoxes[0]!.height).toBeGreaterThan(actionBoxes[1]!.width * actionBoxes[1]!.height)
  expect(actionBoxes[0]!.width).toBeGreaterThan(actionBoxes[2]!.width)
  await expect(summary.getByRole('link', { name: 'Lịch sử bán hàng' })).toHaveAttribute('href', '/sales')
  const receiptWidth = await receipt.evaluate(element => element.getBoundingClientRect().width)
  expect(receiptWidth).toBeGreaterThan(280)
  expect(receiptWidth).toBeLessThan(360)
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
  await captureVisual(page, 'sales-completed-1536x1024.png')
  expect(attempts).toHaveLength(1)
  expect(attempts[0]).toMatchObject({
    customerId: customer.id,
    lines: [
      { productId: product.id, quantity: 2 },
      { productId: products[1].id, quantity: 1 },
      { productId: products[2].id, quantity: 1 },
    ],
    payments: [{ method: 'Cash', amount: 30000 }, { method: 'Transfer', amount: 11000 }],
  })
  expect((attempts[0] as { operationId: string }).operationId).toBeTruthy()
  expect((attempts[0] as { payments: Array<{ method: string }> }).payments.every(payment => ['Cash', 'Transfer'].includes(payment.method))).toBe(true)

  await page.emulateMedia({ media: 'print' })
  await expect(receipt).toBeVisible()
  await expect(page.locator('.app-sidebar')).toBeHidden()
  await expect(page.locator('.app-mobile-header')).toBeHidden()
  await expect(page.locator('.sales-page__header')).toBeHidden()
  await expect(page.locator('.sales-complete__summary')).toBeHidden()
  await expect(page.getByRole('button', { name: 'In hóa đơn', includeHidden: true })).toBeHidden()
  await expect(page.getByRole('button', { name: 'Đơn bán mới', includeHidden: true })).toBeHidden()
  const printWidth = await page.locator('.app-main').evaluate(element => element.getBoundingClientRect().width)
  expect(printWidth).toBeGreaterThan(250)
  expect(printWidth).toBeLessThan(300)
  const productWrap = await receipt.locator('.receipt-product').first().evaluate(element => ({
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

test('Cashier records a deliberate full-debt sale with Ghi nợ toàn bộ and sends no payment', async ({ page }) => {
  const { attempts } = await mockCashierCheckout(page)
  await page.setViewportSize({ width: 1536, height: 1024 })
  await page.goto('/sales/new')
  await page.getByRole('button', { name: `Thêm sản phẩm ${products[1].name}` }).click()

  // Opening "Nhập số tiền" alone is not a debt: completing asks for an amount or an explicit full debt.
  await page.getByRole('button', { name: 'Nhập số tiền' }).click()
  await expect(page.getByRole('note')).toHaveCount(0)
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
  await expect(page.locator('.sales-pos__message')).toHaveText('Nhập số tiền đã thu hoặc chọn Ghi nợ toàn bộ.')

  await page.getByRole('button', { name: 'Ghi nợ toàn bộ' }).click()
  await expect(page.getByRole('button', { name: 'Ghi nợ toàn bộ' })).toHaveAttribute('aria-pressed', 'true')
  await expect(page.locator('.sales-pos__full-debt-state')).toContainText('Chưa thu tiền')
  await expect(page.locator('.sales-pos__full-debt-state')).toContainText('Còn nợ 7.000 đ')
  await expect(page.getByRole('note')).toContainText('Chọn khách hàng để ghi nhận công nợ 7.000 đ.')
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
  await expect(page.locator('.sales-pos__message')).toHaveText('Chọn khách hàng khi đơn còn công nợ.')
  expect(attempts).toHaveLength(0)

  await page.locator('.sales-pos__customer-picker summary').click()
  await page.getByRole('textbox', { name: 'Tìm khách hàng' }).fill('0909')
  await page.getByRole('button', { name: 'Tìm khách hàng' }).click()
  await page.getByRole('button', { name: `Chọn khách hàng ${customer.name}` }).click()
  await expect(page.getByRole('note')).toContainText(`Ghi nhận công nợ 7.000 đ cho ${customer.name}.`)
  await captureVisual(page, 'sales-desktop-full-debt-1536x1024.png')
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
  await expect(page.getByRole('heading', { name: 'Đơn bán đã hoàn tất' })).toBeVisible()
  expect(attempts).toHaveLength(1)
  expect(attempts[0]).toMatchObject({
    customerId: customer.id,
    lines: [{ productId: products[1].id, quantity: 1 }],
    payments: [],
  })
})

test('Unresolved CompleteSale locks the exact attempt and retries it without a new operation', async ({ page }) => {
  const { attempts } = await mockCashierCheckout(page)
  const sent: Array<Record<string, unknown>> = []
  let failNext = true
  await page.route('**/api/sales/complete', route => {
    sent.push(route.request().postDataJSON() as Record<string, unknown>)
    if (!failNext) return route.fallback()
    failNext = false
    return fulfillJson(route, { title: 'Service unavailable' }, 503)
  })
  await page.route('**/api/operations/**', route => fulfillJson(route, { title: 'Not found' }, 404))
  await page.setViewportSize({ width: 1536, height: 1024 })
  await page.goto('/sales/new')
  await page.getByRole('button', { name: `Thêm sản phẩm ${products[1].name}` }).click()
  await page.getByRole('button', { name: `Thêm sản phẩm ${products[2].name}` }).click()
  await page.locator('.sales-pos__customer-picker summary').click()
  await page.getByRole('textbox', { name: 'Tìm khách hàng' }).fill('0909')
  await page.getByRole('button', { name: 'Tìm khách hàng' }).click()
  await page.getByRole('button', { name: `Chọn khách hàng ${customer.name}` }).click()
  await page.getByRole('button', { name: 'Nhập số tiền' }).click()
  await page.getByRole('spinbutton', { name: 'Số tiền thanh toán' }).fill('5000')
  await page.getByRole('button', { name: 'Thêm thanh toán' }).click()
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()

  const state = page.locator('.sales-pos__txn-state')
  await expect(state).toBeVisible()
  await expect(state).toContainText('Chưa xác định được kết quả')
  await expect(state).toContainText('SimpleStore sẽ dùng lại đúng mã thao tác trước đó để tránh tạo đơn trùng.')
  // The state block and its exact-retry CTA stay in view inside the viewport-height checkout.
  await expect(state).toBeInViewport()
  await expect(page.getByRole('button', { name: 'Thử lại đúng thao tác' })).toBeInViewport()
  await captureVisual(page, 'sales-desktop-unresolved-1536x1024.png')
  await expect(page.getByText(/operation ?id/i)).toHaveCount(0)
  await expect(page.locator('.sales-pos__status-chip')).toHaveText('Đã khóa')
  for (const locked of [
    page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }),
    page.getByRole('button', { name: `Thêm sản phẩm ${products[3].name}` }),
    page.getByRole('spinbutton', { name: `Số lượng ${products[1].name}` }),
    page.getByRole('button', { name: `Xóa ${products[1].name}` }),
    page.getByRole('button', { name: 'Tiền mặt' }),
    page.getByRole('button', { name: 'Chuyển khoản' }),
    page.getByRole('button', { name: 'Ghi nợ toàn bộ' }),
    page.getByRole('button', { name: 'Xóa khoản thanh toán 1' }),
    page.getByRole('button', { name: 'Xóa đơn' }),
  ]) await expect(locked).toBeDisabled()
  await expect(page.locator('.sales-pos__customer-picker summary')).toHaveAttribute('aria-disabled', 'true')
  await expect(page.locator('.sales-pos__create-customer summary')).toHaveAttribute('aria-disabled', 'true')
  await page.locator('.sales-pos__customer-picker summary').click()
  await expect(page.getByRole('textbox', { name: 'Tìm khách hàng' })).toBeHidden()
  const shortcut = page.getByRole('link', { name: 'Lịch sử bán hàng' })
  await expect(shortcut).toHaveAttribute('aria-disabled', 'true')
  await shortcut.click({ force: true })
  await expect(page).toHaveURL(/\/sales\/new$/)
  // Locked controls stay legible (tinted, not faded) so the lock reads as protection.
  const lockedStyle = await page.getByRole('button', { name: 'Chuyển khoản' }).evaluate(element => getComputedStyle(element).opacity)
  expect(Number(lockedStyle)).toBe(1)

  await page.getByRole('button', { name: 'Thử lại đúng thao tác' }).click()
  await expect(page.getByRole('heading', { name: 'Đơn bán đã hoàn tất' })).toBeVisible()
  expect(sent).toHaveLength(2)
  expect(sent[1]).toEqual(sent[0])
  expect(attempts).toHaveLength(1)
  expect(attempts[0]).toMatchObject({ customerId: customer.id, payments: [{ method: 'Cash', amount: 5000 }] })
})

test('A rejected CompleteSale returns to editable correction without lock residue', async ({ page }) => {
  const { attempts } = await mockCashierCheckout(page)
  let rejectNext = true
  await page.route('**/api/sales/complete', route => {
    if (!rejectNext) return route.fallback()
    rejectNext = false
    return fulfillJson(route, { title: `Không đủ tồn kho cho ${products[1].name}.` }, 422)
  })
  await page.setViewportSize({ width: 1536, height: 1024 })
  await page.goto('/sales/new')
  await page.getByRole('button', { name: `Thêm sản phẩm ${products[1].name}` }).click()
  await page.getByRole('spinbutton', { name: `Số lượng ${products[1].name}` }).fill('40')
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()

  const error = page.locator('.sales-pos__error')
  await expect(error).toContainText('Chưa hoàn tất đơn bán')
  await expect(error).toContainText(`Không đủ tồn kho cho ${products[1].name}.`)
  await expect(page.getByRole('button', { name: 'Hoàn tất bán hàng' })).toBeInViewport()
  await expect(page.locator('.sales-pos__txn-state')).toHaveCount(0)
  await expect(page.locator('.sales-pos__status-chip')).toHaveText('Đang bán')
  await expect(page.getByRole('link', { name: 'Lịch sử bán hàng' })).not.toHaveAttribute('aria-disabled', 'true')
  for (const editable of [
    page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }),
    page.getByRole('spinbutton', { name: `Số lượng ${products[1].name}` }),
    page.getByRole('button', { name: 'Chuyển khoản' }),
    page.getByRole('button', { name: 'Ghi nợ toàn bộ' }),
  ]) await expect(editable).toBeEnabled()
  const errorBottom = await error.evaluate(element => element.getBoundingClientRect().bottom)
  const ctaTop = await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).evaluate(element => element.getBoundingClientRect().top)
  expect(ctaTop - errorBottom).toBeGreaterThanOrEqual(0)
  expect(ctaTop - errorBottom).toBeLessThan(24)
  await captureVisual(page, 'sales-desktop-rejected-1536x1024.png')

  await page.getByRole('spinbutton', { name: `Số lượng ${products[1].name}` }).fill('2')
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
  await expect(page.getByRole('heading', { name: 'Đơn bán đã hoàn tất' })).toBeVisible()
  expect(attempts).toHaveLength(1)
  expect(attempts[0]).toMatchObject({ lines: [{ productId: products[1].id, quantity: 2 }] })
})

test('A long cart with large amounts keeps the desktop Complete CTA reachable', async ({ page }) => {
  await mockCashierCheckout(page)
  await page.setViewportSize({ width: 1536, height: 1024 })
  await page.goto('/sales/new')
  for (const item of products.slice(0, 9)) {
    await page.getByRole('button', { name: `Thêm sản phẩm ${item.name}` }).click()
  }
  await page.getByRole('spinbutton', { name: `Số lượng ${products[6].name}` }).fill('12500')
  await expect(page.locator('.sales-pos__grand-value')).toHaveText('562.618.000 đ')
  const cta = page.getByRole('button', { name: 'Hoàn tất bán hàng' })
  await expect(cta).toBeInViewport()
  // The ninth line, added last, is scrolled into view inside the cart list.
  await expect(page.getByRole('spinbutton', { name: `Số lượng ${products[8].name}` })).toBeInViewport()
  const layout = await page.evaluate(() => {
    const list = document.querySelector('.sales-pos__order-items')!
    const checkout = document.querySelector('.sales-pos__checkout')!
    return {
      listScrolls: list.scrollHeight > list.clientHeight,
      checkoutBottom: checkout.getBoundingClientRect().bottom,
      checkoutOverflowX: checkout.scrollWidth - checkout.clientWidth,
      amountsFit: [...document.querySelectorAll('.sales-pos__line-amount, .sales-pos__grand-value')]
        .every(element => element.getBoundingClientRect().right <= checkout.getBoundingClientRect().right),
    }
  })
  expect(layout.listScrolls).toBe(true)
  expect(layout.checkoutBottom).toBeLessThanOrEqual(1024)
  expect(layout.checkoutOverflowX).toBeLessThanOrEqual(0)
  expect(layout.amountsFit).toBe(true)
  await expectNoHorizontalOverflow(page, 1536)
  await captureVisual(page, 'sales-desktop-long-cart-1536x1024.png')
})

test('Lịch sử bán hàng shortcut opens Sales history and keeps the working order', async ({ page }) => {
  await mockCashierCheckout(page)
  await page.route('**/api/sales?**', route => fulfillJson(route, { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }))
  await page.setViewportSize({ width: 1536, height: 1024 })
  await page.goto('/sales/new')
  await page.getByRole('button', { name: `Thêm sản phẩm ${products[1].name}` }).click()
  await page.locator('.sales-pos__checkout-tools').getByRole('link', { name: 'Lịch sử bán hàng' }).click()
  await expect(page).toHaveURL(/\/sales$/)
  await expect(page.getByRole('heading', { name: 'Lịch sử bán hàng' })).toBeVisible()
  await page.goBack()
  await expect(page.getByRole('spinbutton', { name: `Số lượng ${products[1].name}` })).toHaveValue('1')
})

test('Live working order survives navigation in one session and is cleared by logout', async ({ page }) => {
  await mockCashierCheckout(page)
  let signedIn = true
  const session = () => signedIn
    ? { isAuthenticated: true, email: 'cashier@example.test', storeId: 'store-1', roles: ['Cashier'], hasStore: true, mustChangePassword: false, isEnabled: true }
    : { isAuthenticated: false, email: null, storeId: null, roles: [], hasStore: false, mustChangePassword: false, isEnabled: false }
  await page.route('**/api/auth/session', route => fulfillJson(route, session()))
  await page.route('**/api/auth/logout', route => { signedIn = false; return route.fulfill({ status: 204, body: '' }) })
  await page.route('**/api/auth/login', route => { signedIn = true; return fulfillJson(route, session()) })
  await page.setViewportSize({ width: 1536, height: 1024 })
  const navigation = page.getByRole('navigation', { name: 'Điều hướng chính', exact: true })

  await page.goto('/sales/new')
  await page.getByRole('button', { name: `Thêm sản phẩm ${products[1].name}` }).click()
  await page.locator('.sales-pos__customer-picker summary').click()
  await page.getByRole('textbox', { name: 'Tìm khách hàng' }).fill('0909')
  await page.getByRole('button', { name: 'Tìm khách hàng' }).click()
  await page.getByRole('button', { name: `Chọn khách hàng ${customer.name}` }).click()
  await page.getByRole('button', { name: 'Nhập số tiền' }).click()
  await page.getByRole('spinbutton', { name: 'Số tiền thanh toán' }).fill('5000')
  await page.getByRole('button', { name: 'Thêm thanh toán' }).click()

  await navigation.getByRole('link', { name: 'Sản phẩm' }).click()
  await expect(page).toHaveURL(/\/products$/)
  await navigation.getByRole('link', { name: 'Bán hàng' }).click()
  await expect(page.getByRole('spinbutton', { name: `Số lượng ${products[1].name}` })).toHaveValue('1')
  await expect(page.locator('.sales-pos__customer-picker summary')).toContainText(customer.name)
  await expect(page.getByRole('list', { name: 'Các khoản đã nhập' })).toContainText('5.000 đ')

  await page.locator('.app-topbar-profile summary').click()
  await page.locator('.app-topbar-profile').getByRole('button', { name: 'Đăng xuất' }).click()
  await expect(page).toHaveURL(/\/login$/)
  await page.locator('#email').fill('cashier@example.test')
  await page.locator('#password').fill('secret')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/products$/)
  await navigation.getByRole('link', { name: 'Bán hàng' }).click()

  await expect(page.getByText('Chưa có sản phẩm. Tìm hoặc quét sản phẩm ở bên trái để bắt đầu đơn bán.')).toBeVisible()
  await expect(page.locator('.sales-pos__customer-picker summary')).toContainText('Khách lẻ')
  await expect(page.getByRole('list', { name: 'Các khoản đã nhập' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Tiền mặt' })).toHaveAttribute('aria-pressed', 'true')
  await expect(page.getByRole('button', { name: 'Nhập số tiền' })).toBeVisible()
})

test('Sales workspace remains usable without horizontal clipping on tablet and mobile', async ({ page }) => {
  await mockCashierCheckout(page)
  for (const viewport of [{ width: 820, height: 900 }, { width: 390, height: 844 }]) {
    await page.setViewportSize(viewport)
    await page.goto('/sales/new')
    await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).fill(product.sku)
    await page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' }).press('Enter')
    await expect(page.getByText(productName)).toBeVisible()
    await expect(page.getByRole('button', { name: `Thêm sản phẩm ${productName}` })).toBeVisible()
    await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
    await expect(page.getByRole('spinbutton', { name: `Số lượng ${productName}` })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Hoàn tất bán hàng' })).toBeVisible()
    await expect(page.locator('.sales-pos__order-rail')).toHaveCount(0)
    await expect(page.locator('.sales-pos__pay-btn')).toHaveText(['Tiền mặt', 'Chuyển khoản'])
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
    await page.locator('.sales-pos__checkout').evaluate(element => {
      element.scrollIntoView({ block: 'start' })
      const mobileHeader = document.querySelector('.app-mobile-header')?.getBoundingClientRect().height ?? 0
      window.scrollBy(0, -(mobileHeader + 8))
    })
    await captureVisual(page, `sales-${viewport.width === 820 ? 'tablet-820x900' : 'mobile-390x844'}.png`)
    await expectNoHorizontalOverflow(page, viewport.width)
    await expectAddCustomerOnOneLine(page)
    // Customer controls stay usable: both dropdowns open inside the viewport.
    for (const picker of ['.sales-pos__create-customer', '.sales-pos__customer-picker']) {
      await page.locator(`${picker} summary`).click()
      const panel = await page.locator(`${picker} .sales-pos__dropdown`).boundingBox()
      expect(panel!.x).toBeGreaterThanOrEqual(0)
      expect(panel!.x + panel!.width).toBeLessThanOrEqual(viewport.width)
      expect(panel!.width).toBeGreaterThan(260)
      await page.locator(`${picker} summary`).click()
    }
    const ctaBox = await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).boundingBox()
    expect(ctaBox!.height).toBeGreaterThanOrEqual(44)
    const payHeights = await page.locator('.sales-pos__pay-btn').evaluateAll(buttons => buttons.map(button => button.getBoundingClientRect().height))
    for (const height of payHeights) expect(height).toBeGreaterThanOrEqual(viewport.width < 600 ? 44 : 40)
    await expect(page.getByRole('button', { name: 'Ghi nợ toàn bộ' })).toBeVisible()
    await captureVisual(page, `sales-${viewport.width === 820 ? 'tablet-820x900' : 'mobile-390x844'}-full.png`, true)

    await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
    await expect(page.getByRole('heading', { name: 'Đơn bán đã hoàn tất' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Đơn bán mới' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'In hóa đơn' })).toBeVisible()
    await expectNoHorizontalOverflow(page, viewport.width)
    await captureVisual(page, `sales-completed-${viewport.width === 820 ? 'tablet-820x900' : 'mobile-390x844'}.png`, true)
    await page.getByRole('button', { name: 'Đơn bán mới' }).click()
    await expect(page.getByRole('textbox', { name: 'Tìm hoặc quét sản phẩm' })).toBeVisible()
  }
})

test('Sales reference preview keeps D-106 visual-only elements without any API write', async ({ page }) => {
  const { attempts } = await mockCashierCheckout(page)
  await page.setViewportSize({ width: 1536, height: 1024 })
  await page.goto('/sales/new')
  await expect(page.getByRole('heading', { name: 'Đơn 1', exact: true })).toBeVisible()
  const writes: string[] = []
  page.on('request', request => {
    if (request.url().includes('/api/') && request.method() !== 'GET') writes.push(`${request.method()} ${request.url()}`)
  })
  await page.locator('.app-topbar').getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  const demo = page.locator('.sales-pos--demo')
  await expect(page.locator('.app-topbar')).toContainText('Việt Anh')
  await expect(page.locator('.sales-pos__product-card:visible')).toHaveCount(12)
  await expect(demo.locator('.sales-pos__product-card img')).toHaveCount(12)
  await expect(demo.locator('.sales-pos__categories .sales-pos__chip')).toHaveCount(7)
  await expect(demo.getByText('Rất ít hàng').first()).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Đơn 1' })).toBeVisible()
  await expect(demo.locator('button.sales-pos__order-pill')).toHaveCount(3)
  await expect(demo.getByText('Danh sách đơn đang chờ (3)')).toBeVisible()
  for (const name of ['Đơn mới', 'Giữ đơn', 'Thêm giảm giá hóa đơn', 'Bán nợ']) {
    await expect(demo.getByRole('button', { name })).toBeVisible()
  }
  await expect(demo.getByLabel('Ghi chú đơn hàng')).toBeVisible()
  await expect(demo.locator('.sales-pos__line-discount')).toBeVisible()
  await expect(demo.locator('.sales-pos__pay-btn')).toHaveText(['Tiền mặt', 'Chuyển khoản', 'Bán nợ'])
  await captureVisual(page, 'sales-reference-preview-1536x1024.png')
  await demo.getByRole('button', { name: 'Bán nợ' }).click()
  await demo.getByRole('button', { name: 'Giữ đơn' }).click()
  await demo.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
  await expect(demo.locator('.sales-pos__message')).toContainText('dữ liệu mẫu')

  for (const viewport of [{ width: 820, height: 900 }, { width: 390, height: 844 }]) {
    await page.setViewportSize(viewport)
    await expect(demo.locator('.sales-pos__checkout')).toBeVisible()
    await captureVisual(page, `sales-reference-preview-${viewport.width === 820 ? 'tablet-820x900' : 'mobile-390x844'}.png`)
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(viewport.width)
  }
  expect(attempts).toHaveLength(0)
  expect(writes).toEqual([])
})
