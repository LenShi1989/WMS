<script setup lang="ts">
import AppModal from './AppModal.vue'

withDefaults(
  defineProps<{
    modelValue: boolean
    title?: string
    message: string
    confirmText?: string
    danger?: boolean
    busy?: boolean
  }>(),
  { title: '請確認', confirmText: '確定', danger: false, busy: false },
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'confirm'): void
}>()
</script>

<template>
  <AppModal
    :model-value="modelValue"
    :title="title"
    size="sm"
    :busy="busy"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <p class="text-sm whitespace-pre-line text-slate-600 dark:text-slate-300">{{ message }}</p>

    <template #footer>
      <button type="button" class="btn-secondary" :disabled="busy" @click="emit('update:modelValue', false)">
        取消
      </button>
      <button
        type="button"
        :class="danger ? 'btn-danger' : 'btn-primary'"
        :disabled="busy"
        @click="emit('confirm')"
      >
        {{ busy ? '處理中…' : confirmText }}
      </button>
    </template>
  </AppModal>
</template>
