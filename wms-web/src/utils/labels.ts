// 各種狀態的中文顯示與配色，集中在這裡讓全站一致。

type Tone = 'gray' | 'blue' | 'green' | 'amber' | 'red' | 'purple' | 'cyan'

export interface LabelMeta {
  text: string
  tone: Tone
}

export const toneClass: Record<Tone, string> = {
  gray: 'bg-slate-100 text-slate-700 ring-slate-200 dark:bg-slate-700/40 dark:text-slate-200 dark:ring-slate-600',
  blue: 'bg-blue-50 text-blue-700 ring-blue-200 dark:bg-blue-500/15 dark:text-blue-300 dark:ring-blue-500/30',
  green: 'bg-emerald-50 text-emerald-700 ring-emerald-200 dark:bg-emerald-500/15 dark:text-emerald-300 dark:ring-emerald-500/30',
  amber: 'bg-amber-50 text-amber-700 ring-amber-200 dark:bg-amber-500/15 dark:text-amber-300 dark:ring-amber-500/30',
  red: 'bg-rose-50 text-rose-700 ring-rose-200 dark:bg-rose-500/15 dark:text-rose-300 dark:ring-rose-500/30',
  purple: 'bg-violet-50 text-violet-700 ring-violet-200 dark:bg-violet-500/15 dark:text-violet-300 dark:ring-violet-500/30',
  cyan: 'bg-cyan-50 text-cyan-700 ring-cyan-200 dark:bg-cyan-500/15 dark:text-cyan-300 dark:ring-cyan-500/30',
}

const dictionary: Record<string, LabelMeta> = {
  // 入庫單
  DRAFT: { text: '草稿', tone: 'gray' },
  RECEIVING: { text: '收貨中', tone: 'blue' },
  RECEIVED: { text: '已收貨', tone: 'cyan' },
  PUTAWAY: { text: '已上架', tone: 'purple' },
  COMPLETED: { text: '已完成', tone: 'green' },
  CANCELLED: { text: '已取消', tone: 'gray' },

  // 明細
  PENDING: { text: '待處理', tone: 'gray' },
  PARTIAL: { text: '部分收貨', tone: 'amber' },

  // 任務
  ASSIGNED: { text: '已指派', tone: 'blue' },
  PROCESSING: { text: '處理中', tone: 'amber' },

  // 出庫單
  ALLOCATED: { text: '已分配', tone: 'cyan' },
  PARTIAL_ALLOCATED: { text: '部分分配', tone: 'amber' },
  PICKING: { text: '揀貨中', tone: 'amber' },
  PICKED: { text: '已揀貨', tone: 'purple' },
  SHIPPING: { text: '出貨中', tone: 'blue' },
  SHIPPED: { text: '已出貨', tone: 'green' },

  // 盤點
  COUNTING: { text: '盤點中', tone: 'amber' },
  COUNTED: { text: '已盤點', tone: 'cyan' },
  RECOUNT: { text: '複盤中', tone: 'amber' },
  PENDING_APPROVAL: { text: '待核准', tone: 'purple' },
  APPROVED: { text: '已核准', tone: 'green' },
  DIFFERENCE: { text: '有差異', tone: 'red' },
  ADJUSTED: { text: '已調整', tone: 'green' },

  // 庫存狀態
  NORMAL: { text: '正常', tone: 'green' },
  HOLD: { text: '暫扣', tone: 'amber' },
  QC: { text: '待檢', tone: 'blue' },
  NG: { text: '不良', tone: 'red' },
  FROZEN: { text: '凍結', tone: 'gray' },
}

export function statusLabel(value?: string | null): LabelMeta {
  if (!value) return { text: '-', tone: 'gray' }
  return dictionary[value] ?? { text: value, tone: 'gray' }
}

export const transactionTypeLabel: Record<string, LabelMeta> = {
  RECEIVE: { text: '收貨', tone: 'green' },
  PUTAWAY: { text: '上架', tone: 'cyan' },
  PICK: { text: '揀貨', tone: 'amber' },
  SHIP: { text: '出貨', tone: 'blue' },
  TRANSFER: { text: '移庫', tone: 'purple' },
  ADJUSTMENT: { text: '調整', tone: 'red' },
  STOCKTAKE: { text: '盤點', tone: 'red' },
  SCRAP: { text: '報廢', tone: 'red' },
  RESERVE: { text: '預留', tone: 'gray' },
  RELEASE: { text: '釋放預留', tone: 'gray' },
}

export const zoneTypeLabel: Record<string, string> = {
  RECEIVING: '收貨區',
  STORAGE: '儲存區',
  PICKING: '揀貨區',
  STAGING: '暫存區',
  SHIPPING: '出貨區',
  QC: '品檢區',
  NG: '不良品區',
}

export const locationTypeLabel: Record<string, string> = {
  SHELF: '層架',
  RACK: '貨架',
  PALLET: '棧板位',
  FLOOR: '地面',
  BIN: '料盒',
  STAGING: '暫存位',
  VIRTUAL: '虛擬儲位',
}

export const sourceTypeLabel: Record<string, string> = {
  MANUAL: '人工建立',
  ERP: 'ERP 拋轉',
  PURCHASE: '採購入庫',
  RETURN: '退貨入庫',
  PRODUCTION: '生產入庫',
}

export const moduleLabel: Record<string, string> = {
  AUTH: '登入',
  MATERIAL: '物料',
  MATERIAL_CATEGORY: '物料分類',
  BARCODE: '條碼',
  WAREHOUSE: '倉庫',
  ZONE: '儲區',
  LOCATION: '儲位',
  INBOUND: '入庫',
  PUTAWAY: '上架',
  OUTBOUND: '出庫',
  PICKING: '揀貨',
  INVENTORY: '庫存',
  TRANSFER: '移庫',
  STOCKTAKE: '盤點',
  USER: '使用者',
  ROLE: '角色',
}

export const actionLabel: Record<string, string> = {
  LOGIN: '登入',
  LOGOUT: '登出',
  CREATE: '建立',
  UPDATE: '修改',
  DELETE: '刪除',
  RECEIVE: '收貨',
  COMPLETE: '完成',
  CANCEL: '取消',
  ASSIGN: '指派',
  START: '開始',
  ALLOCATE: '分配',
  RELEASE: '釋放',
  SHIP: '出貨',
  EXECUTE: '執行',
  ADJUST: '調整',
  COUNT: '盤點',
  RECOUNT: '複盤',
  APPROVE: '核准',
  DEACTIVATE: '停用',
  RESET_PASSWORD: '重設密碼',
  CHANGE_PASSWORD: '變更密碼',
}
