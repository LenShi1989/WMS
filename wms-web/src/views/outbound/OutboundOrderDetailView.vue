<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/PageHeader.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { outboundApi, pickingApi } from '@/api'
import { ApiException } from '@/api/client'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { formatDateTime, formatQty } from '@/utils/format'
import { P } from '@/utils/permissions'
import type { OutboundOrder, PickTask } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const order = ref<OutboundOrder | null>(null)
const tasks = ref<PickTask[]>([])
const loading = ref(true)
const acting = ref(false)

const allocateOpen = ref(false)
const releaseOpen = ref(false)
const shipOpen = ref(false)
const cancelOpen = ref(false)

const canAllocate = computed(
  () => order.value && ['DRAFT', 'ALLOCATED'].includes(order.value.status) && auth.can(P.outboundAllocate),
)

const canRelease = computed(
  () =>
    order.value &&
    !['SHIPPED', 'CANCELLED', 'DRAFT'].includes(order.value.status) &&
    auth.can(P.outboundAllocate),
)

const canShip = computed(
  () => order.value && order.value.status === 'PICKED' && auth.can(P.outboundShip),
)

const openTaskCount = computed(() => tasks.value.filter((t) => !['COMPLETED', 'CANCELLED'].includes(t.status)).length)

async function load() {
  loading.value = true
  try {
    order.value = await outboundApi.get(route.params.id as string)
    tasks.value = (await pickingApi.query({ outboundOrderId: order.value.id, pageSize: 100 })).items
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '載入出庫單失敗。', 'error')
    router.push({ name: 'outbound-orders' })
  } finally {
    loading.value = false
  }
}

async function allocate() {
  if (!order.value) return
  acting.value = true
  try {
    const result = await outboundApi.allocate(order.value.id)
    if (result.fullyAllocated) {
      app.notify(`庫存分配完成，已產生 ${result.createdPickTaskCount} 筆揀貨任務。`)
    } else {
      const shortage = result.lines.filter((l) => l.shortageQuantity > 0)
      app.notify(
        `部分明細庫存不足：${shortage.map((l) => `${l.materialCode} 缺 ${formatQty(l.shortageQuantity)}`).join('、')}`,
        'warning',
      )
    }
    allocateOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '庫存分配失敗。', 'error')
  } finally {
    acting.value = false
  }
}

async function release() {
  if (!order.value) return
  acting.value = true
  try {
    await outboundApi.release(order.value.id)
    app.notify('已取消分配並釋放預留庫存。')
    releaseOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '操作失敗。', 'error')
  } finally {
    acting.value = false
  }
}

async function ship() {
  if (!order.value) return
  acting.value = true
  try {
    await outboundApi.ship(order.value.id)
    app.notify('出貨完成。')
    shipOpen.value = false
    await load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '出貨失敗。', 'error')
  } finally {
    acting.value = false
  }
}

async function cancel() {
  if (!order.value) return
  acting.value = true
  try {
    await outboundApi.cancel(order.value.id)
    app.notify('出庫單已取消。')
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
    <PageHeader :title="order ? `出庫單 ${order.orderNo}` : '出庫單'" description="分配庫存 → 揀貨 → 出貨">
      <template #actions>
        <RouterLink :to="{ name: 'outbound-orders' }" class="btn-secondary">回列表</RouterLink>
        <button
          v-if="order && !['SHIPPED', 'CANCELLED'].includes(order.status) && auth.can(P.outboundCreate)"
          type="button"
          class="btn-secondary"
          @click="cancelOpen = true"
        >
          取消單據
        </button>
        <button v-if="canRelease" type="button" class="btn-secondary" @click="releaseOpen = true">取消分配</button>
        <button v-if="canAllocate" type="button" class="btn-primary" @click="allocateOpen = true">庫存分配</button>
        <button v-if="canShip" type="button" class="btn-primary" @click="shipOpen = true">出貨</button>
      </template>
    </PageHeader>

    <p v-if="loading" class="card p-10 text-center text-slate-400">載入中…</p>

    <template v-else-if="order">
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
            <dt class="text-slate-500 dark:text-slate-400">客戶</dt>
            <dd class="mt-1">{{ order.customerName || order.customerCode || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">外部單號</dt>
            <dd class="mt-1">{{ order.externalOrderNo || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">優先順序</dt>
            <dd class="mt-1">{{ order.priority }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">建立者 / 時間</dt>
            <dd class="mt-1">{{ order.createdByName || '-' }}<span class="text-slate-400"> · {{ formatDateTime(order.createdAt) }}</span></dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">出貨時間</dt>
            <dd class="mt-1">{{ formatDateTime(order.shippedAt) }}</dd>
          </div>
          <div>
            <dt class="text-slate-500 dark:text-slate-400">備註</dt>
            <dd class="mt-1">{{ order.remark || '-' }}</dd>
          </div>
        </dl>
      </section>

      <section class="card mb-4 overflow-hidden">
        <div class="border-b border-slate-200 px-4 py-3 dark:border-slate-700">
          <h2 class="text-sm font-semibold text-slate-700 dark:text-slate-200">出庫明細</h2>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                <th class="px-4 py-2.5 font-medium">#</th>
                <th class="px-4 py-2.5 font-medium">物料</th>
                <th class="px-4 py-2.5 text-right font-medium">需求量</th>
                <th class="px-4 py-2.5 text-right font-medium">已分配</th>
                <th class="px-4 py-2.5 text-right font-medium">已揀貨</th>
                <th class="px-4 py-2.5 text-right font-medium">已出貨</th>
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
                <td class="px-4 py-2.5 text-right">{{ formatQty(d.requestedQuantity) }} {{ d.baseUom }}</td>
                <td class="px-4 py-2.5 text-right" :class="d.allocatedQuantity < d.requestedQuantity ? 'text-amber-600 dark:text-amber-400' : ''">
                  {{ formatQty(d.allocatedQuantity) }}
                </td>
                <td class="px-4 py-2.5 text-right">{{ formatQty(d.pickedQuantity) }}</td>
                <td class="px-4 py-2.5 text-right">{{ formatQty(d.shippedQuantity) }}</td>
                <td class="px-4 py-2.5 text-center"><StatusBadge :status="d.status" /></td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="card overflow-hidden">
        <div class="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-700">
          <h2 class="text-sm font-semibold text-slate-700 dark:text-slate-200">揀貨任務</h2>
          <RouterLink v-if="tasks.length" :to="{ name: 'picking' }" class="link text-xs">前往揀貨作業</RouterLink>
        </div>

        <div v-if="tasks.length" class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                <th class="px-4 py-2.5 font-medium">任務單號</th>
                <th class="px-4 py-2.5 font-medium">物料</th>
                <th class="px-4 py-2.5 font-medium">來源儲位</th>
                <th class="px-4 py-2.5 text-right font-medium">應揀量</th>
                <th class="px-4 py-2.5 text-right font-medium">已揀量</th>
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
                <td class="px-4 py-2.5 font-mono text-xs text-slate-500">{{ t.sourceLocationCode }}</td>
                <td class="px-4 py-2.5 text-right">{{ formatQty(t.quantity) }} {{ t.baseUom }}</td>
                <td class="px-4 py-2.5 text-right">{{ formatQty(t.pickedQuantity) }}</td>
                <td class="px-4 py-2.5 text-center"><StatusBadge :status="t.status" /></td>
              </tr>
            </tbody>
          </table>
        </div>

        <p v-else class="px-4 py-10 text-center text-sm text-slate-400">尚未產生揀貨任務，請先執行庫存分配</p>
      </section>
    </template>

    <ConfirmDialog
      v-model="allocateOpen"
      title="庫存分配"
      message="系統會依可用庫存預留數量並產生揀貨任務。若可用庫存不足，只會分配可以分配的部分。"
      confirm-text="開始分配"
      :busy="acting"
      @confirm="allocate"
    />

    <ConfirmDialog
      v-model="releaseOpen"
      title="取消分配"
      message="將釋放尚未揀出的預留庫存，並取消對應的揀貨任務。已揀出的部分不受影響。"
      confirm-text="確定取消分配"
      danger
      :busy="acting"
      @confirm="release"
    />

    <ConfirmDialog
      v-model="shipOpen"
      title="確認出貨"
      :message="
        openTaskCount > 0
          ? `尚有 ${openTaskCount} 筆揀貨任務未完成，出貨將會失敗。`
          : '確定要出貨嗎？出貨後單據將結案，無法再變更。'
      "
      confirm-text="確定出貨"
      :busy="acting"
      @confirm="ship"
    />

    <ConfirmDialog
      v-model="cancelOpen"
      title="取消出庫單"
      message="確定要取消這張出庫單嗎？系統會釋放所有預留庫存。已有揀貨紀錄的單據無法取消。"
      confirm-text="確定取消"
      danger
      :busy="acting"
      @confirm="cancel"
    />
  </div>
</template>
