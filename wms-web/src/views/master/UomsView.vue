<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { uomApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { P } from '@/utils/permissions'
import type { Uom } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()
const table = useDataTable<Uom>(uomApi.query)

const columns: Column[] = [
  { key: 'code', title: '單位代碼', width: '140px' },
  { key: 'name', title: '單位名稱' },
  { key: 'description', title: '說明', hideOnMobile: true },
  { key: 'isActive', title: '狀態', align: 'center', width: '90px' },
  { key: 'actions', title: '操作', align: 'right', width: '120px' },
]

const emptyForm = () => ({ id: '', code: '', name: '', description: '', isActive: true })
const form = reactive(emptyForm())
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

const confirmOpen = ref(false)
const deleting = ref(false)
const target = ref<Uom | null>(null)

function openCreate() {
  Object.assign(form, emptyForm())
  formError.value = ''
  modalOpen.value = true
}

function openEdit(row: Uom) {
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
      await uomApi.update(form.id, body)
      app.notify('單位已更新。')
    } else {
      await uomApi.create(body)
      app.notify('單位已建立。')
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

function confirmDelete(row: Uom) {
  target.value = row
  confirmOpen.value = true
}

async function remove() {
  if (!target.value) return
  deleting.value = true
  try {
    await uomApi.remove(target.value.id)
    app.notify('單位已刪除。')
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
    <PageHeader title="計量單位" description="物料的基本計量單位">
      <template #actions>
        <button v-if="auth.can(P.uomManage)" type="button" class="btn-primary" @click="openCreate">新增單位</button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋單位代碼或名稱" @search="table.onSearchInput" />

    <DataTable
      :columns="columns"
      :rows="table.items.value"
      :loading="table.loading.value"
      :total="table.total.value"
      :page="table.page.value"
      :page-size="table.pageSize.value"
      empty-text="尚未建立任何單位"
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

      <template #cell-actions="{ row }">
        <div class="flex justify-end gap-1">
          <button v-if="auth.can(P.uomManage)" type="button" class="btn-ghost btn-sm" @click="openEdit(row)">編輯</button>
          <button
            v-if="auth.can(P.uomManage)"
            type="button"
            class="btn-ghost btn-sm !text-rose-600 hover:!bg-rose-50 dark:hover:!bg-rose-500/10"
            @click="confirmDelete(row)"
          >
            刪除
          </button>
        </div>
      </template>
    </DataTable>

    <AppModal v-model="modalOpen" :title="form.id ? '編輯單位' : '新增單位'" size="sm" :busy="saving">
      <form id="uom-form" class="space-y-4" @submit.prevent="save">
        <div>
          <label class="label" for="u-code">單位代碼 <span class="text-rose-500">*</span></label>
          <input id="u-code" v-model="form.code" type="text" required maxlength="20" placeholder="例如 PCS" />
        </div>
        <div>
          <label class="label" for="u-name">單位名稱 <span class="text-rose-500">*</span></label>
          <input id="u-name" v-model="form.name" type="text" required maxlength="100" placeholder="例如 個" />
        </div>
        <div>
          <label class="label" for="u-desc">說明</label>
          <input id="u-desc" v-model="form.description" type="text" maxlength="500" />
        </div>
        <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
          <input v-model="form.isActive" type="checkbox" class="size-4 rounded border-slate-300 text-blue-600" />
          啟用此單位
        </label>
        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="uom-form" class="btn-primary" :disabled="saving">
          {{ saving ? '儲存中…' : '儲存' }}
        </button>
      </template>
    </AppModal>

    <ConfirmDialog
      v-model="confirmOpen"
      title="刪除單位"
      :message="`確定要刪除單位「${target?.code}」嗎？`"
      confirm-text="確定刪除"
      danger
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
