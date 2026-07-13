import { defineStore } from 'pinia'
import { ref } from 'vue'

let toastId = 0

export const useToastStore = defineStore('toast', () => {
  const items = ref([])

  function push({ type = 'info', title, message, duration = 3800 }) {
    const id = ++toastId
    items.value.push({ id, type, title, message })
    if (duration > 0) {
      window.setTimeout(() => dismiss(id), duration)
    }
    return id
  }

  function success(message, title = 'تم بنجاح') {
    return push({ type: 'success', title, message, duration: 3600 })
  }

  function error(message, title = 'حدث خطأ') {
    return push({ type: 'error', title, message, duration: 4800 })
  }

  function info(message, title = 'تنبيه') {
    return push({ type: 'info', title, message, duration: 3600 })
  }

  function dismiss(id) {
    items.value = items.value.filter((t) => t.id !== id)
  }

  function clear() {
    items.value = []
  }

  return { items, push, success, error, info, dismiss, clear }
})
