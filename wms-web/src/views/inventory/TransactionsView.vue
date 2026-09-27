<script setup lang="ts">
import { onMounted } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { inventoryApi } from '@/api'
import { useDataTable } from '@/composables/useDataTable'
import { useMasterStore } from '@/stores/master'
import { formatDateTime, formatQty } from '@/utils/format'
import { transactionTypeLabel } from '@/utils/labels'
import type { InventoryTransaction, TransactionType } from '@/types'

const master = useMasterStore()

const table = useDataTable<InventoryTransaction>(inventoryApi.transactions, {
  warehouseId: undefined,
  materialId: undefined,
  transactionType: undefined,
  dateFrom: undefined,
  dateTo: undefined,
})

const columns: Column[] = [
  { key: 'createdAt', title: '異動時間', width: '160px' },
  { key: 'transactionNo', title: '異動單號', width: '160px', hideOnMobile: true },
  { key: 'transactionType', title: '類型', align: 'center', width: '100px' },
  { key: 'materialCode', title: '物料' },
  { key: 'location', title: '儲位異動', width: '230px' },
  { key: 'quantity', title: '數量', align: 'right', width: '110px' },
  { key: 'balanceAfter', title: '結存', align: 'right', width: '100px', hideOnMobile: true },
  { key: 'referenceNo', title: '來源單號', width: '150px', hideOnMobile: true },
  { key: 'operatorName', title: '操作人員', width: '110px', hideOnMobile: true },
]

const types: TransactionType[] = [
  'RECEIVE', 'PUTAWAY', 'PICK', 'SHIP', 'TRANSFER', 'ADJUSTMENT', 'STOCKTAKE', 'RESERVE', 'RELEASE', 'SCRAP',
]

onMounted(async () => {
  await Promise.all([table.load(), master.loadWarehouses(), master.loadMaterials()])
})
</script>

<template>
  <div>
    <PageHeader title="庫存異動" description="所有庫存變化的完整流水帳，僅能查詢不可修改" />

    <FilterBar v-model="table.keyword.value" placeholder="搜尋異動單號、來源單號或物料" @search="table.onSearchInput">
      <div class="w-40">
        <label class="label" for="f-type">異動類型</label>
        <select id="f-type" v-model="table.filters.transactionType">
          <option :value="undefined">全部類型</option>
          <option v-for="t in types" :key="t" :value="t">{{ transactionTypeLabel[t]?.text ?? t }}</option>
        </select>
      </div>

      <div class="w-44">
        <label class="label" for="f-wh">倉庫</label>
        <select id="f-wh" v-model="table.filters.warehouseId">
          <option :value="undefined">全部倉庫</option>
          <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }}</option>
        </select>
      </div>

      <div class="w-52">
        <label class="label" for="f-mat">物料</label>
        <select id="f-mat" v-model="table.filters.materialId">
          <option :value="undefined">全部物料</option>
          <option v-for="m in master.materials" :key="m.id" :value="m.id">{{ m.code }} － {{ m.name }}</option>
        </select>
      </div>

      <div class="w-40">
        <label class="label" for="f-from">起始日期</label>
        <input id="f-from" v-model="table.filters.dateFrom" type="date" />
      </div>

      <div class="w-40">
        <label class="label" for="f-to">結束日期</label>
        <input id="f-to" v-model="table.filters.dateTo" type="date" />
      </div>
    </FilterBar>

    <DataTable
      :columns="columns"
      :rows="table.items.value"
      :loading="table.loading.value"
      :total="table.total.value"
      :page="table.page.value"
      :page-size="table.pageSize.value"
      empty-text="目前沒有異動紀錄"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-createdAt="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.createdAt) }}</span>
      </template>

      <template #cell-transactionNo="{ row }">
        <span class="font-mono text-xs text-slate-500">{{ row.transactionNo }}</span>
      </template>

      <template #cell-transactionType="{ row }">
        <StatusBadge :meta="transactionTypeLabel[row.transactionType] ?? { text: row.transactionType, tone: 'gray' }" />
      </template>

      <template #cell-materialCode="{ row }">
        <span class="font-mono text-xs">{{ row.materialCode }}</span>
        <span class="ml-2 text-slate-600 dark:text-slate-300">{{ row.materialName }}</span>
      </template>

      <template #cell-location="{ row }">
        <span class="font-mono text-xs text-slate-500">
          <template v-if="row.fromLocationCode && row.toLocationCode">
            {{ row.fromLocationCode }} → {{ row.toLocationCode }}
          </template>
          <template v-else-if="row.toLocationCode">→ {{ row.toLocationCode }}</template>
          <template v-else-if="row.fromLocationCode">{{ row.fromLocationCode }} →</template>
          <template v-else>-</template>
        </span>
      </template>

      <template #cell-quantity="{ row }">
        <span class="font-medium">{{ formatQty(row.quantity) }}</span>
      </template>

      <template #cell-balanceAfter="{ row }">
        <span class="text-slate-500">{{ row.balanceAfter === null || row.balanceAfter === undefined ? '-' : formatQty(row.balanceAfter) }}</span>
      </template>

      <template #cell-referenceNo="{ row }">
        <span class="font-mono text-xs text-slate-500">{{ row.referenceNo || '-' }}</span>
      </template>

      <template #cell-operatorName="{ row }">
        <span class="text-slate-500">{{ row.operatorName || '-' }}</span>
      </template>
    </DataTable>
  </div>
</template>
