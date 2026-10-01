import { ref } from 'vue'
import type { IconName } from '../components/ui/icons'

/** Preview data stays in this browser tab; shifts, comparisons and history are illustrations only (D-096). */
export const todayDemoEnabled = ref(false)

/** The reference pictures are anchored to this business date. */
export const referenceDate = '2024-12-16'
/** Day chosen with the topbar arrows while previewing. */
export const todayDemoDate = ref(referenceDate)
/** Live business date from /api/today, shown by the topbar control. */
export const todayLiveDate = ref('')

export type Shift = 'morning' | 'all' | 'afternoon' | 'evening'
export type ChartMetric = 'revenue' | 'invoices' | 'units'
export type PaymentKind = 'Tiền mặt' | 'Chuyển khoản' | 'Công nợ'

export const shiftOptions: Array<{ value: Shift; label: string }> = [
  { value: 'morning', label: 'Ca sáng (06:00 - 14:23)' },
  { value: 'all', label: 'Tất cả các ca' },
  { value: 'afternoon', label: 'Ca chiều (15:00 - 18:59)' },
  { value: 'evening', label: 'Ca tối (19:00 - 22:59)' },
]
const shiftRanges: Record<Shift, string> = { morning: '00:00 – 14:23', afternoon: '15:00 – 18:59', evening: '19:00 – 22:59', all: '00:00 – 22:59' }
const shiftScopes: Record<Shift, string> = { morning: '00:00 đến 14:23', afternoon: '15:00 đến 18:59', evening: '19:00 đến 22:59', all: '00:00 đến 22:59' }

const hourly = [
  { h: 6, amount: 100000, orders: 2, units: 9 }, { h: 7, amount: 180000, orders: 2, units: 11 }, { h: 8, amount: 340000, orders: 3, units: 16 },
  { h: 9, amount: 420000, orders: 4, units: 22 }, { h: 10, amount: 650000, orders: 5, units: 33 }, { h: 11, amount: 500000, orders: 4, units: 28 },
  { h: 12, amount: 360000, orders: 4, units: 19 }, { h: 13, amount: 220000, orders: 3, units: 13 }, { h: 14, amount: 80000, orders: 1, units: 5 },
  { h: 15, amount: 280000, orders: 3, units: 15 }, { h: 16, amount: 480000, orders: 5, units: 24 }, { h: 17, amount: 420000, orders: 4, units: 21 },
  { h: 18, amount: 300000, orders: 3, units: 16 },
  { h: 19, amount: 220000, orders: 2, units: 10 }, { h: 20, amount: 240000, orders: 3, units: 12 }, { h: 21, amount: 170000, orders: 2, units: 8 },
  { h: 22, amount: 100000, orders: 1, units: 5 },
]

export const referenceCategories = [
  { name: 'Đồ uống', pct: 35, color: '#009567' }, { name: 'Bánh kẹo', pct: 22, color: '#348cff' }, { name: 'Gia vị', pct: 15, color: '#ffa01e' },
  { name: 'Sữa và TP từ sữa', pct: 12, color: '#8548cb' }, { name: 'Đồ gia dụng', pct: 8, color: '#fb617e' }, { name: 'Khác', pct: 8, color: '#a9b2c3' },
]

/** `pack` and `short` draw the CSS product packs of the reference. */
const products = [
  { name: 'Coca Cola 330ml', sku: 'SP0001', unit: 'Lon', qty: 48, total: 480000, pack: 'cola', short: ['Coca', 'Cola'] },
  { name: 'Mì Hảo Hảo 75g', sku: 'SP0007', unit: 'Gói', qty: 36, total: 144000, pack: 'noodle', short: ['Hảo Hảo'] },
  { name: 'Nước suối Aquafina 500ml', sku: 'SP0003', unit: 'Chai', qty: 32, total: 224000, pack: 'water', short: ['Aqua'] },
  { name: 'Sữa Vinamilk 180ml', sku: 'SP0005', unit: 'Hộp', qty: 28, total: 224000, pack: 'milk', short: ['VNM'] },
  { name: 'Bánh Oreo 133g', sku: 'SP0006', unit: 'Gói', qty: 24, total: 528000, pack: 'cookie', short: ['OREO'] },
  { name: 'Dầu ăn Tường An 1L', sku: 'SP0012', unit: 'Chai', qty: 14, total: 588000, pack: 'water', short: ['Dầu'] },
  { name: 'Nước mắm Nam Ngư 500ml', sku: 'SP0014', unit: 'Chai', qty: 11, total: 242000, pack: 'cola', short: ['Nam Ngư'] },
]

interface Transaction { id: string; time: string; items: number; amount: number; payment: PaymentKind; shift?: Shift }
const morningTransactions: Transaction[] = [
  { id: '#HD000123', time: '14:23', items: 3, amount: 120000, payment: 'Tiền mặt' },
  { id: '#HD000122', time: '14:15', items: 5, amount: 250000, payment: 'Chuyển khoản' },
  { id: '#HD000121', time: '13:58', items: 2, amount: 75000, payment: 'Tiền mặt' },
  { id: '#HD000120', time: '13:42', items: 6, amount: 320000, payment: 'Công nợ' },
  { id: '#HD000119', time: '13:30', items: 1, amount: 90000, payment: 'Tiền mặt' },
]
const laterTransactions: Transaction[] = [
  { id: '#HD000140', time: '22:14', items: 4, amount: 100000, payment: 'Tiền mặt', shift: 'evening' },
  { id: '#HD000139', time: '21:40', items: 3, amount: 170000, payment: 'Chuyển khoản', shift: 'evening' },
  { id: '#HD000138', time: '20:17', items: 5, amount: 240000, payment: 'Tiền mặt', shift: 'evening' },
  { id: '#HD000137', time: '18:52', items: 6, amount: 300000, payment: 'Công nợ', shift: 'afternoon' },
  { id: '#HD000136', time: '17:23', items: 3, amount: 420000, payment: 'Chuyển khoản', shift: 'afternoon' },
  { id: '#HD000135', time: '16:45', items: 4, amount: 480000, payment: 'Tiền mặt', shift: 'afternoon' },
  { id: '#HD000134', time: '15:12', items: 2, amount: 280000, payment: 'Tiền mặt', shift: 'afternoon' },
]

export const referenceStock: Array<{ label: string; count: string; icon: IconName; tone: '' | 'red' | 'green'; text: string }> = [
  { label: 'Sắp hết hàng', count: '8 sản phẩm', icon: 'alert', tone: '', text: 'Sản phẩm dự kiến hết trong vài ngày tới.' },
  { label: 'Hết hàng', count: '3 sản phẩm', icon: 'shoppingBag', tone: 'red', text: 'Số lượng khả dụng bằng 0.' },
  { label: 'Tồn kho thấp', count: '12 sản phẩm', icon: 'alert', tone: '', text: 'Số lượng còn lại dưới ngưỡng cảnh báo.' },
  { label: 'Tồn kho bình thường', count: '342 sản phẩm', icon: 'checkCircle', tone: 'green', text: 'Số lượng còn lại trên ngưỡng cảnh báo.' },
]
/** Comparison badges are printed as-is by the reference, whatever the day. */
export const referenceTrends = { revenue: '↑ 12%', invoices: '↑ 17%', units: '↑ 9%', profit: '↑ 11%' }

export const money = (value: number) => `${new Intl.NumberFormat('vi-VN').format(Math.round(value))} ₫`
export const count = (value: number) => new Intl.NumberFormat('vi-VN').format(Math.round(value))
export const displayDate = (iso: string) => iso.split('-').reverse().join('/')
export function weekdayLabel(iso: string) {
  const day = new Date(`${iso}T00:00:00Z`).getUTCDay()
  return day === 0 ? 'Chủ nhật' : `Thứ ${day + 1}`
}
export function shiftIsoDate(iso: string, days: number) {
  const date = new Date(`${iso}T00:00:00Z`)
  date.setUTCDate(date.getUTCDate() + days)
  return date.toISOString().slice(0, 10)
}

/** The reference's deterministic per-day variation around 16/12/2024. */
function factor(date: string) {
  const offset = Math.round((Date.parse(`${date}T00:00:00Z`) - Date.parse(`${referenceDate}T00:00:00Z`)) / 86400000)
  return offset === 0 ? 1 : Math.max(0.68, Math.min(1.28, 1 + (((offset * 37) % 17) - 8) / 60))
}
const roundThousand = (value: number) => Math.round(value / 1000) * 1000
const shiftHours = (shift: Shift) => hourly.filter(row => shift === 'all' || (shift === 'morning' ? row.h < 15 : shift === 'afternoon' ? row.h >= 15 && row.h < 19 : row.h >= 19))

export interface DemoSelection { date: string; shift: Shift }

export function demoTotals({ date, shift }: DemoSelection) {
  const f = factor(date)
  const rows = shiftHours(shift)
  const amount = rows.reduce((sum, row) => sum + row.amount, 0)
  return {
    revenue: roundThousand(amount * f),
    invoices: Math.round(rows.reduce((sum, row) => sum + row.orders, 0) * f),
    units: Math.round(rows.reduce((sum, row) => sum + row.units, 0) * f),
    profit: roundThousand(amount * f * 820 / 2850),
  }
}

export function demoHeader({ date, shift }: DemoSelection) {
  return `${weekdayLabel(date)}, ${displayDate(date)}  (${shiftRanges[shift]})`
}

export function demoChart(selection: DemoSelection, metric: ChartMetric) {
  const f = factor(selection.date)
  const rows = shiftHours(selection.shift)
  const values = rows.map(row => metric === 'revenue' ? roundThousand(row.amount * f) : Math.round(row[metric === 'invoices' ? 'orders' : 'units'] * f))
  const max = Math.max(...values, 1)
  const top = metric === 'revenue' ? Math.ceil(max / 200000) * 200000 : Math.ceil(max / 10) * 10
  const tick = (value: number) => metric === 'revenue' ? `${Math.round(value / 1000)}K` : String(Math.round(value))
  const shown = (value: number) => metric === 'revenue' ? money(value) : count(value)
  return {
    ticks: [top, top * 0.75, top * 0.5, top * 0.25, 0].map(tick),
    bars: rows.map((row, index) => ({
      hour: row.h,
      height: Math.max(1, values[index]! / top * 100),
      tip: `${row.h}:00 · ${shown(values[index]!)}`,
      axis: rows.length <= 9 || index % 2 === 0 ? `${row.h}h` : '',
    })),
  }
}

/** Ranked products scale with the selected revenue, like the reference. */
export function demoProducts(selection: DemoSelection) {
  const scale = demoTotals(selection).revenue / 2850000
  return products.map(product => ({ ...product, qty: product.qty * scale, total: product.total * scale }))
}

export function demoTransactions({ date, shift }: DemoSelection) {
  const items = shift === 'morning' ? morningTransactions : shift === 'all' ? [...laterTransactions, ...morningTransactions] : laterTransactions.filter(item => item.shift === shift)
  return items.map(item => ({ ...item, amount: roundThousand(item.amount * factor(date)) }))
}

export function demoDebt({ date }: DemoSelection) {
  return { amount: roundThousand(1250000 * factor(date)), exact: 1250000 * factor(date), customers: 5, overdue: '2 khách' }
}

/** Drawer content: explanation cards, lists of drill-down items, detail cards and actions. */
export type TodayBlock =
  | { kind: 'intro'; text: string }
  | { kind: 'card'; icon: IconName; title: string; tone?: '' | 'blue' | 'red'; text?: string[]; bullets?: string[]; formula?: string[]; pairs?: Array<[string, string]>; value?: { strong: string; text: string }; action?: { icon: IconName; label: string; open: string } }
  | { kind: 'list'; items: Array<{ title: string; text: string; open: string }> }
  | { kind: 'detail'; title: string; intro?: string; pairs: Array<[string, string]> }
  | { kind: 'action'; icon: IconName; label: string; open: string }
  | { kind: 'notice'; title: string; text: string }
  | { kind: 'sources' }

export interface TodayDrawer { title: string; blocks: TodayBlock[] }

export function demoDrawer(type: string, selection: DemoSelection): TodayDrawer {
  const totals = demoTotals(selection)
  const day = displayDate(selection.date)
  const shiftLabel = shiftOptions.find(option => option.value === selection.shift)!.label
  const [kind, index] = type.split(':') as [string, string | undefined]
  const selected = index === undefined ? null : Number(index)

  if (kind === 'revenue') {
    return { title: 'Cách tính doanh thu', blocks: [
      { kind: 'card', icon: 'receipt', title: '1. Định nghĩa', text: ['Doanh thu là tổng giá trị các giao dịch bán hàng đã hoàn tất trong khoảng thời gian đã chọn.'] },
      { kind: 'card', icon: 'register', title: '2. Công thức', formula: ['Doanh thu = Tổng giá trị Sale hoàn tất', '− Giá trị Return', '− Giá trị Sale bị Void'] },
      { kind: 'card', icon: 'calendar', title: '3. Phạm vi thời gian', tone: 'blue', text: [`Từ ${shiftScopes[selection.shift]}, ngày ${day} (theo múi giờ của cửa hàng).`] },
      { kind: 'card', icon: 'checkCircle', title: '4. Bao gồm', bullets: ['Tất cả giao dịch bán hàng đã hoàn tất.', 'Bao gồm cả tiền mặt, chuyển khoản và công nợ.'] },
      { kind: 'card', icon: 'close', title: '5. Không bao gồm', tone: 'red', bullets: ['Đơn chưa hoàn tất (đơn đang giữ).', 'Giao dịch bị hủy (Void).', 'Các điều chỉnh thủ công không phải bán hàng (nếu có).'] },
      { kind: 'card', icon: 'list', title: '6. Xem chi tiết', tone: 'blue', text: [`Bạn có thể xem danh sách các giao dịch cấu thành doanh thu ${money(totals.revenue)}.`], action: { icon: 'list', label: 'Xem giao dịch trong ca', open: 'transactions' } },
      { kind: 'notice', title: 'Lưu ý', text: 'Số liệu có thể thay đổi nếu có giao dịch được hoàn tất, return hoặc hủy trong ngày.' },
    ] }
  }
  if (kind === 'transactions' || kind === 'transaction') {
    const rows = demoTransactions(selection)
    const row = selected === null ? null : rows[selected]
    if (!row) {
      return { title: 'Giao dịch gần đây', blocks: [
        { kind: 'intro', text: `${day} · ${shiftLabel}. Chọn giao dịch để xem chi tiết.` },
        { kind: 'list', items: rows.map((item, position) => ({ title: `${item.id} · ${money(item.amount)}`, text: `${item.time} · ${item.items} sản phẩm · ${item.payment}`, open: `transaction:${position}` })) },
      ] }
    }
    return { title: 'Chi tiết giao dịch', blocks: [
      { kind: 'detail', title: row.id, pairs: [['Thời gian', `${day} · ${row.time}`], ['Trạng thái', 'Hoàn tất'], ['Số sản phẩm', String(row.items)], ['Thanh toán', row.payment], ['Giá trị', money(row.amount)]] },
      { kind: 'action', icon: 'chevronLeft', label: 'Quay lại danh sách', open: 'transactions' },
    ] }
  }
  if (kind === 'products' || kind === 'product') {
    const rows = demoProducts(selection)
    const row = selected === null ? null : rows[selected]
    if (!row) {
      return { title: 'Sản phẩm bán chạy', blocks: [
        { kind: 'intro', text: 'Xếp hạng theo số lượng bán trong thời gian đã chọn.' },
        { kind: 'list', items: rows.map((item, position) => ({ title: `${position + 1}. ${item.name}`, text: `${item.sku} · ${count(item.qty)} ${item.unit.toLowerCase()} · ${money(item.total)}`, open: `product:${position}` })) },
      ] }
    }
    return { title: 'Chi tiết sản phẩm', blocks: [
      { kind: 'detail', title: row.name, pairs: [['Mã sản phẩm', row.sku], ['Đơn vị', row.unit], ['Số lượng bán', count(row.qty)], ['Doanh thu', money(row.total)]] },
      { kind: 'action', icon: 'chevronLeft', label: 'Quay lại xếp hạng', open: 'products' },
    ] }
  }
  // The reference has no own drawer for the stock info icon; it shows the status list.
  if (kind === 'stock' || kind === 'stockInfo') {
    const row = selected === null ? null : referenceStock[selected]
    if (!row) {
      return { title: 'Tình trạng tồn kho', blocks: [
        { kind: 'intro', text: 'Tình trạng tồn kho hiện tại của cửa hàng.' },
        { kind: 'list', items: referenceStock.map((item, position) => ({ title: item.label, text: `${item.count} · ${item.text}`, open: `stock:${position}` })) },
      ] }
    }
    return { title: row.label, blocks: [
      { kind: 'detail', title: row.label, intro: row.text, pairs: [['Số sản phẩm', row.count], ['Cập nhật', 'Theo tồn kho hiện tại']] },
      { kind: 'action', icon: 'chevronLeft', label: 'Xem mọi tình trạng', open: 'stock' },
    ] }
  }
  if (kind === 'debt' || kind === 'debtInfo') {
    const debt = demoDebt(selection)
    return { title: kind === 'debt' ? 'Công nợ khách hàng' : 'Cách tính công nợ', blocks: [
      { kind: 'card', icon: 'users', title: 'Tổng công nợ', pairs: [['Dư nợ hiện tại', money(debt.exact)], ['Khách còn nợ', String(debt.customers)], ['Khách quá hạn', '2']] },
      { kind: 'card', icon: 'info', title: 'Phạm vi', tone: 'blue', text: ['Đây là số dư công nợ khách hàng tại thời điểm xem. Giao dịch bán trả sau làm tăng công nợ; các khoản thu làm giảm công nợ.'] },
    ] }
  }
  const info: Record<string, [string, string, string]> = {
    invoices: ['Số hóa đơn', 'Đếm các hóa đơn bán hàng đã hoàn tất trong thời gian đã chọn.', count(totals.invoices)],
    units: ['Số sản phẩm bán', 'Tổng số lượng sản phẩm trên các hóa đơn bán hàng đã hoàn tất.', count(totals.units)],
    profit: ['Lợi nhuận ước tính', 'Doanh thu trừ giá vốn ước tính của số hàng đã bán. Con số demo có thể khác lợi nhuận sau chi phí.', money(totals.profit)],
    hourInfo: ['Doanh thu theo giờ', 'Mỗi cột gom các giao dịch hoàn tất trong một giờ. Có thể chuyển sang số hóa đơn hoặc số sản phẩm.', money(totals.revenue)],
    structureInfo: ['Cơ cấu doanh thu', 'Tỷ trọng doanh thu theo nhóm sản phẩm. Chọn một nhóm ở chú giải để xem giá trị tương ứng.', money(totals.revenue)],
    bestInfo: ['Sản phẩm bán chạy', 'Xếp hạng theo số lượng bán trong thời gian đã chọn.', count(totals.units)],
  }
  const [title, text, value] = info[kind] ?? info.invoices!
  return { title, blocks: [
    { kind: 'card', icon: 'info', title: 'Cách hiểu', tone: 'blue', text: [text] },
    { kind: 'card', icon: 'barChart', title: 'Giá trị đang hiển thị', value: { strong: value, text: ` · ${day}` } },
  ] }
}
