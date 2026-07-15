<template>
  <div class="page">
    <header class="page-header rise">
      <div>
        <h1>المزيد</h1>
        <p>الحساب والإعدادات</p>
      </div>
    </header>

    <section class="profile surface rise">
      <div class="avatar">{{ initials }}</div>
      <div>
        <strong>{{ auth.fullName || 'المشترك' }}</strong>
        <p>{{ auth.mobile || '—' }}</p>
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
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useSubscriberStore } from '../stores/subscriber'

const router = useRouter()
const auth = useAuthStore()
const subscriber = useSubscriberStore()

const initials = computed(() => (auth.fullName || 'م').trim().slice(0, 1))

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

.avatar {
  width: 56px;
  height: 56px;
  border-radius: 18px;
  display: grid;
  place-items: center;
  background: linear-gradient(145deg, #12324a, #071a2b);
  color: #fff;
  font-size: 1.3rem;
  font-weight: 800;
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
