import { expect, test } from '@playwright/test'

test('shows the SimpleStore foundation shell', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('heading', { level: 1 })).toContainText(
    'vertical slice đầu tiên',
  )
})
