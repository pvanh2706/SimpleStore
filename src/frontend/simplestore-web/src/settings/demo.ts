import { ref } from 'vue'

/** Preview data stays in this browser tab; Store appearance has no persistence contract yet (D-095, D-102). */
export const settingsDemoEnabled = ref(false)

export type PresetId = 'emerald' | 'ocean' | 'indigo' | 'terracotta' | 'slate' | 'darkpos'
export type DisplayMode = 'light' | 'dark' | 'system'
export type Density = 'compact' | 'standard' | 'comfortable'
export type AppearanceToggle = 'receiptLogo' | 'rounded' | 'tabular'

export interface AppearanceState {
  preset: PresetId
  mode: DisplayMode
  density: Density
  receiptLogo: boolean
  rounded: boolean
  tabular: boolean
  storeName: string
}

/** Swatch colour, accent, strong accent and soft accent of each D-095 preset. */
export const presets: Array<{ id: PresetId; label: string; swatch: string; accent: [string, string, string] }> = [
  { id: 'emerald', label: 'Emerald', swatch: '#0b9a68', accent: ['#0b9a68', '#087c56', '#e7f7f0'] },
  { id: 'ocean', label: 'Ocean', swatch: '#347fd3', accent: ['#347fd3', '#2867ad', '#eaf3ff'] },
  { id: 'indigo', label: 'Indigo', swatch: '#6b55c5', accent: ['#6b55c5', '#5540a8', '#f0edff'] },
  { id: 'terracotta', label: 'Terracotta', swatch: '#eb764f', accent: ['#eb764f', '#c75c3b', '#fff0eb'] },
  { id: 'slate', label: 'Slate', swatch: '#677487', accent: ['#677487', '#4f5b6c', '#eef1f5'] },
  { id: 'darkpos', label: 'Dark POS', swatch: '#30343b', accent: ['#30343b', '#22262c', '#eceef0'] },
]

export const modes: Array<{ id: DisplayMode; label: string; icon: string }> = [
  { id: 'light', label: 'Sáng', icon: '☀' },
  { id: 'dark', label: 'Tối', icon: '☾' },
  { id: 'system', label: 'Theo hệ thống', icon: '▣' },
]

export const densities: Array<{ id: Density; label: string; text: string; icon: string; controlHeight: number }> = [
  { id: 'compact', label: 'Gọn', text: 'Hiển thị nhiều nội dung hơn trên màn hình', icon: '☷', controlHeight: 36 },
  { id: 'standard', label: 'Tiêu chuẩn', text: 'Cân bằng giữa thông tin và không gian', icon: '☰', controlHeight: 42 },
  { id: 'comfortable', label: 'Thoải mái', text: 'Khoảng cách rộng rãi, dễ nhìn hơn', icon: '▤', controlHeight: 48 },
]

export const toggles: Array<{ id: AppearanceToggle; label: string; text: string }> = [
  { id: 'receiptLogo', label: 'Hiển thị logo trên hóa đơn', text: 'Logo cửa hàng sẽ xuất hiện ở đầu hóa đơn.' },
  { id: 'rounded', label: 'Bo góc mềm', text: 'Sử dụng góc bo tròn cho các thành phần giao diện.' },
  { id: 'tabular', label: 'Số liệu dùng tabular numbers', text: 'Giúp các con số thẳng hàng, dễ so sánh hơn.' },
]

/** Defaults of TemplateHTML/Setting: Emerald, light, compact, every option on. */
export const referenceAppearance: AppearanceState = {
  preset: 'emerald', mode: 'light', density: 'compact', receiptLogo: true, rounded: true, tabular: true, storeName: 'Tạp hóa Minh Anh',
}

const image = (name: string) => `/settings-demo/${name}.png`
export const loginPhoto = image('login-photo')

/** Illustrative preview content; it never creates sales or prints receipts. */
export const previewNavigation = ['⌂ Hôm nay', '▤ Bán hàng', '▱ Nhập hàng', '◇ Sản phẩm', '▣ Tồn kho', '▥ Công nợ', '⌁ Báo cáo', '⚙ Cài đặt']
export const previewChips = ['Tất cả', 'Đồ uống', 'Bánh kẹo', 'Gia vị', 'Đồ khô', 'Hóa phẩm']
export const previewProducts = [
  { name: 'Coca Cola 330ml', price: '8.000đ', image: image('coca-cola') },
  { name: 'Sting dâu 330ml', price: '7.000đ', image: image('sting') },
  { name: 'Bia Sài Gòn lon', price: '15.000đ', image: image('bia-saigon') },
  { name: 'Nước suối Lavie 500ml', price: '6.000đ', image: image('lavie') },
  { name: 'Mì Hảo Hảo', price: '4.000đ', image: image('hao-hao') },
  { name: 'Dầu ăn Tường An 1L', price: '42.000đ', image: image('dau-an') },
  { name: 'Đường cát trắng 1kg', price: '22.000đ', image: image('duong') },
  { name: 'Nước mắm Nam Ngư', price: '28.000đ', image: image('nuoc-mam') },
  { name: 'Bánh Oreo', price: '12.000đ', image: image('oreo') },
]
export const previewCart = [
  { name: 'Coca Cola 330ml', unit: '8.000đ', total: '16.000đ', image: image('coca-cola') },
  { name: 'Mì Hảo Hảo', unit: '4.000đ', total: '4.000đ', image: image('hao-hao') },
  { name: 'Dầu ăn Tường An 1L', unit: '42.000đ', total: '42.000đ', image: image('dau-an') },
]
/** Address and phone exist only in the reference; Store has no such fields (D-102). */
export const referenceReceiptContact = ['123 Nguyễn Văn Cừ, P. An Hòa, Q. Ninh Kiều, Cần Thơ', 'ĐT: 0901 234 567']
export const previewReceiptLines = [
  { label: 'Coca Cola 330ml ×2', value: '16.000đ' },
  { label: 'Mì Hảo Hảo ×1', value: '4.000đ' },
  { label: 'Dầu ăn Tường An 1L', value: '42.000đ' },
]
