import { expect, test, type Page } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

async function capture(page: Page, name: string) {
  if (!process.env.SETTINGS_CAPTURE_DIR) return
  await mkdir(process.env.SETTINGS_CAPTURE_DIR, { recursive: true })
  await page.screenshot({ path: join(process.env.SETTINGS_CAPTURE_DIR, `${name}.png`) })
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
  await page.route('**/api/store/operational-settings', route => route.fulfill({ json: { allowNegativeStock: false } }))
}

test('settings show the reference Giao diện preview and keep the real Vận hành form', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  await mockOwner(page)
  await page.route('**/api/store/operational-settings/negative-stock', route => route.fulfill({ json: route.request().postDataJSON() }))

  await page.goto('/settings/operations')
  await expect(page.locator('.app-sidebar a[aria-current="page"]')).toHaveText('Cài đặt')
  await expect(page.locator('#settings-store-name')).toHaveValue('Tạp hóa Việt Anh')
  await expect(page.getByRole('button', { name: '✓ Lưu thay đổi' })).toBeDisabled()
  await capture(page, 'settings-live-appearance')

  await page.getByRole('link', { name: 'Vận hành' }).click()
  await expect(page).toHaveURL(/section=operations/)
  await page.getByRole('switch', { name: /Cho phép bán khi tồn kho không đủ/ }).click()
  await expect(page.getByText(/tồn kho âm và độ tin cậy giá vốn/)).toBeVisible()
  await expect(page.locator('.settings-toggle').first()).toHaveCSS('background-color', 'rgb(11, 154, 104)')
  await capture(page, 'settings-live-operations')
  const saved = page.waitForRequest(request => request.method() === 'PUT')
  await page.getByRole('button', { name: '✓ Lưu thay đổi' }).click()
  expect((await saved).postDataJSON()).toEqual({ allowNegativeStock: true })
  await expect(page.getByText('Đã lưu thiết lập vận hành.')).toBeVisible()

  const writes: string[] = []
  page.on('request', request => { if (request.url().includes('/api/') && request.method() !== 'GET') writes.push(request.url()) })
  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.locator('#settings-store-name')).toHaveValue('Tạp hóa Minh Anh')
  await expect(page.locator('.settings-mini-product img').last()).toHaveJSProperty('complete', true)
  await expect(page.locator('.settings-photo-thumb')).toHaveJSProperty('complete', true)
  await capture(page, 'settings-design')

  await page.getByRole('button', { name: 'Ocean' }).click()
  await page.getByRole('radio', { name: '☾ Tối' }).click()
  await page.getByRole('radio', { name: /Thoải mái/ }).click()
  await page.getByRole('switch', { name: /Bo góc mềm/ }).click()
  await expect(page.getByRole('switch', { name: /Bo góc mềm/ })).toHaveAttribute('aria-checked', 'false')
  await expect(page.locator('.settings-page')).toHaveClass(/square/)
  await expect(page.locator('.settings-card').first()).toHaveCSS('border-radius', '4px')
  await expect(page.getByRole('switch', { name: /Bo góc mềm/ }).locator('.settings-toggle')).toHaveCSS('background-color', 'rgb(207, 216, 223)')
  await capture(page, 'settings-ocean-dark')
  await page.getByRole('button', { name: '↶ Khôi phục mặc định' }).click()
  await page.getByRole('button', { name: '✓ Lưu thay đổi' }).click()
  await expect(page.getByText('Đã lưu thay đổi trong dữ liệu mẫu')).toBeVisible()

  await page.setViewportSize({ width: 390, height: 844 })
  await capture(page, 'settings-mobile')
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390)
  expect(writes).toEqual([])
})
