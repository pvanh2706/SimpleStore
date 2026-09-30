/** Presentation-only navigation. The router and backend remain the authorization boundary. */
export type NavigationGroup = 'overview' | 'sales' | 'catalog' | 'operations' | 'reports' | 'settings'

export interface NavigationItem {
  id: string
  label: string
  to: string
  group: NavigationGroup
}

export const navigationGroups: ReadonlyArray<{ id: NavigationGroup; label: string }> = [
  { id: 'overview', label: 'Tổng quan' },
  { id: 'sales', label: 'Bán hàng' },
  { id: 'catalog', label: 'Sản phẩm' },
  { id: 'operations', label: 'Vận hành' },
  { id: 'reports', label: 'Báo cáo' },
  { id: 'settings', label: 'Cài đặt' },
]

type Role = 'Owner' | 'Cashier'
type NavigationDefinition = NavigationItem & { roles: readonly Role[] }

const bothRoles: readonly Role[] = ['Owner', 'Cashier']
const ownerOnly: readonly Role[] = ['Owner']

const definitions: readonly NavigationDefinition[] = [
  { id: 'today', label: 'Hôm nay', to: '/today', group: 'overview', roles: ownerOnly },
  { id: 'sale-checkout', label: 'Bán hàng', to: '/sales/new', group: 'sales', roles: bothRoles },
  { id: 'sale-history', label: 'Đơn bán', to: '/sales', group: 'sales', roles: bothRoles },
  { id: 'products', label: 'Sản phẩm', to: '/products', group: 'catalog', roles: bothRoles },
  { id: 'inventory', label: 'Tồn kho', to: '/products?view=inventory', group: 'catalog', roles: bothRoles },
  { id: 'product-import', label: 'Nhập từ CSV', to: '/import', group: 'catalog', roles: ownerOnly },
  { id: 'purchases', label: 'Nhập hàng', to: '/purchases', group: 'operations', roles: ownerOnly },
  { id: 'suppliers', label: 'Nhà cung cấp', to: '/suppliers', group: 'operations', roles: ownerOnly },
  { id: 'customer-debts', label: 'Công nợ khách', to: '/customers/debts', group: 'operations', roles: bothRoles },
  { id: 'supplier-debts', label: 'Công nợ NCC', to: '/suppliers/debts', group: 'operations', roles: ownerOnly },
  { id: 'end-of-day', label: 'Cuối ngày', to: '/reports/end-of-day', group: 'reports', roles: ownerOnly },
  { id: 'operational-settings', label: 'Thiết lập', to: '/settings/operations', group: 'settings', roles: ownerOnly },
  { id: 'user-settings', label: 'Nhân viên', to: '/settings/users', group: 'settings', roles: ownerOnly },
]

/** Only mirrors existing supported Owner/Cashier routes for menu presentation. */
export function visibleNavigation(roles: readonly string[]): NavigationItem[] {
  return definitions
    .filter(item => item.roles.some(role => roles.includes(role)))
    .map(({ id, label, to, group }) => ({ id, label, to, group }))
}

function cleanPath(path: string): string {
  const pathname = path.split(/[?#]/, 1)[0] || '/'
  return pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname
}

function includesRoute(path: string, base: string): boolean {
  return path === base || path.startsWith(`${base}/`)
}

function isInventoryView(path: string): boolean {
  return new URLSearchParams(path.split('?', 2)[1]?.split('#', 1)[0] || '').get('view') === 'inventory'
}

/** Explicitly assigns detail/edit flows to their list context, without router-link-active fall-through. */
export function isNavigationActive(item: NavigationItem, path: string): boolean {
  const pathname = cleanPath(path)
  switch (item.id) {
    case 'today':
      return includesRoute(pathname, '/today')
    case 'sale-checkout':
      return includesRoute(pathname, '/sales/new')
    case 'sale-history':
      return (includesRoute(pathname, '/sales') && !includesRoute(pathname, '/sales/new'))
        || includesRoute(pathname, '/returns')
    case 'products':
      return includesRoute(pathname, '/products') && !(pathname === '/products' && isInventoryView(path))
    case 'inventory':
      return pathname === '/products' && isInventoryView(path)
    case 'purchases':
      return includesRoute(pathname, '/purchases')
    default:
      return pathname === item.to
  }
}
