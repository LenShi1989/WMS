<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { putawayApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatQty } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { PutawayTask, TaskStatus, WarehouseLocation } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()

const table = useDataTable<PutawayTask>(putawayApi.query, { warehouseId: undefined, status: 'PENDING' })

const columns: Column[] = [
  { key: 'taskNo', title: '任務單號', width: '160px' },
  { key: 'inboundOrderNo', title: '入庫單號', width: '160px', hideOnMobile: true },
  { key: 'materialCode', title: '物料' },
  { key: 'quantity', title: '數量', align: 'right', width: '110px' },
  { key: 'sourceLocationCode', title: '來源儲位', width: '150px' },
  { key: 'targetLocationCode', title: '建議儲位', width: '170px' },
  { key: 'status', title: '狀態', align: 'center', width: '100px' },
  { key: 'actions', title: '操作', align: 'right', width: '170px' },
]

const statuses: TaskStatus[] = ['PENDING', 'ASSIGNED', 'PROCESSING', 'COMPLETED', 'CANCELLED']

const storageLocations = ref<WarehouseLocation[]>([])
const completeOpen = ref(false)
const completing = ref(false)
const completeError = ref('')
const current = ref<PutawayTask | null>(null)

const completeForm = reactive({ targetLocationId: '', quantity: null as number | null })

// 掃描儲位條碼快速帶入目標儲位。
const scanCode = ref('')

watch(scanCode, (code) => {
  const matched = storageLocations.value.find((l) => l.code.toUpperCase() === code.trim().toUpperCase())
  if (matched) completeForm.targetLocationId = matched.id
})

async function openComplete(task: PutawayTask) {
  current.value = task
  completeError.value = ''
  scanCode.value = ''
  completeForm.targetLocationId = task.targetLocationId ?? ''
  completeForm.quantity = task.quantity

  storageLocations.value = (await master.loadLocations(task.warehouseId)).filter(
    (l) => l.id !== task.sourceLocationId,
  )

  completeOpen.value = true
}

async function submitComplete() {
  if (!current.value) return

  if (!completeForm.targetLocationId) {
    completeError.value = '請選擇上架的目標儲位。'
    return
  }

  const quantity = Number(completeForm.quantity)
  if (!(quantity > 0) || quantity > current.value.quantity) {
    completeError.value = `上架數量必須介於 0 與 ${formatQty(current.value.quantity)} 之間。`
    return
  }

  completing.value = true
  completeError.value = ''
  try {
    await putawayApi.complete(current.value.id, {
      targetLocationId: completeForm.targetLocationId,
      quantity,
    })
    app.notify('上架完成，庫存已更新。')
    completeOpen.value = false
    await table.load()
  } catch (e) {
    completeError.value = e instanceof ApiException ? e.message : '上架失敗。'
  } finally {
    completing.value = false
  }
}

async function start(task: PutawayTask) {
  try {
    await putawayApi.start(task.id)
    app.notify('任務已開始。')
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '操作失敗。', 'error')
  }
}

async function cancel(task: PutawayTask) {
  try {
    await putawayApi.cancel(task.id)
    app.notify('任務已取消。')
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '操作失敗。', 'error')
  }
}

onMounted(async () => {
  await Promise.all([table.load(), master.loadWarehouses()])
})
</script>

<template>
  <div>
    <PageHeader title="上架作業" description="把收貨區的貨品搬到儲存儲位，完成後庫存才會進入正式儲位" />

    <FilterBar v-model="table.keyword.value" placeholder="搜尋任務單號或物料" @search="table.onSearchInput">
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
      empty-text="目前沒有符合條件的上架任務"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-taskNo="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.taskNo }}</span>
      </template>

      <template #cell-inboundOrderNo="{ row }">
        <span class="font-mono text-xs text-slate-500">{{ row.inboundOrderNo }}</span>
      </template>

      <template #cell-materialCode="{ row }">
        <span class="font-mono text-xs">{{ row.materialCode }}</span>
        <span class="ml-2 text-slate-600 dark:text-slate-300">{{ row.materialName }}</span>
      </template>

      <template #cell-quantity="{ row }">{{ formatQty(row.quantity) }} {{ row.baseUom }}</template>

      <template #cell-sourceLocationCode="{ row }">
        <span class="font-mono text-xs text-slate-500">{{ row.sourceLocationCode || '-' }}</span>
      </template>

      <template #cell-targetLocationCode="{ row }">
        <span class="font-mono text-xs" :class="row.targetLocationCode ? 'text-slate-500' : 'text-amber-600'">
          {{ row.targetLocationCode || '待指定' }}
        </span>
      </template>

      <template #cell-status="{ row }"><StatusBadge :status="row.status" /></template>

      <template #cell-actions="{ row }">
        <div v-if="auth.can(P.putawayExecute) && !['COMPLETED', 'CANCELLED'].includes(row.status)" class="flex justify-end gap-1">
          <button v-if="row.status === 'PENDING' || row.status === 'ASSIGNED'" type="button" class="btn-ghost btn-sm" @click="start(row)">
            開始
          </button>
          <button type="button" class="btn-primary btn-sm" @click="openComplete(row)">上架</button>
          <button type="button" class="btn-ghost btn-sm !text-rose-600" @click="cancel(row)">取消</button>
        </div>
        <span v-else class="text-xs text-slate-400">-</span>
      </template>
    </DataTable>

    <AppModal v-model="completeOpen" title="完成上架" size="sm" :busy="completing">
      <form id="putaway-form" class="space-y-4" @submit.prevent="submitComplete">
        <div class="rounded-lg bg-slate-50 p-3 text-sm dark:bg-slate-900/40">
          <p class="font-mono text-xs text-slate-500">{{ current?.taskNo }}</p>
          <p class="mt-1 font-medium text-slate-800 dark:text-slate-100">
            {{ current?.materialCode }} － {{ current?.materialName }}
          </p>
          <p class="mt-1 text-slate-500">
            來源：<span class="font-mono">{{ current?.sourceLocationCode }}</span>
            · 待上架 {{ formatQty(current?.quantity ?? 0) }} {{ current?.baseUom }}
          </p>
        </div>

        <div>
          <label class="label" for="p-scan">掃描儲位條碼</label>
          <input id="p-scan" v-model="scanCode" type="text" class="font-mono" placeholder="掃描或輸入儲位編號自動帶入" />
        </div>

        <div>
          <label class="label" for="p-target">目標儲位 <span class="text-rose-500">*</span></label>
          <select id="p-target" v-model="completeForm.targetLocationId" required>
            <option value="" disabled>請選擇儲位</option>
            <option v-for="l in storageLocations" :key="l.id" :value="l.id">
              {{ l.code }}（現有 {{ formatQty(l.onHandQuantity) }}）
            </option>
          </select>
        </div>

        <div>
          <label class="label" for="p-qty">上架數量 <span class="text-rose-500">*</span></label>
          <input id="p-qty" v-model.number="completeForm.quantity" type="number" min="0.000001" :max="current?.quantity" step="any" required />
        </div>

        <p v-if="completeError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ completeError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="completing" @click="completeOpen = false">取消</button>
        <button type="submit" form="putaway-form" class="btn-primary" :disabled="completing">
          {{ completing ? '處理中…' : '確認上架' }}
        </button>
      </template>
    </AppModal>
  </div>
</template>
