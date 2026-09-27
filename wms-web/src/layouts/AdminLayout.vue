<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import SidebarIcon from '@/components/SidebarIcon.vue'
import ToastContainer from '@/components/ToastContainer.vue'
import { menu, type MenuGroup } from '@/router/menu'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'

const app = useAppStore()
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const openGroups = ref<Set<string>>(new Set())
const userMenuOpen = ref(false)

/** 只顯示使用者有權限的選單項目。 */
const visibleMenu = computed<MenuGroup[]>(() =>
  menu
    .map((group) => ({
      ...group,
      children: group.children?.filter((child) => !child.permission || auth.can(child.permission)),
    }))
    .filter((group) => {
      if (group.children) return group.children.length > 0
      return !group.permission || auth.can(group.permission)
    }),
)

function isGroupActive(group: MenuGroup) {
  if (group.to) return route.path.startsWith(group.to)
  return group.children?.some((c) => route.path.startsWith(c.to)) ?? false
}

function toggleGroup(title: string) {
  const next = new Set(openGroups.value)
  if (next.has(title)) {
    next.delete(title)
  } else {
    next.add(title)
  }
  openGroups.value = next
}

// 進入頁面時自動展開所屬的選單群組。
watch(
  () => route.path,
  () => {
    app.mobileSidebarOpen = false
    userMenuOpen.value = false
    for (const group of visibleMenu.value) {
      if (group.children && isGroupActive(group)) {
        openGroups.value = new Set(openGroups.value).add(group.title)
      }
    }
  },
  { immediate: true },
)

const breadcrumb = computed(() => {
  for (const group of menu) {
    if (group.to && route.path.startsWith(group.to)) return [group.title]
    const child = group.children?.find((c) => route.path.startsWith(c.to))
    if (child) return [group.title, child.title]
  }
  return [route.meta.title as string | undefined].filter(Boolean) as string[]
})

async function logout() {
  await auth.logout()
  app.notify('已登出。', 'info')
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="min-h-screen">
    <!-- 手機版遮罩 -->
    <div
      v-if="app.mobileSidebarOpen"
      class="fixed inset-0 z-30 bg-slate-900/50 lg:hidden"
      @click="app.mobileSidebarOpen = false"
    />

    <!-- 側邊欄 -->
    <aside
      class="fixed inset-y-0 left-0 z-40 flex flex-col border-r border-slate-800 bg-slate-900 text-slate-300 transition-all duration-200"
      :class="[
        app.sidebarOpen ? 'w-64' : 'w-[76px]',
        app.mobileSidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0',
      ]"
    >
      <div class="flex h-16 shrink-0 items-center gap-2.5 border-b border-slate-800 px-5">
        <div class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-blue-600 font-bold text-white">
          W
        </div>
        <div v-if="app.sidebarOpen" class="min-w-0">
          <p class="truncate text-sm font-semibold text-white">WMS</p>
          <p class="truncate text-[11px] text-slate-400">物料管理系統</p>
        </div>
      </div>

      <nav class="flex-1 space-y-0.5 overflow-y-auto px-3 py-4">
        <template v-for="group in visibleMenu" :key="group.title">
          <!-- 單一頁面 -->
          <RouterLink
            v-if="group.to"
            :to="group.to"
            class="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition"
            :class="
              isGroupActive(group)
                ? 'bg-blue-600 text-white'
                : 'text-slate-300 hover:bg-slate-800 hover:text-white'
            "
            :title="app.sidebarOpen ? undefined : group.title"
          >
            <SidebarIcon :name="group.icon" />
            <span v-if="app.sidebarOpen" class="truncate">{{ group.title }}</span>
          </RouterLink>

          <!-- 可展開的群組 -->
          <div v-else>
            <button
              type="button"
              class="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition"
              :class="
                isGroupActive(group)
                  ? 'bg-slate-800 text-white'
                  : 'text-slate-300 hover:bg-slate-800 hover:text-white'
              "
              :title="app.sidebarOpen ? undefined : group.title"
              @click="toggleGroup(group.title)"
            >
              <SidebarIcon :name="group.icon" />
              <template v-if="app.sidebarOpen">
                <span class="flex-1 truncate text-left">{{ group.title }}</span>
                <svg
                  class="size-4 shrink-0 transition-transform"
                  :class="openGroups.has(group.title) ? 'rotate-180' : ''"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                  aria-hidden="true"
                >
                  <path
                    fill-rule="evenodd"
                    d="M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z"
                    clip-rule="evenodd"
                  />
                </svg>
              </template>
            </button>

            <div v-if="app.sidebarOpen && openGroups.has(group.title)" class="mt-0.5 ml-4 space-y-0.5 border-l border-slate-700 pl-3">
              <RouterLink
                v-for="child in group.children"
                :key="child.to"
                :to="child.to"
                class="block rounded-lg px-3 py-2 text-sm transition"
                :class="
                  route.path.startsWith(child.to)
                    ? 'bg-slate-800 font-medium text-blue-400'
                    : 'text-slate-400 hover:bg-slate-800 hover:text-white'
                "
              >
                {{ child.title }}
              </RouterLink>
            </div>
          </div>
        </template>
      </nav>

      <div v-if="app.sidebarOpen" class="shrink-0 border-t border-slate-800 px-5 py-3 text-[11px] text-slate-500">
        WMS v1.0
      </div>
    </aside>

    <!-- 主要區域 -->
    <div class="transition-all duration-200" :class="app.sidebarOpen ? 'lg:pl-64' : 'lg:pl-[76px]'">
      <header
        class="sticky top-0 z-20 flex h-16 items-center gap-3 border-b border-slate-200 bg-white/90 px-4 backdrop-blur sm:px-6 dark:border-slate-700 dark:bg-slate-800/90"
      >
        <button type="button" class="btn-ghost !px-2 lg:hidden" aria-label="開啟選單" @click="app.mobileSidebarOpen = true">
          <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <path stroke-linecap="round" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        <button type="button" class="btn-ghost !px-2 max-lg:hidden" aria-label="收合選單" @click="app.toggleSidebar()">
          <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <path stroke-linecap="round" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        <nav class="min-w-0 flex-1 text-sm text-slate-500 dark:text-slate-400" aria-label="麵包屑">
          <ol class="flex items-center gap-1.5 truncate">
            <li v-for="(crumb, i) in breadcrumb" :key="crumb" class="flex items-center gap-1.5">
              <span v-if="i > 0" class="text-slate-300">/</span>
              <span :class="i === breadcrumb.length - 1 ? 'font-medium text-slate-700 dark:text-slate-200' : ''">
                {{ crumb }}
              </span>
            </li>
          </ol>
        </nav>

        <button type="button" class="btn-ghost !px-2" :aria-label="app.dark ? '切換淺色模式' : '切換深色模式'" @click="app.toggleTheme()">
          <svg v-if="app.dark" class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z" />
          </svg>
          <svg v-else class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z" />
          </svg>
        </button>

        <div class="relative">
          <button
            type="button"
            class="flex items-center gap-2 rounded-lg px-2 py-1.5 transition hover:bg-slate-100 dark:hover:bg-slate-700"
            @click="userMenuOpen = !userMenuOpen"
          >
            <span class="flex size-8 items-center justify-center rounded-full bg-blue-600 text-sm font-semibold text-white">
              {{ auth.user?.displayName?.[0] ?? 'U' }}
            </span>
            <span class="max-sm:hidden">
              <span class="block text-sm font-medium text-slate-700 dark:text-slate-200">
                {{ auth.user?.displayName }}
              </span>
              <span class="block text-[11px] text-slate-400">{{ auth.user?.roles.join('、') }}</span>
            </span>
          </button>

          <div
            v-if="userMenuOpen"
            class="absolute right-0 z-30 mt-1 w-48 overflow-hidden rounded-lg border border-slate-200 bg-white py-1 shadow-lg dark:border-slate-700 dark:bg-slate-800"
          >
            <RouterLink
              to="/system/profile"
              class="block px-4 py-2 text-sm text-slate-700 transition hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-700"
            >
              個人資料
            </RouterLink>
            <button
              type="button"
              class="block w-full px-4 py-2 text-left text-sm text-rose-600 transition hover:bg-rose-50 dark:hover:bg-rose-500/10"
              @click="logout"
            >
              登出
            </button>
          </div>
        </div>
      </header>

      <main class="p-4 sm:p-6">
        <RouterView v-slot="{ Component }">
          <Transition
            mode="out-in"
            enter-active-class="transition duration-150"
            enter-from-class="translate-y-1 opacity-0"
          >
            <component :is="Component" />
          </Transition>
        </RouterView>
      </main>
    </div>

    <ToastContainer />
  </div>
</template>
