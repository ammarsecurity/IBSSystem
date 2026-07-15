<template>
  <div class="page details">
    <header class="page-header rise">
      <div>
        <h1>تفاصيل الاشتراك</h1>
        <p>بيانات الحساب من مزوّد الخدمة</p>
      </div>
      <button class="link" type="button" :disabled="loading" @click="refresh">
        {{ loading ? 'جاري التحديث...' : 'تحديث' }}
      </button>
    </header>

    <div v-if="loading && !subscription" class="stack">
      <div class="skeleton skel" />
      <div class="skeleton skel" />
      <div class="skeleton skel short" />
    </div>

    <div v-else-if="error" class="alert alert-error rise">{{ error }}</div>

    <template v-else>
      <section class="hero surface rise">
        <div class="hero-top">
          <div>
            <span class="eyebrow">الباقة</span>
            <h2>{{ subscription.accountName || '—' }}</h2>
          </div>
          <span class="pill" :class="accountTone">{{ accountStatusAr }}</span>
        </div>
        <div class="hero-meta">
          <div>
            <span>الانتهاء</span>
            <strong>{{ formatWhen(subscription.manualExpirationDate) }}</strong>
          </div>
          <div>
            <span>آخر تجديد</span>
            <strong>{{ formatWhen(subscription.lastRefill) }}</strong>
          </div>
        </div>
        <div class="online" :class="onlineTone">
          <i />
          <span>{{ onlineStatusAr }}</span>
        </div>
      </section>

      <section
        v-for="(group, index) in groups"
        :key="group.title"
        class="card surface rise"
        :style="{ animationDelay: `${0.06 + index * 0.04}s` }"
      >
        <h3>{{ group.title }}</h3>
        <div class="rows">
          <div v-for="row in group.rows" :key="row.label" class="row">
            <span>{{ row.label }}</span>
            <strong :class="row.tone || ''">{{ row.value }}</strong>
          </div>
        </div>
      </section>

      <section class="flags surface rise" style="animation-delay: 0.2s">
        <h3>الصلاحيات</h3>
        <div class="flag-grid">
          <div v-for="flag in flags" :key="flag.label" class="flag" :class="{ on: flag.on }">
            <strong>{{ flag.label }}</strong>
            <span>{{ flag.on ? 'متاح' : 'غير متاح' }}</span>
          </div>
        </div>
      </section>

      <div class="actions rise" style="animation-delay: 0.24s">
        <button class="btn btn-primary" type="button" @click="$router.push('/refill')">
          تجديد الاشتراك
        </button>
        <button class="btn btn-ghost" type="button" @click="$router.push('/payment')">
          تسديد الدين
        </button>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useSubscriberStore } from '../stores/subscriber'
import { useToastStore } from '../stores/toast'
import { getApiMessage } from '../composables/apiMessage'

const store = useSubscriberStore()
const toast = useToastStore()
const loading = ref(false)
const error = ref('')

const subscription = computed(() => store.subscription || null)

function val(value, fallback = '—') {
  if (value === null || value === undefined || value === '') return fallback
  return String(value)
}

function yesNo(v) {
  return v ? 'نعم' : 'لا'
}

function formatWhen(raw) {
  if (!raw) return '—'
  return String(raw)
    .replace(/\bAM\b/i, 'ص')
    .replace(/\bPM\b/i, 'م')
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

const accountTone = computed(() => {
  const s = String(subscription.value?.accountStatus || '')
  if (/active|فعال/i.test(s)) return 'ok'
  if (/expir|disable|منته|موقوف/i.test(s)) return 'bad'
  return ''
})

const onlineTone = computed(() => {
  const color = String(subscription.value?.onlineStatusColor || '').toLowerCase()
  const status = String(subscription.value?.onlineStatus || '').toLowerCase()
  if (status === 'online' || color === 'green') return 'on'
  if (status === 'offline' || color === 'red') return 'off'
  return ''
})

const customer = computed(() => subscription.value?.customer || {})

const groups = computed(() => {
  const s = subscription.value || {}
  const c = customer.value || {}
  return [
    {
      title: 'بيانات المستخدم',
      rows: [
        { label: 'الاسم المعروض', value: val(s.displayName) },
        { label: 'اسم المستخدم', value: val(s.userId) },
        { label: 'رقم الموبايل', value: val(s.mobileNumber) },
        { label: 'فهرس المستخدم', value: val(s.userIndex) },
        { label: 'ملاحظات', value: val(s.userNotes) },
      ],
    },
    {
      title: 'الحساب والباقة',
      rows: [
        { label: 'اسم الباقة', value: val(s.accountName) },
        { label: 'حالة الحساب', value: accountStatusAr.value, tone: accountTone.value },
        { label: 'فهرس الحساب', value: val(s.accountIndex) },
        { label: 'تاريخ الانتهاء', value: formatWhen(s.manualExpirationDate) },
        { label: 'آخر تجديد', value: formatWhen(s.lastRefill) },
      ],
    },
    {
      title: 'الاتصال',
      rows: [
        { label: 'حالة الاتصال', value: onlineStatusAr.value, tone: onlineTone.value === 'on' ? 'ok' : onlineTone.value === 'off' ? 'bad' : '' },
        { label: 'عنوان IP', value: val(s.ipAddress) },
        { label: 'MAC', value: val(s.callerMAC) },
        { label: 'وقت الاتصال', value: val(s.onlineTime) },
        { label: 'مصدر الدخول', value: val(s.loginFrom) },
      ],
    },
    {
      title: 'الوكيل والعميل',
      rows: [
        { label: 'الوكيل', value: val(s.agentName) },
        { label: 'الشبكة / الـ Affiliate', value: val(s.affiliateName) },
        { label: 'اسم العميل', value: val(c.customerFullName) },
        { label: 'هاتف العميل', value: val(c.customerPhoneNumber) },
        { label: 'هاتف إضافي', value: val(c.customerSecondPhoneNumber) },
        { label: 'البريد', value: val(c.email) },
        { label: 'العنوان', value: val(c.address) },
      ],
    },
  ]
})

const flags = computed(() => {
  const s = subscription.value || {}
  return [
    { label: 'التجديد', on: Boolean(s.canRefill) },
    { label: 'التمديد', on: Boolean(s.canExtendUser) },
    { label: 'تغيير الباقة', on: Boolean(s.canChangeAccount) },
    { label: 'الحذف', on: Boolean(s.canDelete) },
    { label: 'المستخدم نشط', on: Boolean(s.userActive) },
    { label: 'إدارة النشاط', on: Boolean(s.userActiveManage) },
    { label: 'ربط اشتراك للعميل', on: Boolean(s.customer?.canAttachSubscription) },
  ]
})

async function refresh() {
  loading.value = true
  error.value = ''
  try {
    await store.loadDashboard()
  } catch (err) {
    error.value = getApiMessage(err.response?.data, 'تعذر تحميل تفاصيل الاشتراك')
    toast.error(error.value, 'خطأ')
  } finally {
    loading.value = false
  }
}

onMounted(refresh)
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.page-header p {
  margin: 6px 0 0;
  color: var(--ink-muted);
}

.link {
  border: none;
  background: none;
  color: var(--accent-deep);
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
}

.stack {
  display: grid;
  gap: 12px;
}

.skel {
  height: 140px;
  border-radius: 16px;
}

.skel.short {
  height: 90px;
}

.hero {
  padding: 18px;
  margin-bottom: 12px;
  background:
    radial-gradient(circle at 100% 0%, rgba(22, 184, 148, 0.16), transparent 40%),
    #fff;
}

.hero-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
}

.eyebrow {
  display: block;
  color: var(--ink-muted);
  font-size: 0.8rem;
}

.hero-top h2 {
  margin: 4px 0 0;
  font-size: 1.35rem;
}

.pill {
  border-radius: 999px;
  padding: 6px 10px;
  font-size: 0.78rem;
  font-weight: 700;
  background: rgba(11, 26, 40, 0.06);
}

.pill.ok {
  background: var(--accent-soft);
  color: var(--accent-deep);
}

.pill.bad {
  background: rgba(214, 69, 69, 0.12);
  color: var(--danger);
}

.hero-meta {
  margin-top: 16px;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}

.hero-meta > div {
  background: var(--surface-soft);
  border-radius: 12px;
  padding: 10px 12px;
  display: grid;
  gap: 4px;
}

.hero-meta span {
  color: var(--ink-muted);
  font-size: 0.76rem;
}

.online {
  margin-top: 14px;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 0.88rem;
  font-weight: 700;
}

.online i {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #9aa4b2;
}

.online.on {
  color: var(--accent-deep);
}

.online.on i {
  background: var(--accent-deep);
}

.online.off {
  color: var(--danger);
}

.online.off i {
  background: var(--danger);
}

.card,
.flags {
  padding: 16px;
  margin-bottom: 12px;
}

.card h3,
.flags h3 {
  margin: 0 0 12px;
  font-size: 0.98rem;
}

.rows {
  display: grid;
  gap: 10px;
}

.row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(11, 26, 40, 0.06);
}

.row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.row span {
  color: var(--ink-muted);
  font-size: 0.86rem;
}

.row strong {
  text-align: end;
  font-size: 0.9rem;
  word-break: break-word;
}

.row .ok {
  color: var(--accent-deep);
}

.row .bad {
  color: var(--danger);
}

.flag-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}

.flag {
  border-radius: 12px;
  padding: 10px 12px;
  background: rgba(214, 69, 69, 0.08);
  display: grid;
  gap: 4px;
}

.flag.on {
  background: var(--accent-soft);
}

.flag strong {
  font-size: 0.86rem;
}

.flag span {
  font-size: 0.78rem;
  color: var(--ink-muted);
}

.flag.on span {
  color: var(--accent-deep);
}

.actions {
  display: grid;
  gap: 10px;
  margin-top: 4px;
}

.actions .btn {
  width: 100%;
}
</style>
