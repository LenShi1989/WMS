<script setup lang="ts">
withDefaults(defineProps<{ code?: number; message?: string }>(), {
  code: 404,
  message: '找不到這個頁面。',
})
</script>

<template>
  <div class="flex min-h-screen flex-col items-center justify-center gap-4 px-4 text-center">
    <p class="text-6xl font-bold text-slate-300 dark:text-slate-600">{{ code }}</p>
    <p class="text-lg text-slate-600 dark:text-slate-300">{{ message }}</p>
    <RouterLink to="/dashboard" class="btn-primary">回到儀表板</RouterLink>
  </div>
</template>
