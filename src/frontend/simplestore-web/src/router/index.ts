import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/products' },
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
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  const session = await auth.loadSession()
  if (!session.isAuthenticated && !to.meta.public) return { name: 'login', query: { redirect: to.fullPath } }
  if (session.isAuthenticated && to.name === 'login') return session.hasStore ? { name: 'products' } : { name: 'setup' }
  if (session.isAuthenticated && !session.hasStore && to.name !== 'setup') return { name: 'setup' }
  if (session.isAuthenticated && session.hasStore && to.name === 'setup') return { name: 'products' }
  return true
})

export default router
