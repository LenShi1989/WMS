<script setup lang="ts">
import { computed } from 'vue'
import { statusLabel, toneClass, type LabelMeta } from '@/utils/labels'

const props = defineProps<{
  /** 狀態代碼，會自動查表翻譯。 */
  status?: string | null
  /** 直接指定顯示內容，優先於 status。 */
  meta?: LabelMeta
}>()

const resolved = computed(() => props.meta ?? statusLabel(props.status))
</script>

<template>
  <span
    class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset whitespace-nowrap"
    :class="toneClass[resolved.tone]"
  >
    {{ resolved.text }}
  </span>
</template>
