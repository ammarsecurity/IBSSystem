import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as subscriberApi from '../api/subscriber'

export const useSubscriberStore = defineStore('subscriber', () => {
  const financial = ref(null)
  const subscription = ref(null)
  const amountDue = ref(0)
  const invoices = ref([])
  const receivables = ref([])
  const packages = ref([])
  const loading = ref(false)
  const error = ref('')

  async function loadDashboard() {
    loading.value = true
    error.value = ''
    try {
      const [fin, sub, due] = await Promise.all([
        subscriberApi.getFinancialInfo(),
        subscriberApi.getSubscriptionInfo(),
        subscriberApi.getAmountDue(),
      ])
      financial.value = fin.data
      subscription.value = sub.data?.data ?? sub.data
      amountDue.value = due.data ?? 0
    } catch (err) {
      error.value = err.response?.data?.message || 'تعذر تحميل البيانات'
    } finally {
      loading.value = false
    }
  }

  async function loadInvoices() {
    loading.value = true
    error.value = ''
    try {
      const { data } = await subscriberApi.getInvoices()
      invoices.value = Array.isArray(data) ? data : []
    } catch (err) {
      error.value = err.response?.data?.message || 'تعذر تحميل الفواتير'
    } finally {
      loading.value = false
    }
  }

  async function loadReceivables() {
    loading.value = true
    error.value = ''
    try {
      const { data } = await subscriberApi.getReceivables()
      receivables.value = Array.isArray(data) ? data : []
    } catch (err) {
      error.value = err.response?.data?.message || 'تعذر تحميل المقبوضات'
    } finally {
      loading.value = false
    }
  }

  async function loadPackages() {
    loading.value = true
    error.value = ''
    try {
      const { data } = await subscriberApi.getPackages()
      packages.value = Array.isArray(data) ? data : []
    } catch (err) {
      error.value = err.response?.data?.message || 'تعذر تحميل الباقات'
      packages.value = []
    } finally {
      loading.value = false
    }
  }

  async function refill(payload) {
    const { data } = await subscriberApi.refill(payload)
    return data
  }

  async function payback() {
    const { data } = await subscriberApi.payback()
    return data
  }

  async function createPayment(payload) {
    const { data } = await subscriberApi.createPayment(payload)
    return data
  }

  async function confirmPayment(payload) {
    const { data } = await subscriberApi.confirmPayment(payload)
    return data
  }

  function reset() {
    financial.value = null
    subscription.value = null
    amountDue.value = 0
    invoices.value = []
    receivables.value = []
    packages.value = []
    error.value = ''
  }

  return {
    financial,
    subscription,
    amountDue,
    invoices,
    receivables,
    packages,
    loading,
    error,
    loadDashboard,
    loadInvoices,
    loadReceivables,
    loadPackages,
    refill,
    payback,
    createPayment,
    confirmPayment,
    reset,
  }
})
