<template>
  <div class="page home">
    <header class="top rise">
      <div class="brand-line">
        <img class="brand-mark" src="/logowithouttext.png" alt="IBS" width="40" height="40" />
        <div>
          <p class="hello">مرحباً</p>
          <h1>{{ subscription?.displayName || auth.fullName || 'المشترك' }}</h1>
        </div>
      </div>
      <button class="avatar" type="button" @click="$router.push('/more')" :aria-label="auth.mobile">
        {{ initials }}
      </button>
    </header>

    <section v-if="store.loading && !store.financial" class="stack">
      <div class="skeleton hero-skel" />
      <div class="skeleton row-skel" />
      <div class="skeleton row-skel" />
    </section>

    <template v-else>
      <section class="balance surface rise" style="animation-delay: 0.05s">
        <div class="balance-label">المبلغ المستحق</div>
        <div class="balance-value num">{{ formatMoney(due) }}</div>
        <div class="balance-meta">
          <span>حد الدين: {{ formatMoney(store.financial?.debtLimit ?? 0) }}</span>
          <span :class="store.financial?.canActiveNoCash ? 'ok' : 'no'">
            {{ store.financial?.canActiveNoCash ? 'تفعيل آجل متاح' : 'تفعيل آجل غير متاح' }}
          </span>
        </div>
        <div class="balance-actions">
          <button
            class="btn btn-primary"
            type="button"
            :disabled="Number(due) <= 0"
            @click="$router.push('/payment')"
          >
            سدّد الدين
          </button>
          <button class="btn btn-ghost" type="button" @click="$router.push('/refill')">
            تجديد الاشتراك
          </button>
        </div>
      </section>

      <section class="actions rise" style="animation-delay: 0.1s">
        <button
          v-for="action in quickActions"
          :key="action.to"
          class="action surface"
          type="button"
          @click="$router.push(action.to)"
        >
          <span class="action-icon" :style="{ background: action.tone }" v-html="action.icon" />
          <span class="action-text">
            <strong>{{ action.title }}</strong>
            <small>{{ action.desc }}</small>
          </span>
        </button>
      </section>

      <section class="info surface rise" style="animation-delay: 0.16s">
        <div class="info-head">
          <h2>حالة الاشتراك</h2>
          <div class="head-actions">
            <button class="link" type="button" @click="refresh">تحديث</button>
            <button class="link strong" type="button" @click="$router.push('/subscription')">
              التفاصيل
            </button>
          </div>
        </div>

        <div v-if="store.error" class="alert alert-error">{{ store.error }}</div>

        <div v-else class="info-grid">
          <div class="info-item">
            <span>الاسم</span>
            <strong>{{ subscription?.displayName || auth.fullName || '—' }}</strong>
          </div>
          <div class="info-item">
            <span>المستخدم</span>
            <strong>{{ subscription?.userId || '—' }}</strong>
          </div>
          <div class="info-item">
            <span>الباقة</span>
            <strong>{{ subscription?.accountName || '—' }}</strong>
          </div>
          <div class="info-item">
            <span>حالة الحساب</span>
            <strong :class="statusClass">{{ accountStatusAr }}</strong>
          </div>
          <div class="info-item">
            <span>الاتصال</span>
            <strong :class="onlineClass">{{ onlineStatusAr }}</strong>
          </div>
          <div class="info-item">
            <span>يمكن التجديد</span>
            <strong :class="subscription?.canRefill ? 'ok' : 'bad'">
              {{ subscription?.canRefill ? 'نعم' : 'لا' }}
            </strong>
          </div>
          <div class="info-item">
            <span>آخر تجديد</span>
            <strong>{{ formatWhen(subscription?.lastRefill) || formatDate(store.financial?.lastActivation) }}</strong>
          </div>
          <div class="info-item">
            <span>الانتهاء</span>
            <strong>{{ formatWhen(subscription?.manualExpirationDate) }}</strong>
          </div>
          <div class="info-item wide">
            <span>الوكيل / الشبكة</span>
            <strong>
              {{ subscription?.agentName || '—' }}
              <template v-if="subscription?.affiliateName"> · {{ subscription.affiliateName }}</template>
            </strong>
          </div>
          <div class="info-item wide" v-if="subscription?.customer?.customerFullName">
            <span>العميل</span>
            <strong>
              {{ subscription.customer.customerFullName }}
              <template v-if="subscription.customer.customerPhoneNumber">
                · {{ subscription.customer.customerPhoneNumber }}
              </template>
            </strong>
          </div>
        </div>

        <button class="details-btn" type="button" @click="$router.push('/subscription')">
          عرض كل التفاصيل
        </button>
      </section>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useSubscriberStore } from '../stores/subscriber'
import { formatMoney, formatDate } from '../composables/format'

const auth = useAuthStore()
const store = useSubscriberStore()

const due = computed(() => store.amountDue ?? store.financial?.amountDue ?? 0)
const subscription = computed(() => store.subscription || {})

const initials = computed(() => {
  const name = subscription.value?.displayName || auth.fullName || 'مشترك'
  return name.trim().slice(0, 1)
})

function formatWhen(raw) {
  if (!raw) return '—'
  return String(raw).replace(/\bAM\b/i, 'ص').replace(/\bPM\b/i, 'م')
}

function mapAccountStatus(status) {
  const s = String(status || '').toLowerCase()
  if (s === 'active') return 'فعّال'
  if (s === 'disabled' || s === 'disable') return 'موقوف'
  if (s.includes('expir')) return 'منتهٍ'
  return status || '—'
}

function mapOnlineStatus(status) {
  const s = String(status || '').toLowerCase()
  if (s === 'online') return 'متصل'
  if (s === 'offline') return 'غير متصل'
  return status || '—'
}

const accountStatusAr = computed(() => mapAccountStatus(subscription.value?.accountStatus))
const onlineStatusAr = computed(() => mapOnlineStatus(subscription.value?.onlineStatus))

const statusClass = computed(() => {
  const status = String(subscription.value?.accountStatus || '')
  if (/active|فعال/i.test(status)) return 'ok'
  if (/expire|disable|منته|موقوف/i.test(status)) return 'bad'
  return ''
})

const onlineClass = computed(() => {
  const status = String(subscription.value?.onlineStatus || '')
  if (/online|متصل/i.test(status)) return 'ok'
  if (/offline|غير/i.test(status)) return 'bad'
  return ''
})

const quickActions = [
  {
    to: '/invoices',
    title: 'الفواتير',
    desc: 'سجل التفعيلات',
    tone: 'rgba(0,174,239,0.14)',
    icon: `<svg viewBox="0 0 24 24" width="22" height="22" fill="none"><path d="M7 3.5h10A1.5 1.5 0 0 1 18.5 5v14l-3-1.5-3 1.5-3-1.5-3 1.5V5A1.5 1.5 0 0 1 7 3.5Z" stroke="#0054a6" stroke-width="1.8"/></svg>`,
  },
  {
    to: '/subscription',
    title: 'الاشتراك',
    desc: 'تفاصيل الحساب',
    tone: 'rgba(247,148,30,0.14)',
    icon: `<svg viewBox="0 0 24 24" width="22" height="22" fill="none"><circle cx="12" cy="8" r="3.2" stroke="#d84c27" stroke-width="1.8"/><path d="M5.5 18.2c1.6-2.8 4-4.2 6.5-4.2s4.9 1.4 6.5 4.2" stroke="#d84c27" stroke-width="1.8" stroke-linecap="round"/></svg>`,
  },
]

async function refresh() {
  await store.loadDashboard()
}

onMounted(refresh)
</script>

<style scoped>
.top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.brand-line {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.brand-mark {
  width: 42px;
  height: 42px;
  object-fit: contain;
  flex-shrink: 0;
  filter: drop-shadow(0 8px 16px rgba(0, 174, 239, 0.25));
}

.hello {
  margin: 0;
  color: var(--ink-muted);
  font-size: 0.9rem;
}

.top h1 {
  margin: 4px 0 0;
  font-size: 1.55rem;
  letter-spacing: -0.03em;
}

.avatar {
  width: 46px;
  height: 46px;
  border: none;
  border-radius: 16px;
  background: linear-gradient(145deg, #0054a6, #003366);
  color: #fff;
  font-weight: 800;
  cursor: pointer;
}

.balance {
  padding: 22px;
  background:
    radial-gradient(circle at 100% 0%, rgba(0, 174, 239, 0.28), transparent 42%),
    radial-gradient(circle at 0% 100%, rgba(247, 148, 30, 0.22), transparent 40%),
    linear-gradient(160deg, #003366, #001a33 70%);
  color: #fff;
  border: none;
}

.balance-label {
  opacity: 0.72;
  font-size: 0.9rem;
}

.balance-value {
  margin-top: 8px;
  font-size: clamp(1.9rem, 7vw, 2.4rem);
  font-weight: 700;
  letter-spacing: -0.04em;
}

.balance-meta {
  margin-top: 14px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px 14px;
  font-size: 0.82rem;
  opacity: 0.85;
}

.balance-meta .ok {
  color: #7ddcff;
}

.balance-meta .no {
  color: #ffc39a;
}

.balance-actions {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 10px;
  margin-top: 20px;
}

.balance-actions .btn-ghost {
  color: #fff;
  border-color: rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.06);
}

.actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin: 14px 0;
}

.action {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px;
  text-align: start;
  cursor: pointer;
  width: 100%;
}

.action-icon {
  width: 42px;
  height: 42px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  flex-shrink: 0;
}

.action-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.action-text strong {
  font-size: 0.95rem;
}

.action-text small {
  color: var(--ink-muted);
  font-size: 0.78rem;
}

.info {
  padding: 18px;
}

.info-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
  gap: 10px;
}

.info-head h2 {
  margin: 0;
  font-size: 1.05rem;
}

.head-actions {
  display: flex;
  gap: 12px;
}

.link {
  border: none;
  background: none;
  color: var(--accent-deep);
  font-weight: 700;
  cursor: pointer;
}

.link.strong {
  text-decoration: underline;
  text-underline-offset: 3px;
}

.info-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.info-item {
  background: var(--surface-soft);
  border-radius: 14px;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.info-item.wide {
  grid-column: 1 / -1;
}

.info-item span {
  color: var(--ink-muted);
  font-size: 0.78rem;
}

.info-item strong {
  font-size: 0.92rem;
  word-break: break-word;
}

.info-item .ok {
  color: var(--accent-deep);
}

.info-item .bad {
  color: var(--danger);
}

.details-btn {
  margin-top: 14px;
  width: 100%;
  border: 1px dashed rgba(11, 26, 40, 0.18);
  background: transparent;
  border-radius: 12px;
  padding: 12px;
  color: var(--accent-deep);
  font-weight: 700;
  cursor: pointer;
}

.hero-skel {
  height: 210px;
  margin-bottom: 14px;
}

.row-skel {
  height: 84px;
  margin-bottom: 12px;
}

.stack {
  display: block;
}
</style>
