import { expect, test } from '@playwright/test'

const email = process.env.SIMPLESTORE_E2E_EMAIL
const password = process.env.SIMPLESTORE_E2E_PASSWORD

test('Owner completes a purchase with partial payment and sees inventory cost and debt', async ({ page }) => {
  if (!email || !password) throw new Error('Run this test through tests/e2e/run-real-slice2.ps1.')

  const suffix = Date.now().toString()
  const productName = `Cà phê Slice 2 ${suffix}`
  const supplierName = `Nhà cung cấp Slice 2 ${suffix}`

  await page.goto('/login')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/(setup|products)$/)
  if (page.url().endsWith('/setup')) {
    await page.locator('#store-name').fill(`Cửa hàng Slice 2 ${suffix}`)
    await page.getByRole('button', { name: 'Bắt đầu quản lý sản phẩm' }).click()
  }

  await expect(page).toHaveURL(/\/products$/)
  await page.getByRole('link', { name: 'Thêm sản phẩm' }).click()
  await page.locator('#name').fill(productName)
  await page.locator('#unit').fill('gói')
  await page.locator('#sku').fill(`S2-${suffix}`)
  await page.locator('#sale-price').fill('18000')
  await page.locator('#opening-quantity').fill('10')
  await page.locator('#opening-cost').fill('10000')
  await page.getByRole('button', { name: 'Lưu sản phẩm' }).click()
  await expect(page).toHaveURL(/\/products\/[0-9a-f-]+$/)

  await page.getByRole('link', { name: 'Nhà cung cấp' }).click()
  await expect(page).toHaveURL(/\/suppliers$/)
  const supplierForm = page.locator('form').first()
  await supplierForm.locator('input').nth(0).fill(supplierName)
  await supplierForm.locator('input').nth(1).fill('0909123456')
  await supplierForm.getByRole('button', { name: 'Lưu' }).click()
  await expect(page.getByRole('row').filter({ hasText: supplierName })).toBeVisible()

  await page.getByRole('link', { name: 'Nhập hàng' }).click()
  await expect(page).toHaveURL(/\/purchases$/)
  await page.getByRole('link', { name: 'Tạo phiếu nhập' }).click()
  await page.getByLabel('Tìm nhà cung cấp').fill(supplierName)
  await page.getByRole('button', { name: 'Tìm nhà cung cấp' }).click()
  await page.getByRole('button', { name: `Chọn nhà cung cấp ${supplierName}` }).click()
  await page.getByLabel('Tìm sản phẩm').fill(productName)
  await page.getByRole('button', { name: 'Tìm sản phẩm' }).click()
  await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
  await page.getByLabel('Số lượng').fill('20')
  await page.getByLabel('Giá nhập').fill('13000')
  await page.getByRole('button', { name: 'Lưu nháp' }).click()

  await expect(page).toHaveURL(/\/purchases\/[0-9a-f-]+$/)
  await expect(page.getByText('260.000 ₫', { exact: true }).first()).toBeVisible()
  await page.getByLabel('Số tiền thanh toán').fill('100000')
  await page.getByRole('button', { name: 'Thêm thanh toán' }).click()
  await expect(page.getByText('160.000 ₫', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Hoàn tất phiếu nhập' }).click()

  await expect(page.getByText('Đã hoàn tất', { exact: true })).toBeVisible()
  await expect(page.getByText('100.000 ₫', { exact: true }).first()).toBeVisible()
  await expect(page.getByText('160.000 ₫', { exact: true }).first()).toBeVisible()
  await page.getByRole('link', { name: productName }).click()
  await expect(page.getByText('30 gói', { exact: true }).first()).toBeVisible()
  await expect(page.getByText('360.000 ₫', { exact: true }).first()).toBeVisible()
  await expect(page.getByText('12.000 ₫', { exact: true }).first()).toBeVisible()
  await expect(page.getByText('Nhập hàng', { exact: true }).first()).toBeVisible()
})
