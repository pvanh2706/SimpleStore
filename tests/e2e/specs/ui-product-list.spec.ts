import { expect, test } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

test('product reference data previews the list and keeps edits in the browser', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  const writes: string[] = []
  page.on('request', request => {
    if (request.url().includes('/api/') && request.method() !== 'GET') writes.push(request.url())
  })
  await page.route('**/api/auth/session', route => route.fulfill({ json: {
    isAuthenticated: true, email: 'owner@example.test', storeId: 'store-1',
    roles: ['Owner'], hasStore: true, mustChangePassword: false, isEnabled: true,
  } }))
  await page.route('**/api/store/current', route => route.fulfill({ json: {
    id: 'store-1', name: 'Tạp hóa Việt Anh', mainWarehouseId: 'warehouse-1',
    mainWarehouseName: 'Kho chính', timeZoneId: 'Asia/Ho_Chi_Minh',
  } }))
  await page.route('**/api/products?**', route => route.fulfill({ json: {
    items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0,
  } }))

  await page.goto('/products')
  await expect(page.getByText('Chưa có sản phẩm phù hợp.')).toBeVisible()
  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.locator('.product-table tbody tr')).toHaveCount(8)
  await expect(page.getByText('Coca Cola 330ml')).toBeVisible()
  await expect(page.getByText('Hiển thị 1 – 8 / 8 sản phẩm')).toBeVisible()
  if (process.env.PRODUCT_CAPTURE_DIR) {
    await mkdir(process.env.PRODUCT_CAPTURE_DIR, { recursive: true })
    await page.screenshot({ path: join(process.env.PRODUCT_CAPTURE_DIR, 'product-list.png') })
  }
  await page.getByRole('button', { name: 'Gia vị (18)' }).click()
  await expect(page.locator('.product-table tbody tr')).toHaveCount(2)
  await page.getByRole('button', { name: 'Tất cả (156)' }).click()
  await page.getByRole('button', { name: 'Bộ lọc' }).click()
  if (process.env.PRODUCT_CAPTURE_DIR) await page.screenshot({ path: join(process.env.PRODUCT_CAPTURE_DIR, 'product-filter.png') })
  await page.getByRole('dialog', { name: 'Bộ lọc sản phẩm' }).getByRole('radio', { name: 'Tồn kho thấp (≤ 8)' }).check()
  await page.getByRole('dialog', { name: 'Bộ lọc sản phẩm' }).getByRole('button', { name: 'Áp dụng' }).click()
  await expect(page.locator('.product-table tbody tr')).toHaveCount(2)
  await page.getByRole('button', { name: 'Bộ lọc' }).click()
  await page.getByRole('dialog', { name: 'Bộ lọc sản phẩm' }).getByRole('button', { name: 'Xóa bộ lọc' }).click()
  await page.getByRole('button', { name: 'Thao tác Trà xanh C2 500ml' }).click()
  await page.getByRole('button', { name: 'Điều chỉnh tồn kho' }).click()
  await page.getByRole('dialog', { name: 'Điều chỉnh tồn kho' }).getByRole('spinbutton').fill('2')
  await page.getByRole('dialog', { name: 'Điều chỉnh tồn kho' }).getByRole('combobox', { name: 'Lý do điều chỉnh' }).selectOption('Cân chỉnh sổ sách')
  await page.getByRole('dialog', { name: 'Điều chỉnh tồn kho' }).getByRole('button', { name: 'Xác nhận' }).click()
  await expect(page.locator('.product-table tbody tr').filter({ hasText: 'Trà xanh C2 500ml' })).toContainText('10')
  await page.getByRole('button', { name: 'Thao tác Trà xanh C2 500ml' }).click()
  await page.getByRole('button', { name: 'Lịch sử tồn kho' }).click()
  await expect(page.getByRole('dialog', { name: 'Lịch sử tồn kho' }).getByText('#HD000123')).toBeVisible()
  await page.getByRole('dialog', { name: 'Lịch sử tồn kho' }).locator('.product-modal-foot button').click()
  await page.getByRole('button', { name: 'Thao tác Trà xanh C2 500ml' }).click()
  await page.getByRole('button', { name: 'Kiểm kho' }).click()
  const stocktake = page.getByRole('dialog', { name: 'Kiểm kho' })
  await stocktake.getByRole('spinbutton').fill('9')
  await stocktake.getByRole('button', { name: 'Tiếp theo' }).click()
  await expect(stocktake.getByText('Số thực đếm: 9')).toBeVisible()
  await stocktake.getByRole('button', { name: 'Tiếp theo' }).click()
  await stocktake.getByRole('button', { name: 'Xác nhận' }).click()
  await expect(page.locator('.product-table tbody tr').filter({ hasText: 'Trà xanh C2 500ml' })).toContainText('9')
  await page.getByRole('button', { name: 'Thêm sản phẩm' }).click()
  await page.getByPlaceholder('Nhập tên sản phẩm').fill('Sản phẩm thử')
  await page.getByPlaceholder('Nhập mã SKU').fill('SP-TEST')
  await page.getByRole('dialog', { name: 'Thêm sản phẩm' }).getByRole('button', { name: 'Lưu sản phẩm' }).click()
  await expect(page.locator('.product-table tbody tr')).toHaveCount(9)
  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.getByText('Chưa có sản phẩm phù hợp.')).toBeVisible()
  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.locator('.product-table tbody tr')).toHaveCount(8)
  expect(writes).toEqual([])
})

test('live products use the same drawers and modals while saving through the API', async ({ page }) => {
  const id = '11111111-1111-4111-8111-111111111111'
  const createdId = '22222222-2222-4222-8222-222222222222'
  const writes: string[] = []
  const product = {
    id, sku: 'SP-LIVE', barcode: '8935049500017', name: 'Coca Cola 330ml', unit: 'Lon',
    salePrice: 10000, referencePurchaseCost: 7500, isActive: true, quantityOnHand: 24,
    inventoryValue: 180000, averageCost: 7500, hasAverageCost: true,
    referencePurchaseCostRevision: 1, createdAt: '2024-12-01T00:00:00Z', updatedAt: '2024-12-16T10:23:00Z',
  }
  const items = [product]
  await page.route('**/api/auth/session', route => route.fulfill({ json: {
    isAuthenticated: true, email: 'owner@example.test', storeId: 'store-1',
    roles: ['Owner'], hasStore: true, mustChangePassword: false, isEnabled: true,
  } }))
  await page.route('**/api/store/current', route => route.fulfill({ json: {
    id: 'store-1', name: 'Tạp hóa Việt Anh', mainWarehouseId: 'warehouse-1',
    mainWarehouseName: 'Kho chính', timeZoneId: 'Asia/Ho_Chi_Minh',
  } }))
  await page.route('**/api/security/antiforgery', route => route.fulfill({ json: { requestToken: 'test-token' } }))
  await page.route('**/api/products?**', route => route.fulfill({ json: {
    items, page: 1, pageSize: 10, totalCount: items.length, totalPages: 1,
  } }))
  await page.route('**/api/products', route => {
    writes.push('create')
    const input = route.request().postDataJSON()
    items.push({ ...product, ...input, id: createdId, quantityOnHand: input.openingQuantity })
    return route.fulfill({ status: 201, json: items[1] })
  })
  await page.route(`**/api/products/${id}`, route => {
    if (route.request().method() === 'PUT') {
      writes.push('edit')
      Object.assign(product, route.request().postDataJSON())
    }
    return route.fulfill({ json: product })
  })
  await page.route(`**/api/products/${id}/movements`, route => route.fulfill({ json: [{
    id: 'movement-1', type: 'Sale', quantityDelta: -3, unitCost: 7500,
    inventoryValueDelta: -22500, sourceType: 'Sale', sourceId: id,
    performedByUserId: id, occurredAt: '2024-12-16T10:23:00Z', costReliability: 'Reliable',
    reason: 'Đơn hàng thật', stocktakeExpectedQuantity: null, stocktakeCountedQuantity: null,
  }] }))
  await page.route('**/api/inventory/adjustments', route => {
    writes.push('adjust')
    const input = route.request().postDataJSON()
    product.quantityOnHand += input.quantityDelta
    return route.fulfill({ json: { quantityAfter: product.quantityOnHand } })
  })
  await page.route(`**/api/inventory/stocktakes/context/${id}`, route => route.fulfill({ json: {
    productId: id, productName: product.name, productSku: product.sku, unit: product.unit,
    expectedQuantity: product.quantityOnHand, expectedRevision: 'revision-1',
    hasAverageCost: true, averageCost: 7500,
  } }))
  await page.route('**/api/inventory/stocktakes', route => {
    writes.push('stocktake')
    const input = route.request().postDataJSON()
    product.quantityOnHand = input.countedQuantity
    return route.fulfill({ json: { countedQuantity: input.countedQuantity } })
  })

  await page.goto('/products')
  await expect(page.getByRole('button', { name: 'Coca Cola 330ml', exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Thêm sản phẩm' }).click()
  const create = page.getByRole('dialog', { name: 'Thêm sản phẩm' })
  await expect(create).toBeVisible()
  await expect(page).toHaveURL(/\/products$/)
  await create.getByPlaceholder('Nhập tên sản phẩm').fill('Pepsi 330ml')
  await create.getByPlaceholder('Nhập mã SKU').fill('SP-NEW')
  await create.getByRole('button', { name: 'Lưu sản phẩm' }).click()
  await expect(page.getByRole('button', { name: 'Pepsi 330ml', exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Thao tác Coca Cola 330ml' }).click()
  await page.getByRole('button', { name: 'Sửa', exact: true }).click()
  const edit = page.getByRole('dialog', { name: 'Sửa sản phẩm' })
  await expect(edit).toBeVisible()
  await edit.getByRole('spinbutton', { name: 'Giá bán (đ)' }).fill('12000')
  await edit.getByRole('button', { name: 'Lưu sản phẩm' }).click()
  await expect(page.locator('.product-table tbody tr').filter({ hasText: 'Coca Cola 330ml' })).toContainText('12.000 đ')
  await page.getByRole('button', { name: 'Coca Cola 330ml', exact: true }).click()
  const detail = page.getByRole('dialog', { name: 'Chi tiết sản phẩm' })
  await expect(detail).toBeVisible()
  await detail.getByRole('button', { name: 'Lịch sử tồn kho' }).click()
  const history = page.getByRole('dialog', { name: 'Lịch sử tồn kho' })
  await expect(history.getByText('Đơn hàng thật')).toBeVisible()
  await history.locator('.product-modal-foot button').click()
  await detail.getByRole('button', { name: 'Điều chỉnh tồn kho' }).click()
  const adjust = page.getByRole('dialog', { name: 'Điều chỉnh tồn kho' })
  await adjust.getByRole('spinbutton', { name: 'Số lượng điều chỉnh' }).fill('2')
  await adjust.getByRole('combobox', { name: 'Lý do điều chỉnh' }).selectOption('Cân chỉnh sổ sách')
  await adjust.getByRole('button', { name: 'Xác nhận' }).click()
  await expect(page.locator('.product-table tbody tr').filter({ hasText: 'Coca Cola 330ml' })).toContainText('26')
  await detail.getByRole('button', { name: 'Kiểm kho' }).click()
  const stocktake = page.getByRole('dialog', { name: 'Kiểm kho' })
  await stocktake.getByRole('spinbutton', { name: 'Số lượng thực đếm' }).fill('25')
  await stocktake.getByRole('button', { name: 'Tiếp theo' }).click()
  await stocktake.getByRole('button', { name: 'Tiếp theo' }).click()
  await stocktake.getByRole('button', { name: 'Xác nhận' }).click()
  await expect(page.locator('.product-table tbody tr').filter({ hasText: 'Coca Cola 330ml' })).toContainText('25')
  await expect(page).toHaveURL(/\/products$/)
  expect(writes).toEqual(['create', 'edit', 'adjust', 'stocktake'])
})
