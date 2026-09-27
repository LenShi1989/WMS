<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ToastContainer from '@/components/ToastContainer.vue'
import { ApiException } from '@/api/client'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const app = useAppStore()
const route = useRoute()
const router = useRouter()

const username = ref('admin')
const password = ref('a12345678')
const showPassword = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  try {
    await auth.login(username.value.trim(), password.value)
    app.notify(`歡迎回來，${auth.user?.displayName}！`)
    const redirect = route.query.redirect as string | undefined
    router.push(redirect || '/dashboard')
  } catch (e) {
    error.value = e instanceof ApiException ? e.message : '登入失敗，請稍後再試。'
  }
}
</script>

<template>
  <div class="flex min-h-screen">
    <!-- 左側品牌區，小螢幕隱藏 -->
    <div class="relative hidden w-1/2 bg-slate-900 lg:flex lg:flex-col lg:justify-center lg:px-16">
      <div
        class="absolute inset-0 opacity-20"
        style="background-image: radial-gradient(circle at 20% 30%, #2563eb, transparent 45%), radial-gradient(circle at 75% 70%, #0ea5e9, transparent 45%)"
      />
      <div class="relative">
        <div class="mb-8 flex items-center gap-3">
          <div class="flex size-12 items-center justify-center rounded-xl bg-blue-600 text-xl font-bold text-white">
            W
          </div>
          <div>
            <p class="text-lg font-semibold text-white">WMS</p>
            <p class="text-sm text-slate-400">Warehouse Management System</p>
          </div>
        </div>

        <h2 class="text-3xl leading-snug font-semibold text-white">
          從收貨到出貨<br />每一筆庫存異動都有跡可循
        </h2>

        <ul class="mt-8 space-y-3 text-sm text-slate-300">
          <li v-for="feature in [
            '入庫 · 收貨 · 上架任務一條龍',
            '庫存預留與防止超賣機制',
            '揀貨 · 出貨 · 移庫 · 盤點',
            '完整的庫存異動 Ledger 與操作紀錄',
          ]" :key="feature" class="flex items-center gap-2.5">
            <svg class="size-5 shrink-0 text-blue-400" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm3.857-9.809a.75.75 0 0 0-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 1 0-1.06 1.061l2.5 2.5a.75.75 0 0 0 1.137-.089l4-5.5Z" clip-rule="evenodd" />
            </svg>
            {{ feature }}
          </li>
        </ul>
      </div>
    </div>

    <!-- 右側登入表單 -->
    <div class="flex w-full items-center justify-center bg-slate-100 px-4 py-12 lg:w-1/2 dark:bg-slate-900">
      <div class="w-full max-w-sm">
        <div class="mb-8 text-center lg:hidden">
          <div class="mx-auto mb-3 flex size-12 items-center justify-center rounded-xl bg-blue-600 text-xl font-bold text-white">
            W
          </div>
          <p class="text-lg font-semibold text-slate-800 dark:text-white">WMS 物料管理系統</p>
        </div>

        <div class="card p-6 sm:p-8">
          <h1 class="text-xl font-semibold text-slate-800 dark:text-white">登入系統</h1>
          <p class="mt-1 mb-6 text-sm text-slate-500 dark:text-slate-400">請輸入您的帳號與密碼</p>

          <form class="space-y-4" @submit.prevent="submit">
            <div>
              <label class="label" for="username">帳號</label>
              <input id="username" v-model="username" type="text" autocomplete="username" required placeholder="請輸入帳號" />
            </div>

            <div>
              <label class="label" for="password">密碼</label>
              <div class="relative">
                <input
                  id="password"
                  v-model="password"
                  :type="showPassword ? 'text' : 'password'"
                  autocomplete="current-password"
                  required
                  placeholder="請輸入密碼"
                  class="!pr-10"
                />
                <button
                  type="button"
                  class="absolute top-1/2 right-2 -translate-y-1/2 rounded p-1 text-slate-400 transition hover:text-slate-600"
                  :aria-label="showPassword ? '隱藏密碼' : '顯示密碼'"
                  @click="showPassword = !showPassword"
                >
                  <svg v-if="showPassword" class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 0 0 1.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.451 10.451 0 0 1 12 4.5c4.756 0 8.773 3.162 10.065 7.498a10.522 10.522 0 0 1-4.293 5.774M6.228 6.228 3 3m3.228 3.228 3.65 3.65m7.894 7.894L21 21m-3.228-3.228-3.65-3.65m0 0a3 3 0 1 0-4.243-4.243" />
                  </svg>
                  <svg v-else class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
                    <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                  </svg>
                </button>
              </div>
            </div>

            <p
              v-if="error"
              class="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-300"
              role="alert"
            >
              {{ error }}
            </p>

            <button type="submit" class="btn-primary w-full !py-2.5" :disabled="auth.loading">
              <svg v-if="auth.loading" class="size-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
              </svg>
              {{ auth.loading ? '登入中…' : '登入' }}
            </button>
          </form>

          <p class="mt-5 rounded-lg bg-slate-50 px-3 py-2 text-center text-xs text-slate-500 dark:bg-slate-700/40 dark:text-slate-400">
            測試帳號：<span class="font-mono font-medium">admin</span> ／
            密碼：<span class="font-mono font-medium">a12345678</span>
          </p>
        </div>
      </div>
    </div>

    <ToastContainer />
  </div>
</template>
