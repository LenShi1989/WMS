<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { materialApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatQty } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { Material } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()

const table = useDataTable<Material>(materialApi.query, { categoryId: undefined, isActive: undefined })

const columns: Column[] = [
  { key: 'code', title: '物料編號', width: '140px' },
  { key: 'name', title: '物料名稱' },
  { key: 'specification', title: '規格', hideOnMobile: true },
  { key: 'categoryName', title: '分類', hideOnMobile: true, width: '110px' },
  { key: 'baseUom', title: '單位', width: '80px', align: 'center' },
  { key: 'onHandQuantity', title: '現有庫存', align: 'right', width: '110px' },
  { key: 'safetyStock', title: '安全庫存', align: 'right', width: '110px', hideOnMobile: true },
  { key: 'isActive', title: '狀態', align: 'center', width: '90px' },
  { key: 'actions', title: '操作', align: 'right', width: '120px' },
]

const emptyForm = () => ({
  id: '',
  code: '',
  name: '',
  specification: '',
  categoryId: '',
  baseUom: 'PCS',
  isActive: true,
  safetyStock: 0,
  minStock: 0,
  maxStock: 0,
})

const form = reactive(emptyForm())
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

const confirmOpen = ref(false)
const deleting = ref(false)
const target = ref<Material | null>(null)

function openCreate() {
  Object.assign(form, emptyForm())
  formError.value = ''
  modalOpen.value = true
}

function openEdit(row: Material) {
  Object.assign(form, {
    id: row.id,
    code: row.code,
    name: row.name,
    specification: row.specification ?? '',
    categoryId: row.categoryId ?? '',
    baseUom: row.baseUom,
    isActive: row.isActive,
    safetyStock: row.safetyStock,
    minStock: row.minStock,
    maxStock: row.maxStock,
  })
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  formError.value = ''

  if (form.maxStock > 0 && form.minStock > form.maxStock) {
    formError.value = '最低庫存不可大於最高庫存。'
    return
  }

  saving.value = true
  try {
    const body = {
      code: form.code.trim(),
      name: form.name.trim(),
      specification: form.specification || null,
      categoryId: form.categoryId || null,
      baseUom: form.baseUom,
      isActive: form.isActive,
      safetyStock: Number(form.safetyStock) || 0,
      minStock: Number(form.minStock) || 0,
      maxStock: Number(form.maxStock) || 0,
    }

    if (form.id) {
      await materialApi.update(form.id, body)
      app.notify('物料已更新。')
    } else {
      await materialApi.create(body)
      app.notify('物料已建立。')
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

function confirmDelete(row: Material) {
  target.value = row
  confirmOpen.value = true
}

async function remove() {
  if (!target.value) return
  deleting.value = true
  try {
    await materialApi.remove(target.value.id)
    app.notify('物料已刪除。')
    confirmOpen.value = false
    master.invalidate()
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '刪除失敗。', 'error')
  } finally {
    deleting.value = false
  }
}

onMounted(async () => {
  await Promise.all([table.load(), master.loadCategories(), master.loadUoms()])
})
</script>

<template>
  <div>
    <PageHeader title="物料主檔" description="維護物料基本資料、單位與安全庫存">
      <template #actions>
        <button v-if="auth.can(P.materialCreate)" type="button" class="btn-primary" @click="openCreate">
          <svg class="size-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path d="M10.75 4.75a.75.75 0 0 0-1.5 0v4.5h-4.5a.75.75 0 0 0 0 1.5h4.5v4.5a.75.75 0 0 0 1.5 0v-4.5h4.5a.75.75 0 0 0 0-1.5h-4.5v-4.5Z" />
          </svg>
          新增物料
        </button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋物料編號、名稱或規格" @search="table.onSearchInput">
      <div class="w-44">
        <label class="label" for="f-category">分類</label>
        <select id="f-category" v-model="table.filters.categoryId">
          <option :value="undefined">全部分類</option>
          <option v-for="c in master.categories" :key="c.id" :value="c.id">{{ c.name }}</option>
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
      empty-text="尚未建立任何物料"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-code="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.code }}</span>
      </template>

      <template #cell-specification="{ row }">
        <span class="text-slate-500">{{ row.specification || '-' }}</span>
      </template>

      <template #cell-categoryName="{ row }">
        <span class="text-slate-500">{{ row.categoryName || '-' }}</span>
      </template>

      <template #cell-onHandQuantity="{ row }">
        <span :class="row.safetyStock > 0 && row.onHandQuantity < row.safetyStock ? 'font-medium text-rose-600 dark:text-rose-400' : ''">
          {{ formatQty(row.onHandQuantity) }}
        </span>
      </template>

      <template #cell-safetyStock="{ row }">
        <span class="text-slate-500">{{ formatQty(row.safetyStock) }}</span>
      </template>

      <template #cell-isActive="{ row }">
        <StatusBadge :meta="row.isActive ? { text: '啟用', tone: 'green' } : { text: '停用', tone: 'gray' }" />
      </template>

      <template #cell-actions="{ row }">
        <div class="flex justify-end gap-1">
          <button v-if="auth.can(P.materialUpdate)" type="button" class="btn-ghost btn-sm" @click="openEdit(row)">
            編輯
          </button>
          <button
            v-if="auth.can(P.materialDelete)"
            type="button"
            class="btn-ghost btn-sm !text-rose-600 hover:!bg-rose-50 dark:hover:!bg-rose-500/10"
            @click="confirmDelete(row)"
          >
            刪除
          </button>
        </div>
      </template>
    </DataTable>

    <!-- 新增 / 編輯 -->
    <AppModal v-model="modalOpen" :title="form.id ? '編輯物料' : '新增物料'" :busy="saving">
      <form id="material-form" class="grid grid-cols-1 gap-4 sm:grid-cols-2" @submit.prevent="save">
        <div>
          <label class="label" for="m-code">物料編號 <span class="text-rose-500">*</span></label>
          <input id="m-code" v-model="form.code" type="text" required maxlength="50" placeholder="例如 MAT001" />
        </div>

        <div>
          <label class="label" for="m-name">物料名稱 <span class="text-rose-500">*</span></label>
          <input id="m-name" v-model="form.name" type="text" required maxlength="200" />
        </div>

        <div class="sm:col-span-2">
          <label class="label" for="m-spec">規格</label>
          <input id="m-spec" v-model="form.specification" type="text" maxlength="500" />
        </div>

        <div>
          <label class="label" for="m-category">分類</label>
          <select id="m-category" v-model="form.categoryId">
            <option value="">未分類</option>
            <option v-for="c in master.categories" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="m-uom">基本單位 <span class="text-rose-500">*</span></label>
          <select id="m-uom" v-model="form.baseUom" required>
            <option v-for="u in master.uoms" :key="u.id" :value="u.code">{{ u.code }} － {{ u.name }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="m-safety">安全庫存</label>
          <input id="m-safety" v-model.number="form.safetyStock" type="number" min="0" step="any" />
        </div>

        <div>
          <label class="label" for="m-min">最低庫存</label>
          <input id="m-min" v-model.number="form.minStock" type="number" min="0" step="any" />
        </div>

        <div>
          <label class="label" for="m-max">最高庫存</label>
          <input id="m-max" v-model.number="form.maxStock" type="number" min="0" step="any" />
        </div>

        <div class="flex items-end">
          <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input v-model="form.isActive" type="checkbox" class="size-4 rounded border-slate-300 text-blue-600" />
            啟用此物料
          </label>
        </div>

        <p v-if="formError" class="sm:col-span-2 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="material-form" class="btn-primary" :disabled="saving">
          {{ saving ? '儲存中…' : '儲存' }}
        </button>
      </template>
    </AppModal>

    <ConfirmDialog
      v-model="confirmOpen"
      title="刪除物料"
      :message="`確定要刪除「${target?.code} ${target?.name}」嗎？\n已有庫存或異動紀錄的物料無法刪除，請改為停用。`"
      confirm-text="確定刪除"
      danger
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
