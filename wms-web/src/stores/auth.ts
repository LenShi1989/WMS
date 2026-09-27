import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { authApi } from '@/api'
import { readStoredAuth, writeStoredAuth } from '@/api/client'
import type { UserProfile } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<UserProfile | null>(null)
  const loading = ref(false)
  const initialized = ref(false)

  const isAuthenticated = computed(() => !!user.value)
  const permissions = computed(() => new Set(user.value?.permissions ?? []))

  /** 判斷是否具備任一權限；未登入時一律為 false。 */
  function can(...codes: string[]): boolean {
    if (!user.value) return false
    return codes.some((code) => permissions.value.has(code))
  }

  async function login(username: string, password: string) {
    loading.value = true
    try {
      const result = await authApi.login(username, password)
      writeStoredAuth({ accessToken: result.accessToken, refreshToken: result.refreshToken })
      user.value = result.user
      initialized.value = true
      return result
    } finally {
      loading.value = false
    }
  }

  /** 重新整理頁面後用既有 Token 還原登入狀態。 */
  async function restore() {
    if (initialized.value) return
    initialized.value = true

    if (!readStoredAuth()?.accessToken) return

    try {
      user.value = await authApi.profile()
    } catch {
      writeStoredAuth(null)
      user.value = null
    }
  }

  async function logout() {
    try {
      await authApi.logout()
    } catch {
      // 後端失敗也要讓前端登出，避免使用者卡在已失效的狀態。
    }
    writeStoredAuth(null)
    user.value = null
  }

  return { user, loading, initialized, isAuthenticated, permissions, can, login, restore, logout }
})

export type { UserProfile }
