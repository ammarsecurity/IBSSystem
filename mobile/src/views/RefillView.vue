<template>
  <div class="page refill">
    <header class="hero rise">
      <div class="hero-copy">
        <p class="eyebrow">التجديد</p>
        <h1>اختر باقتك</h1>
        <p class="sub">فعّل الاشتراك بخطوات واضحة وآمنة</p>
      </div>
      <div class="hero-meta" v-if="subscription">
        <div>
          <span>الحساب الحالي</span>
          <strong>{{ subscription.accountName || '—' }}</strong>
        </div>
        <div>
          <span>آخر تجديد</span>
          <strong>{{ subscription.lastRefill || '—' }}</strong>
        </div>
      </div>
    </header>

    <div class="steps rise" style="animation-delay: 0.04s" aria-hidden="true">
      <div class="step" :class="{ on: true }"><i>1</i><span>الباقة</span></div>
      <div class="rail" />
      <div class="step" :class="{ on: Boolean(selected) }"><i>2</i><span>الدفع</span></div>
      <div class="rail" />
      <div class="step" :class="{ on: Boolean(selected) && !loading }"><i>3</i><span>تأكيد</span></div>
    </div>

    <section class="block rise" style="animation-delay: 0.08s">
      <div class="block-head">
        <h2>الباقات المتاحة</h2>
        <span class="count" v-if="visiblePackages.length">{{ visiblePackages.length }} باقة</span>
      </div>

      <div v-if="store.loading && !packages.length" class="skel-list">
        <div v-for="n in 4" :key="n" class="skeleton skel" />
      </div>

      <div v-else-if="store.error && !packages.length" class="alert alert-error">
        {{ store.error }}
      </div>

      <div v-else-if="!visiblePackages.length" class="empty surface">
        <strong>لا توجد باقات متاحة</strong>
        <span>راجع الوكيل أو حاول لاحقاً</span>
      </div>

      <div v-else class="pkg-grid">
        <button
          v-for="(pkg, index) in visiblePackages"
          :key="pkg.id"
          type="button"
          class="pkg"
          :class="{
            active: form.profileId === pkg.id,
            featured: isFeatured(pkg, index),
          }"
          @click="selectPackage(pkg)"
        >
          <div class="pkg-badge" v-if="isFeatured(pkg, index)">الأكثر طلباً</div>
          <div class="pkg-radio" aria-hidden="true">
            <span />
          </div>
          <div class="pkg-body">
            <strong class="pkg-name">{{ pkg.name || pkg.description }}</strong>
            <p v-if="pkg.description && pkg.description !== pkg.name" class="pkg-desc">
              {{ pkg.description }}
            </p>
            <div class="pkg-price">
              <span class="amount num">{{ formatMoney(pkg.price) }}</span>
              <span class="period"> / تجديد</span>
            </div>
          </div>
        </button>
      </div>
    </section>

    <section class="block rise" style="animation-delay: 0.12s">
      <div class="block-head">
        <h2>طريقة التفعيل</h2>
      </div>

      <div class="pay-switch" role="tablist">
        <button
          type="button"
          role="tab"
          class="pay-opt"
          :class="{ active: form.saleType === true }"
          :aria-selected="form.saleType === true"
          @click="form.saleType = true"
        >
          <span class="pay-icon cash" v-html="icons.cash" />
          <span class="pay-text">
            <strong>دفع إلكتروني</strong>
            <small>بطاقة Qi ثم تفعيل</small>
          </span>
        </button>
        <button
          type="button"
          role="tab"
          class="pay-opt"
          :class="{ active: form.saleType === false, disabled: !canCredit }"
          :aria-selected="form.saleType === false"
          :disabled="!canCredit"
          @click="canCredit && (form.saleType = false)"
        >
          <span class="pay-icon credit" v-html="icons.credit" />
          <span class="pay-text">
            <strong>آجل</strong>
            <small>{{ canCredit ? 'ضمن حد الدين' : 'غير متاح حالياً' }}</small>
          </span>
        </button>
      </div>

    <div v-if="!canCredit" class="hint-line">
      التفعيل الآجل غير مفعّل لحسابك. يمكنك التجديد عبر الدفع الإلكتروني.
    </div>
  </section>

  <div class="confirm-bar rise" style="animation-delay: 0.16s">
      <div class="confirm-summary" v-if="selected">
        <span>المحدد</span>
        <strong>{{ selected.name }}</strong>
        <em class="num">{{ formatMoney(selected.price) }} · {{ form.saleType ? 'دفع إلكتروني' : 'آجل' }}</em>
      </div>
      <div class="confirm-summary muted" v-else>
        <span>لم تُحدد باقة بعد</span>
        <strong>اختر باقة للمتابعة</strong>
      </div>
      <button
        class="btn btn-primary confirm-btn"
        type="button"
        :disabled="loading || !form.profileId"
        @click="onRefill"
      >
        {{ confirmLabel }}
      </button>
    </div>
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

const form = reactive({
  profileId: null,
  saleType: true,
})

const packages = computed(() => store.packages || [])
const subscription = computed(() => store.subscription)
const canCredit = computed(() => Boolean(store.financial?.canActiveNoCash))

const visiblePackages = computed(() => {
  const list = [...packages.value]
  return list.sort((a, b) => {
    const pa = Number(a.price) || 0
    const pb = Number(b.price) || 0
    if (pa === 0 && pb !== 0) return 1
    if (pb === 0 && pa !== 0) return -1
    return pa - pb
  })
})

const selected = computed(() =>
  visiblePackages.value.find((p) => p.id === form.profileId) || null,
)

const confirmLabel = computed(() => {
  if (loading.value) {
    return form.saleType ? 'جاري فتح الدفع...' : 'جاري التفعيل...'
  }
  return form.saleType ? 'ادفع وجدّد' : 'تأكيد التجديد آجلاً'
})

const icons = {
  cash: `<svg viewBox="0 0 24 24" width="20" height="20" fill="none"><rect x="3" y="6" width="18" height="12" rx="2.5" stroke="currentColor" stroke-width="1.8"/><path d="M3 10h18M7 14h4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>`,
  credit: `<svg viewBox="0 0 24 24" width="20" height="20" fill="none"><path d="M12 3v18M8 8h5.2a2.4 2.4 0 0 1 0 4.8H9.2a2.4 2.4 0 0 0 0 4.8H16" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>`,
}

function isFeatured(pkg, index) {
  const paid = visiblePackages.value.filter((p) => Number(p.price) > 0)
  if (paid.length < 2) return false
  const mid = paid[Math.min(1, paid.length - 1)]
  return pkg.id === mid?.id
}

function selectPackage(pkg) {
  form.profileId = pkg.id
}

async function openPaymentUrl(url) {
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

async function onRefill() {
  if (!form.profileId) {
    toast.info('اختر باقة أولاً ثم أكّد التجديد', 'تنبيه')
    return
  }

  loading.value = true
  try {
    if (form.saleType) {
      await startOnlinePayment()
    } else {
      await startCreditRefill()
    }
  } catch (err) {
    const text = getApiMessage(
      err.response?.data,
      err.response?.data?.error || 'تعذر تنفيذ العملية',
    )
    toast.error(text, form.saleType ? 'فشل الدفع' : 'فشل التجديد')
  } finally {
    loading.value = false
  }
}

async function startOnlinePayment() {
  const pkg = selected.value
  const amount = Number(pkg?.price)
  if (!amount || amount <= 0) {
    toast.error('سعر الباقة غير صالح للدفع الإلكتروني', 'تعذر الدفع')
    return
  }

  const data = await store.createPayment({
    amount,
    profileId: Number(form.profileId),
    saleType: true,
    purpose: 'Refill',
  })

  if (isApiError(data)) {
    toast.error(getApiMessage(data, 'فشل إنشاء عملية الدفع'), 'فشل الدفع')
    return
  }

  const payload = data?.data ?? data
  const formUrl =
    payload?.formUrl ||
    payload?.FormUrl ||
    (typeof payload === 'string' && /^https?:\/\//i.test(payload) ? payload : null)

  if (!formUrl) {
    toast.error('لم يتم استلام رابط الدفع', 'فشل الدفع')
    return
  }

  toast.success('أكمل الدفع في الصفحة الجديدة، ثم ستتم إعادة توجيهك للتأكيد', 'جاهز للدفع')
  await openPaymentUrl(formUrl)
}

async function startCreditRefill() {
  const data = await store.refill({
    profileId: Number(form.profileId),
    saleType: false,
  })
  const text = getApiMessage(
    data,
    isApiError(data) ? 'فشل تجديد الاشتراك' : 'تم تجديد الاشتراك بنجاح',
  )
  if (isApiError(data)) {
    toast.error(text, 'فشل التجديد')
  } else {
    toast.success(text, 'تم التفعيل')
    await store.loadDashboard()
  }
}

onMounted(async () => {
  try {
    await Promise.all([
      store.financial && store.subscription ? Promise.resolve() : store.loadDashboard(),
      store.loadPackages(),
    ])
  } catch (err) {
    toast.error(
      getApiMessage(err.response?.data, err.response?.data?.error || 'تعذر تحميل صفحة التجديد'),
      'خطأ',
    )
  }
  if (!canCredit.value) form.saleType = true
})
</script>

<style scoped>
.refill {
  padding-bottom: calc(var(--nav-h) + 118px + var(--safe-bottom));
}

.hero {
  position: relative;
  overflow: hidden;
  border-radius: 24px;
  padding: 22px 20px 18px;
  margin-bottom: 16px;
  color: #fff;
  background:
    radial-gradient(circle at 100% 0%, rgba(22, 184, 148, 0.35), transparent 42%),
    linear-gradient(155deg, #0a1d2f 0%, #071525 70%);
  box-shadow: var(--shadow);
}

.eyebrow {
  margin: 0;
  font-size: 0.78rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  opacity: 0.7;
  font-weight: 600;
}

.hero h1 {
  margin: 8px 0 0;
  font-size: 1.7rem;
  letter-spacing: -0.03em;
}

.sub {
  margin: 8px 0 0;
  opacity: 0.72;
  font-size: 0.92rem;
  line-height: 1.5;
}

.hero-meta {
  margin-top: 18px;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  padding-top: 14px;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.hero-meta span {
  display: block;
  font-size: 0.72rem;
  opacity: 0.65;
  margin-bottom: 4px;
}

.hero-meta strong {
  font-size: 0.88rem;
  font-weight: 600;
  word-break: break-word;
}

.steps {
  display: grid;
  grid-template-columns: auto 1fr auto 1fr auto;
  align-items: center;
  gap: 8px;
  margin-bottom: 18px;
  padding: 0 4px;
}

.step {
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--ink-muted);
  font-size: 0.78rem;
  font-weight: 600;
}

.step i {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-style: normal;
  font-size: 0.75rem;
  background: #dbe3ec;
  color: var(--ink-muted);
  font-family: var(--font-num);
}

.step.on {
  color: var(--ink);
}

.step.on i {
  background: var(--accent);
  color: #fff;
}

.rail {
  height: 2px;
  background: #d5dee8;
  border-radius: 2px;
}

.block {
  margin-bottom: 16px;
}

.block-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  padding: 0 2px;
}

.block-head h2 {
  margin: 0;
  font-size: 1.05rem;
}

.count {
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--accent-deep);
  background: var(--accent-soft);
  padding: 5px 10px;
  border-radius: 999px;
}

.pkg-grid {
  display: grid;
  gap: 10px;
}

.pkg {
  position: relative;
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 12px;
  align-items: start;
  width: 100%;
  text-align: start;
  border: 1px solid var(--line);
  background: var(--surface);
  border-radius: 18px;
  padding: 16px 14px;
  cursor: pointer;
  box-shadow: 0 8px 24px rgba(4, 17, 29, 0.04);
  transition: border-color 0.18s ease, transform 0.18s ease, box-shadow 0.18s ease;
}

.pkg:active {
  transform: scale(0.99);
}

.pkg.featured {
  background:
    linear-gradient(180deg, rgba(22, 184, 148, 0.06), transparent 40%),
    var(--surface);
}

.pkg.active {
  border-color: rgba(22, 184, 148, 0.55);
  box-shadow: 0 12px 28px rgba(14, 143, 114, 0.12);
}

.pkg-badge {
  position: absolute;
  top: -9px;
  inset-inline-start: 14px;
  background: linear-gradient(135deg, var(--accent), var(--accent-deep));
  color: #fff;
  font-size: 0.68rem;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 999px;
}

.pkg-radio {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 2px solid #c5d0db;
  display: grid;
  place-items: center;
  margin-top: 2px;
  flex-shrink: 0;
}

.pkg.active .pkg-radio {
  border-color: var(--accent-deep);
}

.pkg-radio span {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: transparent;
  transition: background 0.15s ease;
}

.pkg.active .pkg-radio span {
  background: var(--accent);
}

.pkg-name {
  display: block;
  font-size: 1.02rem;
  letter-spacing: -0.02em;
}

.pkg-desc {
  margin: 4px 0 0;
  color: var(--ink-muted);
  font-size: 0.8rem;
  line-height: 1.4;
}

.pkg-price {
  margin-top: 10px;
  display: flex;
  align-items: baseline;
  gap: 4px;
}

.amount {
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--ink);
}

.pkg.active .amount {
  color: var(--accent-deep);
}

.period {
  color: var(--ink-muted);
  font-size: 0.78rem;
}

.pay-switch {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}

.pay-opt {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 12px;
  border-radius: 16px;
  border: 1px solid var(--line);
  background: var(--surface);
  text-align: start;
  cursor: pointer;
  transition: border-color 0.18s ease, background 0.18s ease;
}

.pay-opt.active {
  border-color: rgba(22, 184, 148, 0.5);
  background: var(--accent-soft);
}

.pay-opt.disabled,
.pay-opt:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.pay-icon {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  flex-shrink: 0;
}

.pay-icon.cash {
  background: rgba(22, 184, 148, 0.14);
  color: var(--accent-deep);
}

.pay-icon.credit {
  background: var(--warn-soft);
  color: var(--warn);
}

.pay-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.pay-text strong {
  font-size: 0.95rem;
}

.pay-text small {
  color: var(--ink-muted);
  font-size: 0.75rem;
}

.hint-line {
  margin-top: 10px;
  font-size: 0.82rem;
  color: var(--ink-muted);
  line-height: 1.5;
  padding: 0 2px;
}

.confirm-bar {
  position: fixed;
  inset-inline: 14px;
  bottom: calc(var(--nav-h) + 18px + var(--safe-bottom));
  z-index: 30;
  display: grid;
  grid-template-columns: 1.2fr auto;
  gap: 12px;
  align-items: center;
  padding: 12px 12px 12px 14px;
  border-radius: 20px;
  background: rgba(255, 255, 255, 0.94);
  border: 1px solid var(--line);
  box-shadow: 0 18px 40px rgba(4, 17, 29, 0.16);
  backdrop-filter: blur(14px);
}

.confirm-summary {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.confirm-summary span {
  font-size: 0.72rem;
  color: var(--ink-muted);
}

.confirm-summary strong {
  font-size: 0.95rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.confirm-summary em {
  font-style: normal;
  font-size: 0.8rem;
  color: var(--accent-deep);
  font-weight: 700;
}

.confirm-summary.muted strong {
  color: var(--ink-muted);
  font-weight: 600;
}

.confirm-btn {
  white-space: nowrap;
  padding-inline: 18px;
  min-height: 46px;
}

.skel-list {
  display: grid;
  gap: 10px;
}

.skel {
  height: 88px;
  border-radius: 18px;
}

@media (max-width: 380px) {
  .confirm-bar {
    grid-template-columns: 1fr;
  }

  .confirm-btn {
    width: 100%;
  }
}
</style>
