import { ref } from 'vue'

/** Preview data stays in this browser tab and is never sent to the debt API. */
export const debtDemoEnabled = ref(false)

export type DebtKind = 'customer' | 'supplier'
/** Due/overdue tones exist only in the reference picture; the domain has no due date (D-100). */
export type DebtTone = 'paid' | 'overdue' | 'due' | 'owing'

export interface DebtPartyPreview {
  id: string
  name: string
  phone: string
  incurred: number
  paid: number
  debt: number
  last: string
  overdue: number
  status: DebtTone
  note: string
  group: string
  address: string
}

/** Date, type, document, amount incurred, amount paid, remaining — as printed in the reference rows. */
export interface LedgerRow { date: string; type: string; document: string; incurred: number; paid: number; balance: number }
export interface PaymentRow { date: string; document: string; method: string; amount: number; actor: string }

const party = (kind: DebtKind, index: number, name: string, phone: string, incurred: number, paid: number, last: string, overdue: number, status: DebtTone, note: string, address = ''): DebtPartyPreview => ({
  id: `preview-${kind}-${index}`, name, phone, incurred, paid, debt: incurred - paid, last, overdue, status, note, group: 'Khách lẻ', address,
})

/** Mirrors TemplateHTML/Debit: five rows per mode. */
export const referenceParties: Record<DebtKind, DebtPartyPreview[]> = {
  customer: [
    party('customer', 1, 'Nguyễn Văn A', '0901 234 567', 5200000, 2000000, '16/12/2024', 5, 'overdue', 'Khách quen, lấy hàng thường xuyên'),
    party('customer', 2, 'Trần Thị B', '0902 111 222', 3500000, 3500000, '14/12/2024', 0, 'paid', 'Khách lẻ'),
    party('customer', 3, 'Lê Văn C', '0903 333 444', 7800000, 1000000, '12/12/2024', 9, 'overdue', 'Khách quen'),
    party('customer', 4, 'Phạm Thị D', '0904 555 666', 1200000, 500000, '15/12/2024', 2, 'due', 'Khách lẻ'),
    party('customer', 5, 'Hoàng Văn E', '0905 777 888', 980000, 980000, '10/12/2024', 0, 'paid', 'Khách lẻ'),
  ],
  supplier: [
    party('supplier', 1, 'Thiên Long', '0901 234 567', 25600000, 20400000, '16/12/2024', 3, 'due', 'Nhà cung cấp chính', '123 Đường ABC, Quận 1, TP.HCM'),
    party('supplier', 2, 'Việt Tiến', '0902 888 111', 18500000, 18500000, '15/12/2024', 0, 'paid', 'Nhà cung cấp', '45 Lê Lợi, Quận 3, TP.HCM'),
    party('supplier', 3, 'Đại Phát', '0903 666 333', 12000000, 6000000, '14/12/2024', 10, 'overdue', 'Nhà cung cấp', '78 Nguyễn Trãi, Quận 5, TP.HCM'),
    party('supplier', 4, 'Hòa Bình', '0904 222 555', 8600000, 7600000, '13/12/2024', 4, 'due', 'Nhà cung cấp', '12 Hai Bà Trưng, Quận 1, TP.HCM'),
    party('supplier', 5, 'Minh An', '0905 111 999', 7300000, 7300000, '11/12/2024', 0, 'paid', 'Nhà cung cấp', '9 Cách Mạng Tháng 8, Quận 10, TP.HCM'),
  ],
}

/** Customer headline facts follow design-reference.png; supplier facts follow the prototype's own totals. */
export const referenceSummary: Record<DebtKind, { debtors: number; total: number; overdue: number; due: number; parties: number }> = {
  customer: { debtors: 12, total: 18450000, overdue: 5, due: 3, parties: 12 },
  supplier: { debtors: 3, total: 12200000, overdue: 1, due: 2, parties: 9 },
}

/** "Phát sinh gần đây (5)" of the reference detail drawers. */
export const referenceRecent: Record<DebtKind, LedgerRow[]> = {
  customer: [
    { date: '16/12/2024', type: 'Bán hàng', document: 'SO000123', incurred: 1200000, paid: 0, balance: 1200000 },
    { date: '14/12/2024', type: 'Bán hàng', document: 'SO000120', incurred: 800000, paid: 800000, balance: 0 },
    { date: '12/12/2024', type: 'Bán hàng', document: 'SO000118', incurred: 2000000, paid: 0, balance: 2200000 },
    { date: '10/12/2024', type: 'Thu tiền', document: 'RC000021', incurred: 0, paid: 1000000, balance: -1000000 },
    { date: '08/12/2024', type: 'Bán hàng', document: 'SO000115', incurred: 1200000, paid: 1200000, balance: 0 },
  ],
  supplier: [
    { date: '16/12/2024', type: 'Nhập hàng', document: 'PN000045', incurred: 8500000, paid: 0, balance: 8500000 },
    { date: '12/12/2024', type: 'Trả tiền', document: 'PC000012', incurred: 0, paid: 5000000, balance: -5000000 },
    { date: '10/12/2024', type: 'Nhập hàng', document: 'PN000044', incurred: 6200000, paid: 6200000, balance: 0 },
    { date: '05/12/2024', type: 'Nhập hàng', document: 'PN000041', incurred: 4800000, paid: 4800000, balance: 0 },
    { date: '01/12/2024', type: 'Nhập hàng', document: 'PN000039', incurred: 6100000, paid: 4400000, balance: 1700000 },
  ],
}

/** Full "Lịch sử giao dịch": the customer rows are those of design-reference panel 8. */
export const referenceHistory: Record<DebtKind, LedgerRow[]> = {
  customer: [
    { date: '16/12/2024', type: 'Bán hàng', document: 'SO000123', incurred: 1200000, paid: 0, balance: 3200000 },
    { date: '14/12/2024', type: 'Bán hàng', document: 'SO000120', incurred: 800000, paid: 800000, balance: 2000000 },
    { date: '12/12/2024', type: 'Bán hàng', document: 'SO000118', incurred: 2000000, paid: 0, balance: 2800000 },
    { date: '10/12/2024', type: 'Thu tiền', document: 'RC000021', incurred: 0, paid: 1000000, balance: 800000 },
    { date: '08/12/2024', type: 'Bán hàng', document: 'SO000115', incurred: 1200000, paid: 1200000, balance: 800000 },
    { date: '05/12/2024', type: 'Thu tiền', document: 'RC000019', incurred: 0, paid: 500000, balance: 800000 },
    { date: '02/12/2024', type: 'Bán hàng', document: 'SO000110', incurred: 0, paid: 0, balance: 0 },
  ],
  supplier: [
    { date: '16/12/2024', type: 'Nhập hàng', document: 'PN000045', incurred: 8500000, paid: 0, balance: 8500000 },
    { date: '12/12/2024', type: 'Trả tiền', document: 'PC000012', incurred: 0, paid: 5000000, balance: -5000000 },
    { date: '10/12/2024', type: 'Nhập hàng', document: 'PN000044', incurred: 6200000, paid: 6200000, balance: 0 },
    { date: '05/12/2024', type: 'Nhập hàng', document: 'PN000041', incurred: 4800000, paid: 4800000, balance: 0 },
    { date: '05/12/2024', type: 'Trả tiền', document: 'PC000009', incurred: 0, paid: 4800000, balance: -4800000 },
    { date: '01/12/2024', type: 'Nhập hàng', document: 'PN000039', incurred: 6100000, paid: 4400000, balance: 1700000 },
  ],
}

export const referencePayments: Record<DebtKind, PaymentRow[]> = {
  customer: [
    { date: '10/12/2024', document: 'RC000021', method: 'Tiền mặt', amount: 1000000, actor: 'Việt Anh' },
    { date: '05/12/2024', document: 'RC000019', method: 'Chuyển khoản', amount: 500000, actor: 'Việt Anh' },
  ],
  supplier: [
    { date: '12/12/2024', document: 'PC000012', method: 'Chuyển khoản', amount: 5000000, actor: 'Việt Anh' },
    { date: '05/12/2024', document: 'PC000009', method: 'Tiền mặt', amount: 4800000, actor: 'Việt Anh' },
  ],
}

/** Defaults of the reference "Thu tiền khách hàng" and "Trả tiền nhà cung cấp" forms. */
export const referencePaymentForm: Record<DebtKind, { amount: number; method: 'Cash' | 'Transfer'; date: string }> = {
  customer: { amount: 1000000, method: 'Cash', date: '16/12/2024' },
  supplier: { amount: 2000000, method: 'Transfer', date: '16/12/2024' },
}

export const referenceAsOfTime = '10:23'
export const referenceActor = 'Việt Anh'
