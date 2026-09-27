// 後端 DTO 對應的 TypeScript 型別定義。

// ---------- 共用 ----------
export interface ApiError {
  code: string
  message: string
}

export interface ApiResponse<T = unknown> {
  success: boolean
  data?: T
  message?: string | null
  errors: ApiError[]
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export interface PagedQuery {
  page?: number
  pageSize?: number
  keyword?: string
}

// ---------- 列舉 ----------
export type ZoneType = 'RECEIVING' | 'STORAGE' | 'PICKING' | 'STAGING' | 'SHIPPING' | 'QC' | 'NG'
export type LocationType = 'SHELF' | 'RACK' | 'PALLET' | 'FLOOR' | 'BIN' | 'STAGING' | 'VIRTUAL'
export type InboundSourceType = 'MANUAL' | 'ERP' | 'PURCHASE' | 'RETURN' | 'PRODUCTION'
export type InboundStatus = 'DRAFT' | 'RECEIVING' | 'RECEIVED' | 'PUTAWAY' | 'COMPLETED' | 'CANCELLED'
export type InboundDetailStatus = 'PENDING' | 'PARTIAL' | 'RECEIVED' | 'PUTAWAY' | 'COMPLETED' | 'CANCELLED'
export type TaskStatus = 'PENDING' | 'ASSIGNED' | 'PROCESSING' | 'COMPLETED' | 'CANCELLED'
export type OutboundStatus = 'DRAFT' | 'ALLOCATED' | 'PICKING' | 'PICKED' | 'SHIPPING' | 'SHIPPED' | 'CANCELLED'
export type OutboundDetailStatus =
  | 'PENDING' | 'PARTIAL_ALLOCATED' | 'ALLOCATED' | 'PICKING' | 'PICKED' | 'SHIPPED' | 'CANCELLED'
export type InventoryStatus = 'NORMAL' | 'HOLD' | 'QC' | 'NG' | 'FROZEN'
export type TransactionType =
  | 'RECEIVE' | 'PUTAWAY' | 'PICK' | 'SHIP' | 'TRANSFER'
  | 'ADJUSTMENT' | 'STOCKTAKE' | 'SCRAP' | 'RESERVE' | 'RELEASE'
export type TransferStatus = 'DRAFT' | 'PENDING' | 'COMPLETED' | 'CANCELLED'
export type StocktakeStatus =
  | 'DRAFT' | 'COUNTING' | 'COUNTED' | 'RECOUNT' | 'PENDING_APPROVAL' | 'APPROVED' | 'CANCELLED'
export type StocktakeDetailStatus = 'PENDING' | 'COUNTED' | 'DIFFERENCE' | 'RECOUNT' | 'ADJUSTED'
export type ReferenceType =
  | 'INBOUND_ORDER' | 'PUTAWAY_TASK' | 'OUTBOUND_ORDER' | 'PICK_TASK'
  | 'TRANSFER_ORDER' | 'STOCKTAKE' | 'MANUAL'

// ---------- 認證 ----------
export interface UserProfile {
  id: string
  username: string
  displayName: string
  email?: string
  roles: string[]
  permissions: string[]
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  user: UserProfile
}

// ---------- 主檔 ----------
export interface MaterialCategory {
  id: string
  code: string
  name: string
  description?: string
  isActive: boolean
  materialCount: number
  createdAt: string
}

export interface Uom {
  id: string
  code: string
  name: string
  description?: string
  isActive: boolean
}

export interface MaterialBarcode {
  id: string
  materialId: string
  materialCode?: string
  materialName?: string
  barcode: string
  barcodeType: string
  isPrimary: boolean
  createdAt: string
}

export interface Material {
  id: string
  code: string
  name: string
  specification?: string
  categoryId?: string
  categoryName?: string
  baseUom: string
  isActive: boolean
  safetyStock: number
  minStock: number
  maxStock: number
  onHandQuantity: number
  barcodes: MaterialBarcode[]
  createdAt: string
  updatedAt: string
}

// ---------- 倉儲結構 ----------
export interface Warehouse {
  id: string
  code: string
  name: string
  description?: string
  isActive: boolean
  zoneCount: number
  locationCount: number
  createdAt: string
}

export interface Zone {
  id: string
  warehouseId: string
  warehouseCode?: string
  code: string
  name: string
  zoneType: ZoneType
  isActive: boolean
  locationCount: number
  createdAt: string
}

export interface WarehouseLocation {
  id: string
  warehouseId: string
  warehouseCode?: string
  zoneId?: string
  zoneCode?: string
  zoneType?: ZoneType
  code: string
  name: string
  locationType: LocationType
  capacity: number
  weightLimit: number
  isActive: boolean
  onHandQuantity: number
  createdAt: string
}

// ---------- 庫存 ----------
export interface Inventory {
  id: string
  warehouseId: string
  warehouseCode: string
  locationId: string
  locationCode: string
  materialId: string
  materialCode: string
  materialName: string
  baseUom: string
  quantity: number
  reservedQuantity: number
  availableQuantity: number
  status: InventoryStatus
  updatedAt: string
}

export interface InventorySummary {
  materialId: string
  materialCode: string
  materialName: string
  baseUom: string
  safetyStock: number
  quantity: number
  reservedQuantity: number
  availableQuantity: number
  locationCount: number
  belowSafetyStock: boolean
}

export interface InventoryTransaction {
  id: string
  transactionNo: string
  transactionType: TransactionType
  materialId: string
  materialCode: string
  materialName: string
  warehouseId: string
  warehouseCode: string
  fromLocationId?: string
  fromLocationCode?: string
  toLocationId?: string
  toLocationCode?: string
  quantity: number
  balanceAfter?: number
  referenceType?: ReferenceType
  referenceId?: string
  referenceNo?: string
  operatorId?: string
  operatorName?: string
  remark?: string
  createdAt: string
}

// ---------- 入庫 ----------
export interface InboundDetail {
  id: string
  lineNo: number
  materialId: string
  materialCode: string
  materialName: string
  baseUom: string
  orderedQuantity: number
  receivedQuantity: number
  putawayQuantity: number
  status: InboundDetailStatus
  remark?: string
}

export interface InboundOrder {
  id: string
  orderNo: string
  sourceType: InboundSourceType
  externalOrderNo?: string
  supplierCode?: string
  supplierName?: string
  warehouseId: string
  warehouseCode: string
  warehouseName: string
  status: InboundStatus
  expectedArrivalDate?: string
  receivedAt?: string
  completedAt?: string
  remark?: string
  createdByName?: string
  createdAt: string
  lineCount: number
  totalOrderedQuantity: number
  totalReceivedQuantity: number
  details: InboundDetail[]
}

export interface PutawayTask {
  id: string
  taskNo: string
  inboundDetailId: string
  inboundOrderNo: string
  materialId: string
  materialCode: string
  materialName: string
  baseUom: string
  warehouseId: string
  warehouseCode: string
  quantity: number
  sourceLocationId?: string
  sourceLocationCode?: string
  targetLocationId?: string
  targetLocationCode?: string
  status: TaskStatus
  priority: number
  assignedUserId?: string
  assignedUserName?: string
  startedAt?: string
  completedAt?: string
  createdAt: string
}

// ---------- 出庫 ----------
export interface OutboundDetail {
  id: string
  lineNo: number
  materialId: string
  materialCode: string
  materialName: string
  baseUom: string
  requestedQuantity: number
  allocatedQuantity: number
  pickedQuantity: number
  shippedQuantity: number
  status: OutboundDetailStatus
  remark?: string
}

export interface OutboundOrder {
  id: string
  orderNo: string
  externalOrderNo?: string
  customerCode?: string
  customerName?: string
  warehouseId: string
  warehouseCode: string
  warehouseName: string
  status: OutboundStatus
  priority: number
  requestedShipDate?: string
  shippedAt?: string
  remark?: string
  createdByName?: string
  createdAt: string
  lineCount: number
  totalRequestedQuantity: number
  totalAllocatedQuantity: number
  totalPickedQuantity: number
  details: OutboundDetail[]
}

export interface AllocationLine {
  outboundDetailId: string
  materialCode: string
  requestedQuantity: number
  allocatedQuantity: number
  shortageQuantity: number
}

export interface AllocationResult {
  outboundOrderId: string
  orderNo: string
  status: OutboundStatus
  fullyAllocated: boolean
  createdPickTaskCount: number
  lines: AllocationLine[]
}

export interface PickTask {
  id: string
  taskNo: string
  outboundDetailId: string
  outboundOrderNo: string
  materialId: string
  materialCode: string
  materialName: string
  baseUom: string
  warehouseId: string
  warehouseCode: string
  sourceLocationId: string
  sourceLocationCode: string
  quantity: number
  pickedQuantity: number
  status: TaskStatus
  priority: number
  assignedUserId?: string
  assignedUserName?: string
  startedAt?: string
  completedAt?: string
  createdAt: string
}

// ---------- 移庫 ----------
export interface TransferOrder {
  id: string
  transferNo: string
  warehouseId: string
  warehouseCode: string
  materialId: string
  materialCode: string
  materialName: string
  baseUom: string
  fromLocationId: string
  fromLocationCode: string
  toLocationId: string
  toLocationCode: string
  quantity: number
  status: TransferStatus
  reason?: string
  remark?: string
  createdByName?: string
  executedByName?: string
  executedAt?: string
  createdAt: string
}

// ---------- 盤點 ----------
export interface StocktakeDetail {
  id: string
  locationId: string
  locationCode: string
  materialId: string
  materialCode: string
  materialName: string
  baseUom: string
  systemQuantity: number
  countedQuantity?: number | null
  differenceQuantity: number
  status: StocktakeDetailStatus
  countedByName?: string
  countedAt?: string
  remark?: string
}

export interface Stocktake {
  id: string
  stocktakeNo: string
  warehouseId: string
  warehouseCode: string
  warehouseName: string
  zoneId?: string
  zoneCode?: string
  status: StocktakeStatus
  remark?: string
  startedAt?: string
  completedAt?: string
  createdByName?: string
  approvedByName?: string
  approvedAt?: string
  createdAt: string
  lineCount: number
  countedCount: number
  differenceCount: number
  details: StocktakeDetail[]
}

// ---------- 系統 ----------
export interface RoleBrief {
  id: string
  name: string
}

export interface User {
  id: string
  username: string
  displayName: string
  email?: string
  isActive: boolean
  lastLoginAt?: string
  createdAt: string
  roles: RoleBrief[]
}

export interface Role {
  id: string
  name: string
  description?: string
  isSystem: boolean
  userCount: number
  permissions: string[]
  createdAt: string
}

export interface Permission {
  id: string
  code: string
  name: string
  module: string
  action: string
}

export interface PermissionGroup {
  module: string
  permissions: Permission[]
}

export interface AuditLog {
  id: string
  userId?: string
  username?: string
  action: string
  module: string
  referenceType?: string
  referenceId?: string
  referenceNo?: string
  oldValue?: string
  newValue?: string
  ipAddress?: string
  createdAt: string
}

// ---------- 儀表板 ----------
export interface DashboardSummary {
  inboundToday: number
  outboundToday: number
  inventoryTotal: number
  pendingPutaway: number
  pendingPicking: number
  pendingStocktake: number
  materialCount: number
  warehouseCount: number
  locationCount: number
  belowSafetyStockCount: number
  openInboundOrders: number
  openOutboundOrders: number
}

export interface DashboardTrend {
  date: string
  inbound: number
  outbound: number
}

export interface WarehouseStock {
  warehouseCode: string
  warehouseName: string
  quantity: number
}
