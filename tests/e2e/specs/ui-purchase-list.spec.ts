import { expect, test, type Page } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'

async function capture(page: Page, name: string) {
  if (!process.env.PURCHASE_CAPTURE_DIR) return
  await mkdir(process.env.PURCHASE_CAPTURE_DIR, { recursive: true })
  await page.screenshot({ path: join(process.env.PURCHASE_CAPTURE_DIR, `${name}.png`) })
}

test('purchase reference data previews every flow without writing to the API', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 })
  const purchaseCalls: string[] = []
  page.on('request', request => {
    if (request.url().includes('/api/purchases') && !request.url().includes('pageSize=1')) purchaseCalls.push(`${request.method()} ${request.url()}`)
  })
  await page.route('**/api/auth/session', route => route.fulfill({ json: {
    isAuthenticated: true, email: 'owner@example.test', storeId: 'store-1',
    roles: ['Owner'], hasStore: true, mustChangePassword: false, isEnabled: true,
  } }))
  await page.route('**/api/store/current', route => route.fulfill({ json: {
    id: 'store-1', name: 'Tạp hóa Việt Anh', mainWarehouseId: 'warehouse-1',
    mainWarehouseName: 'Kho chính', timeZoneId: 'Asia/Ho_Chi_Minh',
  } }))
  await page.route('**/api/purchases?**', route => route.fulfill({ json: {
    items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0,
  } }))

  await page.goto('/purchases')
  await expect(page.getByText('Chưa có phiếu nhập nào')).toBeVisible()
  const liveCalls = purchaseCalls.length
  await page.getByRole('button', { name: 'Dữ liệu mẫu' }).click()
  await expect(page.locator('.purchase-table tbody tr')).toHaveCount(5)
  await expect(page.getByText('Hiển thị 1 - 5 / 56 phiếu nhập')).toBeVisible()
  await capture(page, 'purchase-list')

  await page.getByRole('button', { name: 'Bộ lọc' }).click()
  await capture(page, 'purchase-filter')
  const filter = page.getByRole('dialog', { name: 'Bộ lọc phiếu nhập' })
  await filter.getByRole('checkbox', { name: 'Chưa thanh toán' }).check()
  await filter.getByRole('button', { name: 'Áp dụng' }).click()
  await expect(page.locator('.purchase-table tbody tr')).toHaveCount(2)
  await page.getByRole('button', { name: 'Bộ lọc' }).click()
  await filter.getByRole('button', { name: 'Xóa bộ lọc' }).click()
  await expect(page.locator('.purchase-table tbody tr')).toHaveCount(5)

  await page.getByRole('button', { name: 'PN000043' }).click()
  const detail = page.getByRole('dialog', { name: 'Chi tiết phiếu nhập' })
  await expect(detail.getByText('2.200.000 đ')).toBeVisible()
  await capture(page, 'purchase-detail')
  await detail.getByRole('tab', { name: 'Lịch sử (3)' }).click()
  await expect(detail.getByText('Ghi nhận thanh toán')).toBeVisible()
  await detail.getByRole('button', { name: 'Đóng chi tiết' }).click()

  await page.getByRole('button', { name: 'Tạo phiếu nhập' }).click()
  const wizard = page.getByRole('dialog', { name: 'Tạo phiếu nhập hàng' })
  await capture(page, 'purchase-wizard-1')
  await wizard.getByRole('button', { name: 'Tiếp theo →' }).click()
  await wizard.getByRole('textbox', { name: 'Tìm sản phẩm' }).fill('dầu')
  await wizard.getByRole('button', { name: /Dầu ăn Tường An 1L/ }).click()
  await expect(wizard.getByText('Tổng tiền hàng: 1.194.000 đ')).toBeVisible()
  await wizard.getByRole('button', { name: 'Xóa Dầu ăn Tường An 1L' }).click()
  await capture(page, 'purchase-wizard-2')
  await wizard.getByRole('button', { name: 'Tiếp theo →' }).click()
  await capture(page, 'purchase-wizard-3')
  await wizard.getByRole('button', { name: 'Tiếp theo →' }).click()
  await capture(page, 'purchase-wizard-4')
  await wizard.getByRole('button', { name: 'Tạo phiếu nhập' }).click()
  await expect(page.locator('.purchase-table tbody tr').first()).toContainText('PN000046')
  await expect(page.getByRole('button', { name: 'Tất cả (57)' })).toBeVisible()

  await detail.getByRole('button', { name: 'Thao tác phiếu nhập' }).click()
  await page.getByRole('button', { name: 'Sửa phiếu nhập' }).click()
  await capture(page, 'purchase-edit')
  await page.getByRole('dialog', { name: 'Sửa phiếu nhập' }).getByRole('button', { name: 'Lưu thay đổi' }).click()

  await page.getByRole('button', { name: 'PN000044' }).click()
  await detail.getByRole('button', { name: 'Thao tác phiếu nhập' }).click()
  await page.getByRole('button', { name: 'Hủy phiếu nhập' }).click()
  const cancel = page.getByRole('dialog', { name: 'Hủy phiếu nhập' })
  await cancel.getByRole('textbox').fill('Nhập trùng phiếu')
  await capture(page, 'purchase-cancel')
  await cancel.getByRole('button', { name: 'Hủy phiếu nhập' }).click()
  await expect(page.locator('.purchase-table tbody tr').filter({ hasText: 'PN000044' })).toContainText('Đã hủy')

  for (const [state, title] of [['empty', 'Chưa có phiếu nhập nào'], ['error', 'Không thể tải dữ liệu']] as const) {
    await page.getByRole('button', { name: 'Demo trạng thái ▴' }).click()
    await page.getByRole('button', { name: state === 'empty' ? 'Trạng thái rỗng' : 'Lỗi tải dữ liệu' }).click()
    await expect(page.getByText(title)).toBeVisible()
    await capture(page, `purchase-state-${state}`)
  }

  expect(purchaseCalls).toHaveLength(liveCalls)
})
