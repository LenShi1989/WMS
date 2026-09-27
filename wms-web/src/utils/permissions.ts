/** 與後端 Wms.Application.Common.Permissions 一一對應的權限碼。 */
export const P = {
  materialView: 'MATERIAL_VIEW',
  materialCreate: 'MATERIAL_CREATE',
  materialUpdate: 'MATERIAL_UPDATE',
  materialDelete: 'MATERIAL_DELETE',

  categoryView: 'CATEGORY_VIEW',
  categoryManage: 'CATEGORY_MANAGE',

  uomView: 'UOM_VIEW',
  uomManage: 'UOM_MANAGE',

  barcodeView: 'BARCODE_VIEW',
  barcodeManage: 'BARCODE_MANAGE',

  warehouseView: 'WAREHOUSE_VIEW',
  warehouseManage: 'WAREHOUSE_MANAGE',

  zoneView: 'ZONE_VIEW',
  zoneManage: 'ZONE_MANAGE',

  locationView: 'LOCATION_VIEW',
  locationManage: 'LOCATION_MANAGE',

  inboundView: 'INBOUND_VIEW',
  inboundCreate: 'INBOUND_CREATE',
  inboundReceive: 'INBOUND_RECEIVE',
  inboundComplete: 'INBOUND_COMPLETE',

  putawayView: 'PUTAWAY_VIEW',
  putawayExecute: 'PUTAWAY_EXECUTE',

  outboundView: 'OUTBOUND_VIEW',
  outboundCreate: 'OUTBOUND_CREATE',
  outboundAllocate: 'OUTBOUND_ALLOCATE',
  outboundShip: 'OUTBOUND_SHIP',

  pickingView: 'PICKING_VIEW',
  pickingExecute: 'PICKING_EXECUTE',

  inventoryView: 'INVENTORY_VIEW',
  inventoryAdjust: 'INVENTORY_ADJUST',

  transferView: 'TRANSFER_VIEW',
  transferExecute: 'TRANSFER_EXECUTE',

  stocktakeView: 'STOCKTAKE_VIEW',
  stocktakeCreate: 'STOCKTAKE_CREATE',
  stocktakeCount: 'STOCKTAKE_COUNT',
  stocktakeApprove: 'STOCKTAKE_APPROVE',

  userView: 'USER_VIEW',
  userManage: 'USER_MANAGE',

  roleView: 'ROLE_VIEW',
  roleManage: 'ROLE_MANAGE',

  auditView: 'AUDIT_VIEW',
  dashboardView: 'DASHBOARD_VIEW',
} as const

export type PermissionCode = (typeof P)[keyof typeof P]
