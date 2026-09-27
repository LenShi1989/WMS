<script setup lang="ts">
defineProps<{
  modelValue?: string
  placeholder?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'search'): void
}>()
</script>

<template>
  <div class="card mb-4 flex flex-wrap items-end gap-3 p-4">
    <div class="min-w-[220px] flex-1">
      <label class="label" for="filter-keyword">關鍵字</label>
      <div class="relative">
        <svg
          class="pointer-events-none absolute top-2.5 left-3 size-4 text-slate-400"
          viewBox="0 0 20 20"
          fill="currentColor"
          aria-hidden="true"
        >
          <path
            fill-rule="evenodd"
            d="M9 3.5a5.5 5.5 0 1 0 3.437 9.798l3.383 3.382a.75.75 0 1 0 1.06-1.06l-3.382-3.383A5.5 5.5 0 0 0 9 3.5ZM5 9a4 4 0 1 1 8 0 4 4 0 0 1-8 0Z"
            clip-rule="evenodd"
          />
        </svg>
        <input
          id="filter-keyword"
          type="search"
          class="!pl-9"
          :placeholder="placeholder || '搜尋…'"
          :value="modelValue"
          @input="emit('update:modelValue', ($event.target as HTMLInputElement).value); emit('search')"
          @keyup.enter="emit('search')"
        />
      </div>
    </div>

    <slot />
  </div>
</template>
