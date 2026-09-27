<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/PageHeader.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { stocktakeApi } from '@/api'
import { ApiException } from '@/api/client'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { formatDateTime, formatQty } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { Stocktake } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const stocktake = ref<Stocktake | null>(null)
const loading = ref(true)
const acting = ref(false)

/** detailId → 使用者輸入的盤點數 */
const counts = ref<Record<string, number | null>>({})
const selected = ref<Set<string>>(new Set())

const startOpen = ref(false)
const completeOpen = ref(false)
const approveOpen = ref(false)
const cancelOpen = ref(false)

const isCounting = computed(() => ['COUNTING', 'RECOUNT'].includes(stocktake.value?.status ?? ''))
const canCount = computed(() => isCounting.value && auth.can(P.stocktakeCount))
const uncountedCount = computed(() => stocktake.value?.details.filter((d) => d.countedQuantity === null).length ?? 0)

const differenceTotal = computed(
  () => stocktake.value?.details.reduce((sum, d) => sum + (d.differenceQuantity ?? 0), 0) ?? 0,
)

async function load() {
  loading.value = true
  try {
    stocktake.value = await stocktakeApi.get(route.params.id as string)
    counts.value = Object.fromEntries(stocktake.value.details.map((d) => [d.id, d.countedQuantity ?? null]))
    selected.value = new Set()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '載入盤點單失敗。', 'error')
    router.push({ name: 'stocktake' })
  } finally {
    loading.value = false
  }
}

watch(() => route.params.id, load)

async function start() {
  if (!stocktake.value) return
  acting.value = true
  try {
    await stocktakeApi.start(stocktake.value.id)
    app.notify('盤點已開始，可以輸入實盤數量。')
    startOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '操作失敗。', 'error')
  } finally {
    acting.value = false
  }
}

/** 把帳面數量一次帶入所有未填的欄位，方便只修改有差異的項目。 */
function fillWithSystemQuantity() {
  if (!stocktake.value) return
  for (const detail of stocktake.value.details) {
    if (counts.value[detail.id] === null || counts.value[detail.id] === undefined) {
      counts.value[detail.id] = detail.systemQuantity
    }
  }
}

async function submitCount() {
  if (!stocktake.value) return

  const lines = Object.entries(counts.value)
    .filter(([, value]) => value !== null && value !== undefined && Number(value) >= 0)
    .map(([detailId, value]) => ({ detailId, countedQuantity: Number(value) }))

  if (lines.length === 0) {
    app.notify('請至少輸入一筆盤點數量。', 'warning')
    return
  }

  acting.value = true
  try {
    await stocktakeApi.count(stocktake.value.id, lines)
    app.notify('盤點數已儲存。')
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '儲存失敗。', 'error')
  } finally {
    acting.value = false
  }
}

async function recount() {
  if (!stocktake.value || selected.value.size === 0) return
  acting.value = true
  try {
    await stocktakeApi.recount(stocktake.value.id, [...selected.value])
    app.notify(`已將 ${selected.value.size} 筆明細轉為複盤。`)
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '操作失敗。', 'error')
  } finally {
    acting.value = false
  }
}

async function complete() {
  if (!stocktake.value) return
  acting.value = true
  try {
    await stocktakeApi.complete(stocktake.value.id)
    app.notify('盤點已結束，等待核准。')
    completeOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '操作失敗。', 'error')
  } finally {
    acting.value = false
  }
}

async function approve() {
  if (!stocktake.value) return
  acting.value = true
  try {
    await stocktakeApi.approve(stocktake.value.id)
    app.notify('盤點已核准，庫存差異已調整。')
    approveOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '核准失敗。', 'error')
  } finally {
    acting.value = false
  }
}

async function cancel() {
  if (!stocktake.value) return
  acting.value = true
  try {
    await stocktakeApi.cancel(stocktake.value.id)
    app.notify('盤點單已取消。')
    cancelOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '取消失敗。', 'error')
  } finally {
    acting.value = false
  }
}

function toggleSelect(id: string) {
  const next = new Set(selected.value)
  if (next.has(id)) {
    next.delete(id)
  } else {
    next.add(id)
  }
  selected.value = next
}

onMounted(load)
</script>

<template>
  <div>
    <PageHeader
      :title="stocktake ? `盤點單 ${stocktake.stocktakeNo}` : '盤點單'"
      description="輸入實盤數量 → 結束盤點 → 核准後系統自動調整庫存"
    >
      <template #actions>
        <RouterLink :to="{ name: 'stocktake' }" class="btn-secondary">回列表</RouterLink>

        <button
          v-if="stocktake && stocktake.status !== 'APPROVED' && stocktake.status !== 'CANCELLED' && auth.can(P.stocktakeCreate)"
          type="button"
          class="btn-secondary"
          @click="cancelOpen = true"
        >
          取消單據
        </button>

        <button
          v-if="stocktake?.status === 'DRAFT' && auth.can(P.stocktakeCount)"
          type="button"
          class="btn-primary"
          @click="startOpen = true"
        >
          開始盤點
        </button>

        <button v-if="canCount" type="button" class="btn-primary" @click="completeOpen = true">結束盤點</button>

        <button
          v-if="stocktake?.status === 'PENDING_APPROVAL' && auth.can(P.stocktakeApprove)"
          type="button"
          class="btn-primary"
          @click="approveOpen = true"
        >
          核准並調整庫存
        </button>
      </template>
    </PageHeader>

    <p v-if="loading" class="card p-10 text-center text-slate-400">載入中…</p>

    <template v-else-if="stocktake">
      <section class="card mb-4 p-5">
        <dl class="grid grid-cols-2 gap-x-6 gap-y-4 text-sm md:grid-cols-4">
          <div>
            <dt class="text-slate-500 dark:text-slate-400">狀態</dt>
            <dd class="mt-1"><StatusBadge :status="stocktake.status" /></dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">範圍</dt>
            <dd class="mt-1 font-medium">
              {{ stocktake.warehouseCode }} · {{ stocktake.zoneCode || '全倉' }}
            </dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">盤點進度</dt>
            <dd class="mt-1">{{ stocktake.countedCount }} / {{ stocktake.lineCount }} 筆</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">差異筆數</dt>
            <dd class="mt-1" :class="stocktake.differenceCount > 0 ? 'font-medium text-rose-600 dark:text-rose-400' : ''">
              {{ stocktake.differenceCount }} 筆（合計 {{ formatQty(differenceTotal) }}）
            </dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">建立者</dt>
            <dd class="mt-1">{{ stocktake.createdByName || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">開始時間</dt>
            <dd class="mt-1">{{ formatDateTime(stocktake.startedAt) }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">核准者 / 時間</dt>
            <dd class="mt-1">
              {{ stocktake.approvedByName || '-' }}
              <span class="text-slate-400"> · {{ formatDateTime(stocktake.approvedAt) }}</span>
            </dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">備註</dt>
            <dd class="mt-1">{{ stocktake.remark || '-' }}</dd>
          </div>
        </dl>
      </section>

      <section class="card overflow-hidden">
        <div class="flex flex-wrap items-center justify-between gap-2 border-b border-slate-200 px-4 py-3 dark:border-slate-700">
          <h2 class="text-sm font-semibold text-slate-700 dark:text-slate-200">盤點明細</h2>

          <div v-if="canCount" class="flex flex-wrap items-center gap-2">
            <button type="button" class="btn-secondary btn-sm" @click="fillWithSystemQuantity">帳面數量帶入未填欄位</button>
            <button
              type="button"
              class="btn-secondary btn-sm"
              :disabled="selected.size === 0 || acting"
              @click="recount"
            >
              指定複盤（{{ selected.size }}）
            </button>
            <button type="button" class="btn-primary btn-sm" :disabled="acting" @click="submitCount">
              {{ acting ? '儲存中…' : '儲存盤點數' }}
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                <th v-if="canCount" class="w-10 px-3 py-2.5" />
                <th class="px-4 py-2.5 font-medium">儲位</th>
                <th class="px-4 py-2.5 font-medium">物料</th>
                <th class="px-4 py-2.5 text-right font-medium">帳面數量</th>
                <th class="px-4 py-2.5 font-medium" style="width: 150px">實盤數量</th>
                <th class="px-4 py-2.5 text-right font-medium">差異</th>
                <th class="px-4 py-2.5 text-center font-medium">狀態</th>
                <th class="px-4 py-2.5 font-medium">盤點人員</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
              <tr v-for="d in stocktake.details" :key="d.id">
                <td v-if="canCount" class="px-3 py-2.5">
                  <input
                    type="checkbox"
                    class="size-4 rounded border-slate-300 text-blue-600"
                    :checked="selected.has(d.id)"
                    @change="toggleSelect(d.id)"
                  />
                </td>
                <td class="px-4 py-2.5 font-mono text-xs">{{ d.locationCode }}</td>
                <td class="px-4 py-2.5">
                  <span class="font-mono text-xs">{{ d.materialCode }}</span>
                  <span class="ml-2 text-slate-600 dark:text-slate-300">{{ d.materialName }}</span>
                </td>
                <td class="px-4 py-2.5 text-right">{{ formatQty(d.systemQuantity) }} {{ d.baseUom }}</td>
                <td class="px-4 py-2.5">
                  <input
                    v-if="canCount"
                    v-model.number="counts[d.id]"
                    type="number"
                    min="0"
                    step="any"
                    class="!py-1 !text-sm"
                    placeholder="未盤"
                  />
                  <span v-else>{{ d.countedQuantity === null ? '未盤' : formatQty(d.countedQuantity) }}</span>
                </td>
                <td
                  class="px-4 py-2.5 text-right font-medium"
                  :class="
                    d.differenceQuantity > 0
                      ? 'text-emerald-600 dark:text-emerald-400'
                      : d.differenceQuantity < 0
                        ? 'text-rose-600 dark:text-rose-400'
                        : 'text-slate-400'
                  "
                >
                  {{ d.differenceQuantity > 0 ? '+' : '' }}{{ formatQty(d.differenceQuantity) }}
                </td>
                <td class="px-4 py-2.5 text-center"><StatusBadge :status="d.status" /></td>
                <td class="px-4 py-2.5 text-xs text-slate-500">
                  {{ d.countedByName || '-' }}
                  <span v-if="d.countedAt" class="block text-slate-400">{{ formatDateTime(d.countedAt) }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </template>

    <ConfirmDialog
      v-model="startOpen"
      title="開始盤點"
      message="開始後即可輸入實盤數量。"
      confirm-text="開始盤點"
      :busy="acting"
      @confirm="start"
    />

    <ConfirmDialog
      v-model="completeOpen"
      title="結束盤點"
      :message="
        uncountedCount > 0
          ? `尚有 ${uncountedCount} 筆明細未輸入盤點數，結束盤點將會失敗。`
          : '確定要結束盤點嗎？結束後將進入待核准狀態，無法再修改盤點數。'
      "
      confirm-text="結束盤點"
      :busy="acting"
      @confirm="complete"
    />

    <ConfirmDialog
      v-model="approveOpen"
      title="核准盤點"
      :message="`核准後系統會依差異自動調整庫存，並為每一筆差異建立 STOCKTAKE 異動紀錄。\n目前共有 ${stocktake?.differenceCount ?? 0} 筆差異，合計 ${formatQty(differenceTotal)}。\n此動作無法復原。`"
      confirm-text="核准並調整庫存"
      :busy="acting"
      @confirm="approve"
    />

    <ConfirmDialog
      v-model="cancelOpen"
      title="取消盤點單"
      message="確定要取消這張盤點單嗎？已輸入的盤點數將不會套用到庫存。"
      confirm-text="確定取消"
      danger
      :busy="acting"
      @confirm="cancel"
    />
  </div>
</template>
