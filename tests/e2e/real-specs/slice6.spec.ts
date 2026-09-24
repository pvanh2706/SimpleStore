import { execFileSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { expect, test, type Page } from '@playwright/test'

const email = process.env.SIMPLESTORE_E2E_EMAIL
const password = process.env.SIMPLESTORE_E2E_PASSWORD
const runId = process.env.SIMPLESTORE_E2E_RUN_ID
const fixtureProject = fileURLToPath(new URL(
  '../SimpleStore.E2E.Fixture/SimpleStore.E2E.Fixture.csproj',
  import.meta.url,
))

interface FixtureSnapshot {
  todayOpened: number
  signalShown: number
  whyOpened: number
  purchaseDraftStarted: number
  purchaseCount: number
}

interface TodaySemanticsSeedResult {
  customerSaleId: string
  customerDebtPaymentId: string
  customerReturnId: string
  fullReturnSaleId: string
  fullReturnId: string
  sameDayVoidSaleId: string
  sameDayVoidId: string
  crossDaySaleId: string
  crossDayVoidId: string
  supplierPurchaseId: string
  supplierDebtPaymentId: string
}

function runFixture(command: 'seed' | 'seed-today-semantics' | 'snapshot') {
  const output = execFileSync('dotnet', [
    'run', '--project', fixtureProject, '--configuration', 'Release', '--no-build', '--', command,
  ], { encoding: 'utf8', env: process.env })
  return JSON.parse(output.trim().split(/\r?\n/).at(-1)!)
}

function snapshot(): FixtureSnapshot {
  return runFixture('snapshot') as FixtureSnapshot
}

async function login(page: Page) {
  if (!email || !password || !runId) throw new Error('Run through run-real-slice6.ps1.')
  await page.goto('/login')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.locator('button[type="submit"]').click()
  await expect(page).toHaveURL(/\/(setup|products|today)$/)
  if (page.url().endsWith('/setup')) {
    await page.locator('#store-name').fill(`Cửa hàng Slice 6 ${runId}`)
    await page.locator('button[type="submit"]').click()
    await expect(page).toHaveURL(/\/products$/)
  }
}

async function openMetricExplanation(page: Page, metricLabel: string) {
  await page.getByText(metricLabel, { exact: true })
    .first()
    .locator('..')
    .getByRole('button', { name: 'Vì sao?' })
    .click()
  await expect(page.getByText(/^Dữ liệu nguồn ·/)).toBeVisible()
}

test.describe.serial('Slice 6B real C14 attention and measurement flow', () => {
  test.setTimeout(120_000)

  test('Owner understands attention evidence and starts an explicit purchase flow', async ({ page }) => {
    await login(page)

    await page.goto('/today')
    await expect(page.getByRole('heading', { name: 'Hôm nay cửa hàng thế nào?' })).toBeVisible()
    await expect(page.getByText('Doanh thu hôm nay', { exact: true })).toBeVisible()
    await expect(page.locator('input[type="date"]')).toHaveCount(0)
    await expect(page.getByText(/Chưa đủ 7 ngày lịch sử/)).toBeVisible()
    await expect.poll(() => snapshot().todayOpened).toBe(1)

    runFixture('seed')
    await page.reload()
    await expect(page.getByRole('heading', { name: 'Hôm nay cửa hàng thế nào?' })).toBeVisible()
    const attention = page.getByRole('heading', { name: 'Cần chú ý' }).locator('..').locator('..')
    await attention.scrollIntoViewIfNeeded()
    await expect(page.getByTestId('attention-preview-item')).toHaveCount(3)
    await expect(page.getByText('Xem tất cả 5 mặt hàng')).toBeVisible()
    await expect(page.getByText('Hidden inactive signal')).toHaveCount(0)
    let exposed = 0
    for (const item of await page.getByTestId('attention-preview-item').all()) {
      await item.evaluate(element => element.scrollIntoView({ block: 'center' }))
      exposed += 1
      await expect.poll(() => snapshot().signalShown).toBeGreaterThanOrEqual(exposed)
    }
    await expect.poll(() => snapshot()).toMatchObject({ todayOpened: 2, signalShown: 3 })

    await page.getByText('Doanh thu hôm nay', { exact: true }).locator('..').getByRole('button', { name: 'Vì sao?' }).click()
    await expect(page.getByText(/^Dữ liệu nguồn ·/)).toBeVisible()
    await page.getByRole('button', { name: 'Đóng' }).click()
    await expect.poll(() => snapshot()).toMatchObject({ todayOpened: 2, signalShown: 3 })

    await page.getByText('Xem tất cả 5 mặt hàng').click()
    await expect(page).toHaveURL(/\/today\/attention$/)
    await expect(page.getByText('5 mặt hàng', { exact: false }).first()).toBeVisible()
    await expect(page.getByText('Hidden inactive signal')).toHaveCount(0)
    const riskItem = page.locator('li').filter({ hasText: 'Alpha risk with evidence' })
    await expect(riskItem).toContainText('Có nguy cơ sắp hết hàng')
    await riskItem.getByRole('button', { name: 'Xem vì sao' }).click()

    await expect(page.getByRole('heading', { name: 'Alpha risk with evidence' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Công thức' })).toBeVisible()
    await expect(page.getByRole('heading', { name: '7 ngày kinh doanh đã hoàn tất' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Dữ liệu nguồn' })).toBeVisible()
    await expect(page.getByText(/10 bán − 1 trả hàng − 2 hủy = 7 lượng bán thuần/)).toBeVisible()
    await expect(page.getByText(/^Sale · \+10$/)).toBeVisible()
    await expect(page.getByText(/^Return · -1$/)).toBeVisible()
    await expect(page.getByText(/^SaleVoid · -2$/)).toBeVisible()
    await expect.poll(() => snapshot().whyOpened).toBe(1)

    await page.getByRole('link', { name: 'Xem sản phẩm' }).click()
    await expect(page).toHaveURL(/\/products\/[0-9a-f-]+$/)
    await expect(page.getByRole('heading', { name: 'Alpha risk with evidence' })).toBeVisible()
    await page.goBack()
    await expect(page.getByRole('heading', { name: 'Alpha risk with evidence' })).toBeVisible()

    await page.getByRole('button', { name: 'Tạo phiếu nhập' }).click()
    await expect(page).toHaveURL(/\/purchases\/new\?productId=[0-9a-f-]+$/)
    await expect(page.getByRole('heading', { name: 'Tạo phiếu nhập' })).toBeVisible()
    await expect(page.getByText('Chưa chọn nhà cung cấp.')).toBeVisible()
    const preselectedRow = page.getByRole('row').filter({ hasText: 'Alpha risk with evidence' })
    await expect(preselectedRow).toBeVisible()
    await expect(preselectedRow.getByLabel('Số lượng')).toHaveValue('')
    await expect(preselectedRow.getByLabel('Giá nhập')).toHaveValue('')

    await expect.poll(() => snapshot()).toMatchObject({
      todayOpened: 2,
      signalShown: 3,
      whyOpened: 1,
      purchaseDraftStarted: 1,
      purchaseCount: 0,
    })
  })

  test('Owner sees D-069 and D-070 Today semantics through cards and explanations', async ({ page }) => {
    await login(page)
    const seeded = runFixture('seed-today-semantics') as TodaySemanticsSeedResult

    await page.goto('/today')
    await expect(page.getByRole('heading', { name: 'Hôm nay cửa hàng thế nào?' })).toBeVisible()

    const customerDebtCard = page.getByText('Công nợ khách mới phát sinh', { exact: true })
      .first()
      .locator('..')
    const supplierDebtCard = page.getByText('Công nợ nhà cung cấp mới phát sinh', { exact: true })
      .first()
      .locator('..')
    const saleCountCard = page.getByText('Số đơn bán', { exact: true })
      .first()
      .locator('..')
    await expect(customerDebtCard).toContainText('700.000 ₫')
    await expect(supplierDebtCard).toContainText('300.000 ₫')
    await expect(saleCountCard).toContainText('2')

    await openMetricExplanation(page, 'Công nợ khách mới phát sinh')
    const customerEvidence = page.locator('li').filter({ hasText: seeded.customerSaleId })
    await expect(customerEvidence).toContainText('Tổng giao dịch: 1.000.000 ₫')
    await expect(customerEvidence).toContainText('Thanh toán trực tiếp: 0 ₫')
    await expect(customerEvidence).toContainText('Nghĩa vụ cơ sở: 1.000.000 ₫')
    await expect(customerEvidence).toContainText('Return cùng ngày: 300.000 ₫')
    await expect(customerEvidence).toContainText('Đóng góp cuối: 700.000 ₫')
    await expect(page.getByText(seeded.customerDebtPaymentId)).toHaveCount(0)
    await page.getByRole('button', { name: 'Đóng' }).click()

    await openMetricExplanation(page, 'Số đơn bán')
    await expect(page.getByText('Dữ liệu nguồn · 3 mục')).toBeVisible()
    const partialReturnCount = page.locator('li').filter({ hasText: seeded.customerSaleId })
    const fullReturnCount = page.locator('li').filter({ hasText: seeded.fullReturnSaleId })
    const sameDayVoidCount = page.locator('li').filter({ hasText: seeded.sameDayVoidSaleId })
    await expect(partialReturnCount).toContainText('Đơn bán được tính')
    await expect(partialReturnCount.getByText('1', { exact: true })).toBeVisible()
    await expect(fullReturnCount).toContainText('Đơn bán được tính')
    await expect(fullReturnCount.getByText('1', { exact: true })).toBeVisible()
    await expect(sameDayVoidCount).toContainText('Đơn bán bị hủy cùng ngày')
    await expect(sameDayVoidCount.getByText('0', { exact: true })).toBeVisible()
    await expect(page.getByText(seeded.crossDaySaleId)).toHaveCount(0)
    await page.getByRole('button', { name: 'Đóng' }).click()

    await openMetricExplanation(page, 'Công nợ nhà cung cấp mới phát sinh')
    const supplierEvidence = page.locator('li').filter({ hasText: seeded.supplierPurchaseId })
    await expect(supplierEvidence).toContainText('Tổng giao dịch: 500.000 ₫')
    await expect(supplierEvidence).toContainText('Thanh toán trực tiếp: 200.000 ₫')
    await expect(supplierEvidence).toContainText('Nghĩa vụ cơ sở: 300.000 ₫')
    await expect(supplierEvidence).toContainText('Đóng góp cuối: 300.000 ₫')
    await expect(page.getByText(seeded.supplierDebtPaymentId)).toHaveCount(0)
  })
})
