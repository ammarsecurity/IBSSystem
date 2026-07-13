<template>
  <div class="login">
    <div class="hero">
      <div class="hero-glow" />
      <div class="brand rise">
        <div class="mark">IBS</div>
        <h1>IBS Mobile</h1>
        <p>إدارة اشتراكك ومدفوعاتك بواجهة سلسة وآمنة</p>
      </div>
    </div>

    <form class="panel rise" style="animation-delay: 0.08s" @submit.prevent="onSubmit">
      <div class="panel-head">
        <h2>تسجيل الدخول</h2>
        <p>أدخل رقم الموبايل واسم الشركة للمتابعة</p>
      </div>

      <div class="fields">
        <div class="field">
          <label for="mobile">رقم الموبايل</label>
          <input
            id="mobile"
            v-model.trim="form.mobile"
            type="tel"
            inputmode="tel"
            placeholder="07xxxxxxxxx"
            autocomplete="tel"
            required
          />
        </div>

        <div class="field">
          <label for="company">الشركة / الشركة التابعة</label>
          <input
            id="company"
            v-model.trim="form.company"
            type="text"
            placeholder="اسم الشركة"
            autocomplete="organization"
            required
          />
        </div>
      </div>

      <div v-if="auth.error" class="alert alert-error">{{ auth.error }}</div>

      <button class="btn btn-primary submit" type="submit" :disabled="auth.loading">
        {{ auth.loading ? 'جاري التحقق...' : 'دخول' }}
      </button>
    </form>
  </div>
</template>

<script setup>
import { reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'

const router = useRouter()
const auth = useAuthStore()
const toast = useToastStore()

const form = reactive({
  mobile: '',
  company: '',
})

async function onSubmit() {
  const ok = await auth.login({
    mobile: form.mobile,
    company: form.company,
  })
  if (ok) {
    toast.success('تم تسجيل الدخول بنجاح', 'مرحباً بك')
    router.push({ name: 'home' })
  } else if (auth.error) {
    toast.error(auth.error, 'فشل الدخول')
  }
}
</script>

<style scoped>
.login {
  min-height: 100dvh;
  display: grid;
  grid-template-rows: 1.05fr auto;
  background: var(--bg-deep);
  color: #fff;
}

.hero {
  position: relative;
  overflow: hidden;
  padding: calc(48px + var(--safe-top)) 24px 36px;
  display: flex;
  align-items: flex-end;
}

.hero-glow {
  position: absolute;
  inset: -20% -10% auto auto;
  width: 70%;
  aspect-ratio: 1;
  background:
    radial-gradient(circle, rgba(22, 184, 148, 0.45), transparent 62%),
    radial-gradient(circle at 20% 80%, rgba(224, 122, 61, 0.28), transparent 55%);
  filter: blur(8px);
  pointer-events: none;
}

.brand {
  position: relative;
  z-index: 1;
  max-width: 340px;
}

.mark {
  display: inline-grid;
  place-items: center;
  width: 58px;
  height: 58px;
  border-radius: 18px;
  background: linear-gradient(145deg, #1fd6aa, #0e8f72);
  font-family: var(--font-num);
  font-weight: 800;
  letter-spacing: 0.04em;
  margin-bottom: 18px;
  box-shadow: 0 16px 36px rgba(22, 184, 148, 0.35);
}

.brand h1 {
  margin: 0;
  font-size: clamp(2.2rem, 8vw, 3rem);
  line-height: 1;
  letter-spacing: -0.04em;
}

.brand p {
  margin: 14px 0 0;
  color: rgba(255, 255, 255, 0.72);
  line-height: 1.7;
  font-size: 0.98rem;
}

.panel {
  background: var(--bg);
  color: var(--ink);
  border-radius: 32px 32px 0 0;
  padding: 28px 22px calc(28px + var(--safe-bottom));
  box-shadow: 0 -20px 50px rgba(0, 0, 0, 0.25);
}

.panel-head h2 {
  margin: 0;
  font-size: 1.35rem;
}

.panel-head p {
  margin: 8px 0 0;
  color: var(--ink-muted);
  font-size: 0.92rem;
}

.fields {
  display: grid;
  gap: 14px;
  margin: 22px 0 16px;
}

.submit {
  width: 100%;
  margin-top: 8px;
  font-size: 1.02rem;
}
</style>
