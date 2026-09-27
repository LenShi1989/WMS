<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useAppStore } from '@/stores/app'

const app = useAppStore()
const { toasts } = storeToRefs(app)

const styles: Record<string, string> = {
  success: 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/15 dark:text-emerald-200',
  error: 'border-rose-200 bg-rose-50 text-rose-800 dark:border-rose-500/30 dark:bg-rose-500/15 dark:text-rose-200',
  warning: 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/15 dark:text-amber-200',
  info: 'border-blue-200 bg-blue-50 text-blue-800 dark:border-blue-500/30 dark:bg-blue-500/15 dark:text-blue-200',
}
</script>

<template>
  <Teleport to="body">
    <div class="pointer-events-none fixed top-4 right-4 z-[60] flex w-80 max-w-[calc(100vw-2rem)] flex-col gap-2">
      <TransitionGroup
        enter-active-class="transition duration-200"
        enter-from-class="translate-x-4 opacity-0"
        leave-active-class="transition duration-150"
        leave-to-class="translate-x-4 opacity-0"
      >
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="pointer-events-auto flex items-start gap-2 rounded-lg border px-4 py-3 text-sm shadow-lg"
          :class="styles[toast.kind]"
          role="status"
        >
          <span class="flex-1 break-words">{{ toast.message }}</span>
          <button type="button" class="shrink-0 opacity-60 hover:opacity-100" aria-label="關閉" @click="app.dismiss(toast.id)">
            <svg class="size-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" />
            </svg>
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>
