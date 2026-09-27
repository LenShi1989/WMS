import axios, { AxiosError, type AxiosInstance, type AxiosRequestConfig } from 'axios'
import type { ApiResponse, PagedResult } from '@/types'

const STORAGE_KEY = 'wms.auth'

interface StoredAuth {
  accessToken: string
  refreshToken: string
}

export function readStoredAuth(): StoredAuth | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as StoredAuth
  } catch {
    return null
  }
}

export function writeStoredAuth(auth: StoredAuth | null) {
  if (auth) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
  } else {
    localStorage.removeItem(STORAGE_KEY)
  }
}

/** 後端回傳 success=false 時丟出的錯誤，帶有錯誤碼方便畫面判斷。 */
export class ApiException extends Error {
  code: string
  status: number

  constructor(message: string, code = 'ERROR', status = 400) {
    super(message)
    this.name = 'ApiException'
    this.code = code
    this.status = status
  }
}

const http: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
})

http.interceptors.request.use((config) => {
  const auth = readStoredAuth()
  if (auth?.accessToken) {
    config.headers.Authorization = `Bearer ${auth.accessToken}`
  }
  return config
})

/** Token 過期時自動換發一次，並把等待中的請求重送。 */
let refreshing: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const auth = readStoredAuth()
  if (!auth?.refreshToken) return null

  try {
    const { data } = await axios.post<ApiResponse<{ accessToken: string; refreshToken: string }>>(
      `${http.defaults.baseURL}/auth/refresh`,
      { refreshToken: auth.refreshToken },
      { headers: { 'Content-Type': 'application/json' } },
    )

    if (!data.success || !data.data) return null

    writeStoredAuth({ accessToken: data.data.accessToken, refreshToken: data.data.refreshToken })
    return data.data.accessToken
  } catch {
    return null
  }
}

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse>) => {
    const original = error.config as AxiosRequestConfig & { _retried?: boolean }
    const status = error.response?.status ?? 0

    if (status === 401 && original && !original._retried && !original.url?.includes('/auth/')) {
      original._retried = true
      refreshing ??= refreshAccessToken().finally(() => {
        refreshing = null
      })

      const token = await refreshing
      if (token) {
        original.headers = { ...original.headers, Authorization: `Bearer ${token}` }
        return http.request(original)
      }

      writeStoredAuth(null)
      if (!location.pathname.startsWith('/login')) {
        location.href = `/login?redirect=${encodeURIComponent(location.pathname + location.search)}`
      }
    }

    const payload = error.response?.data
    const first = payload?.errors?.[0]
    throw new ApiException(
      payload?.message || first?.message || error.message || '連線發生問題，請稍後再試。',
      first?.code || 'NETWORK_ERROR',
      status,
    )
  },
)

/** 統一拆封 ApiResponse，失敗時丟 ApiException。 */
async function unwrap<T>(promise: Promise<{ data: ApiResponse<T> }>): Promise<T> {
  const { data } = await promise
  if (!data.success) {
    const first = data.errors?.[0]
    throw new ApiException(data.message || first?.message || '操作失敗。', first?.code || 'ERROR', 400)
  }
  return data.data as T
}

/** 把 undefined / 空字串的查詢參數濾掉，避免送出 ?status=。 */
function clean(params?: Record<string, unknown>) {
  if (!params) return undefined
  return Object.fromEntries(
    Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== ''),
  )
}

export const api = {
  get: <T>(url: string, params?: Record<string, unknown>) =>
    unwrap<T>(http.get<ApiResponse<T>>(url, { params: clean(params) })),

  getPaged: <T>(url: string, params?: Record<string, unknown>) =>
    unwrap<PagedResult<T>>(http.get<ApiResponse<PagedResult<T>>>(url, { params: clean(params) })),

  post: <T>(url: string, body?: unknown) => unwrap<T>(http.post<ApiResponse<T>>(url, body ?? {})),

  put: <T>(url: string, body?: unknown) => unwrap<T>(http.put<ApiResponse<T>>(url, body ?? {})),

  delete: <T>(url: string) => unwrap<T>(http.delete<ApiResponse<T>>(url)),
}

export default http
