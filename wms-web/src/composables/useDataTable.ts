import { reactive, ref, watch } from 'vue'
import { ApiException } from '@/api/client'
import { useAppStore } from '@/stores/app'
import type { PagedResult } from '@/types'

type Fetcher<T> = (params: Record<string, unknown>) => Promise<PagedResult<T>>

/**
 * 列表頁共用邏輯：分頁、關鍵字、篩選條件與載入狀態。
 * 篩選條件改變時自動回到第 1 頁重新查詢。
 */
export function useDataTable<T>(fetcher: Fetcher<T>, initialFilters: Record<string, unknown> = {}) {
  const app = useAppStore()

  const items = ref<T[]>([]) as { value: T[] }
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(20)
  const keyword = ref('')
  const loading = ref(false)
  const filters = reactive({ ...initialFilters })

  let searchTimer: ReturnType<typeof setTimeout> | undefined

  async function load() {
    loading.value = true
    try {
      const result = await fetcher({
        page: page.value,
        pageSize: pageSize.value,
        keyword: keyword.value || undefined,
        ...filters,
      })
      items.value = result.items
      total.value = result.total

      // 刪除最後一筆後停在空白頁時，自動退回上一頁。
      if (result.items.length === 0 && page.value > 1) {
        page.value -= 1
        await load()
      }
    } catch (error) {
      app.notify(error instanceof ApiException ? error.message : '載入資料失敗。', 'error')
      items.value = []
      total.value = 0
    } finally {
      loading.value = false
    }
  }

  function reload() {
    page.value = 1
    return load()
  }

  /** 搜尋框輸入時延遲送出，避免每敲一個字就打一次 API。 */
  function onSearchInput() {
    clearTimeout(searchTimer)
    searchTimer = setTimeout(reload, 350)
  }

  watch(page, load)
  watch(pageSize, reload)
  watch(() => ({ ...filters }), reload, { deep: true })

  return { items, total, page, pageSize, keyword, loading, filters, load, reload, onSearchInput }
}
