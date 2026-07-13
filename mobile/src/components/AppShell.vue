<template>
  <div class="shell">
    <main class="shell-main">
      <RouterView v-slot="{ Component }">
        <Transition name="page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </RouterView>
    </main>
    <BottomNav />
  </div>
</template>

<script setup>
import BottomNav from './BottomNav.vue'
</script>

<style scoped>
.shell {
  min-height: 100dvh;
  background:
    radial-gradient(circle at 100% 0%, rgba(22, 184, 148, 0.12), transparent 36%),
    radial-gradient(circle at 0% 20%, rgba(224, 122, 61, 0.08), transparent 30%),
    var(--bg);
}

.shell-main {
  min-height: 100dvh;
}

.page-enter-active,
.page-leave-active {
  transition: opacity 0.22s ease, transform 0.22s ease;
}

.page-enter-from,
.page-leave-to {
  opacity: 0;
  transform: translateY(8px);
}
</style>
