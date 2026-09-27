<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { inventoryApi, transferApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatDateTime, formatQty } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { Inventory, TransferOrder, TransferStatus, WarehouseLocation } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()

const table = useDataTable<TransferOrder>(transferApi.query, { warehouseId: undefined, status: undefined })

const columns: Column[] = [
  { key: 'transferNo', title: '移庫單號', width: '160px' },
  { key: 'materialCode', title: '物料' },
  { key: 'route', title: '移動路徑', width: '260px' },
  { key: 'quantity', title: '數量', align: 'right', width: '110px' },
  { key: 'status', title: '狀態', align: 'center', width: '100px' },
  { key: 'executedAt', title: '執行時間', width: '160px', hideOnMobile: true },
  { key: 'actions', title: '操作', align: 'right', width: '130px' },
]

const statuses: TransferStatus[] = ['PENDING', 'COMPLETED', 'CANCELLED']

// ---------- 建立移庫 ----------
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

const sourceInventories = ref<Inventory[]>([])
const targetLocations = ref<WarehouseLocation[]>([])

const form = reactive({
  warehouseId: '',
  inventoryId: '',
  toLocationId: '',
  quantity: null as number | null,
  reason: '',
  executeImmediately: true,
})

const selectedSource = computed(() => sourceInventories.value.find((i) => i.id === form.inventoryId))

watch(
  () => form.warehouseId,
  async (id) => {
    form.inventoryId = ''
    form.toLocationId = ''
    if (!id) {
      sourceInventories.value = []
      targetLocations.value = []
      return
    }
    const [inventory, locations] = await Promise.all([
      inventoryApi.query({ warehouseId: id, onlyInStock: true, pageSize: 200 }),
      master.loadLocations(id),
    ])
    sourceInventories.value = inventory.items.filter((i) => i.availableQuantity > 0)
    targetLocations.value = locations
  },
)

watch(selectedSource, (source) => {
  if (source) form.quantity = source.availableQuantity
})

function openCreate() {
  Object.assign(form, {
    warehouseId: master.warehouses[0]?.id ?? '',
    inventoryId: '',
    toLocationId: '',
    quantity: null,
    reason: '',
    executeImmediately: true,
  })
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  formError.value = ''

  const source = selectedSource.value
  if (!source) {
    formError.value = '請選擇來源庫存。'
    return
  }

  if (!form.toLocationId) {
    formError.value = '請選擇目的儲位。'
    return
  }

  if (form.toLocationId === source.locationId) {
    formError.value = '來源儲位與目的儲位不可相同。'
    return
  }

  const quantity = Number(form.quantity)
  if (!(quantity > 0) || quantity > source.availableQuantity) {
    formError.value = `移庫數量必須介於 0 與可用量 ${formatQty(source.availableQuantity)} 之間。`
    return
  }

  saving.value = true
  try {
    await transferApi.create({
      materialId: source.materialId,
      fromLocationId: source.locationId,
      toLocationId: form.toLocationId,
      quantity,
      reason: form.reason || null,
      executeImmediately: form.executeImmediately,
    })
    app.notify(form.executeImmediately ? '移庫已完成，庫存已轉移。' : '移庫單已建立，待執行。')
    modalOpen.value = false
    await table.load()
  } catch (e) {
    formError.value = e instanceof ApiException ? e.message : '建立移庫失敗。'
  } finally {
    saving.value = false
  }
}

async function execute(row: TransferOrder) {
  try {
    await transferApi.execute(row.id)
    app.notify('移庫已完成，庫存已轉移。')
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '執行失敗。', 'error')
  }
}

async function cancel(row: TransferOrder) {
  try {
    await transferApi.cancel(row.id)
    app.notify('移庫單已取消。')
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '取消失敗。', 'error')
  }
}

onMounted(async () => {
  await Promise.all([table.load(), master.loadWarehouses()])
})
</script>

<template>
  <div>
    <PageHeader title="移庫" description="將庫存從一個儲位搬到另一個儲位，會產生 TRANSFER 異動紀錄">
      <template #actions>
        <button v-if="auth.can(P.transferExecute)" type="button" class="btn-primary" @click="openCreate">
          建立移庫
        </button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋移庫單號、物料或儲位" @search="table.onSearchInput">
      <div class="w-44">
        <label class="label" for="f-wh">倉庫</label>
        <select id="f-wh" v-model="table.filters.warehouseId">
          <option :value="undefined">全部倉庫</option>
          <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }}</option>
        </select>
      </div>

      <div class="w-36">
        <label class="label" for="f-status">狀態</label>
        <select id="f-status" v-model="table.filters.status">
          <option :value="undefined">全部狀態</option>
          <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
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
      empty-text="尚未建立任何移庫單"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-transferNo="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.transferNo }}</span>
      </template>

      <template #cell-materialCode="{ row }">
        <span class="font-mono text-xs">{{ row.materialCode }}</span>
        <span class="ml-2 text-slate-600 dark:text-slate-300">{{ row.materialName }}</span>
      </template>

      <template #cell-route="{ row }">
        <span class="font-mono text-xs text-slate-500">{{ row.fromLocationCode }} → {{ row.toLocationCode }}</span>
      </template>

      <template #cell-quantity="{ row }">{{ formatQty(row.quantity) }} {{ row.baseUom }}</template>

      <template #cell-status="{ row }"><StatusBadge :status="row.status" /></template>

      <template #cell-executedAt="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.executedAt) }}</span>
      </template>

      <template #cell-actions="{ row }">
        <div v-if="auth.can(P.transferExecute) && row.status === 'PENDING'" class="flex justify-end gap-1">
          <button type="button" class="btn-primary btn-sm" @click="execute(row)">執行</button>
          <button type="button" class="btn-ghost btn-sm !text-rose-600" @click="cancel(row)">取消</button>
        </div>
        <span v-else class="text-xs text-slate-400">-</span>
      </template>
    </DataTable>

    <AppModal v-model="modalOpen" title="建立移庫" :busy="saving">
      <form id="transfer-form" class="space-y-4" @submit.prevent="save">
        <div>
          <label class="label" for="t-wh">倉庫 <span class="text-rose-500">*</span></label>
          <select id="t-wh" v-model="form.warehouseId" required>
            <option value="" disabled>請選擇倉庫</option>
            <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }} － {{ w.name }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="t-source">來源庫存 <span class="text-rose-500">*</span></label>
          <select id="t-source" v-model="form.inventoryId" required>
            <option value="" disabled>請選擇有庫存的儲位</option>
            <option v-for="i in sourceInventories" :key="i.id" :value="i.id">
              {{ i.locationCode }} ｜ {{ i.materialCode }} {{ i.materialName }} ｜ 可用 {{ formatQty(i.availableQuantity) }} {{ i.baseUom }}
            </option>
          </select>
          <p v-if="form.warehouseId && sourceInventories.length === 0" class="mt-1 text-xs text-amber-600">
            此倉庫目前沒有可用庫存。
          </p>
        </div>

        <div>
          <label class="label" for="t-target">目的儲位 <span class="text-rose-500">*</span></label>
          <select id="t-target" v-model="form.toLocationId" required>
            <option value="" disabled>請選擇儲位</option>
            <option
              v-for="l in targetLocations"
              :key="l.id"
              :value="l.id"
              :disabled="l.id === selectedSource?.locationId"
            >
              {{ l.code }}（現有 {{ formatQty(l.onHandQuantity) }}）
            </option>
          </select>
        </div>

        <div>
          <label class="label" for="t-qty">移庫數量 <span class="text-rose-500">*</span></label>
          <input
            id="t-qty"
            v-model.number="form.quantity"
            type="number"
            min="0.000001"
            :max="selectedSource?.availableQuantity"
            step="any"
            required
          />
          <p v-if="selectedSource" class="mt-1 text-xs text-slate-500">
            可用量 {{ formatQty(selectedSource.availableQuantity) }} {{ selectedSource.baseUom }}
            （已預留 {{ formatQty(selectedSource.reservedQuantity) }} 不可移動）
          </p>
        </div>

        <div>
          <label class="label" for="t-reason">移庫原因</label>
          <input id="t-reason" v-model="form.reason" type="text" maxlength="200" placeholder="例如：整併儲位、補揀貨區" />
        </div>

        <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
          <input v-model="form.executeImmediately" type="checkbox" class="size-4 rounded border-slate-300 text-blue-600" />
          建立後立即執行移庫
        </label>

        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="transfer-form" class="btn-primary" :disabled="saving">
          {{ saving ? '處理中…' : form.executeImmediately ? '建立並執行' : '建立移庫單' }}
        </button>
      </template>
    </AppModal>
  </div>
</template>
