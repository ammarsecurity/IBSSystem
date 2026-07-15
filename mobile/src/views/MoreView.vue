<template>
  <div class="page">
    <header class="page-header rise">
      <div>
        <h1>المزيد</h1>
        <p>الحساب والإعدادات</p>
      </div>
    </header>

    <section class="profile surface rise">
      <img class="avatar-logo" src="/logowithouttext.png" alt="IBS" width="56" height="56" />
      <div>
        <strong>{{ auth.fullName || 'المشترك' }}</strong>
        <p>{{ auth.mobile || '—' }}</p>
        <p v-if="auth.company" class="company">الشركة: {{ auth.company }}</p>
      </div>
    </section>

    <section class="menu surface rise" style="animation-delay: 0.05s">
      <RouterLink class="row" to="/subscription">
        <span>تفاصيل الاشتراك</span>
        <span class="chev">‹</span>
      </RouterLink>
      <RouterLink class="row" to="/receivables">
        <span>المقبوضات</span>
        <span class="chev">‹</span>
      </RouterLink>
      <RouterLink class="row" to="/invoices">
        <span>الفواتير</span>
        <span class="chev">‹</span>
      </RouterLink>
      <RouterLink class="row" to="/refill">
        <span>تجديد الاشتراك</span>
        <span class="chev">‹</span>
      </RouterLink>
      <RouterLink class="row" to="/payment">
        <span>تسديد الديون</span>
        <span class="chev">‹</span>
      </RouterLink>
    </section>

    <button class="btn logout rise" type="button" style="animation-delay: 0.1s" @click="onLogout">
      تسجيل الخروج
    </button>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useSubscriberStore } from '../stores/subscriber'

const router = useRouter()
const auth = useAuthStore()
const subscriber = useSubscriberStore()

function onLogout() {
  auth.logout()
  subscriber.reset()
  router.push({ name: 'login' })
}
</script>

<style scoped>
.profile {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 18px;
  margin-bottom: 14px;
}

.avatar-logo {
  width: 56px;
  height: 56px;
  object-fit: contain;
  flex-shrink: 0;
  filter: drop-shadow(0 8px 16px rgba(0, 174, 239, 0.22));
}

.profile strong {
  display: block;
  font-size: 1.1rem;
}

.profile p {
  margin: 4px 0 0;
  color: var(--ink-muted);
  font-family: var(--font-num);
}

.profile .company {
  font-family: var(--font);
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--accent-deep);
}

.menu {
  overflow: hidden;
}

.row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 18px;
  border-bottom: 1px solid var(--line);
  font-weight: 600;
}

.row:last-child {
  border-bottom: none;
}

.chev {
  color: var(--ink-muted);
  font-size: 1.2rem;
}

.logout {
  width: 100%;
  margin-top: 18px;
  background: rgba(214, 69, 69, 0.1);
  color: var(--danger);
}
</style>
