import { expect, test } from '@playwright/test'

const email = process.env.SIMPLESTORE_E2E_EMAIL
const password = process.env.SIMPLESTORE_E2E_PASSWORD

test('Owner initializes a store and creates a product with opening stock', async ({ page }) => {
  if (!email || !password) {
    throw new Error('Run this test through tests/e2e/run-real-slice1.ps1.')
  }

  const suffix = Date.now().toString()
  const storeName = `Cửa hàng E2E ${suffix}`
  const productName = `Nước suối E2E ${suffix}`
  const sku = `E2E-${suffix}`

  await page.goto('/login')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.locator('form').getByRole('button', { name: 'Đăng nhập' }).click()

  await expect(page).toHaveURL(/\/setup$/)
  await page.locator('#store-name').fill(storeName)
  await page.locator('form').getByRole('button', { name: 'Bắt đầu quản lý sản phẩm' }).click()

  await expect(page).toHaveURL(/\/products$/)
  await page.getByRole('link', { name: 'Thêm sản phẩm' }).click()
  await page.locator('#name').fill(productName)
  await page.locator('#unit').fill('chai')
  await page.locator('#sku').fill(sku)
  await page.locator('#sale-price').fill('12000')
  await page.locator('#opening-quantity').fill('20')
  await page.locator('#opening-cost').fill('8000')
  await page.locator('form').getByRole('button', { name: 'Lưu sản phẩm' }).click()

  await expect(page).toHaveURL(/\/products\/[0-9a-f-]+$/)
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(productName)
  await expect(page.getByText('20 chai', { exact: true }).first()).toBeVisible()
  await expect(page.getByText('160.000 ₫', { exact: true }).first()).toBeVisible()
  await expect(page.getByText('Tồn đầu', { exact: true }).first()).toBeVisible()

  await page.getByRole('link', { name: /Danh sách sản phẩm/ }).click()
  const row = page.getByRole('row').filter({ hasText: productName })
  await expect(row).toContainText(sku)
  await expect(row).toContainText('20 chai')
})
