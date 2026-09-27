<script setup lang="ts" generic="T extends Record<string, any>">
import { computed } from 'vue'

export interface Column {
  key: string
  title: string
  /** 對齊方式，數量欄位通常靠右。 */
  align?: 'left' | 'right' | 'center'
  width?: string
  /** 小螢幕時隱藏次要欄位。 */
  hideOnMobile?: boolean
}

const props = withDefaults(
  defineProps<{
    columns: Column[]
    rows: T[]
    loading?: boolean
    rowKey?: string
    emptyText?: string
    total?: number
    page?: number
    pageSize?: number
  }>(),
  { loading: false, rowKey: 'id', emptyText: '目前沒有資料', total: 0, page: 1, pageSize: 20 },
)

const emit = defineEmits<{
  (e: 'update:page', value: number): void
  (e: 'update:pageSize', value: number): void
}>()

const showPagination = computed(() => props.total > 0)
const totalPages = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)))
const rangeStart = computed(() => (props.total === 0 ? 0 : (props.page - 1) * props.pageSize + 1))
const rangeEnd = computed(() => Math.min(props.page * props.pageSize, props.total))

/** 產生頁碼按鈕，中間用 -1 代表省略號。 */
const pageButtons = computed<number[]>(() => {
  const last = totalPages.value
  const current = props.page
  if (last <= 7) return Array.from({ length: last }, (_, i) => i + 1)

  const pages = new Set<number>([1, last, current, current - 1, current + 1])
  const sorted = [...pages].filter((p) => p >= 1 && p <= last).sort((a, b) => a - b)

  const result: number[] = []
  let previous = 0
  for (const p of sorted) {
    if (previous && p - previous > 1) result.push(-1)
    result.push(p)
    previous = p
  }
  return result
})

function alignClass(align?: string) {
  return align === 'right' ? 'text-right' : align === 'center' ? 'text-center' : 'text-left'
}
</script>

<template>
  <div class="card overflow-hidden">
    <div class="overflow-x-auto">
      <table class="w-full min-w-full border-collapse text-sm">
        <thead>
          <tr class="border-b border-slate-200 bg-slate-50 dark:border-slate-700 dark:bg-slate-900/40">
            <th
              v-for="col in columns"
              :key="col.key"
              scope="col"
              class="px-4 py-3 font-semibold whitespace-nowrap text-slate-600 dark:text-slate-300"
              :class="[alignClass(col.align), col.hideOnMobile ? 'hidden md:table-cell' : '']"
              :style="col.width ? { width: col.width } : undefined"
            >
              {{ col.title }}
            </th>
          </tr>
        </thead>

        <tbody class="divide-y divide-slate-100 dark:divide-slate-700/60">
          <tr v-if="loading">
            <td :colspan="columns.length" class="px-4 py-16 text-center text-slate-400">
              <span class="inline-flex items-center gap-2">
                <svg class="size-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
                </svg>
                載入中…
              </span>
            </td>
          </tr>

          <tr v-else-if="rows.length === 0">
            <td :colspan="columns.length" class="px-4 py-16 text-center text-slate-400">
              {{ emptyText }}
            </td>
          </tr>

          <tr
            v-else
            v-for="(row, index) in rows"
            :key="(row as any)[rowKey] ?? index"
            class="transition hover:bg-slate-50 dark:hover:bg-slate-700/30"
          >
            <td
              v-for="col in columns"
              :key="col.key"
              class="px-4 py-3 align-middle text-slate-700 dark:text-slate-200"
              :class="[alignClass(col.align), col.hideOnMobile ? 'hidden md:table-cell' : '']"
            >
              <slot :name="`cell-${col.key}`" :row="row" :index="index" :value="(row as any)[col.key]">
                {{ (row as any)[col.key] ?? '-' }}
              </slot>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div
      v-if="showPagination"
      class="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 text-sm dark:border-slate-700"
    >
      <div class="flex items-center gap-2 text-slate-500 dark:text-slate-400">
        <span>第 {{ rangeStart }}–{{ rangeEnd }} 筆，共 {{ total }} 筆</span>
        <select
          class="!w-auto !py-1 !text-xs"
          :value="pageSize"
          @change="emit('update:pageSize', Number(($event.target as HTMLSelectElement).value))"
        >
          <option v-for="size in [10, 20, 50, 100]" :key="size" :value="size">每頁 {{ size }} 筆</option>
        </select>
      </div>

      <nav class="flex items-center gap-1" aria-label="分頁">
        <button
          type="button"
          class="btn-secondary btn-sm"
          :disabled="page <= 1"
          @click="emit('update:page', page - 1)"
        >
          上一頁
        </button>

        <template v-for="(p, i) in pageButtons" :key="`${p}-${i}`">
          <span v-if="p === -1" class="px-1.5 text-slate-400">…</span>
          <button
            v-else
            type="button"
            class="min-w-8 rounded-lg px-2 py-1 text-xs font-medium transition"
            :class="
              p === page
                ? 'bg-blue-600 text-white'
                : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-700'
            "
            @click="emit('update:page', p)"
          >
            {{ p }}
          </button>
        </template>

        <button
          type="button"
          class="btn-secondary btn-sm"
          :disabled="page >= totalPages"
          @click="emit('update:page', page + 1)"
        >
          下一頁
        </button>
      </nav>
    </div>
  </div>
</template>
