<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string
    value: string | number
    unit?: string
    hint?: string
    tone?: 'blue' | 'green' | 'amber' | 'purple' | 'rose' | 'cyan'
    to?: string
  }>(),
  { tone: 'blue' },
)

const toneClass: Record<string, string> = {
  blue: 'bg-blue-50 text-blue-600 dark:bg-blue-500/15 dark:text-blue-300',
  green: 'bg-emerald-50 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-300',
  amber: 'bg-amber-50 text-amber-600 dark:bg-amber-500/15 dark:text-amber-300',
  purple: 'bg-violet-50 text-violet-600 dark:bg-violet-500/15 dark:text-violet-300',
  rose: 'bg-rose-50 text-rose-600 dark:bg-rose-500/15 dark:text-rose-300',
  cyan: 'bg-cyan-50 text-cyan-600 dark:bg-cyan-500/15 dark:text-cyan-300',
}
</script>

<template>
  <component
    :is="to ? 'RouterLink' : 'div'"
    :to="to"
    class="card flex items-center gap-4 p-4 transition"
    :class="to ? 'hover:border-blue-300 hover:shadow-md dark:hover:border-blue-500/50' : ''"
  >
    <div class="flex size-11 shrink-0 items-center justify-center rounded-lg" :class="toneClass[tone]">
      <slot name="icon">
        <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <path stroke-linecap="round" stroke-linejoin="round" d="M4 7h16M4 12h16M4 17h10" />
        </svg>
      </slot>
    </div>

    <div class="min-w-0">
      <p class="truncate text-sm text-slate-500 dark:text-slate-400">{{ label }}</p>
      <p class="mt-0.5 flex items-baseline gap-1">
        <span class="text-2xl font-semibold text-slate-800 dark:text-white">{{ value }}</span>
        <span v-if="unit" class="text-xs text-slate-400">{{ unit }}</span>
      </p>
      <p v-if="hint" class="mt-0.5 truncate text-xs text-slate-400">{{ hint }}</p>
    </div>
  </component>
</template>
