<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { stocktakeApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatDateTime } from '@/utils/format'
import { zoneTypeLabel } from '@/utils/labels'
import { P } from '@/utils/permissions'
import type { Stocktake, StocktakeStatus, Zone } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()
const router = useRouter()

const table = useDataTable<Stocktake>(stocktakeApi.query, { warehouseId: undefined, status: undefined })

const columns: Column[] = [
  { key: 'stocktakeNo', title: '盤點單號', width: '160px' },
  { key: 'warehouseCode', title: '倉庫', width: '100px' },
  { key: 'zoneCode', title: '儲區', width: '100px', hideOnMobile: true },
  { key: 'lineCount', title: '盤點筆數', align: 'right', width: '100px' },
  { key: 'countedCount', title: '已盤', align: 'right', width: '90px' },
  { key: 'differenceCount', title: '差異數', align: 'right', width: '90px' },
  { key: 'status', title: '狀態', align: 'center', width: '110px' },
  { key: 'createdAt', title: '建立時間', width: '160px', hideOnMobile: true },
  { key: 'actions', title: '操作', align: 'right', width: '90px' },
]

const statuses: StocktakeStatus[] = ['DRAFT', 'COUNTING', 'RECOUNT', 'PENDING_APPROVAL', 'APPROVED', 'CANCELLED']

const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')
const formZones = ref<Zone[]>([])

const form = reactive({ warehouseId: '', zoneId: '', remark: '' })

watch(
  () => form.warehouseId,
  async (id) => {
    form.zoneId = ''
    formZones.value = id ? await master.loadZones(id) : []
  },
)

function openCreate() {
  Object.assign(form, { warehouseId: master.warehouses[0]?.id ?? '', zoneId: '', remark: '' })
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    const stocktake = await stocktakeApi.create({
      warehouseId: form.warehouseId,
      zoneId: form.zoneId || null,
      remark: form.remark || null,
    })
    app.notify(`盤點單 ${stocktake.stocktakeNo} 已建立，共 ${stocktake.lineCount} 筆待盤。`)
    modalOpen.value = false
    router.push({ name: 'stocktake-detail', params: { id: stocktake.id } })
  } catch (e) {
    formError.value = e instanceof ApiException ? e.message : '建立失敗。'
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await Promise.all([table.load(), master.loadWarehouses()])
})
</script>

<template>
  <div>
    <PageHeader title="盤點" description="建立盤點單時會 Snapshot 當下的系統庫存，核准後依差異調整">
      <template #actions>
        <button v-if="auth.can(P.stocktakeCreate)" type="button" class="btn-primary" @click="openCreate">
          建立盤點單
        </button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋盤點單號" @search="table.onSearchInput">
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
      empty-text="尚未建立任何盤點單"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-stocktakeNo="{ row }">
        <RouterLink :to="{ name: 'stocktake-detail', params: { id: row.id } }" class="link font-mono text-xs">
          {{ row.stocktakeNo }}
        </RouterLink>
      </template>

      <template #cell-zoneCode="{ row }">
        <span class="text-slate-500">{{ row.zoneCode || '全倉' }}</span>
      </template>

      <template #cell-countedCount="{ row }">
        <span :class="row.countedCount >= row.lineCount ? 'text-emerald-600 dark:text-emerald-400' : ''">
          {{ row.countedCount }}
        </span>
      </template>

      <template #cell-differenceCount="{ row }">
        <span :class="row.differenceCount > 0 ? 'font-medium text-rose-600 dark:text-rose-400' : 'text-slate-400'">
          {{ row.differenceCount }}
        </span>
      </template>

      <template #cell-status="{ row }"><StatusBadge :status="row.status" /></template>

      <template #cell-createdAt="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.createdAt) }}</span>
      </template>

      <template #cell-actions="{ row }">
        <RouterLink :to="{ name: 'stocktake-detail', params: { id: row.id } }" class="btn-ghost btn-sm">檢視</RouterLink>
      </template>
    </DataTable>

    <AppModal v-model="modalOpen" title="建立盤點單" size="sm" :busy="saving">
      <form id="stocktake-form" class="space-y-4" @submit.prevent="save">
        <div>
          <label class="label" for="st-wh">盤點倉庫 <span class="text-rose-500">*</span></label>
          <select id="st-wh" v-model="form.warehouseId" required>
            <option value="" disabled>請選擇倉庫</option>
            <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }} － {{ w.name }}</option>
          </select>
        </div>

        <div>
          <label class="label" for="st-zone">盤點範圍</label>
          <select id="st-zone" v-model="form.zoneId">
            <option value="">全倉盤點</option>
            <option v-for="z in formZones" :key="z.id" :value="z.id">
              {{ z.code }} － {{ z.name }}（{{ zoneTypeLabel[z.zoneType] }}）
            </option>
          </select>
        </div>

        <div>
          <label class="label" for="st-remark">備註</label>
          <input id="st-remark" v-model="form.remark" type="text" maxlength="500" />
        </div>

        <p class="rounded-lg bg-blue-50 px-3 py-2 text-xs text-blue-700 dark:bg-blue-500/10 dark:text-blue-300">
          建立時會把目前有庫存的儲位與數量記錄為帳面值（Snapshot），之後的異動不會影響盤點基準。
        </p>

        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="stocktake-form" class="btn-primary" :disabled="saving">
          {{ saving ? '建立中…' : '建立盤點單' }}
        </button>
      </template>
    </AppModal>
  </div>
</template>
