<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import PageHeader from '@/components/PageHeader.vue'
import { authApi } from '@/api'
import { ApiException } from '@/api/client'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { moduleLabel } from '@/utils/labels'

const app = useAppStore()
const auth = useAuthStore()
const router = useRouter()

const form = reactive({ oldPassword: '', newPassword: '', confirmPassword: '' })
const saving = ref(false)
const error = ref('')

/** 依模組分組顯示目前擁有的權限。 */
const groupedPermissions = computed(() => {
  const groups = new Map<string, string[]>()
  for (const code of auth.user?.permissions ?? []) {
    const [module] = code.split('_')
    const list = groups.get(module) ?? []
    list.push(code)
    groups.set(module, list)
  }
  return [...groups.entries()].map(([module, codes]) => ({ module, codes }))
})

async function changePassword() {
  error.value = ''

  if (form.newPassword.length < 8) {
    error.value = '新密碼至少需要 8 個字元。'
    return
  }

  if (form.newPassword !== form.confirmPassword) {
    error.value = '兩次輸入的新密碼不一致。'
    return
  }

  saving.value = true
  try {
    await authApi.changePassword(form.oldPassword, form.newPassword)
    app.notify('密碼已更新，請重新登入。')
    await auth.logout()
    router.push({ name: 'login' })
  } catch (e) {
    error.value = e instanceof ApiException ? e.message : '變更失敗。'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader title="個人資料" description="檢視帳號資訊與變更密碼" />

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-3">
      <section class="card p-5 lg:col-span-1">
        <div class="mb-4 flex items-center gap-3">
          <span class="flex size-14 items-center justify-center rounded-full bg-blue-600 text-xl font-semibold text-white">
            {{ auth.user?.displayName?.[0] ?? 'U' }}
          </span>
          <div>
            <p class="text-lg font-semibold text-slate-800 dark:text-white">{{ auth.user?.displayName }}</p>
            <p class="font-mono text-xs text-slate-500">{{ auth.user?.username }}</p>
          </div>
        </div>

        <dl class="space-y-3 text-sm">
          <div>
            <dt class="text-slate-500 dark:text-slate-400">Email</dt>
            <dd class="mt-0.5">{{ auth.user?.email || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">角色</dt>
            <dd class="mt-0.5">{{ auth.user?.roles.join('、') || '未指派' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">權限數</dt>
            <dd class="mt-0.5">{{ auth.user?.permissions.length ?? 0 }} 項</dd>
          </div>
        </dl>
      </section>

      <section class="card p-5 lg:col-span-2">
        <h2 class="mb-4 text-sm font-semibold text-slate-700 dark:text-slate-200">變更密碼</h2>

        <form class="max-w-sm space-y-4" @submit.prevent="changePassword">
          <div>
            <label class="label" for="p-old">目前密碼 <span class="text-rose-500">*</span></label>
            <input id="p-old" v-model="form.oldPassword" type="password" required autocomplete="current-password" />
          </div>

          <div>
            <label class="label" for="p-new">新密碼 <span class="text-rose-500">*</span></label>
            <input id="p-new" v-model="form.newPassword" type="password" required minlength="8" autocomplete="new-password" placeholder="至少 8 個字元" />
          </div>

          <div>
            <label class="label" for="p-confirm">確認新密碼 <span class="text-rose-500">*</span></label>
            <input id="p-confirm" v-model="form.confirmPassword" type="password" required minlength="8" autocomplete="new-password" />
          </div>

          <p v-if="error" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
            {{ error }}
          </p>

          <button type="submit" class="btn-primary" :disabled="saving">
            {{ saving ? '處理中…' : '變更密碼' }}
          </button>
        </form>
      </section>

      <section class="card p-5 lg:col-span-3">
        <h2 class="mb-4 text-sm font-semibold text-slate-700 dark:text-slate-200">我的權限</h2>

        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div v-for="group in groupedPermissions" :key="group.module" class="rounded-lg bg-slate-50 p-3 dark:bg-slate-900/40">
            <p class="mb-1.5 text-sm font-medium text-slate-700 dark:text-slate-200">
              {{ moduleLabel[group.module] ?? group.module }}
            </p>
            <ul class="space-y-0.5">
              <li v-for="code in group.codes" :key="code" class="font-mono text-[11px] text-slate-500">{{ code }}</li>
            </ul>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>
