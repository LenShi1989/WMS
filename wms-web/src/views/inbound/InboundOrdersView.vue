<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { inboundApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { formatDateTime, formatQty, toIsoDate } from '@/utils/format'
import { sourceTypeLabel } from '@/utils/labels'
import { P } from '@/utils/permissions'
import type { InboundOrder, InboundSourceType, InboundStatus } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()
const router = useRouter()

const table = useDataTable<InboundOrder>(inboundApi.query, { warehouseId: undefined, status: undefined })

const columns: Column[] = [
  { key: 'orderNo', title: '入庫單號', width: '160px' },
  { key: 'warehouseCode', title: '倉庫', width: '100px' },
  { key: 'supplierName', title: '供應商', hideOnMobile: true },
  { key: 'lineCount', title: '明細數', align: 'right', width: '80px', hideOnMobile: true },
  { key: 'totalOrderedQuantity', title: '應收量', align: 'right', width: '100px' },
  { key: 'totalReceivedQuantity', title: '已收量', align: 'right', width: '100px' },
  { key: 'status', title: '狀態', align: 'center', width: '100px' },
  { key: 'createdAt', title: '建立時間', width: '160px', hideOnMobile: true },
  { key: 'actions', title: '操作', align: 'right', width: '90px' },
]

const statuses: InboundStatus[] = ['DRAFT', 'RECEIVING', 'RECEIVED', 'PUTAWAY', 'COMPLETED', 'CANCELLED']
const sourceTypes: InboundSourceType[] = ['MANUAL', 'PURCHASE', 'ERP', 'RETURN', 'PRODUCTION']

interface DraftLine {
  materialId: string
  orderedQuantity: number | null
  remark: string
}

const form = reactive({
  warehouseId: '',
  sourceType: 'MANUAL' as InboundSourceType,
  externalOrderNo: '',
  supplierCode: '',
  supplierName: '',
  expectedArrivalDate: '',
  remark: '',
  details: [] as DraftLine[],
})

const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

function addLine() {
  form.details.push({ materialId: '', orderedQuantity: null, remark: '' })
}

function removeLine(index: number) {
  form.details.splice(index, 1)
}

function openCreate() {
  Object.assign(form, {
    warehouseId: master.warehouses[0]?.id ?? '',
    sourceType: 'MANUAL',
    externalOrderNo: '',
    supplierCode: '',
    supplierName: '',
    expectedArrivalDate: '',
    remark: '',
    details: [],
  })
  addLine()
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  formError.value = ''

  const lines = form.details.filter((d) => d.materialId && Number(d.orderedQuantity) > 0)
  if (lines.length === 0) {
    formError.value = '請至少填寫一筆有效的明細（物料與數量）。'
    return
  }

  const materialIds = lines.map((l) => l.materialId)
  if (new Set(materialIds).size !== materialIds.length) {
    formError.value = '同一張入庫單不可重複挑選相同物料。'
    return
  }

  saving.value = true
  try {
    const order = await inboundApi.create({
      warehouseId: form.warehouseId,
      sourceType: form.sourceType,
      externalOrderNo: form.externalOrderNo || null,
      supplierCode: form.supplierCode || null,
      supplierName: form.supplierName || null,
      expectedArrivalDate: toIsoDate(form.expectedArrivalDate) ?? null,
      remark: form.remark || null,
      details: lines.map((l) => ({
        materialId: l.materialId,
        orderedQuantity: Number(l.orderedQuantity),
        remark: l.remark || null,
      })),
    })

    app.notify(`入庫單 ${order.orderNo} 已建立。`)
    modalOpen.value = false
    router.push({ name: 'inbound-order-detail', params: { id: order.id } })
  } catch (e) {
    formError.value = e instanceof ApiException ? e.message : '建立失敗。'
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await Promise.all([table.load(), master.loadWarehouses(), master.loadMaterials()])
})
</script>

<template>
  <div>
    <PageHeader title="入庫單" description="建立入庫單後即可進行收貨與上架">
      <template #actions>
        <button v-if="auth.can(P.inboundCreate)" type="button" class="btn-primary" @click="openCreate">
          建立入庫單
        </button>
      </template>
    </PageHeader>

    <FilterBar v-model="table.keyword.value" placeholder="搜尋單號、外部單號或供應商" @search="table.onSearchInput">
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
      empty-text="尚未建立任何入庫單"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-orderNo="{ row }">
        <RouterLink :to="{ name: 'inbound-order-detail', params: { id: row.id } }" class="link font-mono text-xs">
          {{ row.orderNo }}
        </RouterLink>
      </template>

      <template #cell-supplierName="{ row }">
        <span class="text-slate-500">{{ row.supplierName || row.supplierCode || '-' }}</span>
      </template>

      <template #cell-totalOrderedQuantity="{ row }">{{ formatQty(row.totalOrderedQuantity) }}</template>

      <template #cell-totalReceivedQuantity="{ row }">
        <span :class="row.totalReceivedQuantity >= row.totalOrderedQuantity ? 'text-emerald-600 dark:text-emerald-400' : ''">
          {{ formatQty(row.totalReceivedQuantity) }}
        </span>
      </template>

      <template #cell-status="{ row }"><StatusBadge :status="row.status" /></template>

      <template #cell-createdAt="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.createdAt) }}</span>
      </template>

      <template #cell-actions="{ row }">
        <RouterLink :to="{ name: 'inbound-order-detail', params: { id: row.id } }" class="btn-ghost btn-sm">
          檢視
        </RouterLink>
      </template>
    </DataTable>

    <!-- 建立入庫單 -->
    <AppModal v-model="modalOpen" title="建立入庫單" size="xl" :busy="saving">
      <form id="inbound-form" class="space-y-5" @submit.prevent="save">
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div>
            <label class="label" for="i-wh">入庫倉庫 <span class="text-rose-500">*</span></label>
            <select id="i-wh" v-model="form.warehouseId" required>
              <option value="" disabled>請選擇倉庫</option>
              <option v-for="w in master.warehouses" :key="w.id" :value="w.id">{{ w.code }} － {{ w.name }}</option>
            </select>
          </div>

          <div>
            <label class="label" for="i-source">來源類型</label>
            <select id="i-source" v-model="form.sourceType">
              <option v-for="s in sourceTypes" :key="s" :value="s">{{ sourceTypeLabel[s] }}</option>
            </select>
          </div>

          <div>
            <label class="label" for="i-external">外部單號</label>
            <input id="i-external" v-model="form.externalOrderNo" type="text" maxlength="100" placeholder="ERP / 採購單號" />
          </div>

          <div>
            <label class="label" for="i-sup-code">供應商代號</label>
            <input id="i-sup-code" v-model="form.supplierCode" type="text" maxlength="50" />
          </div>

          <div>
            <label class="label" for="i-sup-name">供應商名稱</label>
            <input id="i-sup-name" v-model="form.supplierName" type="text" maxlength="200" />
          </div>

          <div>
            <label class="label" for="i-date">預計到貨日</label>
            <input id="i-date" v-model="form.expectedArrivalDate" type="date" />
          </div>

          <div class="sm:col-span-3">
            <label class="label" for="i-remark">備註</label>
            <input id="i-remark" v-model="form.remark" type="text" maxlength="500" />
          </div>
        </div>

        <div>
          <div class="mb-2 flex items-center justify-between">
            <p class="text-sm font-medium text-slate-700 dark:text-slate-200">入庫明細</p>
            <button type="button" class="btn-secondary btn-sm" @click="addLine">新增一列</button>
          </div>

          <div class="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-700">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                  <th class="w-12 px-3 py-2 font-medium">#</th>
                  <th class="px-3 py-2 font-medium">物料 <span class="text-rose-500">*</span></th>
                  <th class="w-36 px-3 py-2 font-medium">數量 <span class="text-rose-500">*</span></th>
                  <th class="px-3 py-2 font-medium">備註</th>
                  <th class="w-16 px-3 py-2" />
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
                <tr v-for="(line, index) in form.details" :key="index">
                  <td class="px-3 py-2 text-slate-400">{{ index + 1 }}</td>
                  <td class="px-3 py-2">
                    <select v-model="line.materialId" required>
                      <option value="" disabled>請選擇物料</option>
                      <option v-for="m in master.materials" :key="m.id" :value="m.id">
                        {{ m.code }} － {{ m.name }}（{{ m.baseUom }}）
                      </option>
                    </select>
                  </td>
                  <td class="px-3 py-2">
                    <input v-model.number="line.orderedQuantity" type="number" min="0.000001" step="any" required placeholder="0" />
                  </td>
                  <td class="px-3 py-2">
                    <input v-model="line.remark" type="text" maxlength="500" />
                  </td>
                  <td class="px-3 py-2 text-right">
                    <button
                      type="button"
                      class="btn-ghost btn-sm !text-rose-600"
                      :disabled="form.details.length === 1"
                      @click="removeLine(index)"
                    >
                      移除
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="inbound-form" class="btn-primary" :disabled="saving">
          {{ saving ? '建立中…' : '建立入庫單' }}
        </button>
      </template>
    </AppModal>
  </div>
</template>
