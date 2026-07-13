<template>
  <nav class="bottom-nav" aria-label="القائمة الرئيسية">
    <RouterLink
      v-for="item in items"
      :key="item.to"
      :to="item.to"
      class="nav-item"
      :class="{ active: isActive(item) }"
    >
      <span class="icon" v-html="item.icon" />
      <span class="label">{{ item.label }}</span>
    </RouterLink>
  </nav>
</template>

<script setup>
import { useRoute } from 'vue-router'

const route = useRoute()

const items = [
  {
    to: '/',
    name: 'home',
    label: 'الرئيسية',
    icon: `<svg viewBox="0 0 24 24" fill="none"><path d="M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-5v-6H10v6H5a1 1 0 0 1-1-1v-9.5Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"/></svg>`,
  },
  {
    to: '/invoices',
    name: 'invoices',
    label: 'الفواتير',
    icon: `<svg viewBox="0 0 24 24" fill="none"><path d="M7 3.5h10a1.5 1.5 0 0 1 1.5 1.5v14l-3-1.5-3 1.5-3-1.5-3 1.5V5A1.5 1.5 0 0 1 7 3.5Z" stroke="currentColor" stroke-width="1.8"/><path d="M9 8h6M9 12h6M9 16h3" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>`,
  },
  {
    to: '/payment',
    name: 'payment',
    label: 'الدفع',
    icon: `<svg viewBox="0 0 24 24" fill="none"><rect x="3" y="6" width="18" height="12" rx="2.5" stroke="currentColor" stroke-width="1.8"/><path d="M3 10h18M7 14h4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>`,
  },
  {
    to: '/refill',
    name: 'refill',
    label: 'التجديد',
    icon: `<svg viewBox="0 0 24 24" fill="none"><path d="M4.5 12a7.5 7.5 0 0 1 12.8-5.3L19 5v5h-5l1.7-1.7A5.5 5.5 0 1 0 17.5 12" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>`,
  },
  {
    to: '/more',
    name: 'more',
    label: 'المزيد',
    icon: `<svg viewBox="0 0 24 24" fill="none"><circle cx="6" cy="12" r="1.6" fill="currentColor"/><circle cx="12" cy="12" r="1.6" fill="currentColor"/><circle cx="18" cy="12" r="1.6" fill="currentColor"/></svg>`,
  },
]

function isActive(item) {
  if (item.name === 'home') return route.name === 'home'
  return route.path.startsWith(item.to)
}
</script>

<style scoped>
.bottom-nav {
  position: fixed;
  inset-inline: 14px;
  bottom: calc(12px + var(--safe-bottom));
  height: var(--nav-h);
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 2px;
  padding: 10px 8px;
  border-radius: 24px;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid var(--line);
  box-shadow: 0 16px 40px rgba(4, 17, 29, 0.16);
  backdrop-filter: blur(16px);
  z-index: 40;
}

.nav-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  color: var(--ink-muted);
  border-radius: 16px;
  transition: color 0.2s ease, background 0.2s ease;
}

.nav-item.active {
  color: var(--accent-deep);
  background: var(--accent-soft);
}

.icon {
  width: 22px;
  height: 22px;
  display: grid;
  place-items: center;
}

.icon :deep(svg) {
  width: 22px;
  height: 22px;
}

.label {
  font-size: 0.68rem;
  font-weight: 700;
}
</style>
