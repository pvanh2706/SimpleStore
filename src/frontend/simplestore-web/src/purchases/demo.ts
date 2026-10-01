import { ref } from 'vue'

/** Preview data stays in this browser tab and is never sent to the purchase API. */
export const purchaseDemoEnabled = ref(false)

/** Display tone of a row. Payment tones are labels for a non-voided Completed Purchase, never lifecycle values. */
export type PurchaseTone = 'done' | 'unpaid' | 'partial' | 'cancel' | 'draft'
export type PurchaseChip = 'all' | 'done' | 'processing' | 'cancel'

export interface PurchaseLinePreview {
  name: string
  sku: string
  unit: string
  qty: number
  cost: number
}

export interface PurchasePreview {
  id: string
  code: string
  date: string
  supplier: string
  total: number
  paid: number
  debt: number
  tone: PurchaseTone
  creator: string
  updated: string
  note: string
  payMethod: string
  cancelReason: string
  lines: PurchaseLinePreview[]
}

export interface SupplierPreview {
  name: string
  phone: string
  address: string
  debt: number
}

/** Mirrors TemplateHTML/Purchase: every reference Purchase shows the same five-line detail. */
export const referenceLines: PurchaseLinePreview[] = [
  { name: 'Coca Cola 330ml', sku: 'SP0001', unit: 'Lon', qty: 24, cost: 7500 },
  { name: 'Pepsi 330ml', sku: 'SP0002', unit: 'Lon', qty: 18, cost: 7000 },
  { name: 'Nước suối Aquafina 500ml', sku: 'SP0003', unit: 'Chai', qty: 36, cost: 5000 },
  { name: 'Mì Hảo Hảo 75g', sku: 'SP0007', unit: 'Gói', qty: 48, cost: 3500 },
  { name: 'Dầu ăn Tường An 1L', sku: 'SP0008', unit: 'Chai', qty: 12, cost: 45000 },
]

const row = (code: string, date: string, supplier: string, total: number, paid: number, tone: PurchaseTone, creator: string, time: string): PurchasePreview => ({
  id: `preview-${code}`, code, date, supplier, total, paid, debt: total - paid, tone, creator,
  updated: `${date} ${time}`, note: 'Nhập hàng định kỳ', payMethod: 'Tiền mặt', cancelReason: '', lines: referenceLines,
})

export const referencePurchases: PurchasePreview[] = [
  row('PN000045', '16/12/2024', 'Việt Tiến', 2500000, 2500000, 'done', 'Việt Anh', '10:23'),
  row('PN000044', '15/12/2024', 'Đại Phát', 1850000, 0, 'unpaid', 'Việt Anh', '14:10'),
  row('PN000043', '14/12/2024', 'Thiên Long', 3200000, 1000000, 'partial', 'Thu ngân', '09:15'),
  row('PN000042', '12/12/2024', 'Việt Tiến', 980000, 980000, 'done', 'Việt Anh', '16:40'),
  row('PN000041', '10/12/2024', 'Hòa Bình', 1200000, 0, 'unpaid', 'Thu ngân', '08:30'),
]

export const referenceSuppliers: SupplierPreview[] = [
  { name: 'Thiên Long', phone: '0901 234 567', address: '123 Đường ABC, Quận 1, TP.HCM', debt: 5200000 },
  { name: 'Việt Tiến', phone: '0908 765 432', address: '45 Lê Lợi, Quận 3, TP.HCM', debt: 0 },
  { name: 'Đại Phát', phone: '0912 345 678', address: '78 Nguyễn Trãi, Quận 5, TP.HCM', debt: 1850000 },
]

/** Totals printed on the reference chips and pager; only the first page has rows. */
export const referenceChipCounts: Record<PurchaseChip, number> = { all: 56, done: 45, processing: 3, cancel: 2 }

/** The reference wizard opens with this draft: Thiên Long, the first four lines and a 300.000 đ partial payment. */
export const referenceWizard = {
  supplier: 'Thiên Long',
  note: 'Nhập hàng định kỳ',
  date: '16/12/2024',
  payType: 'partial' as 'full' | 'partial' | 'none',
  payAmount: 300000,
  payMethod: 'Tiền mặt',
  lineCount: 4,
  newCode: 'PN000046',
  createdAt: '16/12/2024 14:23',
  creator: 'Việt Anh',
}

export function chipOf(tone: PurchaseTone): Exclude<PurchaseChip, 'all'> {
  return tone === 'done' ? 'done' : tone === 'cancel' ? 'cancel' : 'processing'
}
