<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import DataTable, { type Column } from '@/components/DataTable.vue'
import AppModal from '@/components/AppModal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { warehouseApi, zoneApi } from '@/api'
import { ApiException } from '@/api/client'
import { useDataTable } from '@/composables/useDataTable'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useMasterStore } from '@/stores/master'
import { zoneTypeLabel } from '@/utils/labels'
import { P } from '@/utils/permissions'
import type { Warehouse, Zone, ZoneType } from '@/types'

const app = useAppStore()
const auth = useAuthStore()
const master = useMasterStore()
const table = useDataTable<Warehouse>(warehouseApi.query)

const columns: Column[] = [
  { key: 'code', title: '倉庫編號', width: '130px' },
  { key: 'name', title: '倉庫名稱' },
  { key: 'description', title: '說明', hideOnMobile: true },
  { key: 'zoneCount', title: '儲區數', align: 'right', width: '90px' },
  { key: 'locationCount', title: '儲位數', align: 'right', width: '90px' },
  { key: 'isActive', title: '狀態', align: 'center', width: '90px' },
  { key: 'actions', title: '操作', align: 'right', width: '170px' },
]

// ---------- 倉庫 ----------
const emptyForm = () => ({ id: '', code: '', name: '', description: '', isActive: true })
const form = reactive(emptyForm())
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')

function openCreate() {
  Object.assign(form, emptyForm())
  formError.value = ''
  modalOpen.value = true
}

function openEdit(row: Warehouse) {
  Object.assign(form, { ...row, description: row.description ?? '' })
  formError.value = ''
  modalOpen.value = true
}

async function save() {
  saving.value = true
  formError.value = ''
  try {
    const body = {
      code: form.code.trim(),
      name: form.name.trim(),
      description: form.description || null,
      isActive: form.isActive,
    }
    if (form.id) {
      await warehouseApi.update(form.id, body)
      app.notify('倉庫已更新。')
    } else {
      await warehouseApi.create(body)
      app.notify('倉庫已建立。')
    }
    modalOpen.value = false
    master.invalidate()
    await table.load()
  } catch (e) {
    formError.value = e instanceof ApiException ? e.message : '儲存失敗。'
  } finally {
    saving.value = false
  }
}

const confirmOpen = ref(false)
const deleting = ref(false)
const target = ref<Warehouse | null>(null)

function confirmDelete(row: Warehouse) {
  target.value = row
  confirmOpen.value = true
}

async function remove() {
  if (!target.value) return
  deleting.value = true
  try {
    await warehouseApi.remove(target.value.id)
    app.notify('倉庫已刪除。')
    confirmOpen.value = false
    master.invalidate()
    await table.load()
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '刪除失敗。', 'error')
  } finally {
    deleting.value = false
  }
}

// ---------- 儲區 ----------
const zoneModalOpen = ref(false)
const zoneWarehouse = ref<Warehouse | null>(null)
const zones = ref<Zone[]>([])
const zonesLoading = ref(false)
const zoneTypes: ZoneType[] = ['RECEIVING', 'STORAGE', 'PICKING', 'STAGING', 'SHIPPING', 'QC', 'NG']

const emptyZone = () => ({ id: '', code: '', name: '', zoneType: 'STORAGE' as ZoneType, isActive: true })
const zoneForm = reactive(emptyZone())
const zoneSaving = ref(false)
const zoneError = ref('')

async function openZones(row: Warehouse) {
  zoneWarehouse.value = row
  zoneModalOpen.value = true
  Object.assign(zoneForm, emptyZone())
  zoneError.value = ''
  await loadZones()
}

async function loadZones() {
  if (!zoneWarehouse.value) return
  zonesLoading.value = true
  try {
    zones.value = (await zoneApi.query({ warehouseId: zoneWarehouse.value.id, pageSize: 100 })).items
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '載入儲區失敗。', 'error')
  } finally {
    zonesLoading.value = false
  }
}

function editZone(zone: Zone) {
  Object.assign(zoneForm, {
    id: zone.id,
    code: zone.code,
    name: zone.name,
    zoneType: zone.zoneType,
    isActive: zone.isActive,
  })
  zoneError.value = ''
}

async function saveZone() {
  if (!zoneWarehouse.value) return
  zoneSaving.value = true
  zoneError.value = ''
  try {
    const body = {
      warehouseId: zoneWarehouse.value.id,
      code: zoneForm.code.trim(),
      name: zoneForm.name.trim(),
      zoneType: zoneForm.zoneType,
      isActive: zoneForm.isActive,
    }
    if (zoneForm.id) {
      await zoneApi.update(zoneForm.id, body)
      app.notify('儲區已更新。')
    } else {
      await zoneApi.create(body)
      app.notify('儲區已建立。')
    }
    Object.assign(zoneForm, emptyZone())
    master.invalidate()
    await Promise.all([loadZones(), table.load()])
  } catch (e) {
    zoneError.value = e instanceof ApiException ? e.message : '儲存失敗。'
  } finally {
    zoneSaving.value = false
  }
}

async function removeZone(zone: Zone) {
  try {
    await zoneApi.remove(zone.id)
    app.notify('儲區已刪除。')
    master.invalidate()
    await Promise.all([loadZones(), table.load()])
  } catch (e) {
    app.notify(e instanceof ApiException ? e.message : '刪除失敗。', 'error')
  }
}

onMounted(table.load)
</script>

<template>
  <div>
    <PageHeader title="倉庫與儲區" description="維護倉庫以及倉庫底下的收貨、儲存、揀貨等儲區">
      <template #actions>
        <button v-if="auth.can(P.warehouseManage)" type="button" class="btn-primary" @click="openCreate">
          新增倉庫
        </button>
      </template>
    </PageHeader>

    <DataTable
      :columns="columns"
      :rows="table.items.value"
      :loading="table.loading.value"
      :total="table.total.value"
      :page="table.page.value"
      :page-size="table.pageSize.value"
      empty-text="尚未建立任何倉庫"
      @update:page="table.page.value = $event"
      @update:page-size="table.pageSize.value = $event"
    >
      <template #cell-code="{ row }">
        <span class="font-mono text-xs font-medium">{{ row.code }}</span>
      </template>

      <template #cell-description="{ row }">
        <span class="text-slate-500">{{ row.description || '-' }}</span>
      </template>

      <template #cell-isActive="{ row }">
        <StatusBadge :meta="row.isActive ? { text: '啟用', tone: 'green' } : { text: '停用', tone: 'gray' }" />
      </template>

      <template #cell-actions="{ row }">
        <div class="flex justify-end gap-1">
          <button type="button" class="btn-ghost btn-sm" @click="openZones(row)">儲區</button>
          <button v-if="auth.can(P.warehouseManage)" type="button" class="btn-ghost btn-sm" @click="openEdit(row)">編輯</button>
          <button
            v-if="auth.can(P.warehouseManage)"
            type="button"
            class="btn-ghost btn-sm !text-rose-600 hover:!bg-rose-50 dark:hover:!bg-rose-500/10"
            @click="confirmDelete(row)"
          >
            刪除
          </button>
        </div>
      </template>
    </DataTable>

    <!-- 倉庫表單 -->
    <AppModal v-model="modalOpen" :title="form.id ? '編輯倉庫' : '新增倉庫'" size="sm" :busy="saving">
      <form id="warehouse-form" class="space-y-4" @submit.prevent="save">
        <div>
          <label class="label" for="w-code">倉庫編號 <span class="text-rose-500">*</span></label>
          <input id="w-code" v-model="form.code" type="text" required maxlength="50" placeholder="例如 WH01" />
        </div>
        <div>
          <label class="label" for="w-name">倉庫名稱 <span class="text-rose-500">*</span></label>
          <input id="w-name" v-model="form.name" type="text" required maxlength="200" />
        </div>
        <div>
          <label class="label" for="w-desc">說明</label>
          <textarea id="w-desc" v-model="form.description" rows="2" maxlength="500" />
        </div>
        <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
          <input v-model="form.isActive" type="checkbox" class="size-4 rounded border-slate-300 text-blue-600" />
          啟用此倉庫
        </label>
        <p v-if="formError" class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
          {{ formError }}
        </p>
      </form>

      <template #footer>
        <button type="button" class="btn-secondary" :disabled="saving" @click="modalOpen = false">取消</button>
        <button type="submit" form="warehouse-form" class="btn-primary" :disabled="saving">
          {{ saving ? '儲存中…' : '儲存' }}
        </button>
      </template>
    </AppModal>

    <!-- 儲區管理 -->
    <AppModal v-model="zoneModalOpen" :title="`${zoneWarehouse?.code} 儲區管理`" size="lg">
      <div class="space-y-5">
        <div class="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-700">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-left text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
                <th class="px-3 py-2 font-medium">編號</th>
                <th class="px-3 py-2 font-medium">名稱</th>
                <th class="px-3 py-2 font-medium">類型</th>
                <th class="px-3 py-2 text-right font-medium">儲位數</th>
                <th class="px-3 py-2 text-center font-medium">狀態</th>
                <th class="px-3 py-2 text-right font-medium">操作</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
              <tr v-if="zonesLoading">
                <td colspan="6" class="px-3 py-8 text-center text-slate-400">載入中…</td>
              </tr>
              <tr v-else-if="zones.length === 0">
                <td colspan="6" class="px-3 py-8 text-center text-slate-400">此倉庫尚未建立儲區</td>
              </tr>
              <tr v-else v-for="zone in zones" :key="zone.id">
                <td class="px-3 py-2 font-mono text-xs">{{ zone.code }}</td>
                <td class="px-3 py-2">{{ zone.name }}</td>
                <td class="px-3 py-2">{{ zoneTypeLabel[zone.zoneType] }}</td>
                <td class="px-3 py-2 text-right">{{ zone.locationCount }}</td>
                <td class="px-3 py-2 text-center">
                  <StatusBadge :meta="zone.isActive ? { text: '啟用', tone: 'green' } : { text: '停用', tone: 'gray' }" />
                </td>
                <td class="px-3 py-2 text-right">
                  <button v-if="auth.can(P.zoneManage)" type="button" class="btn-ghost btn-sm" @click="editZone(zone)">編輯</button>
                  <button
                    v-if="auth.can(P.zoneManage)"
                    type="button"
                    class="btn-ghost btn-sm !text-rose-600"
                    @click="removeZone(zone)"
                  >
                    刪除
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <form v-if="auth.can(P.zoneManage)" class="rounded-lg bg-slate-50 p-4 dark:bg-slate-900/40" @submit.prevent="saveZone">
          <p class="mb-3 text-sm font-medium text-slate-700 dark:text-slate-200">
            {{ zoneForm.id ? '編輯儲區' : '新增儲區' }}
          </p>

          <div class="grid grid-cols-1 gap-3 sm:grid-cols-4">
            <div>
              <label class="label" for="z-code">編號 <span class="text-rose-500">*</span></label>
              <input id="z-code" v-model="zoneForm.code" type="text" required maxlength="50" placeholder="RCV" />
            </div>
            <div>
              <label class="label" for="z-name">名稱 <span class="text-rose-500">*</span></label>
              <input id="z-name" v-model="zoneForm.name" type="text" required maxlength="200" />
            </div>
            <div>
              <label class="label" for="z-type">類型</label>
              <select id="z-type" v-model="zoneForm.zoneType">
                <option v-for="t in zoneTypes" :key="t" :value="t">{{ zoneTypeLabel[t] }}</option>
              </select>
            </div>
            <div class="flex items-end gap-2">
              <button type="submit" class="btn-primary flex-1" :disabled="zoneSaving">
                {{ zoneSaving ? '儲存中…' : zoneForm.id ? '更新' : '新增' }}
              </button>
              <button
                v-if="zoneForm.id"
                type="button"
                class="btn-secondary"
                @click="Object.assign(zoneForm, { id: '', code: '', name: '', zoneType: 'STORAGE', isActive: true })"
              >
                取消
              </button>
            </div>
          </div>

          <p v-if="zoneError" class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">
            {{ zoneError }}
          </p>
        </form>
      </div>
    </AppModal>

    <ConfirmDialog
      v-model="confirmOpen"
      title="刪除倉庫"
      :message="`確定要刪除倉庫「${target?.code} ${target?.name}」嗎？\n倉庫底下若還有儲位或庫存將無法刪除。`"
      confirm-text="確定刪除"
      danger
      :busy="deleting"
      @confirm="remove"
    />
  </div>
</template>
