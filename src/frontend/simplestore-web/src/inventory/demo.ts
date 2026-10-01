import { ref } from 'vue'

/** Preview data stays in this browser tab and is never sent to the inventory API. */
export const inventoryDemoEnabled = ref(false)

/** `soon` is C14 LowStockRisk on live data; `low` exists only in the reference picture (no approved threshold). */
export type StockTone = 'ok' | 'out' | 'low' | 'negative' | 'soon' | 'off'
export type InventoryChip = 'all' | 'out' | 'low' | 'negative' | 'soon' | 'other'

export interface InventoryPreview {
  id: string
  name: string
  sku: string
  barcode: string
  unit: string
  category: string
  stock: number
  avg: number
  value: number
  salePrice: number
  status: StockTone
  updated: string
  image: string
  isActive: boolean
}

export interface MovementPreview {
  date: string
  type: string
  quantity: number
  cost: number
  stock: string
  actor: string
}

/** The reference pictures share their product images with the Product List preview. */
const image = (id: number) => `/product-demo/${id}.png`
const item = (id: number, name: string, unit: string, stock: number, avg: number, status: StockTone, updated: string, category: string, salePrice: number): InventoryPreview => ({
  id: `preview-inventory-${id}`, name, sku: `SP000${id}`, barcode: `89350495000${16 + id}`, unit, category, stock, avg,
  value: stock * avg, salePrice, status, updated, image: image(id), isActive: true,
})

/** Mirrors TemplateHTML/Inventory: seven rows of a 156-product store. */
export const referenceInventory: InventoryPreview[] = [
  item(1, 'Coca Cola 330ml', 'Lon', 24, 5000, 'ok', '16/12/2024 10:23', 'Đồ uống', 10000),
  item(2, 'Pepsi 330ml', 'Lon', 0, 5200, 'out', '15/12/2024 14:10', 'Đồ uống', 10000),
  item(3, 'Nước suối Aquafina 500ml', 'Chai', 36, 3000, 'ok', '14/12/2024 09:18', 'Đồ uống', 7000),
  item(4, 'Trà xanh C2 500ml', 'Chai', -3, 4500, 'negative', '13/12/2024 11:05', 'Đồ uống', 10000),
  item(5, 'Sữa Vinamilk 180ml', 'Hộp', 12, 6000, 'ok', '12/12/2024 14:20', 'Sữa và TP từ sữa', 8000),
  item(6, 'Bánh Oreo 133g', 'Gói', 5, 16000, 'low', '12/12/2024 09:18', 'Bánh kẹo', 22000),
  item(7, 'Mì Hảo Hảo 75g', 'Gói', 48, 2500, 'ok', '11/12/2024 10:55', 'Gia vị', 4000),
]

/** Totals printed on the reference summary cards, chips and pager. */
export const referenceChipCounts: Record<InventoryChip, number> = { all: 156, out: 8, low: 12, negative: 3, soon: 5, other: 128 }
export const referencePageCount = 156

/** "Now" of the reference pictures, used for anything changed in the preview. */
export const referenceNow = '16/12/2024 14:23'
export const referenceActor = 'Việt Anh'

export const referenceMovements: MovementPreview[] = [
  { date: '16/12/2024 10:23', type: 'Bán hàng', quantity: -3, cost: 5000, stock: '24', actor: 'Thu ngân' },
  { date: '15/12/2024 14:10', type: 'Nhập hàng', quantity: 20, cost: 5200, stock: '27', actor: 'Việt Anh' },
  { date: '14/12/2024 09:15', type: 'Bán hàng', quantity: -5, cost: 5000, stock: '7', actor: 'Thu ngân' },
  { date: '12/12/2024 16:40', type: 'Điều chỉnh', quantity: -2, cost: 5000, stock: '12', actor: 'Việt Anh' },
  { date: '10/12/2024 08:30', type: 'Kiểm kho', quantity: 1, cost: 5000, stock: '14', actor: 'Việt Anh' },
]

/** Counted quantities of the reference stocktake: four differences on page one, Mì Hảo Hảo on the next. */
export const referenceCounts: Record<string, number> = {
  SP0001: 24, SP0002: 2, SP0003: 35, SP0004: 0, SP0005: 10, SP0006: 5, SP0007: 50,
}
export const referenceStocktake = { name: 'Kiểm kho cuối tháng 12/2024', date: '16/12/2024', total: 156, mismatched: 16 }

/** Prototype rules: the reference uses "≤ 8" for its low/soon pictures only. */
export function previewTone(stock: number): StockTone {
  return stock < 0 ? 'negative' : stock === 0 ? 'out' : stock <= 8 ? 'low' : 'ok'
}
