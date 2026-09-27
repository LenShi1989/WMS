<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { inventoryApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatQty } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { Inventory, InventorySummary, WarehouseLocation } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()

const mode = ref<'location' | 'material'>('location')

const detailTable = useDataTable<Inventory>(inventoryApi.query, {
  warehouseId: undefined,
  materialId: undefined,
  onlyInStock: true,
})

const summaryTable = useDataTable<InventorySummary>(inventoryApi.summary, {
  warehouseId: undefined,
  materialId: undefined,
  onlyInStock: true,
})

const detailColumns: Column[] = [
  { key: 'materialCode', title: '物料編號', width: '130px' },
  { key: 'materialName', title: '物料名稱' },
  { key: 'warehouseCode', title: '倉庫', width: '90px', hideOnMobile: true },
  { key: 'locationCode', title: '儲位', width: '170px' },
  { key: 'quantity', title: '庫存量', align: 'right', width: '110px' },
  { key: 'reservedQuantity', title: '已預留', align: 'right', width: '100px' },
  { key: 'availableQuantity', title: '可用量', align: 'right', width: '110px' },
  { key: 'status', title: '狀態', align: 'center', width: '90px' },
  { key: 'actions', title: '操作', align: 'right', width: '90px' },
]

const summaryColumns: Column[] = [
  { key: 'materialCode', title: '物料編號', width: '130px' },
  { key: 'materialName', title: '物料名稱' },
  { key: 'locationCount', title: '儲位數', align: 'right', width: '90px' },
  { key: 'quantity', title: '庫存量', align: 'right', width: '120px' },
  { key: 'reservedQuantity', title: '已預留', align: 'right', width: '110px' },
  { key: 'availableQuantity', title: '可用量', align: 'right', width: '120px' },
  { key: 'safetyStock', title: '安全庫存', align: 'right', width: '110px', hideOnMobile: true },
]

// ---------- 庫存調整 ----------
const adjustOpen = ref(false)
const adjusting = ref(false)
const adjustError = ref('')
const adjustLocations = ref<WarehouseLocation[]>([])

const adjustForm = reactive({
  warehouseId: '',
  locationId: '',
  materialId: '',
  currentQuantity: 0,
  newQuantity: null as number | null,
  reason: '',
})

watch(
  () => adjustForm.warehouseId,
  async (id) => {
    adjustForm.locationId = ''
    adjustLocations.value = id ? await master.loadLocations(id) : []
  },
)

function openAdjust(row?: Inventory) {
  adjustError.value = ''
  if (row) {
    Object.assign(adjustForm, {
      warehouseId: row.warehouseId,
      locationId: row.locationId,
      materialId: row.materialId,
      currentQuantity: row.quantity,
      newQuantity: row.quantity,
      reason: '',
    })
    master.loadLocations(row.warehouseId).then((list) => (adjustLocations.value = list))
  } else {
    Object.assign(adjustForm, {
      warehouseId: master.warehouses[0]?.id ?? '',
      locationId: '',
      materialId: '',
      currentQuantity: 0,
      newQuantity: null,
      reason: '',
    })
  }
  adjustOpen.value = true
}

async function submitAdjust() {
  adjustError.value = ''

  if (!adjustForm.locationId || !adjustForm.materialId) {
    adjustError.value = '請選擇儲位與物料。'
    return
  }

  if (adjustForm.newQuantity === null || Number(adjustForm.newQuantity) < 0) {
    adjustError.value = '請輸入有效的調整後數量。'
    return
  }

  adjusting.value = true
  try {
    await inventoryApi.adjust({
      locationId: adjustForm.locationId,
      materialId: adjustForm.materialId,
      newQuantity: Number(adjustForm.newQuantity),
      reason: adjustForm.reason.trim(),
    })
    app.notify('庫存已調整，並已寫入異動紀錄。')
    adjustOpen.value = false
    await reload()
  } catch (e) {
    adjustError.value = e instanceof ApiException ? e.message : '調整失敗。'
  } finally {
    adjusting.value = false
  }
}

function reload() {
  return mode.value === 'location' ? detailTable.load() : summaryTable.load()
}

watch(mode, reload)

onMounted(async () => {
  await Promise.all([detailTable.load(), master.loadWarehouses(), master.loadMaterials()])
})
</script>

<template>
  <div>
    <PageHeader title="即時庫存" description="可用量 = 庫存量 − 已預留，數量僅能透過作業流程異動">
      <template #actions>
        <div class="inline-flex overflow-hidden rounded-lg border border-slate-300 dark:border-slate-600">
          <button
            type="button"
            class="px-3 py-1.5 text-sm transition"
            :class="mode === 'location' ? 'bg-blue-600 text-white' : 'bg-white text-slate-600 dark:bg-slate-800 dark:text-slate-300'"
            @click="mode = 'location'"
          >
            依儲位
          </button>
          <button
            type="button"
            class="px-3 py-1.5 text-sm transition"
            :class="mode === 'material' ? 'bg-blue-600 text-white' : 'bg-white text-slate-600 dark:bg-slate-800 dark:text-slate-300'"
            @click="mode = 'material'"
          >
            依物料
          </button>
        </div>

        <button v-if="auth.can(P.inventoryAdjust)" type="button" class="btn-primary" @click="openAdjust()">
          庫存調整
        </button>
      </template>
    </PageHeader>

    <!-- 依儲位 -->
    <template v-if="mode === 'location'">
      <FilterBar v-model="detailTable.keyword.value" placeholder="搜尋物料或儲位" @search="detailTable.onSearchInput">
        <div class="w-44">
          <label class="label" for="f-wh">倉庫</label>
          <select id="f-wh" v-model="detailTable.filters.warehouseId">
            <option :value="undefined">全部倉庫</option>
            <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }}</option>
          </select>
        </div>

        <div class="w-56">
          <label class="label" for="f-mat">物料</label>
          <select id="f-mat" v-model="detailTable.filters.materialId">
            <option :value="undefined">全部物料</option>
            <option v-for="m in master.materials" :key="m.id" :value="m.id">{{ m.code }} － {{ m.name }}</option>
          </select>
        </div>

        <div class="w-36">
          <label class="label" for="f-stock">顯示範圍</label>
          <select id="f-stock" v-model="detailTable.filters.onlyInStock">
            <option :value="true">僅有庫存</option>
            <option :value="undefined">全部</option>
          </select>
        </div>
      </FilterBar>

      <DataTable
        :columns="detailColumns"
        :rows="detailTable.items.value"
        :loading="detailTable.loading.value"
        :total="detailTable.total.value"
        :page="detailTable.page.value"
        :page-size="detailTable.pageSize.value"
        empty-text="目前沒有庫存資料"
        @update:page="detailTable.page.value = $event"
        @update:page-size="detailTable.pageSize.value = $event"
      >
        <template #cell-materialCode="{ row }">
          <span class="font-mono text-xs font-medium">{{ row.materialCode }}</span>
        </template>

        <template #cell-locationCode="{ row }">
          <span class="font-mono text-xs">{{ row.locationCode }}</span>
        </template>

        <template #cell-quantity="{ row }">
          <span class="font-medium">{{ formatQty(row.quantity) }}</span>
          <span class="ml-1 text-xs text-slate-400">{{ row.baseUom }}</span>
        </template>

        <template #cell-reservedQuantity="{ row }">
          <span :class="row.reservedQuantity > 0 ? 'text-amber-600 dark:text-amber-400' : 'text-slate-400'">
            {{ formatQty(row.reservedQuantity) }}
          </span>
        </template>

        <template #cell-availableQuantity="{ row }">
          <span class="font-medium text-emerald-600 dark:text-emerald-400">{{ formatQty(row.availableQuantity) }}</span>
        </template>

        <template #cell-status="{ row }"><StatusBadge :status="row.status" /></template>

        <template #cell-actions="{ row }">
          <button v-if="auth.can(P.inventoryAdjust)" type="button" class="btn-ghost btn-sm" @click="openAdjust(row)">
            調整
          </button>
        </template>
      </DataTable>
    </template>

    <!-- 依物料彙總 -->
    <template v-else>
      <FilterBar v-model="summaryTable.keyword.value" placeholder="搜尋物料編號或名稱" @search="summaryTable.onSearchInput">
        <div class="w-44">
          <label class="label" for="s-wh">倉庫</label>
          <select id="s-wh" v-model="summaryTable.filters.warehouseId">
            <option :value="undefined">全部倉庫</option>
            <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }}</option>
          </select>
        </div>
      </FilterBar>

      <DataTable
        :columns="summaryColumns"
        :rows="summaryTable.items.value"
        :loading="summaryTable.loading.value"
        :total="summaryTable.total.value"
        :page="summaryTable.page.value"
        :page-size="summaryTable.pageSize.value"
        row-key="materialId"
        empty-text="目前沒有庫存資料"
        @update:page="summaryTable.page.value = $event"
        @update:page-size="summaryTable.pageSize.value = $event"
      >
        <template #cell-materialCode="{ row }">
          <span class="font-mono text-xs font-medium">{{ row.materialCode }}</span>
        </template>

        <template #cell-quantity="{ row }">
          <span class="font-medium" :class="row.belowSafetyStock ? 'text-rose-600 dark:text-rose-400' : ''">
            {{ formatQty(row.quantity) }}
          </span>
          <span class="ml-1 text-xs text-slate-400">{{ row.baseUom }}</span>
        </template>

        <template #cell-reservedQuantity="{ row }">
          <span class="text-slate-500">{{ formatQty(row.reservedQuantity) }}</span>
        </template>

        <template #cell-availableQuantity="{ row }">
          <span class="font-medium text-emerald-600 dark:text-emerald-400">{{ formatQty(row.availableQuantity) }}</span>
        </template>

        <template #cell-safetyStock="{ row }">
          <span class="text-slate-500">{{ formatQty(row.safetyStock) }}</span>
        </template>
      </DataTable>
    </template>

    <!-- 庫存調整 -->
    <AppModal v-model="adjustOpen" title="庫存調整" size="sm" :busy="adjusting">
      <form id="adjust-form" class="space-y-4" @submit.prevent="submitAdjust">
        <div>
          <label class="label" for="a-wh">倉庫 <span class="text-rose-500">*</span></label>
          <select id="a-wh" v-model="adjustForm.warehouseId" required>
            <option value="" disabled>請選擇倉庫</option>
            <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }} － {{ w.name }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="a-loc">儲位 <span class="text-rose-500">*</span></label>
          <select id="a-loc" v-model="adjustForm.locationId" required>
            <option value="" disabled>請選擇儲位</option>
            <option v-for="l in adjustLocations" :key="l.id" :value="l.id">{{ l.code }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="a-mat">物料 <span class="text-rose-500">*</span></label>
          <select id="a-mat" v-model="adjustForm.materialId" required>
            <option value="" disabled>請選擇物料</option>
            <option v-for="m in master.materials" :key="m.id" :value="m.id">{{ m.code }} － {{ m.name }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="a-qty">調整後數量 <span class="text-rose-500">*</span></label>
          <input id="a-qty" v-model.number="adjustForm.newQuantity" type="number" min="0" step="any" required />
          <p class="mt-1 text-xs text-slate-500">
            輸入盤點或修正後的實際數量，系統會自動計算差額並寫入 ADJUSTMENT 異動。
          </p>
        </div>

        <div>
          <label class="label" for="a-reason">調整原因 <span class="text-rose-500">*</span></label>
          <input id="a-reason" v-model="adjustForm.reason" type="text" required maxlength="200" placeholder="例如：破損報廢、帳差修正" />
        </div>

        <p v-if="adjustError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ adjustError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="adjusting" @click="adjustOpen = false">取消</button>
        <button type="submit" form="adjust-form" class="btn-primary" :disabled="adjusting">
          {{ adjusting ? '處理中…' : '確認調整' }}
        </button>
      </template>
    </AppModal>
  </div>
</template>
