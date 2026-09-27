<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/PageHeader.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { inboundApi, locationApi, putawayApi } from '@/api'
import { ApiException } from '@/api/client'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { formatDateTime, formatQty } from '@/utils/format'
import { sourceTypeLabel } from '@/utils/labels'
import { P } from '@/utils/permissions'
import type { InboundOrder, PutawayTask, WarehouseLocation } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const order = ref<InboundOrder | null>(null)
const tasks = ref<PutawayTask[]>([])
const loading = ref(true)

const receiveOpen = ref(false)
const receiving = ref(false)
const receiveError = ref('')
const receivingLocations = ref<WarehouseLocation[]>([])

const receiveForm = reactive({
  receivingLocationId: '',
  remark: '',
  lines: [] as { detailId: string; materialCode: string; materialName: string; remaining: number; quantity: number | null }[],
})

const completeOpen = ref(false)
const cancelOpen = ref(false)
const acting = ref(false)

const canReceive = computed(
  () => order.value && !['COMPLETED', 'CANCELLED'].includes(order.value.status) && auth.can(P.inboundReceive),
)

const canComplete = computed(
  () =>
    order.value &&
    !['COMPLETED', 'CANCELLED', 'DRAFT'].includes(order.value.status) &&
    auth.can(P.inboundComplete),
)

const openTaskCount = computed(() => tasks.value.filter((t) => !['COMPLETED', 'CANCELLED'].includes(t.status)).length)

async function load() {
  loading.value = true
  try {
    order.value = await inboundApi.get(route.params.id as string)
    tasks.value = (
      await putawayApi.query({ warehouseId: order.value.warehouseId, pageSize: 100 })
    ).items.filter((t) => t.inboundOrderNo === order.value?.orderNo)
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '載入入庫單失敗。', 'error')
    router.push({ name: 'inbound-orders' })
  } finally {
    loading.value = false
  }
}

async function openReceive() {
  if (!order.value) return

  receiveError.value = ''
  receiveForm.remark = ''
  receiveForm.lines = order.value.details
    .map((d) => ({
      detailId: d.id,
      materialCode: d.materialCode,
      materialName: d.materialName,
      remaining: d.orderedQuantity - d.receivedQuantity,
      quantity: d.orderedQuantity - d.receivedQuantity || null,
    }))
    .filter((l) => l.remaining > 0)

  if (receiveForm.lines.length === 0) {
    app.notify('所有明細都已收貨完成。', 'info')
    return
  }

  // 收貨儲位預設帶該倉庫的收貨區。
  const locations = await locationApi.query({ warehouseId: order.value.warehouseId, pageSize: 200, isActive: true })
  receivingLocations.value = locations.items.filter((l) => l.zoneType === 'RECEIVING' || l.locationType === 'STAGING')
  receiveForm.receivingLocationId = receivingLocations.value[0]?.id ?? ''

  receiveOpen.value = true
}

async function submitReceive() {
  if (!order.value) return

  const lines = receiveForm.lines
    .filter((l) => Number(l.quantity) > 0)
    .map((l) => ({ inboundDetailId: l.detailId, receivedQuantity: Number(l.quantity) }))

  if (lines.length === 0) {
    receiveError.value = '請至少輸入一筆收貨數量。'
    return
  }

  const over = receiveForm.lines.find((l) => Number(l.quantity) > l.remaining)
  if (over) {
    receiveError.value = `${over.materialCode} 收貨數量不可超過未收數量 ${formatQty(over.remaining)}。`
    return
  }

  receiving.value = true
  receiveError.value = ''
  try {
    await inboundApi.receive(order.value.id, {
      lines,
      receivingLocationId: receiveForm.receivingLocationId || null,
      remark: receiveForm.remark || null,
    })
    app.notify('收貨完成，已自動產生上架任務。')
    receiveOpen.value = false
    await load()
  } catch (e) {
    receiveError.value = e instanceof ApiException ? e.message : '收貨失敗。'
  } finally {
    receiving.value = false
  }
}

async function complete() {
  if (!order.value) return
  acting.value = true
  try {
    await inboundApi.complete(order.value.id)
    app.notify('入庫單已結案。')
    completeOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '結案失敗。', 'error')
  } finally {
    acting.value = false
  }
}

async function cancel() {
  if (!order.value) return
  acting.value = true
  try {
    await inboundApi.cancel(order.value.id)
    app.notify('入庫單已取消。')
    cancelOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '取消失敗。', 'error')
  } finally {
    acting.value = false
  }
}

onMounted(load)
</script>

<template>
  <div>
    <PageHeader :title="order ? `入庫單 ${order.orderNo}` : '入庫單'" description="收貨後系統會自動產生上架任務">
      <template #actions>
        <RouterLink :to="{ name: 'inbound-orders' }" class="btn-secondary">回列表</RouterLink>
        <button
          v-if="order?.status === 'DRAFT' && auth.can(P.inboundCreate)"
          type="button"
          class="btn-secondary"
          @click="cancelOpen = true"
        >
          取消單據
        </button>
        <button v-if="canReceive" type="button" class="btn-primary" @click="openReceive">收貨</button>
        <button v-if="canComplete" type="button" class="btn-primary" @click="completeOpen = true">結案</button>
      </template>
    </PageHeader>

    <p v-if="loading" class="card p-10 text-center text-slate-400">載入中…</p>

    <template v-else-if="order">
      <!-- 單頭 -->
      <section class="card mb-4 p-5">
        <dl class="grid grid-cols-2 gap-x-6 gap-y-4 text-sm md:grid-cols-4">
          <div>
            <dt class="text-slate-500 dark:text-slate-400">狀態</dt>
            <dd class="mt-1"><StatusBadge :status="order.status" /></dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">倉庫</dt>
            <dd class="mt-1 font-medium">{{ order.warehouseCode }} － {{ order.warehouseName }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">來源</dt>
            <dd class="mt-1">{{ sourceTypeLabel[order.sourceType] }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">外部單號</dt>
            <dd class="mt-1">{{ order.externalOrderNo || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">供應商</dt>
            <dd class="mt-1">{{ order.supplierName || order.supplierCode || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">建立者 / 時間</dt>
            <dd class="mt-1">{{ order.createdByName || '-' }}<span class="text-slate-400"> · {{ formatDateTime(order.createdAt) }}</span></dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">收貨時間</dt>
            <dd class="mt-1">{{ formatDateTime(order.receivedAt) }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">備註</dt>
            <dd class="mt-1">{{ order.remark || '-' }}</dd>
          </div>
        </dl>
      </section>

      <!-- 明細 -->
      <section class="card mb-4 overflow-hidden">
        <div class="border-b border-slate-200 px-4 py-3 dark:border-slate-700">
          <h2 class="text-sm font-semibold text-slate-700 dark:text-slate-200">入庫明細</h2>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                <th class="px-4 py-2.5 font-medium">#</th>
                <th class="px-4 py-2.5 font-medium">物料</th>
                <th class="px-4 py-2.5 text-right font-medium">應收量</th>
                <th class="px-4 py-2.5 text-right font-medium">已收量</th>
                <th class="px-4 py-2.5 text-right font-medium">已上架</th>
                <th class="px-4 py-2.5 text-center font-medium">狀態</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
              <tr v-for="d in order.details" :key="d.id">
                <td class="px-4 py-2.5 text-slate-400">{{ d.lineNo }}</td>
                <td class="px-4 py-2.5">
                  <span class="font-mono text-xs font-medium">{{ d.materialCode }}</span>
                  <span class="ml-2 text-slate-600 dark:text-slate-300">{{ d.materialName }}</span>
                </td>
                <td class="px-4 py-2.5 text-right">{{ formatQty(d.orderedQuantity) }} {{ d.baseUom }}</td>
                <td class="px-4 py-2.5 text-right" :class="d.receivedQuantity >= d.orderedQuantity ? 'text-emerald-600 dark:text-emerald-400' : ''">
                  {{ formatQty(d.receivedQuantity) }}
                </td>
                <td class="px-4 py-2.5 text-right">{{ formatQty(d.putawayQuantity) }}</td>
                <td class="px-4 py-2.5 text-center"><StatusBadge :status="d.status" /></td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <!-- 上架任務 -->
      <section class="card overflow-hidden">
        <div class="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-700">
          <h2 class="text-sm font-semibold text-slate-700 dark:text-slate-200">上架任務</h2>
          <RouterLink v-if="tasks.length" :to="{ name: 'putaway' }" class="link text-xs">前往上架作業</RouterLink>
        </div>

        <div v-if="tasks.length" class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                <th class="px-4 py-2.5 font-medium">任務單號</th>
                <th class="px-4 py-2.5 font-medium">物料</th>
                <th class="px-4 py-2.5 text-right font-medium">數量</th>
                <th class="px-4 py-2.5 font-medium">來源儲位</th>
                <th class="px-4 py-2.5 font-medium">目標儲位</th>
                <th class="px-4 py-2.5 text-center font-medium">狀態</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
              <tr v-for="t in tasks" :key="t.id">
                <td class="px-4 py-2.5 font-mono text-xs">{{ t.taskNo }}</td>
                <td class="px-4 py-2.5">
                  <span class="font-mono text-xs">{{ t.materialCode }}</span>
                  <span class="ml-2 text-slate-600 dark:text-slate-300">{{ t.materialName }}</span>
                </td>
                <td class="px-4 py-2.5 text-right">{{ formatQty(t.quantity) }} {{ t.baseUom }}</td>
                <td class="px-4 py-2.5 font-mono text-xs text-slate-500">{{ t.sourceLocationCode || '-' }}</td>
                <td class="px-4 py-2.5 font-mono text-xs text-slate-500">{{ t.targetLocationCode || '未指定' }}</td>
                <td class="px-4 py-2.5 text-center"><StatusBadge :status="t.status" /></td>
              </tr>
            </tbody>
          </table>
        </div>

        <p v-else class="px-4 py-10 text-center text-sm text-slate-400">尚未產生上架任務，請先執行收貨</p>
      </section>
    </template>

    <!-- 收貨 -->
    <AppModal v-model="receiveOpen" title="收貨" size="lg" :busy="receiving">
      <form id="receive-form" class="space-y-4" @submit.prevent="submitReceive">
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label class="label" for="r-location">收貨暫存儲位</label>
            <select id="r-location" v-model="receiveForm.receivingLocationId">
              <option value="">系統自動判斷</option>
              <option v-for="l in receivingLocations" :key="l.id" :value="l.id">{{ l.code }} － {{ l.name }}</option>
            </select>
          </div>
          <div>
            <label class="label" for="r-remark">備註</label>
            <input id="r-remark" v-model="receiveForm.remark" type="text" maxlength="500" />
          </div>
        </div>

        <div class="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-700">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                <th class="px-3 py-2 font-medium">物料</th>
                <th class="w-28 px-3 py-2 text-right font-medium">未收數量</th>
                <th class="w-40 px-3 py-2 font-medium">本次收貨</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
              <tr v-for="line in receiveForm.lines" :key="line.detailId">
                <td class="px-3 py-2">
                  <span class="font-mono text-xs font-medium">{{ line.materialCode }}</span>
                  <span class="ml-2 text-slate-600 dark:text-slate-300">{{ line.materialName }}</span>
                </td>
                <td class="px-3 py-2 text-right text-slate-500">{{ formatQty(line.remaining) }}</td>
                <td class="px-3 py-2">
                  <input v-model.number="line.quantity" type="number" min="0" :max="line.remaining" step="any" placeholder="0" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <p
          class="rounded-lg bg-blue-50 px-3 py-2 text-xs text-blue-700 dark:bg-blue-500/10 dark:text-blue-300"
        >
          收貨後庫存會先進入收貨暫存區，並自動產生對應的上架任務。
        </p>

        <p v-if="receiveError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ receiveError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="receiving" @click="receiveOpen = false">取消</button>
        <button type="submit" form="receive-form" class="btn-primary" :disabled="receiving">
          {{ receiving ? '處理中…' : '確認收貨' }}
        </button>
      </template>
    </AppModal>

    <ConfirmDialog
      v-model="completeOpen"
      title="入庫單結案"
      :message="
        openTaskCount > 0
          ? `尚有 ${openTaskCount} 筆上架任務未完成，結案將會失敗。請先完成所有上架任務。`
          : '確定要將此入庫單結案嗎？結案後將無法再收貨。'
      "
      confirm-text="確定結案"
      :busy="acting"
      @confirm="complete"
    />

    <ConfirmDialog
      v-model="cancelOpen"
      title="取消入庫單"
      message="確定要取消這張入庫單嗎？只有草稿狀態的單據可以取消。"
      confirm-text="確定取消"
      danger
      :busy="acting"
      @confirm="cancel"
    />
  </div>
</template>
