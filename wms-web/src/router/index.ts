import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import AdminLayout from '@/layouts/AdminLayout.vue'
import { useAuthStore } from '@/stores/auth'
import { P } from '@/utils/permissions'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    /** 進入此頁需要的權限；未填代表只要登入即可。 */
    permission?: string
    public?: boolean
  }
}

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { title: '登入', public: true },
  },
  {
    path: '/',
    component: AdminLayout,
    children: [
      { path: '', redirect: '/dashboard' },
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/views/dashboard/DashboardView.vue'),
        meta: { title: '儀表板', permission: P.dashboardView },
      },

      // ---------- 基礎資料 ----------
      {
        path: 'master/materials',
        name: 'materials',
        component: () => import('@/views/master/MaterialsView.vue'),
        meta: { title: '物料', permission: P.materialView },
      },
      {
        path: 'master/categories',
        name: 'categories',
        component: () => import('@/views/master/CategoriesView.vue'),
        meta: { title: '物料分類', permission: P.categoryView },
      },
      {
        path: 'master/uoms',
        name: 'uoms',
        component: () => import('@/views/master/UomsView.vue'),
        meta: { title: '單位', permission: P.uomView },
      },
      {
        path: 'master/warehouses',
        name: 'warehouses',
        component: () => import('@/views/master/WarehousesView.vue'),
        meta: { title: '倉庫與儲區', permission: P.warehouseView },
      },
      {
        path: 'master/locations',
        name: 'locations',
        component: () => import('@/views/master/LocationsView.vue'),
        meta: { title: '儲位', permission: P.locationView },
      },
      {
        path: 'master/barcodes',
        name: 'barcodes',
        component: () => import('@/views/master/BarcodesView.vue'),
        meta: { title: '條碼', permission: P.barcodeView },
      },

      // ---------- 入庫管理 ----------
      {
        path: 'inbound/orders',
        name: 'inbound-orders',
        component: () => import('@/views/inbound/InboundOrdersView.vue'),
        meta: { title: '入庫單', permission: P.inboundView },
      },
      {
        path: 'inbound/orders/:id',
        name: 'inbound-order-detail',
        component: () => import('@/views/inbound/InboundOrderDetailView.vue'),
        meta: { title: '入庫單明細', permission: P.inboundView },
      },
      {
        path: 'inbound/putaway',
        name: 'putaway',
        component: () => import('@/views/inbound/PutawayView.vue'),
        meta: { title: '上架作業', permission: P.putawayView },
      },

      // ---------- 出庫管理 ----------
      {
        path: 'outbound/orders',
        name: 'outbound-orders',
        component: () => import('@/views/outbound/OutboundOrdersView.vue'),
        meta: { title: '出庫單', permission: P.outboundView },
      },
      {
        path: 'outbound/orders/:id',
        name: 'outbound-order-detail',
        component: () => import('@/views/outbound/OutboundOrderDetailView.vue'),
        meta: { title: '出庫單明細', permission: P.outboundView },
      },
      {
        path: 'outbound/picking',
        name: 'picking',
        component: () => import('@/views/outbound/PickingView.vue'),
        meta: { title: '揀貨作業', permission: P.pickingView },
      },
      {
        path: 'outbound/shipping',
        name: 'shipping',
        component: () => import('@/views/outbound/ShippingView.vue'),
        meta: { title: '出貨作業', permission: P.outboundShip },
      },

      // ---------- 庫存管理 ----------
      {
        path: 'inventory',
        name: 'inventory',
        component: () => import('@/views/inventory/InventoryView.vue'),
        meta: { title: '即時庫存', permission: P.inventoryView },
      },
      {
        path: 'inventory/transactions',
        name: 'inventory-transactions',
        component: () => import('@/views/inventory/TransactionsView.vue'),
        meta: { title: '庫存異動', permission: P.inventoryView },
      },
      {
        path: 'inventory/transfer',
        name: 'transfer',
        component: () => import('@/views/inventory/TransferView.vue'),
        meta: { title: '移庫', permission: P.transferView },
      },

      // ---------- 盤點管理 ----------
      {
        path: 'stocktake',
        name: 'stocktake',
        component: () => import('@/views/stocktake/StocktakesView.vue'),
        meta: { title: '盤點', permission: P.stocktakeView },
      },
      {
        path: 'stocktake/:id',
        name: 'stocktake-detail',
        component: () => import('@/views/stocktake/StocktakeDetailView.vue'),
        meta: { title: '盤點作業', permission: P.stocktakeView },
      },

      // ---------- 系統管理 ----------
      {
        path: 'system/users',
        name: 'users',
        component: () => import('@/views/system/UsersView.vue'),
        meta: { title: '使用者', permission: P.userView },
      },
      {
        path: 'system/roles',
        name: 'roles',
        component: () => import('@/views/system/RolesView.vue'),
        meta: { title: '角色與權限', permission: P.roleView },
      },
      {
        path: 'system/audit-logs',
        name: 'audit-logs',
        component: () => import('@/views/system/AuditLogsView.vue'),
        meta: { title: '操作紀錄', permission: P.auditView },
      },
      {
        path: 'system/profile',
        name: 'profile',
        component: () => import('@/views/system/ProfileView.vue'),
        meta: { title: '個人資料' },
      },
    ],
  },
  {
    path: '/403',
    name: 'forbidden',
    component: () => import('@/views/ErrorView.vue'),
    props: { code: 403, message: '您沒有存取這個頁面的權限。' },
    meta: { title: '權限不足', public: true },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/ErrorView.vue'),
    props: { code: 404, message: '找不到這個頁面。' },
    meta: { title: '找不到頁面', public: true },
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  await auth.restore()

  document.title = to.meta.title ? `${to.meta.title} · WMS` : 'WMS 物料管理系統'

  if (to.meta.public) {
    // 已登入時不需要再看到登入頁。
    if (to.name === 'login' && auth.isAuthenticated) return { name: 'dashboard' }
    return true
  }

  if (!auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.permission && !auth.can(to.meta.permission)) {
    return { name: 'forbidden' }
  }

  return true
})

export default router
