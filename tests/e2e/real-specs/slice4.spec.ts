import { expect, test, type Page } from '@playwright/test'

const email = process.env.SIMPLESTORE_E2E_EMAIL
const password = process.env.SIMPLESTORE_E2E_PASSWORD
const runId = process.env.SIMPLESTORE_E2E_RUN_ID

async function login(page: Page) {
  if (!email || !password || !runId) throw new Error('Run through run-real-slice4.ps1.')
  await page.goto('/login')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/(setup|products)$/)
  if (page.url().endsWith('/setup')) {
    await page.locator('#store-name').fill(`Cửa hàng Slice 4 ${runId}`)
    await page.getByRole('button', { name: 'Bắt đầu quản lý sản phẩm' }).click()
  }
}

async function createProduct(page: Page, name: string, sku: string, openingQuantity: string) {
  await page.getByRole('link', { name: 'Sản phẩm', exact: true }).click()
  await page.getByRole('link', { name: 'Thêm sản phẩm' }).click()
  await page.locator('#name').fill(name)
  await page.locator('#unit').fill('gói')
  await page.locator('#sku').fill(sku)
  await page.locator('#sale-price').fill('12000')
  await page.locator('#opening-quantity').fill(openingQuantity)
  await page.locator('#opening-cost').fill('8000')
  await page.getByRole('button', { name: 'Lưu sản phẩm' }).click()
  await expect(page).toHaveURL(/\/products\/[0-9a-f-]+$/)
}

async function completeSale(page: Page, productName: string, quantity: string) {
  await page.getByRole('link', { name: 'Bán hàng' }).first().click()
  await page.getByLabel('Tìm hoặc quét sản phẩm').fill(productName)
  await page.getByRole('button', { name: 'Tìm sản phẩm' }).click()
  await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
  await page.getByLabel(`Số lượng ${productName}`).fill(quantity)
  await page.getByLabel('Số tiền thanh toán').fill(String(Number(quantity) * 12000))
  await page.getByRole('button', { name: 'Thêm thanh toán' }).click()
  await page.getByRole('button', { name: 'Hoàn tất bán hàng' }).click()
  await expect(page.getByText('Đơn bán đã hoàn tất. Việc in không thay đổi trạng thái giao dịch.')).toBeVisible()
  await page.getByRole('link', { name: 'Đơn bán', exact: true }).click()
  await page.getByRole('row').nth(1).getByRole('link').click()
  await expect(page).toHaveURL(/\/sales\/[0-9a-f-]+$/)
}

async function openProduct(page: Page, productName: string) {
  await page.getByRole('link', { name: 'Sản phẩm', exact: true }).click()
  await page.getByLabel('Tìm sản phẩm').fill(productName)
  await page.getByRole('button', { name: 'Tìm kiếm' }).click()
  await page.getByRole('link', { name: productName }).click()
}

test.describe.serial('Slice 4 real correction flows', () => {
  test('Owner completes two Returns with Restock/NoRestock and authoritative preview', async ({ page }) => {
    await login(page)
    const productName = `Return E2E ${runId}`
    await createProduct(page, productName, `S4-R-${runId!.slice(-8)}`, '5')
    await completeSale(page, productName, '2')

    await page.getByRole('link', { name: 'Trả hàng' }).click()
    await page.getByLabel(`Chọn trả ${productName}`).check()
    await page.getByLabel(`Số lượng trả ${productName}`).fill('1')
    await page.getByLabel('Nhập lại kho', { exact: true }).check()
    await page.getByRole('button', { name: 'Cập nhật xem trước' }).click()
    await expect(page.getByRole('region', { name: 'Xem trước trả hàng' })).toContainText('12.000 ₫')
    await page.getByLabel('Phương thức hoàn tiền').selectOption('Cash')
    await page.getByRole('button', { name: 'Hoàn tất trả hàng' }).click()
    await expect(page).toHaveURL(/\/sales\/[0-9a-f-]+$/)
    await expect(page.getByText('Lịch sử trả hàng')).toBeVisible()
    await expect(page.getByText('Đã trả hàng').locator('..')).toContainText('12.000 ₫')
    await expect(page.getByText('Đã hoàn', { exact: true }).locator('..')).toContainText('12.000 ₫')
    const saleUrl = page.url()

    await openProduct(page, productName)
    await expect(page.getByText('4 gói', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('Trả hàng nhập lại kho', { exact: true })).toHaveCount(1)
    await page.goto(saleUrl)

    await page.getByRole('link', { name: 'Trả hàng' }).click()
    await page.getByLabel(`Chọn trả ${productName}`).check()
    await page.getByLabel(`Số lượng trả ${productName}`).fill('2')
    await page.getByLabel('Không nhập lại kho', { exact: true }).check()
    await page.getByRole('button', { name: 'Cập nhật xem trước' }).click()
    await expect(page.getByRole('alert')).toContainText('không vượt quá 1')
    await page.getByLabel(`Số lượng trả ${productName}`).fill('1')
    await page.getByRole('button', { name: 'Cập nhật xem trước' }).click()
    await page.getByLabel('Phương thức hoàn tiền').selectOption('Transfer')
    await page.getByRole('button', { name: 'Hoàn tất trả hàng' }).click()
    await expect(page).toHaveURL(/\/sales\/[0-9a-f-]+$/)
    await expect(page.getByRole('link', { name: 'Trả hàng' })).toHaveCount(0)

    await openProduct(page, productName)
    await expect(page.getByText('4 gói', { exact: true }).first()).toBeVisible()
  })

  test('Owner voids a Sale, keeps its history and restores inventory exactly once', async ({ page }) => {
    await login(page)
    const productName = `Sale Void E2E ${runId}`
    await createProduct(page, productName, `S4-SV-${runId!.slice(-8)}`, '5')
    await completeSale(page, productName, '1')

    await page.getByRole('button', { name: 'Hủy giao dịch', exact: true }).click()
    const panel = page.getByRole('region', { name: 'Xác nhận hủy giao dịch' })
    await panel.getByLabel('Lý do hủy').fill('Thu ngân nhập nhầm giao dịch')
    await panel.getByRole('button', { name: 'Hủy giao dịch' }).click()
    await expect(page.getByText('Đã hủy', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('Thu ngân nhập nhầm giao dịch')).toBeVisible()
    await expect(page.getByRole('region', { name: 'Hóa đơn bán hàng' })).toContainText(productName)
    await page.reload()
    await expect(page.getByText('Đã hủy', { exact: true }).first()).toBeVisible()

    await openProduct(page, productName)
    await expect(page.getByText('5 gói', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('Hủy đơn bán', { exact: true })).toHaveCount(1)
  })

  test('Owner safely voids a Purchase and inventory returns to its exact previous state', async ({ page }) => {
    await login(page)
    const productName = `Purchase Void E2E ${runId}`
    const supplierName = `Supplier Void E2E ${runId}`
    await createProduct(page, productName, `S4-PV-${runId!.slice(-8)}`, '2')

    await page.getByRole('link', { name: 'Nhà cung cấp' }).click()
    const supplierForm = page.locator('form').first()
    await supplierForm.locator('input').nth(0).fill(supplierName)
    await supplierForm.getByRole('button', { name: 'Lưu' }).click()
    await expect(page.getByRole('row').filter({ hasText: supplierName })).toBeVisible()

    await page.getByRole('link', { name: 'Nhập hàng' }).click()
    await page.getByRole('link', { name: 'Tạo phiếu nhập' }).click()
    await page.getByLabel('Tìm nhà cung cấp').fill(supplierName)
    await page.getByRole('button', { name: 'Tìm nhà cung cấp' }).click()
    await page.getByRole('button', { name: `Chọn nhà cung cấp ${supplierName}` }).click()
    await page.getByLabel('Tìm sản phẩm').fill(productName)
    await page.getByRole('button', { name: 'Tìm sản phẩm' }).click()
    await page.getByRole('button', { name: `Thêm sản phẩm ${productName}` }).click()
    await page.getByLabel('Số lượng').fill('3')
    await page.getByLabel('Giá nhập').fill('9000')
    await page.getByRole('button', { name: 'Lưu nháp' }).click()
    await page.getByRole('button', { name: 'Hoàn tất phiếu nhập' }).click()
    await expect(page.getByText('Đã hoàn tất', { exact: true })).toBeVisible()

    await page.getByRole('button', { name: 'Hủy phiếu nhập', exact: true }).click()
    const panel = page.getByRole('region', { name: 'Xác nhận hủy phiếu nhập' })
    await panel.getByLabel('Lý do hủy').fill('Phiếu nhập tạo nhầm')
    await panel.getByRole('button', { name: 'Hủy phiếu nhập' }).click()
    await expect(page.getByText('Đã hủy', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('Phiếu nhập tạo nhầm')).toBeVisible()

    await openProduct(page, productName)
    await expect(page.getByText('2 gói', { exact: true }).first()).toBeVisible()
    await expect(page.getByText('Hủy phiếu nhập', { exact: true })).toHaveCount(1)
  })
})
