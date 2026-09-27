<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { roleApi, userApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { formatDateTime } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { Role, User } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const table = useDataTable<User>(userApi.query, { isActive: undefined, roleId: undefined })

const roles = ref<Role[]>([])

const columns: Column[] = [
  { key: 'username', title: '帳號', width: '140px' },
  { key: 'displayName', title: '顯示名稱' },
  { key: 'email', title: 'Email', hideOnMobile: true },
  { key: 'roles', title: '角色', width: '200px' },
  { key: 'lastLoginAt', title: '最後登入', width: '160px', hideOnMobile: true },
  { key: 'isActive', title: '狀態', align: 'center', width: '90px' },
  { key: 'actions', title: '操作', align: 'right', width: '190px' },
]

const emptyForm = () => ({
  id: '',
  username: '',
  displayName: '',
  email: '',
  password: '',
  isActive: true,
  roleIds: [] as string[],
})

const form = reactive(emptyForm())
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

const resetOpen = ref(false)
const resetPassword = ref('')
const deactivateOpen = ref(false)
const acting = ref(false)
const target = ref<User | null>(null)

function openCreate() {
  Object.assign(form, emptyForm())
  formError.value = ''
  modalOpen.value = true
}

function openEdit(row: User) {
  Object.assign(form, {
    id: row.id,
    username: row.username,
    displayName: row.displayName,
    email: row.email ?? '',
    password: '',
    isActive: row.isActive,
    roleIds: row.roles.map((r) => r.id),
  })
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  formError.value = ''

  if (!form.id && form.password.length < 8) {
    formError.value = '新增使用者時密碼至少需要 8 個字元。'
    return
  }

  if (form.id && form.password && form.password.length < 8) {
    formError.value = '密碼至少需要 8 個字元。'
    return
  }

  saving.value = true
  try {
    const body = {
      username: form.username.trim(),
      displayName: form.displayName.trim(),
      email: form.email || null,
      password: form.password || null,
      isActive: form.isActive,
      roleIds: form.roleIds,
    }

    if (form.id) {
      await userApi.update(form.id, body)
      app.notify('使用者已更新。')
    } else {
      await userApi.create(body)
      app.notify('使用者已建立。')
    }

    modalOpen.value = false
    await table.load()
  } catch (e) {
    formError.value = e instanceof ApiException ? e.message : '儲存失敗。'
  } finally {
    saving.value = false
  }
}

function openReset(row: User) {
  target.value = row
  resetPassword.value = ''
  resetOpen.value = true
}

async function submitReset() {
  if (!target.value) return
  if (resetPassword.value.length < 8) {
    app.notify('密碼至少需要 8 個字元。', 'warning')
    return
  }

  acting.value = true
  try {
    await userApi.resetPassword(target.value.id, resetPassword.value)
    app.notify(`已重設 ${target.value.username} 的密碼。`)
    resetOpen.value = false
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '重設失敗。', 'error')
  } finally {
    acting.value = false
  }
}

function openDeactivate(row: User) {
  target.value = row
  deactivateOpen.value = true
}

async function deactivate() {
  if (!target.value) return
  acting.value = true
  try {
    await userApi.deactivate(target.value.id)
    app.notify('使用者已停用。')
    deactivateOpen.value = false
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '操作失敗。', 'error')
  } finally {
    acting.value = false
  }
}

onMounted(async () => {
  await table.load()
  if (auth.can(P.roleView)) {
    roles.value = (await roleApi.query({ pageSize: 100 })).items
  }
})
</script>

<template>
  <div>
    <PageHeader title="使用者" description="管理系統帳號與角色指派">
      <template #actions>
        <button v-if="auth.can(P.userManage)" type="button" class="btn-primary" @click="openCreate">
          新增使用者
        </button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋帳號、名稱或 Email" @search="table.onSearchInput">
      <div class="w-44">
        <label class="label" for="f-role">角色</label>
        <select id="f-role" v-model="table.filters.roleId">
          <option :value="undefined">全部角色</option>
          <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }}</option>
        </select>
      </div>

      <div class="w-32">
        <label class="label" for="f-active">狀態</label>
        <select id="f-active" v-model="table.filters.isActive">
          <option :value="undefined">全部</option>
          <option :value="true">啟用</option>
          <option :value="false">停用</option>
        </select>
      </div>
    </FilterBar>

    <DataTable
      :columns="columns"
      :rows="table.items.value"
      :loading="table.loading.value"
      :total="table.total.value"
      :page="table.page.value"
      :page-size="table.pageSize.value"
      empty-text="尚未建立任何使用者"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-username="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.username }}</span>
      </template>

      <template #cell-email="{ row }">
        <span class="text-slate-500">{{ row.email || '-' }}</span>
      </template>

      <template #cell-roles="{ row }">
        <div class="flex flex-wrap gap-1">
          <StatusBadge v-for="r in row.roles" :key="r.id" :meta="{ text: r.name, tone: 'blue' }" />
          <span v-if="row.roles.length === 0" class="text-slate-400">未指派</span>
        </div>
      </template>

      <template #cell-lastLoginAt="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.lastLoginAt) }}</span>
      </template>

      <template #cell-isActive="{ row }">
        <StatusBadge :meta="row.isActive ? { text: '啟用', tone: 'green' } : { text: '停用', tone: 'gray' }" />
      </template>

      <template #cell-actions="{ row }">
        <div v-if="auth.can(P.userManage)" class="flex justify-end gap-1">
          <button type="button" class="btn-ghost btn-sm" @click="openEdit(row)">編輯</button>
          <button type="button" class="btn-ghost btn-sm" @click="openReset(row)">重設密碼</button>
          <button
            v-if="row.isActive"
            type="button"
            class="btn-ghost btn-sm !text-rose-600 hover:!bg-rose-50 dark:hover:!bg-rose-500/10"
            @click="openDeactivate(row)"
          >
            停用
          </button>
        </div>
      </template>
    </DataTable>

    <AppModal v-model="modalOpen" :title="form.id ? '編輯使用者' : '新增使用者'" :busy="saving">
      <form id="user-form" class="grid grid-cols-1 gap-4 sm:grid-cols-2" @submit.prevent="save">
        <div>
          <label class="label" for="u-username">帳號 <span class="text-rose-500">*</span></label>
          <input id="u-username" v-model="form.username" type="text" required maxlength="50" autocomplete="off" />
        </div>

        <div>
          <label class="label" for="u-display">顯示名稱 <span class="text-rose-500">*</span></label>
          <input id="u-display" v-model="form.displayName" type="text" required maxlength="100" />
        </div>

        <div>
          <label class="label" for="u-email">Email</label>
          <input id="u-email" v-model="form.email" type="email" maxlength="200" />
        </div>

        <div>
          <label class="label" for="u-password">
            密碼 <span v-if="!form.id" class="text-rose-500">*</span>
          </label>
          <input
            id="u-password"
            v-model="form.password"
            type="password"
            minlength="8"
            autocomplete="new-password"
            :required="!form.id"
            :placeholder="form.id ? '留空表示不變更' : '至少 8 個字元'"
          />
        </div>

        <div class="sm:col-span-2">
          <p class="label">角色</p>
          <div class="grid grid-cols-2 gap-2 rounded-lg border border-slate-200 p-3 dark:border-slate-700">
            <label v-for="r in roles" :key="r.id" class="flex cursor-pointer items-center gap-2 text-sm">
              <input v-model="form.roleIds" type="checkbox" :value="r.id" class="size-4 rounded border-slate-300 text-blue-600" />
              <span>{{ r.name }}</span>
            </label>
            <p v-if="roles.length === 0" class="col-span-2 text-sm text-slate-400">沒有可指派的角色</p>
          </div>
        </div>

        <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700 sm:col-span-2 dark:text-slate-300">
          <input v-model="form.isActive" type="checkbox" class="size-4 rounded border-slate-300 text-blue-600" />
          啟用此帳號
        </label>

        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 sm:col-span-2 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="user-form" class="btn-primary" :disabled="saving">
          {{ saving ? '儲存中…' : '儲存' }}
        </button>
      </template>
    </AppModal>

    <AppModal v-model="resetOpen" :title="`重設 ${target?.username} 的密碼`" size="sm" :busy="acting">
      <form id="reset-form" class="space-y-4" @submit.prevent="submitReset">
        <div>
          <label class="label" for="r-password">新密碼 <span class="text-rose-500">*</span></label>
          <input id="r-password" v-model="resetPassword" type="password" required minlength="8" autocomplete="new-password" placeholder="至少 8 個字元" />
        </div>
        <p class="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700 dark:bg-amber-500/10 dark:text-amber-300">
          重設後該使用者目前的登入狀態會失效，需要重新登入。
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="acting" @click="resetOpen = false">取消</button>
        <button type="submit" form="reset-form" class="btn-primary" :disabled="acting">
          {{ acting ? '處理中…' : '重設密碼' }}
        </button>
      </template>
    </AppModal>

    <ConfirmDialog
      v-model="deactivateOpen"
      title="停用使用者"
      :message="`確定要停用「${target?.displayName}（${target?.username}）」嗎？\n為保留歷史紀錄，系統採停用而非刪除。`"
      confirm-text="確定停用"
      danger
      :busy="acting"
      @confirm="deactivate"
    />
  </div>
</template>
