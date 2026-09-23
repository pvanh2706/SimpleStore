import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/today' },
    { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
    { path: '/setup', name: 'setup', component: () => import('../views/StoreSetupView.vue') },
    { path: '/products', name: 'products', component: () => import('../views/ProductListView.vue') },
    { path: '/products/new', name: 'product-create', component: () => import('../views/ProductFormView.vue') },
    { path: '/products/:id', name: 'product-detail', component: () => import('../views/ProductDetailView.vue') },
    { path: '/products/:id/edit', name: 'product-edit', component: () => import('../views/ProductFormView.vue') },
    { path: '/import', name: 'product-import', component: () => import('../views/ProductImportView.vue') },
    { path: '/suppliers', name: 'suppliers', component: () => import('../views/SupplierListView.vue') },
    { path: '/purchases', name: 'purchases', component: () => import('../views/PurchaseListView.vue') },
    { path: '/purchases/new', name: 'purchase-create', component: () => import('../views/PurchaseFormView.vue') },
    { path: '/purchases/:id', name: 'purchase-detail', component: () => import('../views/PurchaseDetailView.vue') },
    { path: '/purchases/:id/edit', name: 'purchase-edit', component: () => import('../views/PurchaseFormView.vue') },
    { path: '/sales', name: 'sales', component: () => import('../views/SaleListView.vue') },
    { path: '/sales/new', name: 'sale-checkout', component: () => import('../views/SaleCheckoutView.vue') },
    { path: '/sales/:id', name: 'sale-detail', component: () => import('../views/SaleDetailView.vue') },
    { path: '/sales/:id/return', name: 'sale-return', component: () => import('../views/ReturnCreateView.vue'), meta: { ownerOnly: true } },
    { path: '/returns/:id', name: 'return-detail', component: () => import('../views/ReturnDetailView.vue'), meta: { ownerOnly: true } },
    { path: '/customers/debts', name: 'customer-debts', component: () => import('../views/DebtManagementView.vue'), props: { kind: 'customer' } },
    { path: '/suppliers/debts', name: 'supplier-debts', component: () => import('../views/DebtManagementView.vue'), props: { kind: 'supplier' }, meta: { ownerOnly: true } },
    { path: '/reports/end-of-day', name: 'end-of-day', component: () => import('../views/EndOfDayView.vue'), meta: { ownerOnly: true } },
    { path: '/today', name: 'today', component: () => import('../views/TodayView.vue'), meta: { ownerOnly: true } },
    { path: '/settings/operations', name: 'operational-settings', component: () => import('../views/OperationalSettingsView.vue'), meta: { ownerOnly: true } },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  const session = await auth.loadSession()
  if (!session.isAuthenticated && !to.meta.public) return { name: 'login', query: { redirect: to.fullPath } }
  if (session.isAuthenticated && to.name === 'login') {
    const redirect = safeRedirect(to.query.redirect)
    if (redirect) return redirect
    return session.hasStore
      ? { name: session.roles.includes('Owner') ? 'today' : 'products' }
      : { name: 'setup' }
  }
  if (session.isAuthenticated && !session.hasStore && to.name !== 'setup') return { name: 'setup' }
  if (session.isAuthenticated && session.hasStore && to.name === 'setup') {
    return { name: session.roles.includes('Owner') ? 'today' : 'products' }
  }
  if (to.meta.ownerOnly && !session.roles.includes('Owner')) return { name: 'products' }
  return true
})

function safeRedirect(value: unknown) {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/login')
    ? value
    : null
}

export default router
