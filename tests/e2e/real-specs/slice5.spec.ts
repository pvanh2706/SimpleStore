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
    await page.getByRole('button', { name: 'Tìm sản phẩm' }).click()
    await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
    await page.getByLabel(`Số lượng ${productName}`).fill('1')
    await page.getByLabel('Tên khách hàng mới').fill(customerName)
    await page.getByLabel('Số điện thoại khách hàng mới').fill('0901000001')
    await page.getByRole('button', { name: 'Tạo và chọn khách hàng' }).click()
    await expect(page.getByText(customerName, { exact: true })).toBeVisible()
    await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
    await expect(page.getByText('Đơn bán đã hoàn tất. Việc in không thay đổi trạng thái giao dịch.')).toBeVisible()

    await page.getByRole('link', { name: 'Công nợ khách' }).click()
    const customerRow = page.getByRole('row').filter({ hasText: customerName })
    await expect(customerRow).toContainText('12.000 ₫')
    await customerRow.getByRole('button', { name: 'thu nợ' }).click()
    await page.locator('form.card input[type="number"]').fill('4000')
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
    page.once('dialog', dialog => dialog.accept())
    await page.getByRole('button', { name: 'Xác nhận thu nợ' }).click()
    await expect(page.getByRole('status')).toContainText('Công nợ còn lại: 8.000 ₫')
    expect(recoveredRequests).toHaveLength(2)
    expect(recoveredRequests[0].operationId).toBe(recoveredRequests[1].operationId)
    expect(recoveredRequests[0].amount).toBe(recoveredRequests[1].amount)
    await page.unroute('**/api/customers/*/debt-payments')

    await page.locator('form.card input[type="number"]').fill('8000')
    page.once('dialog', dialog => dialog.accept())
    await page.getByRole('button', { name: 'Xác nhận thu nợ' }).click()
    await expect(page.getByRole('status')).toContainText('Công nợ còn lại: 0 ₫')

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

    await page.getByRole('link', { name: 'Công nợ NCC' }).click()
    const supplierDebtRow = page.getByRole('row').filter({ hasText: supplierName })
    await expect(supplierDebtRow).toContainText('9.000 ₫')
    await supplierDebtRow.getByRole('button', { name: 'trả nợ' }).click()
    await page.locator('form.card input[type="number"]').fill('3000')
    page.once('dialog', dialog => dialog.accept())
    await page.getByRole('button', { name: 'Xác nhận trả nợ' }).click()
    await expect(page.getByRole('status')).toContainText('Công nợ còn lại: 6.000 ₫')

    await page.getByRole('link', { name: 'Cuối ngày' }).click()
    await expect(page.getByRole('heading', { name: 'Báo cáo cuối ngày' })).toBeVisible()
    await expect(page.getByText('Doanh thu', { exact: true }).locator('..')).toContainText('12.000 ₫')
    await expect(page.getByText('Tiền thu thuần', { exact: true }).first().locator('..')).toContainText('12.000 ₫')
    await expect(page.getByText('Công nợ cuối ngày').locator('..')).toContainText('0 ₫')
    await expect(page.getByText('Công nợ cuối ngày').locator('..')).toContainText('6.000 ₫')
    await expect(page.getByText('Thanh toán nhà cung cấp').locator('..')).toContainText('3.000 ₫')
  })
})
