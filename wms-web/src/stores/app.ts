import { defineStore } from 'pinia'
import { ref } from 'vue'

export type ToastKind = 'success' | 'error' | 'info' | 'warning'

export interface Toast {
  id: number
  kind: ToastKind
  message: string
}

let toastId = 0

export const useAppStore = defineStore('app', () => {
  const sidebarOpen = ref(true)
  const mobileSidebarOpen = ref(false)
  const dark = ref(localStorage.getItem('wms.theme') === 'dark')
  const toasts = ref<Toast[]>([])

  applyTheme()

  function applyTheme() {
    document.documentElement.classList.toggle('dark', dark.value)
    document.documentElement.dataset.theme = dark.value ? 'dark' : 'light'
  }

  function toggleTheme() {
    dark.value = !dark.value
    localStorage.setItem('wms.theme', dark.value ? 'dark' : 'light')
    applyTheme()
  }

  function toggleSidebar() {
    sidebarOpen.value = !sidebarOpen.value
  }

  function notify(message: string, kind: ToastKind = 'success') {
    const id = ++toastId
    toasts.value.push({ id, kind, message })
    setTimeout(() => dismiss(id), kind === 'error' ? 6000 : 3500)
  }

  function dismiss(id: number) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  return {
    sidebarOpen, mobileSidebarOpen, dark, toasts,
    toggleSidebar, toggleTheme, notify, dismiss,
  }
})
