import { ref } from 'vue'
import type { CustomerPage, ProductListItem, ProductPage } from '../api/types'
import { createOrderBook, emptyOrder, type OrderBook } from './orders'

/** Preview data lives only in the browser tab and never enters the sales API. */
export const salesDemoEnabled = ref(false)

/** Header identity shown with the sample data, as in the approved Sales visual reference. */
export const demoIdentity = {
  storeName: 'Cửa hàng Tạp Hóa Việt Anh',
  userName: 'Việt Anh',
  initials: 'VA',
  dateLabel: 'Thứ 2, 16/12/2024\u00a0\u00a014:23',
}

export const demoProducts: ProductListItem[] = [
  { id: 'demo-coke', sku: 'SP0001', barcode: '8934588012228', name: 'Coca Cola 330ml', unit: 'lon', salePrice: 10000, isActive: true, quantityOnHand: 24 },
  { id: 'demo-pepsi', sku: 'SP0002', barcode: '8934588012229', name: 'Pepsi 330ml', unit: 'lon', salePrice: 10000, isActive: true, quantityOnHand: 18 },
  { id: 'demo-aquafina', sku: 'SP0003', barcode: '8934588012230', name: 'Nước suối Aquafina 500ml', unit: 'chai', salePrice: 7000, isActive: true, quantityOnHand: 36 },
  { id: 'demo-c2', sku: 'SP0004', barcode: '8934588012231', name: 'Trà xanh C2 500ml', unit: 'chai', salePrice: 10000, isActive: true, quantityOnHand: 20 },
  { id: 'demo-milk', sku: 'SP0005', barcode: '8934588012232', name: 'Sữa Vinamilk 180ml', unit: 'hộp', salePrice: 8000, isActive: true, quantityOnHand: 3 },
  { id: 'demo-oreo', sku: 'SP0006', barcode: '8934588012233', name: 'Bánh Oreo 133g', unit: 'gói', salePrice: 22000, isActive: true, quantityOnHand: 15 },
  { id: 'demo-haohao', sku: 'SP0007', barcode: '8934588012234', name: 'Mì Hảo Hảo 75g', unit: 'gói', salePrice: 4000, isActive: true, quantityOnHand: 48 },
  { id: 'demo-oil', sku: 'SP0008', barcode: '8934588012235', name: 'Dầu ăn Tường An 1L', unit: 'chai', salePrice: 45000, isActive: true, quantityOnHand: 2 },
  { id: 'demo-fishsauce', sku: 'SP0009', barcode: '8934588012236', name: 'Nước mắm Nam Ngư 500ml', unit: 'chai', salePrice: 28000, isActive: true, quantityOnHand: 10 },
  { id: 'demo-sugar', sku: 'SP0010', barcode: '8934588012237', name: 'Đường cát trắng 1kg', unit: 'gói', salePrice: 24000, isActive: true, quantityOnHand: 6 },
  { id: 'demo-knorr', sku: 'SP0011', barcode: '8934588012238', name: 'Hạt nêm Knorr 400g', unit: 'gói', salePrice: 32000, isActive: true, quantityOnHand: 9 },
  { id: 'demo-tissue', sku: 'SP0012', barcode: '8934588012239', name: 'Giấy vệ sinh Pulppy 10 cuộn', unit: 'lốc', salePrice: 38000, isActive: true, quantityOnHand: 2 },
]

const images = ['coke', 'pepsi', 'aquafina', 'c2', 'milk', 'oreo', 'haohao', 'oil', 'fishsauce', 'sugar', 'knorr', 'tissue']
export const demoImageById = Object.fromEntries(demoProducts.map((product, index) => [product.id, `/sales-demo/${images[index]}.png`]))

/** Mirrors the three working orders of the approved Sales visual reference. */
export function createDemoOrderBook(): OrderBook {
  const [coke, pepsi, aquafina, c2, , , haohao, , fishsauce] = demoProducts
  return createOrderBook([
    { ...emptyOrder(1), cart: [
      { product: coke!, quantity: 2, discountPercent: 10 },
      { product: aquafina!, quantity: 1 },
      { product: haohao!, quantity: 3 },
    ] },
    { ...emptyOrder(2), cart: [{ product: pepsi!, quantity: 1 }, { product: c2!, quantity: 1 }, { product: fishsauce!, quantity: 1 }] },
    { ...emptyOrder(3), cart: [{ product: haohao!, quantity: 3 }] },
  ])
}

export async function searchDemoProducts(search: string, page: number): Promise<ProductPage> {
  const query = search.trim().toLocaleLowerCase('vi-VN')
  const matches = demoProducts.filter(product => `${product.name} ${product.sku} ${product.barcode}`.toLocaleLowerCase('vi-VN').includes(query))
  const pageSize = 20
  return { items: matches.slice((page - 1) * pageSize, page * pageSize), page, pageSize, totalCount: matches.length, totalPages: Math.ceil(matches.length / pageSize) }
}

export async function searchDemoCustomers(): Promise<CustomerPage> {
  return { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }
}
