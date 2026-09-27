import { api } from './client'
import type {
  AllocationResult, AuditLog, DashboardSummary, DashboardTrend, Inventory, InventorySummary,
  InventoryTransaction, InboundOrder, LoginResponse, Material, MaterialBarcode, MaterialCategory,
  OutboundOrder, PagedResult, PermissionGroup, PickTask, PutawayTask, Role, Stocktake,
  TransferOrder, Uom, User, UserProfile, Warehouse, WarehouseLocation, WarehouseStock, Zone,
} from '@/types'

type Query = Record<string, unknown>

// ---------- 認證 ----------
export const authApi = {
  login: (username: string, password: string) =>
    api.post<LoginResponse>('/auth/login', { username, password }),
  profile: () => api.get<UserProfile>('/auth/profile'),
  logout: () => api.post<void>('/auth/logout'),
  changePassword: (oldPassword: string, newPassword: string) =>
    api.post<void>('/auth/change-password', { oldPassword, newPassword }),
}

// ---------- 物料 ----------
export const materialApi = {
  query: (params?: Query) => api.getPaged<Material>('/materials', params),
  get: (id: string) => api.get<Material>(`/materials/${id}`),
  getByBarcode: (barcode: string) => api.get<Material>(`/materials/by-barcode/${encodeURIComponent(barcode)}`),
  create: (body: unknown) => api.post<Material>('/materials', body),
  update: (id: string, body: unknown) => api.put<Material>(`/materials/${id}`, body),
  remove: (id: string) => api.delete<void>(`/materials/${id}`),
}

export const categoryApi = {
  query: (params?: Query) => api.getPaged<MaterialCategory>('/material-categories', params),
  create: (body: unknown) => api.post<MaterialCategory>('/material-categories', body),
  update: (id: string, body: unknown) => api.put<MaterialCategory>(`/material-categories/${id}`, body),
  remove: (id: string) => api.delete<void>(`/material-categories/${id}`),
}

export const uomApi = {
  query: (params?: Query) => api.getPaged<Uom>('/uoms', params),
  create: (body: unknown) => api.post<Uom>('/uoms', body),
  update: (id: string, body: unknown) => api.put<Uom>(`/uoms/${id}`, body),
  remove: (id: string) => api.delete<void>(`/uoms/${id}`),
}

export const barcodeApi = {
  query: (params?: Query) => api.getPaged<MaterialBarcode>('/barcodes', params),
  create: (body: unknown) => api.post<MaterialBarcode>('/barcodes', body),
  update: (id: string, body: unknown) => api.put<MaterialBarcode>(`/barcodes/${id}`, body),
  remove: (id: string) => api.delete<void>(`/barcodes/${id}`),
}

// ---------- 倉儲結構 ----------
export const warehouseApi = {
  query: (params?: Query) => api.getPaged<Warehouse>('/warehouses', params),
  get: (id: string) => api.get<Warehouse>(`/warehouses/${id}`),
  create: (body: unknown) => api.post<Warehouse>('/warehouses', body),
  update: (id: string, body: unknown) => api.put<Warehouse>(`/warehouses/${id}`, body),
  remove: (id: string) => api.delete<void>(`/warehouses/${id}`),
}

export const zoneApi = {
  query: (params?: Query) => api.getPaged<Zone>('/zones', params),
  create: (body: unknown) => api.post<Zone>('/zones', body),
  update: (id: string, body: unknown) => api.put<Zone>(`/zones/${id}`, body),
  remove: (id: string) => api.delete<void>(`/zones/${id}`),
}

export const locationApi = {
  query: (params?: Query) => api.getPaged<WarehouseLocation>('/locations', params),
  get: (id: string) => api.get<WarehouseLocation>(`/locations/${id}`),
  getByCode: (code: string) => api.get<WarehouseLocation>(`/locations/by-code/${encodeURIComponent(code)}`),
  create: (body: unknown) => api.post<WarehouseLocation>('/locations', body),
  update: (id: string, body: unknown) => api.put<WarehouseLocation>(`/locations/${id}`, body),
  remove: (id: string) => api.delete<void>(`/locations/${id}`),
}

// ---------- 入庫 ----------
export const inboundApi = {
  query: (params?: Query) => api.getPaged<InboundOrder>('/inbound-orders', params),
  get: (id: string) => api.get<InboundOrder>(`/inbound-orders/${id}`),
  create: (body: unknown) => api.post<InboundOrder>('/inbound-orders', body),
  receive: (id: string, body: unknown) => api.post<InboundOrder>(`/inbound-orders/${id}/receive`, body),
  complete: (id: string) => api.post<InboundOrder>(`/inbound-orders/${id}/complete`),
  cancel: (id: string) => api.post<InboundOrder>(`/inbound-orders/${id}/cancel`),
}

export const putawayApi = {
  query: (params?: Query) => api.getPaged<PutawayTask>('/putaway-tasks', params),
  get: (id: string) => api.get<PutawayTask>(`/putaway-tasks/${id}`),
  assign: (id: string, userId: string) => api.post<PutawayTask>(`/putaway-tasks/${id}/assign`, { userId }),
  start: (id: string) => api.post<PutawayTask>(`/putaway-tasks/${id}/start`),
  complete: (id: string, body: unknown) => api.post<PutawayTask>(`/putaway-tasks/${id}/complete`, body),
  cancel: (id: string) => api.post<PutawayTask>(`/putaway-tasks/${id}/cancel`),
}

// ---------- 出庫 ----------
export const outboundApi = {
  query: (params?: Query) => api.getPaged<OutboundOrder>('/outbound-orders', params),
  get: (id: string) => api.get<OutboundOrder>(`/outbound-orders/${id}`),
  create: (body: unknown) => api.post<OutboundOrder>('/outbound-orders', body),
  allocate: (id: string) => api.post<AllocationResult>(`/outbound-orders/${id}/allocate`),
  release: (id: string) => api.post<OutboundOrder>(`/outbound-orders/${id}/release`),
  ship: (id: string, remark?: string) => api.post<OutboundOrder>(`/outbound-orders/${id}/ship`, { remark }),
  cancel: (id: string) => api.post<OutboundOrder>(`/outbound-orders/${id}/cancel`),
}

export const pickingApi = {
  query: (params?: Query) => api.getPaged<PickTask>('/pick-tasks', params),
  get: (id: string) => api.get<PickTask>(`/pick-tasks/${id}`),
  assign: (id: string, userId: string) => api.post<PickTask>(`/pick-tasks/${id}/assign`, { userId }),
  start: (id: string) => api.post<PickTask>(`/pick-tasks/${id}/start`),
  complete: (id: string, pickedQuantity?: number) =>
    api.post<PickTask>(`/pick-tasks/${id}/complete`, { pickedQuantity }),
  cancel: (id: string) => api.post<PickTask>(`/pick-tasks/${id}/cancel`),
}

// ---------- 庫存 ----------
export const inventoryApi = {
  query: (params?: Query) => api.getPaged<Inventory>('/inventory', params),
  summary: (params?: Query) => api.getPaged<InventorySummary>('/inventory/summary', params),
  adjust: (body: unknown) => api.post<Inventory>('/inventory/adjust', body),
  transactions: (params?: Query) => api.getPaged<InventoryTransaction>('/inventory-transactions', params),
}

export const transferApi = {
  query: (params?: Query) => api.getPaged<TransferOrder>('/transfers', params),
  get: (id: string) => api.get<TransferOrder>(`/transfers/${id}`),
  create: (body: unknown) => api.post<TransferOrder>('/transfers', body),
  execute: (id: string) => api.post<TransferOrder>(`/transfers/${id}/execute`),
  cancel: (id: string) => api.post<TransferOrder>(`/transfers/${id}/cancel`),
}

// ---------- 盤點 ----------
export const stocktakeApi = {
  query: (params?: Query) => api.getPaged<Stocktake>('/stocktakes', params),
  get: (id: string) => api.get<Stocktake>(`/stocktakes/${id}`),
  create: (body: unknown) => api.post<Stocktake>('/stocktakes', body),
  start: (id: string) => api.post<Stocktake>(`/stocktakes/${id}/start`),
  count: (id: string, lines: unknown[]) => api.post<Stocktake>(`/stocktakes/${id}/count`, { lines }),
  recount: (id: string, detailIds: string[]) => api.post<Stocktake>(`/stocktakes/${id}/recount`, { detailIds }),
  complete: (id: string) => api.post<Stocktake>(`/stocktakes/${id}/complete`),
  approve: (id: string) => api.post<Stocktake>(`/stocktakes/${id}/approve`),
  cancel: (id: string) => api.post<Stocktake>(`/stocktakes/${id}/cancel`),
}

// ---------- 系統 ----------
export const userApi = {
  query: (params?: Query) => api.getPaged<User>('/users', params),
  get: (id: string) => api.get<User>(`/users/${id}`),
  create: (body: unknown) => api.post<User>('/users', body),
  update: (id: string, body: unknown) => api.put<User>(`/users/${id}`, body),
  resetPassword: (id: string, newPassword: string) =>
    api.post<void>(`/users/${id}/reset-password`, { newPassword }),
  deactivate: (id: string) => api.delete<void>(`/users/${id}`),
}

export const roleApi = {
  query: (params?: Query) => api.getPaged<Role>('/roles', params),
  get: (id: string) => api.get<Role>(`/roles/${id}`),
  permissions: () => api.get<PermissionGroup[]>('/roles/permissions'),
  create: (body: unknown) => api.post<Role>('/roles', body),
  update: (id: string, body: unknown) => api.put<Role>(`/roles/${id}`, body),
  remove: (id: string) => api.delete<void>(`/roles/${id}`),
}

export const auditApi = {
  query: (params?: Query) => api.getPaged<AuditLog>('/audit-logs', params),
}

export const dashboardApi = {
  summary: () => api.get<DashboardSummary>('/dashboard/summary'),
  trend: (days = 7) => api.get<DashboardTrend[]>('/dashboard/trend', { days }),
  warehouseStock: () => api.get<WarehouseStock[]>('/dashboard/warehouse-stock'),
  lowStock: (top = 10) => api.get<InventorySummary[]>('/dashboard/low-stock', { top }),
}

export type { PagedResult }
export { ApiException } from './client'
