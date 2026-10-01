import { ref } from 'vue'

/** Preview data stays in this browser tab; closing a day here is never sent to the API (D-048, D-101). */
export const dayCloseDemoEnabled = ref(false)

export type Tone = '' | 'good' | 'bad'
export interface Cell { text: string; tone?: Tone }
export interface MiniTable { columns: string[]; numeric: number[]; rows: Array<{ cells: Cell[]; total?: boolean }> }

/** One drawer block; `section` wraps it in a titled box as in the reference drawers. */
export type DrawerBlock = { section?: string } & (
  | { kind: 'pairs'; rows: Array<{ label: string; value: string; tone?: Tone; strong?: boolean }> }
  | { kind: 'table'; table: MiniTable }
  | { kind: 'note'; tone: 'warn' | 'info'; text: string }
  | { kind: 'text'; text: string }
)

export interface ReportKpi { key: string; label: string; value: string; tone: '' | 'green' | 'red'; note?: string; warn?: boolean }
export interface ReportExplain {
  title: string
  definition: string
  formula?: { left: string; op: '−' | '+'; right: string; result: string; captions?: [string, string, string] }
}
/** Everything the report page draws, produced from the reference below or from a live EndOfDayReport. */
export interface ReportModel {
  kpis: ReportKpi[]
  payments: { title: string; centerLabel: string; centerValue: string; slices: Array<{ label: string; value: string; color: string; share: number }> }
  categories: Array<{ label: string; value: string; width: number; color: string }> | null
  tables: Array<{ title: string; table: MiniTable }>
  details: Record<string, { title: string; blocks: DrawerBlock[] }>
  explains: Record<string, ReportExplain>
  scope: string
  scopeNote: string
}

export interface DayRow { date: string; time: string; closed: boolean; actor: string; note: string }

const cell = (text: string, tone: Tone = ''): Cell => ({ text, tone })
const cells = (...values: Array<string | Cell>) => values.map(value => typeof value === 'string' ? cell(value) : value)

/** Mirrors TemplateHTML/EndOfDay: page one of a 20-day list, today still open. */
export const referenceDays: DayRow[] = [
  { date: '16/12/2024', time: '', closed: false, actor: '', note: '' },
  { date: '15/12/2024', time: '23:52', closed: true, actor: 'Việt Anh', note: '' },
  { date: '14/12/2024', time: '23:48', closed: true, actor: 'Việt Anh', note: '' },
  { date: '13/12/2024', time: '23:51', closed: true, actor: 'Thu ngân', note: '' },
  { date: '12/12/2024', time: '23:55', closed: true, actor: 'Việt Anh', note: '' },
  { date: '11/12/2024', time: '23:49', closed: true, actor: 'Việt Anh', note: '' },
]
/** Footer and pager as printed in the reference ("Hiển thị 1 – 6 / 20 ngày", pages 1…20). */
export const referenceDayCount = 20
export const referencePageCount = 20
/** "Now" of the reference pictures; a day closed in the preview gets this time and actor. */
export const referenceCloseTime = '14:23'
export const referenceActor = 'Việt Anh'

/** The wizard summary ("Số liệu ngày …") printed in all three close steps. */
export const referenceCloseSummary: Array<{ label: string; value: string; tone?: Tone }> = [
  { label: 'Doanh thu bán hàng', value: '12.450.000 đ' },
  { label: 'Thực thu tiền', value: '9.800.000 đ' },
  { label: 'Công nợ tăng', value: '3.200.000 đ', tone: 'bad' },
  { label: 'Giá vốn hàng bán', value: '7.320.000 đ' },
  { label: 'Lợi nhuận ước tính', value: '5.130.000 đ', tone: 'good' },
  { label: 'Số lượng hóa đơn', value: '48' },
]
export const referenceReconciliation = [
  { title: 'Đối soát bán hàng', text: '48 hóa đơn hoàn tất, 5 giao dịch trả hàng đã được tính giảm doanh thu.' },
  { title: 'Đối soát thanh toán', text: 'Tiền mặt 5.200.000 đ · Chuyển khoản 4.600.000 đ.' },
]

const paymentTable: MiniTable = {
  columns: ['Hình thức', 'Số GD', 'Số tiền', 'Tỷ lệ'], numeric: [2],
  rows: [
    { cells: cells('Tiền mặt', '30', '5.200.000 đ', '53.1%') },
    { cells: cells('Chuyển khoản', '18', '4.600.000 đ', '46.9%') },
  ],
}

/** Every figure exactly as printed in the reference report of 15/12/2024 and its drawers. */
export const referenceReport: ReportModel = {
  kpis: [
    { key: 'sales', label: 'Doanh thu bán hàng', value: '12.450.000 đ', tone: 'green' },
    { key: 'cash', label: 'Thực thu tiền', value: '9.800.000 đ', tone: 'green' },
    { key: 'debt', label: 'Công nợ phát sinh', value: '2.650.000 đ', tone: 'red' },
    { key: 'cogs', label: 'Giá vốn hàng bán', value: '7.320.000 đ', tone: 'red' },
    { key: 'profit', label: 'Lợi nhuận ước tính', value: '5.130.000 đ', tone: 'green' },
    { key: 'invoice', label: 'Số lượng hóa đơn', value: '48', tone: '' },
  ],
  payments: {
    title: 'Cơ cấu thanh toán', centerLabel: 'Tổng thực thu', centerValue: '9.800.000 đ',
    slices: [
      { label: 'Tiền mặt', value: '5.200.000 đ (53.1%)', color: '#0aa06a', share: 53.1 },
      { label: 'Chuyển khoản', value: '4.600.000 đ (46.9%)', color: '#2f6fe4', share: 46.9 },
      { label: 'Khác', value: '0 đ', color: '#aab6c5', share: 0 },
    ],
  },
  categories: [
    { label: 'Đồ uống', value: '3.850.000 đ (30.9%)', width: 100, color: '#0aa06a' },
    { label: 'Thực phẩm', value: '2.980.000 đ (23.9%)', width: 77, color: '#2f6fe4' },
    { label: 'Gia dụng', value: '1.920.000 đ (15.4%)', width: 50, color: '#ff9f43' },
    { label: 'Bánh kẹo', value: '1.560.000 đ (12.5%)', width: 41, color: '#805ad5' },
    { label: 'Khác', value: '2.140.000 đ (17.2%)', width: 56, color: '#97a6ba' },
  ],
  tables: [
    {
      title: 'Chi tiết doanh thu bán hàng',
      table: {
        columns: ['Loại giao dịch', 'Số HĐ', 'Doanh thu', 'Giảm giá', 'Doanh thu thuần'], numeric: [2, 3, 4],
        rows: [
          { cells: cells('Bán hàng', '45', '12.350.000 đ', '150.000 đ', '12.200.000 đ') },
          { cells: cells('Trả hàng', cell('-5', 'bad'), '300.000 đ', '0 đ', cell('-300.000 đ', 'bad')) },
          { cells: cells('Tổng', '48', '12.650.000 đ', '150.000 đ', '12.450.000 đ'), total: true },
        ],
      },
    },
    {
      title: 'Chi tiết thực thu tiền',
      table: {
        ...paymentTable,
        rows: [
          ...paymentTable.rows,
          { cells: cells('Khác', '0', '0 đ', '0%') },
          { cells: cells('Tổng', '48', '9.800.000 đ', '100%'), total: true },
        ],
      },
    },
    {
      title: 'Chi tiết công nợ phát sinh',
      table: {
        columns: ['Đối tượng', 'Phát sinh nợ', 'Đã thanh toán', 'Công nợ tăng'], numeric: [1, 2, 3],
        rows: [
          { cells: cells('Khách hàng', '5.200.000 đ', '2.000.000 đ', cell('3.200.000 đ', 'bad')) },
          { cells: cells('Nhà cung cấp', '0 đ', '0 đ', cell('0 đ', 'good')) },
          { cells: cells('Tổng', '5.200.000 đ', '2.000.000 đ', cell('3.200.000 đ', 'bad')), total: true },
        ],
      },
    },
  ],
  details: {
    sales: {
      title: 'Chi tiết doanh thu bán hàng',
      blocks: [
        { section: 'Tóm tắt', kind: 'pairs', rows: [
          { label: 'Tổng tiền bán hàng', value: '12.650.000 đ' },
          { label: 'Trả hàng', value: '-300.000 đ', tone: 'bad' },
          { label: 'Doanh thu bán hàng', value: '12.450.000 đ', tone: 'good' },
        ] },
        { section: 'Giao dịch cấu thành', kind: 'table', table: {
          columns: [], numeric: [],
          rows: [
            { cells: cells('SO000123', '520.000 đ') },
            { cells: cells('SO000124', '320.000 đ') },
            { cells: cells('RT000021', cell('-150.000 đ', 'bad')) },
          ],
        } },
      ],
    },
    cash: { title: 'Chi tiết thực thu tiền', blocks: [{ kind: 'table', table: paymentTable }] },
    debt: {
      title: 'Chi tiết công nợ phát sinh',
      blocks: [{ kind: 'table', table: {
        columns: ['Đối tượng', 'Phát sinh', 'Đã thanh toán', 'Tăng nợ'], numeric: [],
        rows: [
          { cells: cells('Khách hàng', '5.200.000 đ', '2.000.000 đ', cell('3.200.000 đ', 'bad')) },
          { cells: cells('Nhà cung cấp', '0 đ', '0 đ', cell('0 đ', 'good')) },
        ],
      } }],
    },
    cogs: {
      title: 'Chi tiết giá vốn hàng bán',
      blocks: [
        { kind: 'note', tone: 'warn', text: 'Giá vốn được tính theo phương pháp Bình quân gia quyền và phản ánh giá vốn tại thời điểm phát sinh từng giao dịch.' },
        { kind: 'pairs', rows: [
          { label: 'Bán hàng', value: '7.350.000 đ' },
          { label: 'Trả hàng', value: '-30.000 đ', tone: 'bad' },
          { label: 'Tổng', value: '7.320.000 đ' },
        ] },
      ],
    },
    profit: {
      title: 'Chi tiết lợi nhuận ước tính',
      blocks: [
        { kind: 'pairs', rows: [
          { label: 'Doanh thu thuần', value: '12.450.000 đ' },
          { label: 'Giá vốn hàng bán', value: '7.320.000 đ' },
          { label: 'Lợi nhuận ước tính', value: '5.130.000 đ', tone: 'good' },
          { label: 'Tỷ lệ lợi nhuận', value: '41.2%' },
        ] },
        { kind: 'note', tone: 'info', text: 'Lợi nhuận ước tính chỉ mang tính tham khảo và phụ thuộc vào độ tin cậy của giá vốn.' },
      ],
    },
    invoice: {
      title: 'Chi tiết số lượng hóa đơn',
      blocks: [{ section: '48 hóa đơn', kind: 'text', text: 'Bao gồm các hóa đơn bán hàng đã hoàn tất trong ngày, sau khi loại trừ giao dịch bị hủy.' }],
    },
  },
  explains: {
    sales: { title: 'Giải thích Doanh thu bán hàng', definition: 'Doanh thu bán hàng là tổng tiền của các hóa đơn bán hàng đã hoàn tất trong ngày, trừ tiền trả hàng.', formula: { left: '12.650.000 đ', op: '−', right: '300.000 đ', result: '12.450.000 đ' } },
    // The reference prints "−" for every formula; collected money is a sum, so this one reads "+".
    cash: { title: 'Giải thích Thực thu tiền', definition: 'Thực thu là tổng số tiền thực tế đã nhận qua các phương thức thanh toán.', formula: { left: '5.200.000 đ', op: '+', right: '4.600.000 đ', result: '9.800.000 đ' } },
    debt: { title: 'Giải thích Công nợ phát sinh', definition: 'Công nợ phát sinh là phần giá trị giao dịch chưa được thanh toán trong ngày.', formula: { left: '5.200.000 đ', op: '−', right: '2.550.000 đ', result: '2.650.000 đ' } },
    cogs: { title: 'Giải thích Giá vốn', definition: 'Giá vốn phản ánh chi phí của hàng hóa đã bán theo phương pháp giá vốn được cấu hình.', formula: { left: '7.350.000 đ', op: '−', right: '30.000 đ', result: '7.320.000 đ' } },
    profit: { title: 'Giải thích Lợi nhuận ước tính', definition: 'Lợi nhuận ước tính = Doanh thu bán hàng − Giá vốn hàng bán.', formula: { left: '12.450.000 đ', op: '−', right: '7.320.000 đ', result: '5.130.000 đ' } },
    invoice: { title: 'Giải thích Số lượng hóa đơn', definition: 'Số hóa đơn là số giao dịch bán hàng đã hoàn tất trong ngày.', formula: { left: '48', op: '−', right: '0', result: '48' } },
  },
  scope: '00:00 đến giờ đóng ngày, theo múi giờ cửa hàng.',
  scopeNote: 'Số liệu được chốt tại thời điểm đóng ngày và không thay đổi bởi giao dịch phát sinh sau giờ đóng.',
}
