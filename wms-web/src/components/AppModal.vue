<script setup lang="ts">
import { onBeforeUnmount, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    title: string
    /** sm / md / lg / xl，對應不同寬度的對話框。 */
    size?: 'sm' | 'md' | 'lg' | 'xl'
    /** 表單送出中時停用關閉，避免重複送出。 */
    busy?: boolean
  }>(),
  { size: 'md', busy: false },
)

const emit = defineEmits<{ (e: 'update:modelValue', value: boolean): void }>()

const widthClass = {
  sm: 'max-w-md',
  md: 'max-w-xl',
  lg: 'max-w-3xl',
  xl: 'max-w-5xl',
}

function close() {
  if (!props.busy) emit('update:modelValue', false)
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') close()
}

watch(
  () => props.modelValue,
  (open) => {
    document.body.style.overflow = open ? 'hidden' : ''
    if (open) {
      window.addEventListener('keydown', onKeydown)
    } else {
      window.removeEventListener('keydown', onKeydown)
    }
  },
)

onBeforeUnmount(() => {
  document.body.style.overflow = ''
  window.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      leave-active-class="transition duration-100"
      leave-to-class="opacity-0"
    >
      <div v-if="modelValue" class="fixed inset-0 z-50 overflow-y-auto">
        <div class="fixed inset-0 bg-slate-900/50 backdrop-blur-sm" @click="close" />

        <div class="flex min-h-full items-center justify-center p-4">
          <div
            role="dialog"
            aria-modal="true"
            class="relative w-full rounded-xl bg-white shadow-xl dark:bg-slate-800"
            :class="widthClass[size]"
          >
            <div class="flex items-center justify-between border-b border-slate-200 px-5 py-4 dark:border-slate-700">
              <h2 class="text-base font-semibold text-slate-800 dark:text-white">{{ title }}</h2>
              <button
                type="button"
                class="rounded-lg p-1 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-700"
                :disabled="busy"
                aria-label="關閉"
                @click="close"
              >
                <svg class="size-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                  <path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" />
                </svg>
              </button>
            </div>

            <div class="max-h-[70vh] overflow-y-auto px-5 py-4">
              <slot />
            </div>

            <div
              v-if="$slots.footer"
              class="flex items-center justify-end gap-2 border-t border-slate-200 px-5 py-4 dark:border-slate-700"
            >
              <slot name="footer" />
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
