<template>
  <div class="toast-host" aria-live="polite" aria-relevant="additions">
    <TransitionGroup name="toast">
      <div
        v-for="toast in toastStore.items"
        :key="toast.id"
        class="toast"
        :class="toast.type"
        role="status"
      >
        <div class="toast-icon" v-html="iconFor(toast.type)" />
        <div class="toast-body">
          <strong>{{ toast.title }}</strong>
          <p>{{ toast.message }}</p>
        </div>
        <button class="toast-close" type="button" aria-label="إغلاق" @click="toastStore.dismiss(toast.id)">
          ×
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<script setup>
import { useToastStore } from '../stores/toast'

const toastStore = useToastStore()

const icons = {
  success: `<svg viewBox="0 0 24 24" width="22" height="22" fill="none"><circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8"/><path d="m8.5 12.2 2.4 2.4 4.6-5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>`,
  error: `<svg viewBox="0 0 24 24" width="22" height="22" fill="none"><circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8"/><path d="M12 8v5.2M12 15.8h.01" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>`,
  info: `<svg viewBox="0 0 24 24" width="22" height="22" fill="none"><circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8"/><path d="M12 11.2V16M12 8.2h.01" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>`,
}

function iconFor(type) {
  return icons[type] || icons.info
}
</script>

<style scoped>
.toast-host {
  position: fixed;
  inset-inline: 14px;
  top: calc(14px + var(--safe-top));
  z-index: 120;
  display: grid;
  gap: 10px;
  pointer-events: none;
}

.toast {
  pointer-events: auto;
  display: grid;
  grid-template-columns: auto 1fr auto;
  gap: 12px;
  align-items: start;
  padding: 14px 14px 14px 12px;
  border-radius: 18px;
  border: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.96);
  box-shadow: 0 18px 40px rgba(4, 17, 29, 0.16);
  backdrop-filter: blur(14px);
}

.toast.success {
  border-color: rgba(0, 174, 239, 0.28);
  background: linear-gradient(135deg, rgba(0, 174, 239, 0.12), rgba(255, 255, 255, 0.96) 42%);
}

.toast.error {
  border-color: rgba(214, 69, 69, 0.28);
  background: linear-gradient(135deg, rgba(214, 69, 69, 0.12), rgba(255, 255, 255, 0.96) 42%);
}

.toast.info {
  border-color: rgba(11, 26, 40, 0.1);
}

.toast-icon {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  flex-shrink: 0;
}

.toast.success .toast-icon {
  background: var(--accent-soft);
  color: var(--accent-deep);
}

.toast.error .toast-icon {
  background: rgba(214, 69, 69, 0.12);
  color: var(--danger);
}

.toast.info .toast-icon {
  background: rgba(11, 26, 40, 0.06);
  color: var(--ink);
}

.toast-body {
  min-width: 0;
  padding-top: 2px;
}

.toast-body strong {
  display: block;
  font-size: 0.95rem;
  margin-bottom: 4px;
}

.toast-body p {
  margin: 0;
  color: var(--ink-muted);
  font-size: 0.86rem;
  line-height: 1.55;
  word-break: break-word;
}

.toast-close {
  border: none;
  background: transparent;
  color: var(--ink-muted);
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
  padding: 2px 4px;
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.28s ease;
}

.toast-enter-from {
  opacity: 0;
  transform: translateY(-12px) scale(0.98);
}

.toast-leave-to {
  opacity: 0;
  transform: translateY(-8px) scale(0.98);
}
</style>
