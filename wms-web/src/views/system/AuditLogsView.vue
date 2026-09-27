<script setup lang="ts">
import { onMounted, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import FilterBar from '@/components/FilterBar.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { auditApi } from '@/api'
import { useDataTable } from '@/composables/useDataTable'
import { formatDateTime } from '@/utils/format'
import { actionLabel, moduleLabel } from '@/utils/labels'
import type { AuditLog } from '@/types'

const table = useDataTable<AuditLog>(auditApi.query, {
  module: undefined,
  action: undefined,
  dateFrom: undefined,
  dateTo: undefined,
})

const columns: Column[] = [
  { key: 'createdAt', title: '時間', width: '160px' },
  { key: 'username', title: '操作人員', width: '130px' },
  { key: 'module', title: '模組', width: '120px' },
  { key: 'action', title: '動作', width: '110px' },
  { key: 'referenceNo', title: '對象', width: '180px' },
  { key: 'ipAddress', title: 'IP', width: '130px', hideOnMobile: true },
  { key: 'actions', title: '', align: 'right', width: '80px' },
]

const modules = Object.keys(moduleLabel)
const actions = Object.keys(actionLabel)

const detailOpen = ref(false)
const current = ref<AuditLog | null>(null)

function openDetail(row: AuditLog) {
  current.value = row
  detailOpen.value = true
}

/** 後端存的是 jsonb 字串，這裡排版後顯示。 */
function pretty(value?: string) {
  if (!value) return '-'
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

onMounted(table.load)
</script>

<template>
  <div>
    <PageHeader title="操作紀錄" description="系統所有重要操作的稽核軌跡" />

    <FilterBar v-model="table.keyword.value" placeholder="搜尋操作人員或單號" @search="table.onSearchInput">
      <div class="w-40">
        <label class="label" for="f-module">模組</label>
        <select id="f-module" v-model="table.filters.module">
          <option :value="undefined">全部模組</option>
          <option v-for="m in modules" :key="m" :value="m">{{ moduleLabel[m] }}</option>
        </select>
      </div>

      <div class="w-36">
        <label class="label" for="f-action">動作</label>
        <select id="f-action" v-model="table.filters.action">
          <option :value="undefined">全部動作</option>
          <option v-for="a in actions" :key="a" :value="a">{{ actionLabel[a] }}</option>
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
      empty-text="目前沒有操作紀錄"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-createdAt="{ row }">
        <span class="text-xs text-slate-500">{{ formatDateTime(row.createdAt) }}</span>
      </template>

      <template #cell-username="{ row }">{{ row.username || '系統' }}</template>

      <template #cell-module="{ row }">
        <StatusBadge :meta="{ text: moduleLabel[row.module] ?? row.module, tone: 'blue' }" />
      </template>

      <template #cell-action="{ row }">{{ actionLabel[row.action] ?? row.action }}</template>

      <template #cell-referenceNo="{ row }">
        <span class="font-mono text-xs text-slate-500">{{ row.referenceNo || '-' }}</span>
      </template>

      <template #cell-ipAddress="{ row }">
        <span class="font-mono text-xs text-slate-500">{{ row.ipAddress || '-' }}</span>
      </template>

      <template #cell-actions="{ row }">
        <button
          v-if="row.oldValue || row.newValue"
          type="button"
          class="btn-ghost btn-sm"
          @click="openDetail(row)"
        >
          詳情
        </button>
      </template>
    </DataTable>

    <AppModal v-model="detailOpen" title="操作明細" size="lg">
      <div v-if="current" class="space-y-4">
        <dl class="grid grid-cols-2 gap-4 text-sm">
          <div>
            <dt class="text-slate-500">時間</dt>
            <dd class="mt-0.5">{{ formatDateTime(current.createdAt) }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">操作人員</dt>
            <dd class="mt-0.5">{{ current.username || '系統' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">模組 / 動作</dt>
            <dd class="mt-0.5">
              {{ moduleLabel[current.module] ?? current.module }} · {{ actionLabel[current.action] ?? current.action }}
            </dd>
          </div>
          <div>
            <dt class="text-slate-500">對象</dt>
            <dd class="mt-0.5 font-mono text-xs">{{ current.referenceNo || current.referenceId || '-' }}</dd>
          </div>
        </dl>

        <div v-if="current.oldValue">
          <p class="label">變更前</p>
          <pre class="max-h-56 overflow-auto rounded-lg bg-slate-50 p-3 text-xs dark:bg-slate-900/60">{{ pretty(current.oldValue) }}</pre>
        </div>

        <div v-if="current.newValue">
          <p class="label">變更後</p>
          <pre class="max-h-56 overflow-auto rounded-lg bg-slate-50 p-3 text-xs dark:bg-slate-900/60">{{ pretty(current.newValue) }}</pre>
        </div>
      </div>

      <template #footer>
        <button type="button" class="btn-secondary" @click="detailOpen = false">關閉</button>
      </template>
    </AppModal>
  </div>
</template>
