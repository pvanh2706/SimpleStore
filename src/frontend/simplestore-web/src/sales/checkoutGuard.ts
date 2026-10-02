import { ref } from 'vue'
import type { NavigationGuard } from 'vue-router'

/**
 * True while a live CompleteSale outcome is unknown (completing, checking or retryable). The exact attempt —
 * OperationId and snapshot — lives only in the checkout's memory (RAM-only, D-107), so the checkout must stay
 * mounted until the outcome is known or the exact retry succeeds; otherwise a later Complete would start a new
 * OperationId for the same order (D-107/D-108 F).
 */
export const saleOutcomePending = ref(false)

/** Counts blocked attempts to leave, so the checkout can explain why it stays. */
export const blockedSaleLeaves = ref(0)

/** Whether leaving the live checkout is safe now; a refused leave is recorded for the checkout to explain. */
export function canLeaveSales(): boolean {
  if (!saleOutcomePending.value) return true
  blockedSaleLeaves.value++
  return false
}

/**
 * Router guard keeping `/sales/new` while its CompleteSale outcome is pending: covers every in-app navigation
 * (sidebar, topbar, shortcuts, programmatic pushes and browser Back). A session that has already ended is not
 * held, since the attempt cannot be retried without it and the RAM order is reset with the session.
 */
export function keepPendingSale(isAuthenticated: () => boolean): NavigationGuard {
  return (to, from) => {
    if (from.name !== 'sale-checkout' || to.name === 'sale-checkout' || !isAuthenticated()) return true
    return canLeaveSales()
  }
}
