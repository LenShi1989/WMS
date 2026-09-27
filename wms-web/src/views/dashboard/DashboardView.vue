<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { BarChart, LineChart, PieChart } from 'echarts/charts'
import { GridComponent, LegendComponent, TooltipComponent } from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import PageHeader from '@/components/PageHeader.vue'
import StatCard from '@/components/StatCard.vue'
import { dashboardApi } from '@/api'
import { ApiException } from '@/api/client'
import { useAppStore } from '@/stores/app'
import { formatNumber, formatQty } from '@/utils/format'
import type { DashboardSummary, DashboardTrend, InventorySummary, WarehouseStock } from '@/types'

use([BarChart, LineChart, PieChart, GridComponent, TooltipComponent, LegendComponent, CanvasRenderer])

const app = useAppStore()

const summary = ref<DashboardSummary | null>(null)
const trend = ref<DashboardTrend[]>([])
const warehouseStock = ref<WarehouseStock[]>([])
const lowStock = ref<InventorySummary[]>([])
const loading = ref(true)

const axisColor = computed(() => (app.dark ? '#94a3b8' : '#64748b'))
const splitColor = computed(() => (app.dark ? 'rgba(148,163,184,0.15)' : 'rgba(100,116,139,0.15)'))

const trendOption = computed(() => ({
  tooltip: { trigger: 'axis' },
  legend: { data: ['入庫', '出庫'], textStyle: { color: axisColor.value }, top: 0 },
  grid: { left: 8, right: 16, top: 40, bottom: 8, containLabel: true },
  xAxis: {
    type: 'category',
    data: trend.value.map((t) => t.date),
    axisLine: { lineStyle: { color: splitColor.value } },
    axisLabel: { color: axisColor.value },
  },
  yAxis: {
    type: 'value',
    splitLine: { lineStyle: { color: splitColor.value } },
    axisLabel: { color: axisColor.value },
  },
  series: [
    {
      name: '入庫',
      type: 'line',
      smooth: true,
      showSymbol: false,
      data: trend.value.map((t) => t.inbound),
      itemStyle: { color: '#2563eb' },
      areaStyle: { color: 'rgba(37,99,235,0.15)' },
    },
    {
      name: '出庫',
      type: 'line',
      smooth: true,
      showSymbol: false,
      data: trend.value.map((t) => t.outbound),
      itemStyle: { color: '#f59e0b' },
      areaStyle: { color: 'rgba(245,158,11,0.15)' },
    },
  ],
}))

const warehouseOption = computed(() => ({
  tooltip: { trigger: 'item', formatter: '{b}<br/>{c} ({d}%)' },
  legend: { bottom: 0, textStyle: { color: axisColor.value } },
  series: [
    {
      type: 'pie',
      radius: ['45%', '70%'],
      center: ['50%', '45%'],
      itemStyle: { borderRadius: 6, borderColor: app.dark ? '#1e293b' : '#fff', borderWidth: 2 },
      label: { show: false },
      data: warehouseStock.value.map((w) => ({ name: w.warehouseName, value: w.quantity })),
      color: ['#2563eb', '#0ea5e9', '#8b5cf6', '#10b981', '#f59e0b', '#f43f5e'],
    },
  ],
}))

async function load() {
  loading.value = true
  try {
    const [s, t, w, l] = await Promise.all([
      dashboardApi.summary(),
      dashboardApi.trend(7),
      dashboardApi.warehouseStock(),
      dashboardApi.lowStock(8),
    ])
    summary.value = s
    trend.value = t
    warehouseStock.value = w
    lowStock.value = l
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '載入儀表板資料失敗。', 'error')
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div>
    <PageHeader title="儀表板" description="今日倉儲作業概況">
      <template #actions>
        <button type="button" class="btn-secondary" :disabled="loading" @click="load">
          <svg class="size-4" :class="loading ? 'animate-spin' : ''" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M16.023 9.348h4.992V4.356m-.582 4.992-2.74-2.74A8.25 8.25 0 0 0 3.633 8.87m-.648 8.782H7.977v4.992m-.65-4.992 2.74 2.74a8.25 8.25 0 0 0 14.06-2.51" />
          </svg>
          重新整理
        </button>
      </template>
    </PageHeader>

    <!-- 統計卡片 -->
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <StatCard label="今日入庫量" :value="formatQty(summary?.inboundToday ?? 0)" tone="green" :hint="`未結案入庫單 ${summary?.openInboundOrders ?? 0} 張`">
        <template #icon>
          <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 9.75v6.75m0 0-3-3m3 3 3-3M6.75 19.5a4.5 4.5 0 0 1-1.41-8.775 5.25 5.25 0 0 1 10.233-2.33 3 3 0 0 1 3.758 3.848A3.752 3.752 0 0 1 18 19.5H6.75Z" />
          </svg>
        </template>
      </StatCard>

      <StatCard label="今日出庫量" :value="formatQty(summary?.outboundToday ?? 0)" tone="amber" :hint="`未出貨出庫單 ${summary?.openOutboundOrders ?? 0} 張`">
        <template #icon>
          <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 16.5V9.75m0 0 3 3m-3-3-3 3M6.75 19.5a4.5 4.5 0 0 1-1.41-8.775 5.25 5.25 0 0 1 10.233-2.33 3 3 0 0 1 3.758 3.848A3.752 3.752 0 0 1 18 19.5H6.75Z" />
          </svg>
        </template>
      </StatCard>

      <StatCard label="庫存總量" :value="formatQty(summary?.inventoryTotal ?? 0)" tone="blue" to="/inventory" :hint="`物料 ${summary?.materialCount ?? 0} 項 · 儲位 ${summary?.locationCount ?? 0} 個`">
        <template #icon>
          <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M20.25 7.5 12 12m8.25-4.5L12 3 3.75 7.5M20.25 7.5v9L12 21m0-9L3.75 7.5M12 12v9M3.75 7.5v9L12 21" />
          </svg>
        </template>
      </StatCard>

      <StatCard label="低於安全庫存" :value="formatNumber(summary?.belowSafetyStockCount ?? 0)" unit="項" tone="rose" hint="需要補貨的物料數">
        <template #icon>
          <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
          </svg>
        </template>
      </StatCard>
    </div>

    <!-- 待辦任務 -->
    <div class="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-3">
      <StatCard label="待上架任務" :value="formatNumber(summary?.pendingPutaway ?? 0)" unit="筆" tone="cyan" to="/inbound/putaway" />
      <StatCard label="待揀貨任務" :value="formatNumber(summary?.pendingPicking ?? 0)" unit="筆" tone="purple" to="/outbound/picking" />
      <StatCard label="進行中盤點" :value="formatNumber(summary?.pendingStocktake ?? 0)" unit="張" tone="amber" to="/stocktake" />
    </div>

    <!-- 圖表 -->
    <div class="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-3">
      <section class="card p-4 lg:col-span-2">
        <h2 class="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">近 7 日出入庫趨勢</h2>
        <VChart v-if="trend.length" class="h-72 w-full" :option="trendOption" autoresize />
        <p v-else class="py-20 text-center text-sm text-slate-400">尚無資料</p>
      </section>

      <section class="card p-4">
        <h2 class="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">各倉庫庫存佔比</h2>
        <VChart v-if="warehouseStock.length" class="h-72 w-full" :option="warehouseOption" autoresize />
        <p v-else class="py-20 text-center text-sm text-slate-400">尚無庫存資料</p>
      </section>
    </div>

    <!-- 安全庫存警示 -->
    <section class="card mt-4 overflow-hidden">
      <div class="border-b border-slate-200 px-4 py-3 dark:border-slate-700">
        <h2 class="text-sm font-semibold text-slate-700 dark:text-slate-200">低於安全庫存的物料</h2>
      </div>

      <div v-if="lowStock.length" class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
              <th class="px-4 py-2.5 font-medium">物料編號</th>
              <th class="px-4 py-2.5 font-medium">物料名稱</th>
              <th class="px-4 py-2.5 text-right font-medium">現有庫存</th>
              <th class="px-4 py-2.5 text-right font-medium">安全庫存</th>
              <th class="px-4 py-2.5 text-right font-medium">缺口</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
            <tr v-for="item in lowStock" :key="item.materialId">
              <td class="px-4 py-2.5 font-mono text-xs">{{ item.materialCode }}</td>
              <td class="px-4 py-2.5">{{ item.materialName }}</td>
              <td class="px-4 py-2.5 text-right">{{ formatQty(item.quantity) }} {{ item.baseUom }}</td>
              <td class="px-4 py-2.5 text-right text-slate-500">{{ formatQty(item.safetyStock) }}</td>
              <td class="px-4 py-2.5 text-right font-medium text-rose-600 dark:text-rose-400">
                {{ formatQty(item.safetyStock - item.quantity) }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <p v-else class="px-4 py-10 text-center text-sm text-slate-400">所有物料庫存都在安全水位以上</p>
    </section>
  </div>
</template>
