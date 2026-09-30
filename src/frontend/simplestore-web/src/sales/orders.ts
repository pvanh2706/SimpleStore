import { computed, ref, type ComputedRef, type Ref } from 'vue'
import type { Customer, ProductListItem } from '../api/types'

export type PaymentMethod = 'Cash' | 'Transfer'
/** "Debt" records no payment, so the whole order stays outstanding for the selected customer. */
export type PayMode = PaymentMethod | 'Debt'
export interface PaymentInput { amount: number; method: PaymentMethod }
/** discountPercent is browser-only preview data until the backend supports discounts. */
export interface CartLine { product: ProductListItem; quantity: number; discountPercent?: number }
export interface SaleOrder {
  number: number
  cart: CartLine[]
  customer: Customer | null
  payments: PaymentInput[]
  payMode: PayMode
  /** Not persisted yet: the sales API has no order note. */
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
 * Held orders live only in this browser tab. The sales API still receives exactly one
 * completed order at a time; persisting held orders is a later backend capability.
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

/** Survives the checkout remount after a completed sale, so held orders are not lost. */
export const liveOrderBook = createOrderBook()
