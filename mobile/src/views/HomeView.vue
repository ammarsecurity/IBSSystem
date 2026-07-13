<template>
  <div class="page home">
    <header class="top rise">
      <div>
        <p class="hello">مرحباً</p>
        <h1>{{ auth.fullName || 'المشترك' }}</h1>
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
          <button class="btn btn-primary" type="button" @click="$router.push('/payment')">
            ادفع الآن
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
          <button class="link" type="button" @click="refresh">تحديث</button>
        </div>

        <div v-if="store.error" class="alert alert-error">{{ store.error }}</div>

        <div v-else class="info-grid">
          <div class="info-item">
            <span>الحساب</span>
            <strong>{{ subscription?.accountName || '—' }}</strong>
          </div>
          <div class="info-item">
            <span>الحالة</span>
            <strong :class="statusClass">{{ subscription?.accountStatus || subscription?.onlineStatus || '—' }}</strong>
          </div>
          <div class="info-item">
            <span>آخر تجديد</span>
            <strong>{{ subscription?.lastRefill || formatDate(store.financial?.lastActivation) }}</strong>
          </div>
          <div class="info-item">
            <span>الانتهاء</span>
            <strong>{{ subscription?.manualExpirationDate || '—' }}</strong>
          </div>
          <div class="info-item wide">
            <span>الوكيل</span>
            <strong>{{ subscription?.agentName || subscription?.affiliateName || '—' }}</strong>
          </div>
        </div>
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
  const name = auth.fullName || 'مشترك'
  return name.trim().slice(0, 1)
})

const statusClass = computed(() => {
  const status = String(subscription.value?.accountStatus || subscription.value?.onlineStatus || '')
  if (/active|online|فعال|متصل/i.test(status)) return 'ok'
  if (/expire|disable|منته|موقوف/i.test(status)) return 'bad'
  return ''
})

const quickActions = [
  {
    to: '/invoices',
    title: 'الفواتير',
    desc: 'سجل التفعيلات',
    tone: 'rgba(22,184,148,0.14)',
    icon: `<svg viewBox="0 0 24 24" width="22" height="22" fill="none"><path d="M7 3.5h10A1.5 1.5 0 0 1 18.5 5v14l-3-1.5-3 1.5-3-1.5-3 1.5V5A1.5 1.5 0 0 1 7 3.5Z" stroke="#0e8f72" stroke-width="1.8"/></svg>`,
  },
  {
    to: '/receivables',
    title: 'المقبوضات',
    desc: 'سجل الدفعات',
    tone: 'rgba(224,122,61,0.14)',
    icon: `<svg viewBox="0 0 24 24" width="22" height="22" fill="none"><path d="M12 3v18M7 8h7.5a2.5 2.5 0 0 1 0 5H9.5a2.5 2.5 0 0 0 0 5H17" stroke="#e07a3d" stroke-width="1.8" stroke-linecap="round"/></svg>`,
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
  background: linear-gradient(145deg, #12324a, #071a2b);
  color: #fff;
  font-weight: 800;
  cursor: pointer;
}

.balance {
  padding: 22px;
  background:
    radial-gradient(circle at 100% 0%, rgba(22, 184, 148, 0.22), transparent 42%),
    linear-gradient(160deg, #0a1d2f, #071525 70%);
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
  color: #7dffcf;
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
}

.info-head h2 {
  margin: 0;
  font-size: 1.05rem;
}

.link {
  border: none;
  background: none;
  color: var(--accent-deep);
  font-weight: 700;
  cursor: pointer;
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
