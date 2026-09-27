<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { categoryApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatDateTime } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { MaterialCategory } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()
const table = useDataTable<MaterialCategory>(categoryApi.query)

const columns: Column[] = [
  { key: 'code', title: '分類編號', width: '140px' },
  { key: 'name', title: '分類名稱' },
  { key: 'description', title: '說明', hideOnMobile: true },
  { key: 'materialCount', title: '物料數', align: 'right', width: '90px' },
  { key: 'isActive', title: '狀態', align: 'center', width: '90px' },
  { key: 'createdAt', title: '建立時間', width: '160px', hideOnMobile: true },
  { key: 'actions', title: '操作', align: 'right', width: '120px' },
]

const emptyForm = () => ({ id: '', code: '', name: '', description: '', isActive: true })
const form = reactive(emptyForm())
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

const confirmOpen = ref(false)
const deleting = ref(false)
const target = ref<MaterialCategory | null>(null)

function openCreate() {
  Object.assign(form, emptyForm())
  formError.value = ''
  modalOpen.value = true
}

function openEdit(row: MaterialCategory) {
  Object.assign(form, { ...row, description: row.description ?? '' })
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    const body = {
      code: form.code.trim(),
      name: form.name.trim(),
      description: form.description || null,
      isActive: form.isActive,
    }
    if (form.id) {
      await categoryApi.update(form.id, body)
      app.notify('物料分類已更新。')
    } else {
      await categoryApi.create(body)
      app.notify('物料分類已建立。')
    }
    modalOpen.value = false
    master.invalidate()
    await table.load()
  } catch (e) {
    formError.value = e instanceof ApiException ? e.message : '儲存失敗。'
  } finally {
    saving.value = false
  }
}

function confirmDelete(row: MaterialCategory) {
  target.value = row
  confirmOpen.value = true
}

async function remove() {
  if (!target.value) return
  deleting.value = true
  try {
    await categoryApi.remove(target.value.id)
    app.notify('物料分類已刪除。')
    confirmOpen.value = false
    master.invalidate()
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '刪除失敗。', 'error')
  } finally {
    deleting.value = false
  }
}

onMounted(table.load)
</script>

<template>
  <div>
    <PageHeader title="物料分類" description="用於物料的分群與統計">
      <template #actions>
        <button v-if="auth.can(P.categoryManage)" type="button" class="btn-primary" @click="openCreate">
          新增分類
        </button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋分類編號或名稱" @search="table.onSearchInput" />

    <DataTable
      :columns="columns"
      :rows="table.items.value"
      :loading="table.loading.value"
      :total="table.total.value"
      :page="table.page.value"
      :page-size="table.pageSize.value"
      empty-text="尚未建立任何分類"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-code="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.code }}</span>
      </template>

      <template #cell-description="{ row }">
        <span class="text-slate-500">{{ row.description || '-' }}</span>
      </template>

      <template #cell-isActive="{ row }">
        <StatusBadge :meta="row.isActive ? { text: '啟用', tone: 'green' } : { text: '停用', tone: 'gray' }" />
      </template>

      <template #cell-createdAt="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.createdAt) }}</span>
      </template>

      <template #cell-actions="{ row }">
        <div class="flex justify-end gap-1">
          <button v-if="auth.can(P.categoryManage)" type="button" class="btn-ghost btn-sm" @click="openEdit(row)">編輯</button>
          <button
            v-if="auth.can(P.categoryManage)"
            type="button"
            class="btn-ghost btn-sm !text-rose-600 hover:!bg-rose-50 dark:hover:!bg-rose-500/10"
            @click="confirmDelete(row)"
          >
            刪除
          </button>
        </div>
      </template>
    </DataTable>

    <AppModal v-model="modalOpen" :title="form.id ? '編輯分類' : '新增分類'" size="sm" :busy="saving">
      <form id="category-form" class="space-y-4" @submit.prevent="save">
        <div>
          <label class="label" for="c-code">分類編號 <span class="text-rose-500">*</span></label>
          <input id="c-code" v-model="form.code" type="text" required maxlength="50" placeholder="例如 RAW" />
        </div>
        <div>
          <label class="label" for="c-name">分類名稱 <span class="text-rose-500">*</span></label>
          <input id="c-name" v-model="form.name" type="text" required maxlength="200" />
        </div>
        <div>
          <label class="label" for="c-desc">說明</label>
          <textarea id="c-desc" v-model="form.description" rows="2" maxlength="500" />
        </div>
        <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
          <input v-model="form.isActive" type="checkbox" class="size-4 rounded border-slate-300 text-blue-600" />
          啟用此分類
        </label>
        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="category-form" class="btn-primary" :disabled="saving">
          {{ saving ? '儲存中…' : '儲存' }}
        </button>
      </template>
    </AppModal>

    <ConfirmDialog
      v-model="confirmOpen"
      title="刪除分類"
      :message="`確定要刪除分類「${target?.name}」嗎？`"
      confirm-text="確定刪除"
      danger
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
