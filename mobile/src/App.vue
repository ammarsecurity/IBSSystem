<template>
  <RouterView />
  <AppToast />
</template>

<script setup>
import { onMounted } from 'vue'
import { Capacitor } from '@capacitor/core'
import { StatusBar, Style } from '@capacitor/status-bar'
import AppToast from './components/AppToast.vue'

onMounted(async () => {
  if (!Capacitor.isNativePlatform()) return
  try {
    await StatusBar.setStyle({ style: Style.Dark })
  } catch {
    // web / unsupported
  }
})
</script>
