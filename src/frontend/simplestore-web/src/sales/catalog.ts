import type { ProductListItem } from '../api/types'

export const productCategories = ['Tất cả', 'Đồ uống', 'Bánh kẹo', 'Gia vị', 'Sữa và TP từ sữa', 'Đồ gia dụng', 'Khác'] as const
export type ProductCategory = typeof productCategories[number]

/**
 * MOCK until a Category master exists in the backend: groups by product-name keywords.
 * Order matters, e.g. "Nước mắm" is Gia vị before "nước" matches Đồ uống.
 */
const keywordGroups: ReadonlyArray<[ProductCategory, readonly string[]]> = [
  ['Sữa và TP từ sữa', ['sữa', 'milk', 'yaourt', 'phô mai']],
  ['Gia vị', ['mắm', 'đường', 'hạt nêm', 'muối', 'dầu ăn', 'bột ngọt', 'nước tương', 'tương ớt', 'tiêu', 'knorr']],
  ['Đồ gia dụng', ['giấy', 'xà phòng', 'nước rửa', 'bột giặt', 'khăn', 'bàn chải']],
  ['Bánh kẹo', ['bánh', 'kẹo', 'oreo', 'snack', 'socola', 'chocolate']],
  ['Đồ uống', ['nước', 'coca', 'pepsi', 'trà', 'bia', 'cà phê', 'cafe', 'c2', 'aquafina', 'sting', '7up']],
]

export function productCategory(product: ProductListItem): ProductCategory {
  const name = product.name.toLocaleLowerCase('vi-VN')
  return keywordGroups.find(([, words]) => words.some(word => name.includes(word)))?.[0] ?? 'Khác'
}

export type StockTone = 'warn' | 'danger'

export function stockStatus(quantityOnHand: number): { label: string; tone: StockTone } | null {
  if (quantityOnHand <= 0) return { label: 'Hết hàng', tone: 'danger' }
  if (quantityOnHand <= 2) return { label: 'Rất ít hàng', tone: 'danger' }
  if (quantityOnHand <= 5) return { label: 'Sắp hết hàng', tone: 'warn' }
  return null
}
