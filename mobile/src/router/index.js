import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  {
    path: '/login',
    name: 'login',
    component: () => import('../views/LoginView.vue'),
    meta: { guest: true },
  },
  {
    path: '/',
    component: () => import('../components/AppShell.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'home',
        component: () => import('../views/HomeView.vue'),
      },
      {
        path: 'invoices',
        name: 'invoices',
        component: () => import('../views/InvoicesView.vue'),
      },
      {
        path: 'receivables',
        name: 'receivables',
        component: () => import('../views/ReceivablesView.vue'),
      },
      {
        path: 'refill',
        name: 'refill',
        component: () => import('../views/RefillView.vue'),
      },
      {
        path: 'subscription',
        name: 'subscription',
        component: () => import('../views/SubscriptionDetailsView.vue'),
      },
      {
        path: 'payment',
        name: 'payment',
        component: () => import('../views/PaymentView.vue'),
      },
      {
        path: 'payment/notification',
        name: 'payment-notification',
        component: () => import('../views/PaymentNotificationView.vue'),
      },
      {
        path: 'more',
        name: 'more',
        component: () => import('../views/MoreView.vue'),
      },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login' }
  }
  if (to.meta.guest && auth.isAuthenticated) {
    return { name: 'home' }
  }
})

export default router
