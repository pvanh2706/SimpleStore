import { expect, test } from '@playwright/test'

test('redirects an unauthenticated user to login', async ({ page }) => {
  await page.route('**/api/auth/session', (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ isAuthenticated: false, email: null, storeId: null, roles: [], hasStore: false }),
  }))
  await page.goto('/products')

  await expect(page).toHaveURL(/\/login/)
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Đăng nhập cửa hàng')
})
