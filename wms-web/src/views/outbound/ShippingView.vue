<script setup lang="ts">
import { onMounted, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { outboundApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatDateTime, formatQty } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { OutboundOrder } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()

// 出貨作業只關心已揀完、等待出貨的單據。
const table = useDataTable<OutboundOrder>(outboundApi.query, { warehouseId: undefined, status: 'PICKED' })

const columns: Column[] = [
  { key: 'orderNo', title: '出庫單號', width: '160px' },
  { key: 'warehouseCode', title: '倉庫', width: '100px' },
  { key: 'customerName', title: '客戶' },
  { key: 'totalPickedQuantity', title: '已揀量', align: 'right', width: '110px' },
  { key: 'requestedShipDate', title: '需求出貨日', width: '140px', hideOnMobile: true },
  { key: 'status', title: '狀態', align: 'center', width: '100px' },
  { key: 'actions', title: '操作', align: 'right', width: '110px' },
]

const shipOpen = ref(false)
const shipping = ref(false)
const shipError = ref('')
const current = ref<OutboundOrder | null>(null)
const remark = ref('')

async function openShip(order: OutboundOrder) {
  current.value = await outboundApi.get(order.id)
  remark.value = ''
  shipError.value = ''
  shipOpen.value = true
}

async function submitShip() {
  if (!current.value) return
  shipping.value = true
  shipError.value = ''
  try {
    await outboundApi.ship(current.value.id, remark.value || undefined)
    app.notify(`出庫單 ${current.value.orderNo} 出貨完成。`)
    shipOpen.value = false
    await table.load()
  } catch (e) {
    shipError.value = e instanceof ApiException ? e.message : '出貨失敗。'
  } finally {
    shipping.value = false
  }
}

onMounted(async () => {
  await Promise.all([table.load(), master.loadWarehouses()])
})
</script>

<template>
  <div>
    <PageHeader title="出貨作業" description="揀貨完成的出庫單在這裡確認出貨並結案" />

    <FilterBar v-model="table.keyword.value" placeholder="搜尋單號或客戶" @search="table.onSearchInput">
      <div class="w-44">
        <label class="label" for="f-wh">倉庫</label>
        <select id="f-wh" v-model="table.filters.warehouseId">
          <option :value="undefined">全部倉庫</option>
          <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }}</option>
        </select>
      </div>

      <div class="w-40">
        <label class="label" for="f-status">狀態</label>
        <select id="f-status" v-model="table.filters.status">
          <option value="PICKED">待出貨</option>
          <option value="SHIPPED">已出貨</option>
          <option :value="undefined">全部</option>
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
      empty-text="目前沒有待出貨的單據"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-orderNo="{ row }">
        <RouterLink :to="{ name: 'outbound-order-detail', params: { id: row.id } }" class="link font-mono text-xs">
          {{ row.orderNo }}
        </RouterLink>
      </template>

      <template #cell-customerName="{ row }">
        <span class="text-slate-600 dark:text-slate-300">{{ row.customerName || row.customerCode || '-' }}</span>
      </template>

      <template #cell-totalPickedQuantity="{ row }">{{ formatQty(row.totalPickedQuantity) }}</template>

      <template #cell-requestedShipDate="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.requestedShipDate) }}</span>
      </template>

      <template #cell-status="{ row }"><StatusBadge :status="row.status" /></template>

      <template #cell-actions="{ row }">
        <button
          v-if="row.status === 'PICKED' && auth.can(P.outboundShip)"
          type="button"
          class="btn-primary btn-sm"
          @click="openShip(row)"
        >
          出貨
        </button>
        <span v-else class="text-xs text-slate-400">-</span>
      </template>
    </DataTable>

    <AppModal v-model="shipOpen" :title="`出貨確認 ${current?.orderNo ?? ''}`" size="lg" :busy="shipping">
      <form id="ship-form" class="space-y-4" @submit.prevent="submitShip">
        <div class="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-700">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                <th class="px-3 py-2 font-medium">物料</th>
                <th class="px-3 py-2 text-right font-medium">需求量</th>
                <th class="px-3 py-2 text-right font-medium">出貨量</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
              <tr v-for="d in current?.details ?? []" :key="d.id">
                <td class="px-3 py-2">
                  <span class="font-mono text-xs font-medium">{{ d.materialCode }}</span>
                  <span class="ml-2 text-slate-600 dark:text-slate-300">{{ d.materialName }}</span>
                </td>
                <td class="px-3 py-2 text-right text-slate-500">{{ formatQty(d.requestedQuantity) }}</td>
                <td class="px-3 py-2 text-right font-medium">{{ formatQty(d.pickedQuantity) }} {{ d.baseUom }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div>
          <label class="label" for="s-remark">出貨備註</label>
          <input id="s-remark" v-model="remark" type="text" maxlength="500" placeholder="車號、司機、物流單號…" />
        </div>

        <p class="rounded-lg bg-blue-50 px-3 py-2 text-xs text-blue-700 dark:bg-blue-500/10 dark:text-blue-300">
          庫存已於揀貨時扣除，出貨會產生 SHIP 異動紀錄並把單據結案。
        </p>

        <p v-if="shipError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ shipError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="shipping" @click="shipOpen = false">取消</button>
        <button type="submit" form="ship-form" class="btn-primary" :disabled="shipping">
          {{ shipping ? '處理中…' : '確認出貨' }}
        </button>
      </template>
    </AppModal>
  </div>
</template>
