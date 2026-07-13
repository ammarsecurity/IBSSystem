<template>
  <div class="page">
    <header class="page-header rise">
      <div>
        <h1>الدفع</h1>
        <p>سدّد المبلغ المستحق إلكترونياً</p>
      </div>
    </header>

    <section class="due surface rise">
      <span>المبلغ المستحق حالياً</span>
      <strong class="num">{{ formatMoney(due) }}</strong>
      <button class="chip" type="button" @click="form.amount = Number(due) || ''">
        استخدام المبلغ الكامل
      </button>
    </section>

    <section class="surface form rise" style="animation-delay: 0.06s">
      <div class="field">
        <label for="amount">مبلغ الدفع</label>
        <input
          id="amount"
          v-model.number="form.amount"
          type="number"
          min="0.01"
          step="0.01"
          placeholder="أدخل المبلغ"
          required
        />
      </div>

      <div class="presets">
        <button
          v-for="preset in presets"
          :key="preset"
          type="button"
          class="preset"
          @click="form.amount = preset"
        >
          {{ formatMoney(preset) }}
        </button>
      </div>

      <button
        class="btn btn-primary submit"
        type="button"
        :disabled="loading || !form.amount || form.amount < 0.01"
        @click="onPay"
      >
        {{ loading ? 'جاري إنشاء الدفع...' : 'متابعة الدفع' }}
      </button>

      <button
        class="btn btn-ghost submit"
        type="button"
        :disabled="paybackLoading"
        @click="onPayback"
      >
        {{ paybackLoading ? 'جاري التسوية...' : 'تسوية الدين (Payback)' }}
      </button>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { Capacitor } from '@capacitor/core'
import { useSubscriberStore } from '../stores/subscriber'
import { useToastStore } from '../stores/toast'
import { formatMoney } from '../composables/format'
import { getApiMessage, isApiError } from '../composables/apiMessage'

const store = useSubscriberStore()
const toast = useToastStore()
const loading = ref(false)
const paybackLoading = ref(false)

const form = reactive({
  amount: null,
})

const due = computed(() => store.amountDue ?? store.financial?.amountDue ?? 0)
const presets = [10000, 25000, 50000]

async function openUrl(url) {
  if (!url) return
  if (Capacitor.isNativePlatform()) {
    try {
      const { Browser } = await import('@capacitor/browser')
      await Browser.open({ url })
      return
    } catch {
      // fallback
    }
  }
  window.open(url, '_blank')
}

async function onPay() {
  if (!form.amount || form.amount < 0.01) {
    toast.info('أدخل مبلغاً صالحاً للمتابعة', 'تنبيه')
    return
  }
  loading.value = true
  try {
    const returnUrl = `${window.location.origin}/payment`
    const data = await store.createPayment({
      amount: Number(form.amount),
      returnUrl,
    })

    if (isApiError(data)) {
      toast.error(getApiMessage(data, 'فشل إنشاء عملية الدفع'), 'فشل الدفع')
      return
    }

    const paymentUrl =
      (typeof data?.data === 'string' && /^https?:\/\//i.test(data.data) && data.data) ||
      data?.data?.url ||
      data?.data?.paymentUrl ||
      data?.data?.redirectUrl

    toast.success(getApiMessage(data, 'تم إنشاء رابط الدفع'), 'جاهز للدفع')

    if (paymentUrl) await openUrl(paymentUrl)
  } catch (err) {
    toast.error(
      getApiMessage(err.response?.data, err.response?.data?.error || 'تعذر إنشاء عملية الدفع'),
      'فشل الدفع',
    )
  } finally {
    loading.value = false
  }
}

async function onPayback() {
  paybackLoading.value = true
  try {
    const data = await store.payback()
    const text = getApiMessage(
      data,
      isApiError(data) ? 'فشلت التسوية' : 'تمت التسوية بنجاح',
    )
    if (isApiError(data)) {
      toast.error(text, 'فشل التسوية')
    } else {
      toast.success(text, 'تمت التسوية')
      await store.loadDashboard()
    }
  } catch (err) {
    toast.error(
      getApiMessage(err.response?.data, err.response?.data?.error || 'تعذر تنفيذ التسوية'),
      'فشل التسوية',
    )
  } finally {
    paybackLoading.value = false
  }
}

onMounted(async () => {
  if (!store.financial) await store.loadDashboard()
  form.amount = Number(due.value) || null
})
</script>

<style scoped>
.due {
  padding: 20px;
  margin-bottom: 14px;
  background:
    radial-gradient(circle at 0% 0%, rgba(224, 122, 61, 0.18), transparent 45%),
    #fff;
  display: grid;
  gap: 8px;
}

.due span {
  color: var(--ink-muted);
  font-size: 0.9rem;
}

.due strong {
  font-size: 1.8rem;
  letter-spacing: -0.03em;
}

.chip {
  justify-self: start;
  border: none;
  background: var(--warn-soft);
  color: var(--warn);
  border-radius: 999px;
  padding: 8px 12px;
  font-weight: 700;
  font-size: 0.8rem;
  cursor: pointer;
}

.form {
  padding: 18px;
  display: grid;
  gap: 14px;
}

.presets {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
}

.preset {
  border: 1px solid var(--line);
  background: var(--surface-soft);
  border-radius: 12px;
  padding: 10px 6px;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
}

.submit {
  width: 100%;
}
</style>
