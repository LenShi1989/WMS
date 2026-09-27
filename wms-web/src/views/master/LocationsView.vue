<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { locationApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatQty } from '@/utils/format'
import { locationTypeLabel, zoneTypeLabel } from '@/utils/labels'
import { P } from '@/utils/permissions'
import type { LocationType, WarehouseLocation, Zone } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()

const table = useDataTable<WarehouseLocation>(locationApi.query, {
  warehouseId: undefined,
  zoneId: undefined,
  isActive: undefined,
})

const columns: Column[] = [
  { key: 'code', title: '儲位編號', width: '180px' },
  { key: 'name', title: '名稱' },
  { key: 'warehouseCode', title: '倉庫', width: '100px', hideOnMobile: true },
  { key: 'zoneCode', title: '儲區', width: '140px', hideOnMobile: true },
  { key: 'locationType', title: '類型', width: '100px', hideOnMobile: true },
  { key: 'onHandQuantity', title: '現有庫存', align: 'right', width: '110px' },
  { key: 'isActive', title: '狀態', align: 'center', width: '90px' },
  { key: 'actions', title: '操作', align: 'right', width: '120px' },
]

const locationTypes: LocationType[] = ['SHELF', 'RACK', 'PALLET', 'FLOOR', 'BIN', 'STAGING', 'VIRTUAL']

const filterZones = ref<Zone[]>([])
const formZones = ref<Zone[]>([])

const emptyForm = () => ({
  id: '',
  warehouseId: '',
  zoneId: '',
  code: '',
  name: '',
  locationType: 'SHELF' as LocationType,
  capacity: 0,
  weightLimit: 0,
  isActive: true,
})

const form = reactive(emptyForm())
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

const confirmOpen = ref(false)
const deleting = ref(false)
const target = ref<WarehouseLocation | null>(null)

// 篩選倉庫改變時，同步刷新儲區選項。
watch(
  () => table.filters.warehouseId,
  async (id) => {
    table.filters.zoneId = undefined
    filterZones.value = id ? await master.loadZones(id as string) : []
  },
)

watch(
  () => form.warehouseId,
  async (id, previous) => {
    if (previous !== undefined && id !== previous) form.zoneId = ''
    formZones.value = id ? await master.loadZones(id) : []
  },
)

async function openCreate() {
  Object.assign(form, emptyForm())
  form.warehouseId = (table.filters.warehouseId as string) || master.warehouses[0]?.id || ''
  formError.value = ''
  modalOpen.value = true
}

async function openEdit(row: WarehouseLocation) {
  Object.assign(form, {
    id: row.id,
    warehouseId: row.warehouseId,
    zoneId: row.zoneId ?? '',
    code: row.code,
    name: row.name,
    locationType: row.locationType,
    capacity: row.capacity,
    weightLimit: row.weightLimit,
    isActive: row.isActive,
  })
  formZones.value = await master.loadZones(row.warehouseId)
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    const body = {
      warehouseId: form.warehouseId,
      zoneId: form.zoneId || null,
      code: form.code.trim(),
      name: form.name.trim() || form.code.trim(),
      locationType: form.locationType,
      capacity: Number(form.capacity) || 0,
      weightLimit: Number(form.weightLimit) || 0,
      isActive: form.isActive,
    }
    if (form.id) {
      await locationApi.update(form.id, body)
      app.notify('儲位已更新。')
    } else {
      await locationApi.create(body)
      app.notify('儲位已建立。')
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

function confirmDelete(row: WarehouseLocation) {
  target.value = row
  confirmOpen.value = true
}

async function remove() {
  if (!target.value) return
  deleting.value = true
  try {
    await locationApi.remove(target.value.id)
    app.notify('儲位已刪除。')
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
  await Promise.all([table.load(), master.loadWarehouses()])
})
</script>

<template>
  <div>
    <PageHeader title="儲位" description="倉庫內實際存放物料的位置，例如 WH01-A01-R01-L01">
      <template #actions>
        <button v-if="auth.can(P.locationManage)" type="button" class="btn-primary" @click="openCreate">
          新增儲位
        </button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋儲位編號或名稱" @search="table.onSearchInput">
      <div class="w-44">
        <label class="label" for="f-wh">倉庫</label>
        <select id="f-wh" v-model="table.filters.warehouseId">
          <option :value="undefined">全部倉庫</option>
          <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }} － {{ w.name }}</option>
        </select>
      </div>

      <div class="w-44">
        <label class="label" for="f-zone">儲區</label>
        <select id="f-zone" v-model="table.filters.zoneId" :disabled="!table.filters.warehouseId">
          <option :value="undefined">全部儲區</option>
          <option v-for="z in filterZones" :key="z.id" :value="z.id">
            {{ z.code }} － {{ zoneTypeLabel[z.zoneType] }}
          </option>
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
      empty-text="尚未建立任何儲位"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-code="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.code }}</span>
      </template>

      <template #cell-zoneCode="{ row }">
        <span class="text-slate-500">
          {{ row.zoneCode ? `${row.zoneCode}（${zoneTypeLabel[row.zoneType!] ?? ''}）` : '-' }}
        </span>
      </template>

      <template #cell-locationType="{ row }">
        <span class="text-slate-500">{{ locationTypeLabel[row.locationType] }}</span>
      </template>

      <template #cell-onHandQuantity="{ row }">{{ formatQty(row.onHandQuantity) }}</template>

      <template #cell-isActive="{ row }">
        <StatusBadge :meta="row.isActive ? { text: '啟用', tone: 'green' } : { text: '停用', tone: 'gray' }" />
      </template>

      <template #cell-actions="{ row }">
        <div class="flex justify-end gap-1">
          <button v-if="auth.can(P.locationManage)" type="button" class="btn-ghost btn-sm" @click="openEdit(row)">編輯</button>
          <button
            v-if="auth.can(P.locationManage)"
            type="button"
            class="btn-ghost btn-sm !text-rose-600 hover:!bg-rose-50 dark:hover:!bg-rose-500/10"
            @click="confirmDelete(row)"
          >
            刪除
          </button>
        </div>
      </template>
    </DataTable>

    <AppModal v-model="modalOpen" :title="form.id ? '編輯儲位' : '新增儲位'" :busy="saving">
      <form id="location-form" class="grid grid-cols-1 gap-4 sm:grid-cols-2" @submit.prevent="save">
        <div>
          <label class="label" for="l-wh">倉庫 <span class="text-rose-500">*</span></label>
          <select id="l-wh" v-model="form.warehouseId" required>
            <option value="" disabled>請選擇倉庫</option>
            <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }} － {{ w.name }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="l-zone">儲區</label>
          <select id="l-zone" v-model="form.zoneId">
            <option value="">未指定</option>
            <option v-for="z in formZones" :key="z.id" :value="z.id">
              {{ z.code }} － {{ z.name }}（{{ zoneTypeLabel[z.zoneType] }}）
            </option>
          </select>
        </div>

        <div>
          <label class="label" for="l-code">儲位編號 <span class="text-rose-500">*</span></label>
          <input id="l-code" v-model="form.code" type="text" required maxlength="100" placeholder="WH01-A01-R01-L01" />
        </div>

        <div>
          <label class="label" for="l-name">名稱</label>
          <input id="l-name" v-model="form.name" type="text" maxlength="200" placeholder="留空時同儲位編號" />
        </div>

        <div>
          <label class="label" for="l-type">儲位類型</label>
          <select id="l-type" v-model="form.locationType">
            <option v-for="t in locationTypes" :key="t" :value="t">{{ locationTypeLabel[t] }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="l-capacity">容量</label>
          <input id="l-capacity" v-model.number="form.capacity" type="number" min="0" step="any" />
        </div>

        <div>
          <label class="label" for="l-weight">承重上限（kg）</label>
          <input id="l-weight" v-model.number="form.weightLimit" type="number" min="0" step="any" />
        </div>

        <div class="flex items-end">
          <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input v-model="form.isActive" type="checkbox" class="size-4 rounded border-slate-300 text-blue-600" />
            啟用此儲位
          </label>
        </div>

        <p v-if="formError" class="sm:col-span-2 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="location-form" class="btn-primary" :disabled="saving">
          {{ saving ? '儲存中…' : '儲存' }}
        </button>
      </template>
    </AppModal>

    <ConfirmDialog
      v-model="confirmOpen"
      title="刪除儲位"
      :message="`確定要刪除儲位「${target?.code}」嗎？\n尚有庫存的儲位無法刪除，請改為停用。`"
      confirm-text="確定刪除"
      danger
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
