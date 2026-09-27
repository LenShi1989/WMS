import { P } from '@/utils/permissions'

export interface MenuItem {
  title: string
  to: string
  permission?: string
}

export interface MenuGroup {
  title: string
  icon: string
  /** 單一頁面的群組直接連過去，不展開子選單。 */
  to?: string
  permission?: string
  children?: MenuItem[]
}

/** icon 用 heroicons 的 path，畫在 SidebarIcon 元件裡。 */
export const menu: MenuGroup[] = [
  {
    title: '儀表板',
    icon: 'dashboard',
    to: '/dashboard',
    permission: P.dashboardView,
  },
  {
    title: '基礎資料',
    icon: 'database',
    children: [
      { title: '物料', to: '/master/materials', permission: P.materialView },
      { title: '物料分類', to: '/master/categories', permission: P.categoryView },
      { title: '單位', to: '/master/uoms', permission: P.uomView },
      { title: '倉庫與儲區', to: '/master/warehouses', permission: P.warehouseView },
      { title: '儲位', to: '/master/locations', permission: P.locationView },
      { title: '條碼', to: '/master/barcodes', permission: P.barcodeView },
    ],
  },
  {
    title: '入庫管理',
    icon: 'inbound',
    children: [
      { title: '入庫單', to: '/inbound/orders', permission: P.inboundView },
      { title: '上架作業', to: '/inbound/putaway', permission: P.putawayView },
    ],
  },
  {
    title: '出庫管理',
    icon: 'outbound',
    children: [
      { title: '出庫單', to: '/outbound/orders', permission: P.outboundView },
      { title: '揀貨作業', to: '/outbound/picking', permission: P.pickingView },
      { title: '出貨作業', to: '/outbound/shipping', permission: P.outboundShip },
    ],
  },
  {
    title: '庫存管理',
    icon: 'inventory',
    children: [
      { title: '即時庫存', to: '/inventory', permission: P.inventoryView },
      { title: '庫存異動', to: '/inventory/transactions', permission: P.inventoryView },
      { title: '移庫', to: '/inventory/transfer', permission: P.transferView },
    ],
  },
  {
    title: '盤點管理',
    icon: 'clipboard',
    to: '/stocktake',
    permission: P.stocktakeView,
  },
  {
    title: '系統管理',
    icon: 'settings',
    children: [
      { title: '使用者', to: '/system/users', permission: P.userView },
      { title: '角色與權限', to: '/system/roles', permission: P.roleView },
      { title: '操作紀錄', to: '/system/audit-logs', permission: P.auditView },
    ],
  },
]
