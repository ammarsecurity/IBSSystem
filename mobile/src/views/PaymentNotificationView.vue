<template>
  <div class="page notify">
    <section class="card surface rise">
      <div class="icon" :class="state">
        <span v-if="state === 'loading'" class="spinner" />
        <span v-else-if="state === 'success'" v-html="icons.success" />
        <span v-else v-html="icons.error" />
      </div>

      <h1>{{ title }}</h1>
      <p class="desc">{{ description }}</p>

      <p v-if="state === 'success' && redirectIn > 0" class="redirect-hint">
        سيتم نقلك إلى {{ redirectLabel }} خلال {{ redirectIn }} ثانية...
      </p>

      <div v-if="meta.paymentId" class="meta">
        <div><span>المعرف</span><strong class="num">{{ meta.paymentId }}</strong></div>
        <div v-if="meta.requestId"><span>الطلب</span><strong class="num">{{ meta.requestId }}</strong></div>
        <div v-if="meta.status"><span>الحالة</span><strong>{{ meta.status }}</strong></div>
      </div>

      <div class="actions">
        <button
          v-if="state === 'success' && auth.isAuthenticated"
          class="btn btn-primary"
          type="button"
          @click="goNext"
        >
          {{ primaryActionLabel }}
        </button>
        <button
          v-if="state === 'success' && !auth.isAuthenticated"
          class="btn btn-primary"
          type="button"
          @click="router.replace({ name: 'login' })"
        >
          تسجيل الدخول للمتابعة
        </button>
        <button
          class="btn btn-ghost"
          type="button"
          @click="router.replace(auth.isAuthenticated ? '/' : { name: 'login' })"
        >
          {{ auth.isAuthenticated ? 'الصفحة الرئيسية' : 'تسجيل الدخول' }}
        </button>
        <button
          v-if="state !== 'success' && auth.isAuthenticated"
          class="btn btn-ghost"
          type="button"
          @click="$router.replace(purpose === 'Debt' ? '/payment' : '/refill')"
        >
          {{ purpose === 'Debt' ? 'العودة لتسديد الدين' : 'العودة للتجديد' }}
        </button>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSubscriberStore } from '../stores/subscriber'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { getApiMessage, getApiPurpose, isApiError } from '../composables/apiMessage'
import { clearPendingPayment } from '../composables/pendingPayment'

const route = useRoute()
const router = useRouter()
const store = useSubscriberStore()
const auth = useAuthStore()
const toast = useToastStore()

const state = ref('loading')
const title = ref('جاري تأكيد الدفع')
const description = ref('نتحقق من عملية الدفع لدى بوابة الدفع...')
const redirectIn = ref(0)
const purpose = ref('Refill')

const meta = reactive({
  paymentId: '',
  requestId: '',
  status: '',
  paymentType: '',
})

let redirectTimer = null
let countdownTimer = null

const redirectLabel = computed(() =>
  purpose.value === 'Debt' ? 'المستحقات' : 'الاشتراكات',
)

const primaryActionLabel = computed(() =>
  purpose.value === 'Debt' ? 'الانتقال إلى المستحقات الآن' : 'الانتقال إلى الاشتراكات الآن',
)

const icons = {
  success: `<svg viewBox="0 0 24 24" width="34" height="34" fill="none"><circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8"/><path d="m8.2 12.2 2.6 2.6 5-5.2" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>`,
  error: `<svg viewBox="0 0 24 24" width="34" height="34" fill="none"><circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8"/><path d="m9 9 6 6M15 9l-6 6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>`,
}

function localizeMessage(text) {
  if (!text || typeof text !== 'string') return text

  const expirationMatch = text.match(/expiration date is\s+(.+)$/i)
  if (
    /payment was accepted/i.test(text) ||
    /account has been updated/i.test(text)
  ) {
    const datePart = expirationMatch?.[1]
      ?.trim()
      ?.replace(/\bAM\b/i, 'ص')
      ?.replace(/\bPM\b/i, 'م')

    return datePart
      ? `تم قبول الدفع وتحديث حسابك بنجاح. تاريخ انتهاء الاشتراك القادم: ${datePart}`
      : 'تم قبول الدفع وتحديث حسابك بنجاح'
  }

  return text
}

function goNext() {
  clearTimers()
  if (!auth.isAuthenticated) {
    router.replace({ name: 'login' })
    return
  }
  router.replace(purpose.value === 'Debt' ? '/receivables' : '/refill')
}

function clearTimers() {
  if (redirectTimer) {
    clearTimeout(redirectTimer)
    redirectTimer = null
  }
  if (countdownTimer) {
    clearInterval(countdownTimer)
    countdownTimer = null
  }
}

function startRedirectCountdown(seconds = 7) {
  redirectIn.value = seconds
  countdownTimer = window.setInterval(() => {
    redirectIn.value -= 1
    if (redirectIn.value <= 0) {
      clearInterval(countdownTimer)
      countdownTimer = null
    }
  }, 1000)

  redirectTimer = window.setTimeout(() => {
    goNext()
  }, seconds * 1000)
}

onBeforeUnmount(clearTimers)

onMounted(async () => {
  meta.paymentId = String(route.query.paymentId || '')
  meta.requestId = String(route.query.requestId || '')
  meta.status = String(route.query.status || '')
  meta.paymentType = String(route.query.paymentType || '')
  const company = String(route.query.company || auth.company || '').trim()

  // Clear ASAP so resume listeners don't loop back here.
  clearPendingPayment()

  if (!meta.paymentId) {
    state.value = 'error'
    title.value = 'رابط غير صالح'
    description.value = 'معرف الدفع غير موجود في الرابط.'
    toast.error(description.value, 'فشل التأكيد')
    return
  }

  try {
    const data = await store.confirmPayment({
      paymentId: meta.paymentId,
      requestId: meta.requestId || undefined,
      status: meta.status || undefined,
      company: company || undefined,
      Company: company || undefined,
    })

    purpose.value = getApiPurpose(data) || 'Refill'
    const raw = getApiMessage(
      data,
      purpose.value === 'Debt' ? 'تم تسديد الدين بنجاح' : 'تم تأكيد الدفع',
    )
    const text = localizeMessage(raw)

    if (isApiError(data)) {
      state.value = 'error'
      title.value = purpose.value === 'Debt' ? 'لم يكتمل تسديد الدين' : 'لم يكتمل التجديد'
      description.value = text
      toast.error(text, 'فشل التأكيد')
      return
    }

    state.value = 'success'
    title.value = 'تم بنجاح'
    description.value =
      typeof text === 'string'
        ? text
        : purpose.value === 'Debt'
          ? 'تم تسديد الدين بنجاح'
          : 'تم الدفع وتجديد الاشتراك بنجاح'
    toast.success(description.value, purpose.value === 'Debt' ? 'تم التسديد' : 'تم التفعيل')
    if (auth.isAuthenticated) {
      await store.loadDashboard()
      startRedirectCountdown(7)
    }
  } catch (err) {
    const text = localizeMessage(
      getApiMessage(
        err.response?.data,
        err.response?.data?.error || 'تعذر تأكيد عملية الدفع',
      ),
    )
    state.value = 'error'
    title.value = 'فشل التأكيد'
    description.value = text
    toast.error(text, 'فشل التأكيد')
  }
})
</script>

<style scoped>
.notify {
  display: grid;
  place-items: center;
  min-height: calc(100dvh - var(--nav-h) - 40px);
}

.card {
  width: min(100%, 440px);
  padding: 28px 22px 22px;
  text-align: center;
}

.icon {
  width: 72px;
  height: 72px;
  border-radius: 22px;
  display: grid;
  place-items: center;
  margin: 0 auto 18px;
}

.icon.loading {
  background: rgba(11, 26, 40, 0.06);
}

.icon.success {
  background: var(--accent-soft);
  color: var(--accent-deep);
}

.icon.error {
  background: rgba(214, 69, 69, 0.12);
  color: var(--danger);
}

.spinner {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  border: 3px solid rgba(11, 26, 40, 0.12);
  border-top-color: var(--accent-deep);
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

h1 {
  margin: 0;
  font-size: 1.4rem;
}

.desc {
  margin: 10px 0 0;
  color: var(--ink-muted);
  line-height: 1.7;
  font-size: 0.95rem;
}

.redirect-hint {
  margin: 14px 0 0;
  color: var(--accent-deep);
  font-size: 0.9rem;
  font-weight: 600;
}

.meta {
  margin-top: 18px;
  display: grid;
  gap: 10px;
  text-align: start;
  background: var(--surface-soft);
  border-radius: 14px;
  padding: 12px 14px;
}

.meta > div {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 0.86rem;
}

.meta span {
  color: var(--ink-muted);
}

.meta strong {
  word-break: break-all;
}

.actions {
  margin-top: 20px;
  display: grid;
  gap: 10px;
}

.actions .btn {
  width: 100%;
}
</style>
