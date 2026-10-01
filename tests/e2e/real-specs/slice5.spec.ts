import { expect, test, type Page } from '@playwright/test'

const email = process.env.SIMPLESTORE_E2E_EMAIL
const password = process.env.SIMPLESTORE_E2E_PASSWORD
const runId = process.env.SIMPLESTORE_E2E_RUN_ID

async function login(page: Page) {
  if (!email || !password || !runId) throw new Error('Run through run-real-slice5.ps1.')
  await page.goto('/login')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.locator('button[type="submit"]').click()
  await expect(page).toHaveURL(/\/(setup|products)$/)
  if (page.url().endsWith('/setup')) {
    await page.locator('#store-name').fill(`Cửa hàng Slice 5B ${runId}`)
    await page.locator('button[type="submit"]').click()
    await expect(page).toHaveURL(/\/products$/)
  }
}

async function createProduct(page: Page, productName: string) {
  await page.goto('/products/new')
  await page.locator('#name').fill(productName)
  await page.locator('#unit').fill('gói')
  await page.locator('#sku').fill(`S5B-${runId!.slice(-8)}`)
  await page.locator('#sale-price').fill('12000')
  await page.locator('#opening-quantity').fill('5')
  await page.locator('#opening-cost').fill('8000')
  await page.locator('button[type="submit"]').click()
  await expect(page).toHaveURL(/\/products\/[0-9a-f-]+$/)
}

test.describe.serial('Slice 5B real debt and end-of-day flows', () => {
  test.setTimeout(90_000)
  test('Owner records customer and supplier debt payments then verifies EOD', async ({ page }) => {
    await login(page)
    const productName = `Debt E2E ${runId}`
    const customerName = `Customer E2E ${runId}`
    const supplierName = `Supplier E2E ${runId}`
    await createProduct(page, productName)

    await page.goto('/sales/new')
    await page.getByLabel('Tìm hoặc quét sản phẩm').fill(productName)
    await page.getByLabel('Tìm hoặc quét sản phẩm').press('Enter')
    await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
    await page.getByRole('spinbutton', { name: `Số lượng ${productName}` }).fill('1')
    await page.locator('.sales-pos__create-customer summary').click()
    await page.getByLabel('Tên khách hàng mới').fill(customerName)
    await page.getByLabel('Số điện thoại khách hàng mới').fill('0901000001')
    await page.getByRole('button', { name: 'Tạo và chọn khách hàng' }).click()
    await expect(page.getByText(customerName, { exact: true })).toBeVisible()
    await page.getByRole('button', { name: 'Ghi nợ toàn bộ' }).click()
    await expect(page.locator('.sales-pos__full-debt-state')).toContainText('Chưa thu tiền')
    await expect(page.locator('.sales-pos__full-debt-state')).toContainText('Còn nợ 12.000 đ')
    await expect(page.getByRole('note')).toContainText(`Ghi nhận công nợ 12.000 đ cho ${customerName}.`)
    const completeRequest = page.waitForRequest(request => request.method() === 'POST' && request.url().includes('/api/sales/complete'))
    await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
    const completePayload = (await completeRequest).postDataJSON() as { customerId: string | null; payments: unknown[] }
    expect(completePayload.payments).toEqual([])
    expect(completePayload.customerId).toBeTruthy()
    await expect(page.getByRole('heading', { name: 'Đơn bán đã hoàn tất' })).toBeVisible()

    await page.goto('/customers/debts')
    const customerRow = page.getByRole('row').filter({ hasText: customerName })
    await expect(customerRow).toContainText('12.000 đ')
    await customerRow.getByRole('button', { name: 'Thu tiền' }).click()
    const collect = page.getByRole('dialog', { name: 'Thu tiền khách hàng' })
    await collect.getByRole('spinbutton').fill('4000')
    const recoveredRequests: Array<Record<string, unknown>> = []
    let firstAttempt = true
    await page.route('**/api/customers/*/debt-payments', async route => {
      if (route.request().method() !== 'POST') return route.continue()
      recoveredRequests.push(route.request().postDataJSON() as Record<string, unknown>)
      if (firstAttempt) {
        firstAttempt = false
        return route.abort('timedout')
      }
      return route.continue()
    })
    await collect.getByRole('button', { name: 'Thu tiền', exact: true }).click()
    await expect(collect).toContainText('Đây là tiền thực thu, không phải doanh thu')
    await collect.getByRole('button', { name: 'Xác nhận thu tiền' }).click()
    await expect(collect.getByLabel('Chi tiết thanh toán công nợ')).toContainText('Công nợ còn lại: 8.000 đ')
    expect(recoveredRequests).toHaveLength(2)
    expect(recoveredRequests[0].operationId).toBe(recoveredRequests[1].operationId)
    expect(recoveredRequests[0].amount).toBe(recoveredRequests[1].amount)
    await page.unroute('**/api/customers/*/debt-payments')

    await collect.getByRole('button', { name: 'Xong' }).click()
    await customerRow.getByRole('button', { name: 'Thu tiền' }).click()
    await collect.getByRole('spinbutton').fill('8000')
    await collect.getByRole('button', { name: 'Thu tiền', exact: true }).click()
    await collect.getByRole('button', { name: 'Xác nhận thu tiền' }).click()
    await expect(collect.getByLabel('Chi tiết thanh toán công nợ')).toContainText('8.000 đ → 0 đ')
    await collect.getByRole('button', { name: 'Xong' }).click()

    await page.goto('/suppliers')
    const supplierForm = page.locator('form').first()
    await supplierForm.locator('input').nth(0).fill(supplierName)
    await supplierForm.getByRole('button', { name: 'Lưu' }).click()
    await expect(page.getByRole('row').filter({ hasText: supplierName })).toBeVisible()
    await page.goto('/purchases/new')
    await page.getByLabel('Tìm nhà cung cấp').fill(supplierName)
    await page.getByRole('button', { name: 'Tìm nhà cung cấp' }).click()
    await page.getByRole('button', { name: `Chọn nhà cung cấp ${supplierName}` }).click()
    await page.getByLabel('Tìm sản phẩm').fill(productName)
    await page.getByRole('button', { name: 'Tìm sản phẩm' }).click()
    await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
    await page.getByLabel('Số lượng').fill('1')
    await page.getByLabel('Giá nhập').fill('9000')
    await page.getByRole('button', { name: 'Lưu nháp' }).click()
    await page.getByRole('button', { name: 'Hoàn tất phiếu nhập' }).click()
    await expect(page.getByText('Đã hoàn tất', { exact: true })).toBeVisible()

    await page.goto('/suppliers/debts')
    const supplierDebtRow = page.getByRole('row').filter({ hasText: supplierName })
    await expect(supplierDebtRow).toContainText('9.000 đ')
    await supplierDebtRow.getByRole('button', { name: 'Trả tiền' }).click()
    const settle = page.getByRole('dialog', { name: 'Trả tiền nhà cung cấp' })
    await settle.getByRole('spinbutton').fill('3000')
    await settle.getByRole('button', { name: 'Trả tiền', exact: true }).click()
    await settle.getByRole('button', { name: 'Xác nhận trả tiền' }).click()
    await expect(settle.getByLabel('Chi tiết thanh toán công nợ')).toContainText('Công nợ còn lại: 6.000 đ')
    await settle.getByRole('button', { name: 'Xong' }).click()

    await page.getByRole('navigation', { name: 'Điều hướng chính', exact: true }).getByRole('link', { name: 'Báo cáo', exact: true }).click()
    await expect(page.getByRole('heading', { name: 'Báo cáo cuối ngày' })).toBeVisible()
    await expect(page.getByText('Doanh thu', { exact: true }).locator('..')).toContainText('12.000 ₫')
    await expect(page.getByText('Tiền thu thuần', { exact: true }).first().locator('..')).toContainText('12.000 ₫')
    await expect(page.getByText('Công nợ cuối ngày').locator('..')).toContainText('0 ₫')
    await expect(page.getByText('Công nợ cuối ngày').locator('..')).toContainText('6.000 ₫')
    await expect(page.getByText('Thanh toán nhà cung cấp').locator('..')).toContainText('3.000 ₫')
  })
})
