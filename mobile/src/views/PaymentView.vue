<template>
  <div class="page">
    <header class="page-header rise">
      <div>
        <h1>تسديد الديون</h1>
        <p>ادفع المبالغ المستحقة السابقة إلكترونياً عبر Qi</p>
      </div>
    </header>

    <section class="due surface rise">
      <span>المبلغ المستحق حالياً</span>
      <strong class="num">{{ formatMoney(due) }}</strong>
      <button
        class="chip"
        type="button"
        :disabled="!hasDebt"
        @click="form.amount = Number(due)"
      >
        استخدام المبلغ الكامل
      </button>
    </section>

    <section v-if="!hasDebt" class="empty surface rise" style="animation-delay: 0.04s">
      <strong>لا يوجد دين مستحق</strong>
      <span>عمليات تسديد الدين معطّلة حالياً لأن المبلغ المستحق صفر.</span>
    </section>

    <section class="surface form rise" style="animation-delay: 0.06s" :class="{ disabled: !hasDebt }">
      <div class="field">
        <label for="amount">مبلغ تسديد الدين</label>
        <input
          id="amount"
          v-model.number="form.amount"
          type="number"
          min="0.01"
          step="0.01"
          placeholder="أدخل المبلغ"
          :disabled="!hasDebt || loading"
          required
        />
      </div>

      <div class="presets">
        <button
          v-for="preset in presets"
          :key="preset"
          type="button"
          class="preset"
          :disabled="!hasDebt || loading"
          @click="form.amount = preset"
        >
          {{ formatMoney(preset) }}
        </button>
      </div>

      <p class="hint">
        هذا الدفع لتسديد الديون السابقة فقط، ولا يجدّد الاشتراك. لتجديد الباقة استخدم
        <router-link to="/refill">صفحة التجديد</router-link>.
      </p>

      <button
        class="btn btn-primary submit"
        type="button"
        :disabled="!canPay"
        @click="onPay"
      >
        {{ payButtonLabel }}
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
import {
  extractPaymentIds,
  savePendingPayment,
} from '../composables/pendingPayment'
import { useAuthStore } from '../stores/auth'

const store = useSubscriberStore()
const toast = useToastStore()
const auth = useAuthStore()
const loading = ref(false)

const form = reactive({
  amount: null,
})

const due = computed(() => Number(store.amountDue ?? store.financial?.amountDue ?? 0))
const hasDebt = computed(() => due.value > 0)
const presets = [10000, 25000, 50000]

const canPay = computed(
  () => hasDebt.value && !loading.value && Number(form.amount) >= 0.01,
)

const payButtonLabel = computed(() => {
  if (!hasDebt.value) return 'لا يوجد دين للدفع'
  if (loading.value) return 'جاري فتح الدفع الإلكتروني...'
  return 'ادفع الدين إلكترونياً'
})

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
  window.open(url, '_blank', 'noopener,noreferrer')
}

async function onPay() {
  if (!hasDebt.value) {
    toast.info('لا يوجد مبلغ دين مستحق', 'تنبيه')
    return
  }
  if (!form.amount || form.amount < 0.01) {
    toast.info('أدخل مبلغاً صالحاً لتسديد الدين', 'تنبيه')
    return
  }

  loading.value = true
  try {
    // Send both casings so binding never falls back to Refill.
    const data = await store.createPayment({
      amount: Number(form.amount),
      purpose: 'Debt',
      Purpose: 'Debt',
      profileId: null,
      ProfileId: null,
    })

    if (isApiError(data)) {
      toast.error(getApiMessage(data, 'فشل إنشاء عملية الدفع'), 'فشل الدفع')
      return
    }

    const payload = data?.data ?? data
    const paymentUrl =
      payload?.formUrl ||
      payload?.FormUrl ||
      payload?.url ||
      payload?.paymentUrl ||
      payload?.redirectUrl ||
      (typeof payload === 'string' && /^https?:\/\//i.test(payload) ? payload : null)

    if (!paymentUrl) {
      toast.error('لم يتم استلام رابط الدفع الإلكتروني', 'فشل الدفع')
      return
    }

    const ids = extractPaymentIds(payload)
    savePendingPayment({
      paymentId: ids.paymentId,
      requestId: ids.requestId,
      purpose: 'Debt',
      company: auth.company || '',
    })

    toast.success(
      Capacitor.isNativePlatform()
        ? 'أكمل دفع الدين ثم أغلق صفحة الدفع للعودة للتطبيق والتأكيد'
        : 'أكمل دفع الدين في الصفحة الجديدة، ثم ستتم إعادة توجيهك للتأكيد',
      'جاهز للدفع',
    )
    await openUrl(paymentUrl)
  } catch (err) {
    toast.error(
      getApiMessage(err.response?.data, err.response?.data?.error || 'تعذر إنشاء عملية الدفع'),
      'فشل الدفع',
    )
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  if (!store.financial) await store.loadDashboard()
  form.amount = hasDebt.value ? Number(due.value) : null
})
</script>

<style scoped>
.hint {
  margin: 0;
  font-size: 0.86rem;
  color: var(--ink-muted);
  line-height: 1.6;
}

.hint a {
  color: var(--accent-deep);
}

.due {
  padding: 20px;
  margin-bottom: 14px;
  background:
    radial-gradient(circle at 0% 0%, rgba(247, 148, 30, 0.2), transparent 45%),
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

.chip:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.empty {
  padding: 18px;
  margin-bottom: 14px;
  display: grid;
  gap: 6px;
  text-align: center;
}

.empty span {
  color: var(--ink-muted);
  font-size: 0.9rem;
  line-height: 1.6;
}

.form {
  padding: 18px;
  display: grid;
  gap: 14px;
}

.form.disabled {
  opacity: 0.55;
  pointer-events: none;
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

.preset:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.submit {
  width: 100%;
}

.submit:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
</style>
