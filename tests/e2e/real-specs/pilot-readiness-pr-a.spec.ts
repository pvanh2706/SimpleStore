import { expect, test, type Browser, type Page } from '@playwright/test'

const ownerEmail = process.env.SIMPLESTORE_E2E_EMAIL
const ownerPassword = process.env.SIMPLESTORE_E2E_PASSWORD
const runId = process.env.SIMPLESTORE_E2E_RUN_ID

async function login(page: Page, email: string, password: string) {
  await page.goto('/login')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.locator('button[type="submit"]').click()
}

async function postJson(page: Page, path: string, body: unknown) {
  return await page.evaluate(async ({ requestPath, requestBody }) => {
    const tokenResponse = await fetch('/api/security/antiforgery', { credentials: 'include' })
    const { requestToken } = await tokenResponse.json()
    const response = await fetch(requestPath, {
      method: 'POST', credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': requestToken },
      body: JSON.stringify(requestBody),
    })
    let responseBody: unknown = null
    try { responseBody = await response.json() } catch { /* no body */ }
    return { status: response.status, body: responseBody }
  }, { requestPath: path, requestBody: body })
}

async function createProduct(page: Page, name: string, sku: string, openingQuantity: string, openingCost?: string) {
  await page.goto('/products/new')
  await page.locator('#name').fill(name)
  await page.locator('#unit').fill('item')
  await page.locator('#sku').fill(sku)
  await page.locator('#sale-price').fill('100')
  await page.locator('#opening-quantity').fill(openingQuantity)
  if (openingCost) await page.locator('#opening-cost').fill(openingCost)
  await page.locator('button[type="submit"]').click()
  await expect(page).toHaveURL(/\/products\/[0-9a-f-]+$/)
  return page.url().split('/').at(-1)!
}

async function assertCashierCannotAdjust(page: Page, productId: string) {
  const forbidden = await postJson(page, '/api/inventory/adjustments', {
    operationId: crypto.randomUUID(), productId, quantityDelta: -1,
    adjustmentUnitCost: null, reason: 'Cashier must be forbidden',
  })
  expect(forbidden.status).toBe(403)
}

async function loginAndChangeTemporaryPassword(
  browser: Browser,
  email: string,
  temporaryPassword: string,
  newPassword: string,
) {
  const context = await browser.newContext({ ignoreHTTPSErrors: true })
  const page = await context.newPage()
  await login(page, email, temporaryPassword)
  await expect(page).toHaveURL(/\/change-password$/)
  await expect(page.locator('nav')).toHaveCount(0)
  await page.locator('#current-password').fill(temporaryPassword)
  await page.locator('#new-password').fill(newPassword)
  await page.locator('#confirm-password').fill(newPassword)
  await page.locator('button[type="submit"]').click()
  await expect(page).toHaveURL(/\/products$/)
  return { context, page }
}

test.describe.serial('Pilot Readiness PR-A real critical flow', () => {
  test.setTimeout(180_000)

  test('production bootstrap, accounts, Adjustment and Stocktake work end to end', async ({ page, browser }) => {
    if (!ownerEmail || !ownerPassword || !runId) throw new Error('Run through run-real-pr-a.ps1.')

    await login(page, ownerEmail, ownerPassword)
    await expect(page).toHaveURL(/\/setup$/)
    await page.locator('#store-name').fill(`PR-A Store ${runId}`)
    await page.locator('button[type="submit"]').click()
    await expect(page).toHaveURL(/\/products$/)

    const reliableProductId = await createProduct(page, 'Reliable PR-A product', `PRA-R-${runId.slice(0, 8)}`, '10', '20')
    await page.locator('#adjustment-quantity').fill('2')
    await page.locator('#adjustment-reason').fill('Found stock')
    await page.locator('form').filter({ has: page.locator('#adjustment-quantity') }).locator('button[type="submit"]').click()
    await expect(page.getByText('Found stock', { exact: true })).toBeVisible()
    await expect(page.getByText('+2 item', { exact: true })).toBeVisible()

    await page.locator('#adjustment-quantity').fill('-1')
    await page.locator('#adjustment-reason').fill('Damaged stock')
    await page.locator('form').filter({ has: page.locator('#adjustment-quantity') }).locator('button[type="submit"]').click()
    await expect(page.getByText('Damaged stock', { exact: true })).toBeVisible()

    await page.getByRole('button', { name: 'Bắt đầu kiểm kho' }).click()
    await expect(page.getByText(/Số tồn lúc bắt đầu:.*11 item/)).toBeVisible()
    await page.locator('#counted-quantity').fill('9')
    await page.locator('#stocktake-note').fill('Physical count')
    await page.locator('form').filter({ has: page.locator('#counted-quantity') }).locator('button[type="submit"]').click()
    await expect(page.getByText('Physical count', { exact: true })).toBeVisible()
    await expect(page.getByText('Dự kiến 11 · Đếm 9', { exact: true })).toBeVisible()

    await page.getByRole('button', { name: 'Tải lại số tồn' }).click()
    const concurrentPage = await page.context().newPage()
    await concurrentPage.goto(`/products/${reliableProductId}`)
    await concurrentPage.locator('#adjustment-quantity').fill('1')
    await concurrentPage.locator('#adjustment-reason').fill('Concurrent movement')
    await concurrentPage.locator('form').filter({ has: concurrentPage.locator('#adjustment-quantity') }).locator('button[type="submit"]').click()
    await expect(concurrentPage.getByText('Concurrent movement', { exact: true })).toBeVisible()
    await concurrentPage.close()
    await page.locator('#counted-quantity').fill('9')
    await page.locator('form').filter({ has: page.locator('#counted-quantity') }).locator('button[type="submit"]').click()
    await expect(page.getByRole('alert')).toContainText('Tồn kho đã thay đổi trong lúc đếm')
    await page.getByRole('button', { name: 'Bắt đầu kiểm kho' }).click()
    await page.locator('#counted-quantity').fill('10')
    await page.locator('form').filter({ has: page.locator('#counted-quantity') }).locator('button[type="submit"]').click()
    await expect(page.getByText(/không tạo biến động giả/)).toBeVisible()

    const retryOperationId = crypto.randomUUID()
    const exactCommand = {
      operationId: retryOperationId, productId: reliableProductId,
      quantityDelta: -1, adjustmentUnitCost: null, reason: 'Idempotent API proof',
    }
    expect((await postJson(page, '/api/inventory/adjustments', exactCommand)).status).toBe(200)
    const exactRetry = await postJson(page, '/api/inventory/adjustments', exactCommand)
    expect(exactRetry.status).toBe(200)
    expect((exactRetry.body as { wasAlreadyCompleted: boolean }).wasAlreadyCompleted).toBe(true)
    const changedRetry = await postJson(page, '/api/inventory/adjustments', { ...exactCommand, quantityDelta: -2 })
    expect(changedRetry.status).toBe(409)
    expect((changedRetry.body as { code: string }).code).toBe('operation-intent-conflict')

    const explicitProductId = await createProduct(page, 'Unknown-cost PR-A product', `PRA-U-${runId.slice(0, 8)}`, '0')
    await page.locator('#adjustment-quantity').fill('2')
    await expect(page.locator('#adjustment-cost')).toBeVisible()
    await page.locator('#adjustment-cost').fill('30')
    await page.locator('#adjustment-reason').fill('Counted previously unknown stock')
    await page.locator('form').filter({ has: page.locator('#adjustment-quantity') }).locator('button[type="submit"]').click()
    await expect(page.getByText('Counted previously unknown stock', { exact: true })).toBeVisible()
    await expect(page.getByText('Tin cậy · 30 ₫/đv', { exact: true })).toBeVisible()

    await page.goto('/settings/users')
    const cashierEmail = `cashier-${runId}@example.test`
    await page.locator('#cashier-email').fill(cashierEmail)
    await page.locator('form').locator('button[type="submit"]').click()
    const temporaryPassword = await page.getByTestId('temporary-password').textContent()
    expect(temporaryPassword).toBeTruthy()
    const changedPassword = `Changed-${runId}!aA1`
    const cashier = await loginAndChangeTemporaryPassword(browser, cashierEmail, temporaryPassword!, changedPassword)
    await assertCashierCannotAdjust(cashier.page, explicitProductId)

    const cashierRow = page.locator('article').filter({ hasText: cashierEmail })
    page.once('dialog', dialog => dialog.accept())
    await cashierRow.getByRole('button', { name: 'Cấp lại mật khẩu' }).click()
    const resetPassword = await page.getByTestId('temporary-password').textContent()
    expect(resetPassword).toBeTruthy()
    await cashier.page.goto('/products')
    await expect(cashier.page).toHaveURL(/\/login(?:\?.*)?$/)
    await login(cashier.page, cashierEmail, changedPassword)
    await expect(cashier.page.getByRole('alert')).toContainText('Email hoặc mật khẩu không đúng')

    await login(cashier.page, cashierEmail, resetPassword!)
    await expect(cashier.page).toHaveURL(/\/change-password$/)
    page.once('dialog', dialog => dialog.accept())
    await cashierRow.getByRole('button', { name: 'Vô hiệu hóa' }).click()
    await cashier.page.goto('/products')
    await expect(cashier.page).toHaveURL(/\/login(?:\?.*)?$/)
    await login(cashier.page, cashierEmail, resetPassword!)
    await expect(cashier.page.getByRole('alert')).toContainText('Email hoặc mật khẩu không đúng')
    await cashier.context.close()
  })
})
