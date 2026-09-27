<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { barcodeApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatDateTime } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { MaterialBarcode } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()
const table = useDataTable<MaterialBarcode>(barcodeApi.query, { materialId: undefined })

const columns: Column[] = [
  { key: 'barcode', title: '條碼', width: '200px' },
  { key: 'barcodeType', title: '條碼類型', width: '120px' },
  { key: 'materialCode', title: '物料編號', width: '140px' },
  { key: 'materialName', title: '物料名稱' },
  { key: 'isPrimary', title: '主要條碼', align: 'center', width: '100px' },
  { key: 'createdAt', title: '建立時間', width: '160px', hideOnMobile: true },
  { key: 'actions', title: '操作', align: 'right', width: '120px' },
]

const barcodeTypes = ['CODE128', 'CODE39', 'EAN13', 'EAN8', 'QRCODE', 'DATAMATRIX']

const emptyForm = () => ({ id: '', materialId: '', barcode: '', barcodeType: 'CODE128', isPrimary: false })
const form = reactive(emptyForm())
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

const confirmOpen = ref(false)
const deleting = ref(false)
const target = ref<MaterialBarcode | null>(null)

function openCreate() {
  Object.assign(form, emptyForm())
  form.materialId = (table.filters.materialId as string) || ''
  formError.value = ''
  modalOpen.value = true
}

function openEdit(row: MaterialBarcode) {
  Object.assign(form, {
    id: row.id,
    materialId: row.materialId,
    barcode: row.barcode,
    barcodeType: row.barcodeType,
    isPrimary: row.isPrimary,
  })
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    const body = {
      materialId: form.materialId,
      barcode: form.barcode.trim(),
      barcodeType: form.barcodeType,
      isPrimary: form.isPrimary,
    }
    if (form.id) {
      await barcodeApi.update(form.id, body)
      app.notify('條碼已更新。')
    } else {
      await barcodeApi.create(body)
      app.notify('條碼已建立。')
    }
    modalOpen.value = false
    await table.load()
  } catch (e) {
    formError.value = e instanceof ApiException ? e.message : '儲存失敗。'
  } finally {
    saving.value = false
  }
}

function confirmDelete(row: MaterialBarcode) {
  target.value = row
  confirmOpen.value = true
}

async function remove() {
  if (!target.value) return
  deleting.value = true
  try {
    await barcodeApi.remove(target.value.id)
    app.notify('條碼已刪除。')
    confirmOpen.value = false
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '刪除失敗。', 'error')
  } finally {
    deleting.value = false
  }
}

onMounted(async () => {
  await Promise.all([table.load(), master.loadMaterials()])
})
</script>

<template>
  <div>
    <PageHeader title="物料條碼" description="一個物料可以綁定多組條碼，掃碼作業會以此對應物料">
      <template #actions>
        <button v-if="auth.can(P.barcodeManage)" type="button" class="btn-primary" @click="openCreate">
          新增條碼
        </button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋條碼或物料" @search="table.onSearchInput">
      <div class="w-56">
        <label class="label" for="f-material">物料</label>
        <select id="f-material" v-model="table.filters.materialId">
          <option :value="undefined">全部物料</option>
          <option v-for="m in master.materials" :key="m.id" :value="m.id">{{ m.code }} － {{ m.name }}</option>
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
      empty-text="尚未建立任何條碼"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-barcode="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.barcode }}</span>
      </template>

      <template #cell-materialCode="{ row }">
        <span class="font-mono text-xs">{{ row.materialCode }}</span>
      </template>

      <template #cell-isPrimary="{ row }">
        <StatusBadge v-if="row.isPrimary" :meta="{ text: '主要', tone: 'blue' }" />
        <span v-else class="text-slate-400">-</span>
      </template>

      <template #cell-createdAt="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.createdAt) }}</span>
      </template>

      <template #cell-actions="{ row }">
        <div class="flex justify-end gap-1">
          <button v-if="auth.can(P.barcodeManage)" type="button" class="btn-ghost btn-sm" @click="openEdit(row)">編輯</button>
          <button
            v-if="auth.can(P.barcodeManage)"
            type="button"
            class="btn-ghost btn-sm !text-rose-600 hover:!bg-rose-50 dark:hover:!bg-rose-500/10"
            @click="confirmDelete(row)"
          >
            刪除
          </button>
        </div>
      </template>
    </DataTable>

    <AppModal v-model="modalOpen" :title="form.id ? '編輯條碼' : '新增條碼'" size="sm" :busy="saving">
      <form id="barcode-form" class="space-y-4" @submit.prevent="save">
        <div>
          <label class="label" for="b-material">物料 <span class="text-rose-500">*</span></label>
          <select id="b-material" v-model="form.materialId" required :disabled="!!form.id">
            <option value="" disabled>請選擇物料</option>
            <option v-for="m in master.materials" :key="m.id" :value="m.id">{{ m.code }} － {{ m.name }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="b-code">條碼 <span class="text-rose-500">*</span></label>
          <input id="b-code" v-model="form.barcode" type="text" required maxlength="100" class="font-mono" />
        </div>

        <div>
          <label class="label" for="b-type">條碼類型</label>
          <select id="b-type" v-model="form.barcodeType">
            <option v-for="t in barcodeTypes" :key="t" :value="t">{{ t }}</option>
          </select>
        </div>

        <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
          <input v-model="form.isPrimary" type="checkbox" class="size-4 rounded border-slate-300 text-blue-600" />
          設為此物料的主要條碼
        </label>

        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="barcode-form" class="btn-primary" :disabled="saving">
          {{ saving ? '儲存中…' : '儲存' }}
        </button>
      </template>
    </AppModal>

    <ConfirmDialog
      v-model="confirmOpen"
      title="刪除條碼"
      :message="`確定要刪除條碼「${target?.barcode}」嗎？`"
      confirm-text="確定刪除"
      danger
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
