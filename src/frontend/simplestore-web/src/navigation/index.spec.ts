import { describe, expect, it } from 'vitest'
import { isNavigationActive, visibleNavigation } from './index'

function activeIds(path: string, roles: readonly string[] = ['Owner']) {
  return visibleNavigation(roles).filter(item => isNavigationActive(item, path)).map(item => item.id)
}

describe('navigation presentation', () => {
  it('shows all supported Owner destinations, including CSV import and both Settings routes', () => {
    expect(visibleNavigation(['Owner']).map(item => item.to)).toEqual([
      '/today', '/sales/new', '/sales', '/products', '/products?view=inventory', '/import', '/purchases', '/suppliers',
      '/customers/debts', '/suppliers/debts', '/reports/end-of-day', '/day-close',
      '/settings/operations', '/settings/users',
    ])
  })

  it('omits Owner-only entries for Cashier and shows no menu for an unknown role', () => {
    expect(visibleNavigation(['Cashier']).map(item => item.to)).toEqual([
      '/sales/new', '/sales', '/products', '/products?view=inventory', '/customers/debts',
    ])
    expect(visibleNavigation([])).toEqual([])
    expect(visibleNavigation(['Unknown'])).toEqual([])
  })

  it.each(['/products', '/products/new', '/products/123', '/products/123/edit'])(
    'keeps Product navigation active for %s', path => {
      expect(activeIds(path)).toEqual(['products'])
    },
  )

  it('uses the stock view of the supported Product list for Tồn kho', () => {
    expect(activeIds('/products?view=inventory')).toEqual(['inventory'])
    expect(activeIds('/products?view=inventory#stock')).toEqual(['inventory'])
  })

  it.each(['/purchases', '/purchases/new', '/purchases/123', '/purchases/123/edit'])(
    'keeps Purchase navigation active for %s', path => {
      expect(activeIds(path)).toEqual(['purchases'])
    },
  )

  it.each([
    ['/sales/new', 'sale-checkout'],
    ['/sales', 'sale-history'],
    ['/sales/123', 'sale-history'],
    ['/sales/123/return', 'sale-history'],
    ['/returns/456', 'sale-history'],
  ])('assigns %s to %s', (path, item) => {
    expect(activeIds(path)).toEqual([item])
  })

  it('distinguishes sibling destinations and excludes lookalike path prefixes', () => {
    expect(activeIds('/settings/operations')).toEqual(['operational-settings'])
    expect(activeIds('/settings/users')).toEqual(['user-settings'])
    expect(activeIds('/suppliers')).toEqual(['suppliers'])
    expect(activeIds('/suppliers/debts')).toEqual(['customer-debts', 'supplier-debts'])
    expect(activeIds('/import')).toEqual(['product-import'])
    expect(activeIds('/day-close')).toEqual(['day-close'])
    expect(activeIds('/reports/end-of-day')).toEqual(['end-of-day'])
    expect(activeIds('/products-other')).toEqual([])
    expect(activeIds('/purchases-other')).toEqual([])
  })

  it('normalizes query, hash and trailing slash without changing the active item', () => {
    expect(activeIds('/today/attention/sku-1?tab=stock#details')).toEqual(['today'])
    expect(activeIds('/products/')).toEqual(['products'])
    expect(activeIds('/sales/new?barcode=1', ['Cashier'])).toEqual(['sale-checkout'])
  })
})
