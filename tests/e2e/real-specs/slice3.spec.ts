import { expect, test } from '@playwright/test'

const ownerEmail = process.env.SIMPLESTORE_E2E_EMAIL
const ownerPassword = process.env.SIMPLESTORE_E2E_PASSWORD
const cashierEmail = process.env.SIMPLESTORE_E2E_CASHIER_EMAIL
const cashierPassword = process.env.SIMPLESTORE_E2E_CASHIER_PASSWORD
const runId = process.env.SIMPLESTORE_E2E_RUN_ID

test('Owner prepares Slice 3 cashier scenario', async ({ page }) => {
  if (!ownerEmail || !ownerPassword || !runId) throw new Error('Run through run-real-slice3.ps1.')
  const productName = `Sale E2E ${runId}`
  await page.goto('/login')
  await page.locator('#email').fill(ownerEmail)
  await page.locator('#password').fill(ownerPassword)
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/(setup|products)$/)
  if (page.url().endsWith('/setup')) {
    await page.locator('#store-name').fill(`Cửa hàng Slice 3 ${runId}`)
    await page.getByRole('button', { name: 'Bắt đầu quản lý sản phẩm' }).click()
  }
  await page.getByRole('link', { name: 'Thêm sản phẩm' }).click()
  await page.locator('#name').fill(productName)
  await page.locator('#unit').fill('gói')
  await page.locator('#sku').fill(`S3-${runId}`)
  await page.locator('#barcode').fill(`893${runId.slice(-9)}`)
  await page.locator('#sale-price').fill('12000')
  await page.locator('#opening-quantity').fill('5')
  await page.locator('#opening-cost').fill('8000')
  await page.getByRole('button', { name: 'Lưu sản phẩm' }).click()
  await expect(page).toHaveURL(/\/products\/[0-9a-f-]+$/)
})

test('Cashier completes, prints and reprints a real sale without duplicate effects', async ({ page }) => {
  if (!cashierEmail || !cashierPassword || !runId) throw new Error('Run through run-real-slice3.ps1.')
  const productName = `Sale E2E ${runId}`
  await page.addInitScript(() => {
    window.print = () => sessionStorage.setItem('simplestore-print-called', 'true')
  })
  await page.goto('/login')
  await page.locator('#email').fill(cashierEmail)
  await page.locator('#password').fill(cashierPassword)
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/products$/)
  await page.getByRole('link', { name: 'Bán hàng' }).first().click()
  await page.getByLabel('Tìm hoặc quét sản phẩm').fill(productName)
  await page.getByRole('button', { name: 'Tìm sản phẩm' }).click()
  await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
  await page.getByLabel(`Số lượng ${productName}`).fill('2')
  await page.getByLabel('Số tiền thanh toán').fill('24000')
  await page.getByRole('button', { name: 'Thêm thanh toán' }).click()
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()

  await expect(page.getByText('Đơn bán đã hoàn tất. Việc in không thay đổi trạng thái giao dịch.')).toBeVisible()
  await expect(page.getByRole('region', { name: 'Hóa đơn bán hàng' })).toContainText(productName)
  await page.getByRole('button', { name: 'In hóa đơn' }).click()
  expect(await page.evaluate(() => sessionStorage.getItem('simplestore-print-called'))).toBe('true')

  await page.getByRole('link', { name: 'Đơn bán', exact: true }).click()
  await expect(page.getByRole('row')).toHaveCount(2)
  await page.getByRole('row').nth(1).getByRole('link').click()
  await expect(page.getByRole('region', { name: 'Hóa đơn bán hàng' })).toContainText(productName)
  await page.evaluate(() => sessionStorage.removeItem('simplestore-print-called'))
  await page.getByRole('button', { name: 'In hóa đơn' }).click()
  expect(await page.evaluate(() => sessionStorage.getItem('simplestore-print-called'))).toBe('true')

  await page.getByRole('link', { name: 'Sản phẩm' }).click()
  await page.getByLabel('Tìm sản phẩm').fill(productName)
  await page.getByRole('button', { name: 'Tìm kiếm' }).click()
  await page.getByRole('link', { name: productName }).click()
  await expect(page.getByText('3 gói', { exact: true }).first()).toBeVisible()
})
