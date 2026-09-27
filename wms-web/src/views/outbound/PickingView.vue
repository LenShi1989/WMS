<script setup lang="ts">
import { onMounted, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { pickingApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatQty } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { PickTask, TaskStatus } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()

const table = useDataTable<PickTask>(pickingApi.query, { warehouseId: undefined, status: 'PENDING' })

const columns: Column[] = [
  { key: 'taskNo', title: '任務單號', width: '160px' },
  { key: 'outboundOrderNo', title: '出庫單號', width: '160px', hideOnMobile: true },
  { key: 'materialCode', title: '物料' },
  { key: 'sourceLocationCode', title: '揀貨儲位', width: '170px' },
  { key: 'quantity', title: '應揀量', align: 'right', width: '110px' },
  { key: 'pickedQuantity', title: '已揀量', align: 'right', width: '100px' },
  { key: 'status', title: '狀態', align: 'center', width: '100px' },
  { key: 'actions', title: '操作', align: 'right', width: '170px' },
]

const statuses: TaskStatus[] = ['PENDING', 'ASSIGNED', 'PROCESSING', 'COMPLETED', 'CANCELLED']

const completeOpen = ref(false)
const completing = ref(false)
const completeError = ref('')
const current = ref<PickTask | null>(null)
const pickedQuantity = ref<number | null>(null)

function openComplete(task: PickTask) {
  current.value = task
  pickedQuantity.value = task.quantity
  completeError.value = ''
  completeOpen.value = true
}

async function submitComplete() {
  if (!current.value) return

  const quantity = Number(pickedQuantity.value)
  if (!(quantity > 0) || quantity > current.value.quantity) {
    completeError.value = `揀貨數量必須介於 0 與 ${formatQty(current.value.quantity)} 之間。`
    return
  }

  completing.value = true
  completeError.value = ''
  try {
    await pickingApi.complete(current.value.id, quantity)
    app.notify(
      quantity < current.value.quantity
        ? '已記錄短揀，未揀出的預留量已釋放。'
        : '揀貨完成，庫存已扣除。',
    )
    completeOpen.value = false
    await table.load()
  } catch (e) {
    completeError.value = e instanceof ApiException ? e.message : '揀貨失敗。'
  } finally {
    completing.value = false
  }
}

async function start(task: PickTask) {
  try {
    await pickingApi.start(task.id)
    app.notify('任務已開始。')
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '操作失敗。', 'error')
  }
}

async function cancel(task: PickTask) {
  try {
    await pickingApi.cancel(task.id)
    app.notify('任務已取消，預留庫存已釋放。')
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
    <PageHeader title="揀貨作業" description="依任務到指定儲位揀出貨品，完成後庫存立即扣除" />

    <FilterBar v-model="table.keyword.value" placeholder="搜尋任務單號、物料或儲位" @search="table.onSearchInput">
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
      empty-text="目前沒有符合條件的揀貨任務"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-taskNo="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.taskNo }}</span>
      </template>

      <template #cell-outboundOrderNo="{ row }">
        <span class="font-mono text-xs text-slate-500">{{ row.outboundOrderNo }}</span>
      </template>

      <template #cell-materialCode="{ row }">
        <span class="font-mono text-xs">{{ row.materialCode }}</span>
        <span class="ml-2 text-slate-600 dark:text-slate-300">{{ row.materialName }}</span>
      </template>

      <template #cell-sourceLocationCode="{ row }">
        <span class="font-mono text-xs font-medium text-blue-600 dark:text-blue-400">{{ row.sourceLocationCode }}</span>
      </template>

      <template #cell-quantity="{ row }">{{ formatQty(row.quantity) }} {{ row.baseUom }}</template>

      <template #cell-pickedQuantity="{ row }">{{ formatQty(row.pickedQuantity) }}</template>

      <template #cell-status="{ row }"><StatusBadge :status="row.status" /></template>

      <template #cell-actions="{ row }">
        <div v-if="auth.can(P.pickingExecute) && !['COMPLETED', 'CANCELLED'].includes(row.status)" class="flex justify-end gap-1">
          <button v-if="row.status === 'PENDING' || row.status === 'ASSIGNED'" type="button" class="btn-ghost btn-sm" @click="start(row)">
            開始
          </button>
          <button type="button" class="btn-primary btn-sm" @click="openComplete(row)">揀貨</button>
          <button type="button" class="btn-ghost btn-sm !text-rose-600" @click="cancel(row)">取消</button>
        </div>
        <span v-else class="text-xs text-slate-400">-</span>
      </template>
    </DataTable>

    <AppModal v-model="completeOpen" title="完成揀貨" size="sm" :busy="completing">
      <form id="pick-form" class="space-y-4" @submit.prevent="submitComplete">
        <!-- PDA 風格的任務卡 -->
        <div class="rounded-lg border border-blue-200 bg-blue-50 p-4 dark:border-blue-500/30 dark:bg-blue-500/10">
          <p class="font-mono text-xs text-blue-700 dark:text-blue-300">{{ current?.taskNo }}</p>
          <p class="mt-2 text-lg font-semibold text-slate-800 dark:text-white">{{ current?.materialCode }}</p>
          <p class="text-sm text-slate-600 dark:text-slate-300">{{ current?.materialName }}</p>

          <div class="mt-3 flex items-center justify-between border-t border-blue-200 pt-3 dark:border-blue-500/30">
            <div>
              <p class="text-xs text-slate-500 dark:text-slate-400">揀貨儲位</p>
              <p class="font-mono text-base font-semibold text-blue-700 dark:text-blue-300">
                {{ current?.sourceLocationCode }}
              </p>
            </div>
            <div class="text-right">
              <p class="text-xs text-slate-500 dark:text-slate-400">應揀數量</p>
              <p class="text-base font-semibold text-slate-800 dark:text-white">
                {{ formatQty(current?.quantity ?? 0) }} {{ current?.baseUom }}
              </p>
            </div>
          </div>
        </div>

        <div>
          <label class="label" for="pk-qty">實際揀出數量 <span class="text-rose-500">*</span></label>
          <input
            id="pk-qty"
            v-model.number="pickedQuantity"
            type="number"
            min="0.000001"
            :max="current?.quantity"
            step="any"
            required
            class="!text-lg"
          />
          <p class="mt-1 text-xs text-slate-500">數量少於應揀量時，差額的預留庫存會自動釋放。</p>
        </div>

        <p v-if="completeError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ completeError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="completing" @click="completeOpen = false">取消</button>
        <button type="submit" form="pick-form" class="btn-primary" :disabled="completing">
          {{ completing ? '處理中…' : '確認揀貨' }}
        </button>
      </template>
    </AppModal>
  </div>
</template>
