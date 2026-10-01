import { computed, ref, type ComputedRef, type Ref } from 'vue'
import type { Customer, ProductListItem } from '../api/types'

export type PaymentMethod = 'Cash' | 'Transfer'
/** "Debt" (`Bán nợ`) is Demo / Visual Reference only (D-107); live debt is Total − Actual Payments. */
export type PayMode = PaymentMethod | 'Debt'
export interface PaymentInput { amount: number; method: PaymentMethod }
/** discountPercent is Demo-only preview data; live totals ignore it (D-107). */
export interface CartLine { product: ProductListItem; quantity: number; discountPercent?: number }
export interface SaleOrder {
  number: number
  cart: CartLine[]
  customer: Customer | null
  payments: PaymentInput[]
  payMode: PayMode
  /** Demo only: the sales API has no Sale note, so live Sales never shows or sends it (D-107). */
  note: string
}

export function lineAmount(line: CartLine): number {
  if (!Number.isFinite(line.quantity) || line.quantity <= 0) return 0
  return Math.round(line.quantity * line.product.salePrice * 100) / 100
}

export function lineDiscount(line: CartLine): number {
  return line.discountPercent ? Math.round(lineAmount(line) * line.discountPercent / 100) : 0
}

export function orderTotal(order: SaleOrder): number {
  return order.cart.reduce((sum, line) => sum + lineAmount(line) - lineDiscount(line), 0)
}

export function emptyOrder(number: number): SaleOrder {
  return { number, cart: [], customer: null, payments: [], payMode: 'Cash', note: '' }
}

export interface OrderBook {
  orders: Ref<SaleOrder[]>
  active: ComputedRef<SaleOrder>
  activate: (number: number) => void
  /** Starts another working order, reusing an existing empty one instead of piling them up. */
  create: () => void
  clearActive: () => void
  completeActive: () => void
  reset: () => void
}

/**
 * Working orders held in this browser tab only. Demo uses several to mirror the D-106 rail; live Sales
 * exposes exactly one active order (D-107). Nothing here is persisted or sent beyond CompleteSale.
 */
export function createOrderBook(seed: SaleOrder[] = []): OrderBook {
  const orders = ref<SaleOrder[]>(seed.length ? seed : [emptyOrder(1)])
  const activeNumber = ref(orders.value[0]!.number)
  const active = computed(() => orders.value.find(order => order.number === activeNumber.value) ?? orders.value[0]!)

  function activate(number: number) {
    if (orders.value.some(order => order.number === number)) activeNumber.value = number
  }

  function create() {
    const empty = orders.value.find(order => order.cart.length === 0 && order.number !== activeNumber.value)
    if (empty) {
      activeNumber.value = empty.number
      return
    }
    if (active.value.cart.length === 0) return
    const order = emptyOrder(Math.max(...orders.value.map(item => item.number)) + 1)
    orders.value.push(order)
    activeNumber.value = order.number
  }

  function clearActive() {
    Object.assign(active.value, emptyOrder(active.value.number))
  }

  function completeActive() {
    orders.value = orders.value.filter(order => order.number !== activeNumber.value)
    if (!orders.value.length) orders.value = [emptyOrder(1)]
    activeNumber.value = orders.value[0]!.number
  }

  function reset() {
    orders.value = [emptyOrder(1)]
    activeNumber.value = 1
  }

  return { orders, active, activate, create, clearActive, completeActive, reset }
}

/** The one live working order, kept in memory across checkout remounts in this tab; never persisted. */
export const liveOrderBook = createOrderBook()
