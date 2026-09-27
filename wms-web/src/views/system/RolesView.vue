<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { roleApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { moduleLabel } from '@/utils/labels'
import { P } from '@/utils/permissions'
import type { PermissionGroup, Role } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const table = useDataTable<Role>(roleApi.query)

const groups = ref<PermissionGroup[]>([])

const columns: Column[] = [
  { key: 'name', title: '角色名稱', width: '160px' },
  { key: 'description', title: '說明' },
  { key: 'permissions', title: '權限數', align: 'right', width: '100px' },
  { key: 'userCount', title: '使用者數', align: 'right', width: '100px' },
  { key: 'isSystem', title: '類型', align: 'center', width: '100px' },
  { key: 'actions', title: '操作', align: 'right', width: '120px' },
]

const emptyForm = () => ({ id: '', name: '', description: '', permissionIds: [] as string[], isSystem: false })
const form = reactive(emptyForm())
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

const confirmOpen = ref(false)
const deleting = ref(false)
const target = ref<Role | null>(null)

/** 把權限碼換成 id，因為後端 API 以 id 傳遞。 */
function codesToIds(codes: string[]) {
  const map = new Map(groups.value.flatMap((g) => g.permissions.map((p) => [p.code, p.id] as const)))
  return codes.map((c) => map.get(c)).filter((id): id is string => !!id)
}

function openCreate() {
  Object.assign(form, emptyForm())
  formError.value = ''
  modalOpen.value = true
}

function openEdit(row: Role) {
  Object.assign(form, {
    id: row.id,
    name: row.name,
    description: row.description ?? '',
    permissionIds: codesToIds(row.permissions),
    isSystem: row.isSystem,
  })
  formError.value = ''
  modalOpen.value = true
}

function toggleModule(group: PermissionGroup, checked: boolean) {
  const ids = group.permissions.map((p) => p.id)
  form.permissionIds = checked
    ? [...new Set([...form.permissionIds, ...ids])]
    : form.permissionIds.filter((id) => !ids.includes(id))
}

function isModuleFullySelected(group: PermissionGroup) {
  return group.permissions.every((p) => form.permissionIds.includes(p.id))
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    const body = {
      name: form.name.trim(),
      description: form.description || null,
      permissionIds: form.permissionIds,
    }
    if (form.id) {
      await roleApi.update(form.id, body)
      app.notify('角色已更新。')
    } else {
      await roleApi.create(body)
      app.notify('角色已建立。')
    }
    modalOpen.value = false
    await table.load()
  } catch (e) {
    formError.value = e instanceof ApiException ? e.message : '儲存失敗。'
  } finally {
    saving.value = false
  }
}

function confirmDelete(row: Role) {
  target.value = row
  confirmOpen.value = true
}

async function remove() {
  if (!target.value) return
  deleting.value = true
  try {
    await roleApi.remove(target.value.id)
    app.notify('角色已刪除。')
    confirmOpen.value = false
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '刪除失敗。', 'error')
  } finally {
    deleting.value = false
  }
}

onMounted(async () => {
  await Promise.all([table.load(), roleApi.permissions().then((g) => (groups.value = g))])
})
</script>

<template>
  <div>
    <PageHeader title="角色與權限" description="以角色為單位控管各模組的操作權限">
      <template #actions>
        <button v-if="auth.can(P.roleManage)" type="button" class="btn-primary" @click="openCreate">新增角色</button>
      </template>
    </PageHeader>

    <DataTable
      :columns="columns"
      :rows="table.items.value"
      :loading="table.loading.value"
      :total="table.total.value"
      :page="table.page.value"
      :page-size="table.pageSize.value"
      empty-text="尚未建立任何角色"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-description="{ row }">
        <span class="text-slate-500">{{ row.description || '-' }}</span>
      </template>

      <template #cell-permissions="{ row }">{{ row.permissions.length }}</template>

      <template #cell-isSystem="{ row }">
        <StatusBadge :meta="row.isSystem ? { text: '系統內建', tone: 'purple' } : { text: '自訂', tone: 'gray' }" />
      </template>

      <template #cell-actions="{ row }">
        <div class="flex justify-end gap-1">
          <button v-if="auth.can(P.roleManage)" type="button" class="btn-ghost btn-sm" @click="openEdit(row)">
            {{ row.isSystem ? '檢視權限' : '編輯' }}
          </button>
          <button
            v-if="auth.can(P.roleManage) && !row.isSystem"
            type="button"
            class="btn-ghost btn-sm !text-rose-600 hover:!bg-rose-50 dark:hover:!bg-rose-500/10"
            @click="confirmDelete(row)"
          >
            刪除
          </button>
        </div>
      </template>
    </DataTable>

    <AppModal v-model="modalOpen" :title="form.id ? '編輯角色' : '新增角色'" size="lg" :busy="saving">
      <form id="role-form" class="space-y-5" @submit.prevent="save">
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label class="label" for="ro-name">角色名稱 <span class="text-rose-500">*</span></label>
            <input id="ro-name" v-model="form.name" type="text" required maxlength="50" :disabled="form.isSystem" />
          </div>
          <div>
            <label class="label" for="ro-desc">說明</label>
            <input id="ro-desc" v-model="form.description" type="text" maxlength="200" />
          </div>
        </div>

        <div>
          <p class="label">權限設定（已選 {{ form.permissionIds.length }} 項）</p>

          <div class="max-h-96 space-y-3 overflow-y-auto rounded-lg border border-slate-200 p-3 dark:border-slate-700">
            <div v-for="group in groups" :key="group.module" class="rounded-lg bg-slate-50 p-3 dark:bg-slate-900/40">
              <label class="mb-2 flex cursor-pointer items-center gap-2 text-sm font-medium text-slate-700 dark:text-slate-200">
                <input
                  type="checkbox"
                  class="size-4 rounded border-slate-300 text-blue-600"
                  :checked="isModuleFullySelected(group)"
                  @change="toggleModule(group, ($event.target as HTMLInputElement).checked)"
                />
                {{ moduleLabel[group.module] ?? group.module }}
              </label>

              <div class="grid grid-cols-2 gap-1.5 pl-6 sm:grid-cols-3">
                <label v-for="p in group.permissions" :key="p.id" class="flex cursor-pointer items-center gap-1.5 text-sm">
                  <input v-model="form.permissionIds" type="checkbox" :value="p.id" class="size-3.5 rounded border-slate-300 text-blue-600" />
                  <span class="text-slate-600 dark:text-slate-300">{{ p.name }}</span>
                </label>
              </div>
            </div>
          </div>
        </div>

        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">關閉</button>
        <button type="submit" form="role-form" class="btn-primary" :disabled="saving">
          {{ saving ? '儲存中…' : '儲存' }}
        </button>
      </template>
    </AppModal>

    <ConfirmDialog
      v-model="confirmOpen"
      title="刪除角色"
      :message="`確定要刪除角色「${target?.name}」嗎？\n仍有使用者屬於此角色時無法刪除。`"
      confirm-text="確定刪除"
      danger
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
