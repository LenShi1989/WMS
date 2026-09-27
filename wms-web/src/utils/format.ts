/** 數量格式化：去掉無意義的小數 0，並加上千分位。 */
export function formatQty(value: number | null | undefined, fallback = '-'): string {
  if (value === null || value === undefined) return fallback
  const rounded = Math.round(value * 1e6) / 1e6
  return rounded.toLocaleString('zh-TW', { maximumFractionDigits: 6 })
}

export function formatNumber(value: number | null | undefined, fallback = '0'): string {
  if (value === null || value === undefined) return fallback
  return value.toLocaleString('zh-TW')
}

export function formatDateTime(value: string | null | undefined, fallback = '-'): string {
  if (!value) return fallback
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return fallback
  return date.toLocaleString('zh-TW', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', hour12: false,
  })
}

export function formatDate(value: string | null | undefined, fallback = '-'): string {
  if (!value) return fallback
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return fallback
  return date.toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit' })
}

/** 轉成後端可接受的 ISO 字串；日期輸入框給的是 yyyy-MM-dd。 */
export function toIsoDate(value: string | null | undefined): string | undefined {
  if (!value) return undefined
  return new Date(`${value}T00:00:00`).toISOString()
}
