import { ref } from 'vue'
import type { ProductListItem } from '../api/types'
import type { ProductCategory } from '../sales/catalog'

/** Preview data stays in memory and is never sent to the product API. */
export const productDemoEnabled = ref(false)

export interface ProductPreview extends ProductListItem {
  category: ProductCategory
  updatedAt: string
  image: string
  referenceCost: number
}

const image = (id: number) => `/product-demo/${id}.png`

export const referenceProducts: ProductPreview[] = [
  { id: 'preview-coke', sku: 'SP0001', barcode: '8935049500017', name: 'Coca Cola 330ml', unit: 'Lon', salePrice: 10000, isActive: true, quantityOnHand: 24, category: 'Đồ uống', updatedAt: '16/12/2024 10:23', image: image(1), referenceCost: 7500 },
  { id: 'preview-pepsi', sku: 'SP0002', barcode: '8934588592123', name: 'Pepsi 330ml', unit: 'Lon', salePrice: 10000, isActive: true, quantityOnHand: 18, category: 'Đồ uống', updatedAt: '15/12/2024 10:45', image: image(2), referenceCost: 7000 },
  { id: 'preview-aquafina', sku: 'SP0003', barcode: '8934588000123', name: 'Nước suối Aquafina 500ml', unit: 'Chai', salePrice: 7000, isActive: true, quantityOnHand: 36, category: 'Đồ uống', updatedAt: '14/12/2024 16:04', image: image(3), referenceCost: 4500 },
  { id: 'preview-c2', sku: 'SP0004', barcode: '8934563123123', name: 'Trà xanh C2 500ml', unit: 'Chai', salePrice: 10000, isActive: true, quantityOnHand: 8, category: 'Đồ uống', updatedAt: '13/12/2024 11:05', image: image(4), referenceCost: 6800 },
  { id: 'preview-milk', sku: 'SP0005', barcode: '8934678000001', name: 'Sữa Vinamilk 180ml', unit: 'Hộp', salePrice: 8000, isActive: true, quantityOnHand: 12, category: 'Sữa và TP từ sữa', updatedAt: '12/12/2024 14:20', image: image(5), referenceCost: 5600 },
  { id: 'preview-oreo', sku: 'SP0006', barcode: '8934803123456', name: 'Bánh Oreo 133g', unit: 'Gói', salePrice: 22000, isActive: true, quantityOnHand: 15, category: 'Bánh kẹo', updatedAt: '12/12/2024 09:18', image: image(6), referenceCost: 16000 },
  { id: 'preview-haohao', sku: 'SP0007', barcode: '8934567890123', name: 'Mì Hảo Hảo 75g', unit: 'Gói', salePrice: 4000, isActive: true, quantityOnHand: 48, category: 'Gia vị', updatedAt: '11/12/2024 10:55', image: image(7), referenceCost: 2800 },
  { id: 'preview-oil', sku: 'SP0008', barcode: '8934671234567', name: 'Dầu ăn Tường An 1L', unit: 'Chai', salePrice: 45000, isActive: true, quantityOnHand: 8, category: 'Gia vị', updatedAt: '10/12/2024 13:22', image: image(8), referenceCost: 36000 },
]

export const referenceCategoryCounts: Record<ProductCategory, number> = {
  'Tất cả': 156, 'Đồ uống': 28, 'Bánh kẹo': 24, 'Gia vị': 18,
  'Sữa và TP từ sữa': 20, 'Đồ gia dụng': 12, 'Khác': 54,
}
