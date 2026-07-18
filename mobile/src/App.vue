<template>
  <RouterView />
  <AppToast />
</template>

<script setup>
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Capacitor } from '@capacitor/core'
import { StatusBar, Style } from '@capacitor/status-bar'
import AppToast from './components/AppToast.vue'
import { installPaymentReturnHandler } from './composables/paymentReturn'

const router = useRouter()

onMounted(async () => {
  if (!Capacitor.isNativePlatform()) return
  try {
    await StatusBar.setStyle({ style: Style.Dark })
  } catch {
    // web / unsupported
  }
  await installPaymentReturnHandler(router)
})
</script>
