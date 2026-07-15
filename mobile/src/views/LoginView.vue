<template>
  <div class="login">
    <div class="sky" aria-hidden="true">
      <span class="orb orb-a" />
      <span class="orb orb-b" />
      <span class="orb orb-c" />
    </div>

    <header class="hero rise">
      <div class="logo-wrap">
        <img
          class="logo"
          src="/logo.png"
          alt="IBS Internet"
          width="280"
          height="280"
        />
      </div>
      <p class="tagline">اشتراكك ومدفوعاتك في مكان واحد</p>
    </header>

    <form class="sheet rise" style="animation-delay: 0.1s" @submit.prevent="onSubmit">
      <div class="sheet-handle" aria-hidden="true" />

      <div class="sheet-head">
        <h1>دخول المشترك</h1>
        <p>اختر شركتك ثم أدخل رقم الموبايل</p>
      </div>

      <div class="field">
        <label for="company">الشركة التابعة</label>
        <div class="company-grid" role="listbox" aria-label="اختر الشركة">
          <button
            v-for="c in companies"
            :key="c.id"
            type="button"
            role="option"
            class="company-tile"
            :class="{ on: form.company === c.id }"
            :aria-selected="form.company === c.id"
            @click="form.company = c.id"
          >
            <strong>{{ c.label }}</strong>
            <small>{{ c.hint }}</small>
          </button>
        </div>
      </div>

      <div class="field">
        <label for="mobile">رقم الموبايل</label>
        <div class="input-wrap">
          <span class="input-icon" v-html="icons.phone" />
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
      </div>

      <div v-if="auth.error" class="alert alert-error">{{ auth.error }}</div>

      <button
        class="btn btn-primary submit"
        type="submit"
        :disabled="auth.loading || !form.company || !form.mobile"
      >
        {{ auth.loading ? 'جاري التحقق...' : 'متابعة' }}
      </button>

      <p class="foot">بالدخول فإنك تستخدم حساب اشتراك IBS Internet</p>
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

const companies = [
  { id: 'WAEL', label: 'WAEL', hint: 'واصل' },
  { id: 'Wi-Fi', label: 'Wi-Fi', hint: 'واي فاي' },
  { id: 'Connect', label: 'Connect', hint: 'كونكت' },
  { id: 'KGD', label: 'KGD', hint: 'كي جي دي' },
  { id: 'SASNET', label: 'SASNET', hint: 'ساس نت' },
  { id: 'NetSpeed', label: 'NetSpeed', hint: 'نت سبيد' },
  { id: 'Online', label: 'Online', hint: 'أونلاين' },
  { id: 'Arjwan', label: 'Arjwan', hint: 'أرجوان' },
]

const icons = {
  phone: `<svg viewBox="0 0 24 24" width="20" height="20" fill="none"><path d="M8.4 4.8h2.2l1.1 3.3-1.4 1.4a12.4 12.4 0 0 0 4.6 4.6l1.4-1.4 3.3 1.1v2.2A2.2 2.2 0 0 1 17.4 18 13.4 13.4 0 0 1 4 4.6 2.2 2.2 0 0 1 6.2 2.4H8.4Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>`,
}

const form = reactive({
  mobile: '',
  company: 'WAEL',
})

async function onSubmit() {
  if (!form.company) {
    toast.info('اختر الشركة التابعة أولاً', 'تنبيه')
    return
  }
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
  position: relative;
  min-height: 100dvh;
  display: grid;
  grid-template-rows: minmax(240px, 38dvh) 1fr;
  background: #000;
  color: #fff;
  overflow: hidden;
}

.sky {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(40px);
  opacity: 0.55;
}

.orb-a {
  width: 220px;
  height: 220px;
  top: 8%;
  right: -40px;
  background: rgba(0, 174, 239, 0.45);
  animation: float 8s ease-in-out infinite;
}

.orb-b {
  width: 180px;
  height: 180px;
  top: 18%;
  left: -50px;
  background: rgba(247, 148, 30, 0.35);
  animation: float 10s ease-in-out infinite reverse;
}

.orb-c {
  width: 120px;
  height: 120px;
  top: 42%;
  left: 40%;
  background: rgba(0, 84, 166, 0.35);
  animation: float 7s ease-in-out infinite 0.6s;
}

@keyframes float {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-12px);
  }
}

.hero {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-end;
  text-align: center;
  padding: calc(28px + var(--safe-top)) 24px 18px;
  gap: 10px;
}

.logo-wrap {
  position: relative;
  width: min(42vw, 140px);
}

.logo-wrap::before {
  content: '';
  position: absolute;
  inset: 18% 10% 22%;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(0, 174, 239, 0.35), transparent 70%);
  filter: blur(18px);
  z-index: 0;
}

.logo {
  position: relative;
  z-index: 1;
  width: 100%;
  height: auto;
  filter: drop-shadow(0 16px 32px rgba(0, 0, 0, 0.45));
  animation: float 6s ease-in-out infinite;
}

.tagline {
  margin: 0;
  max-width: 260px;
  color: rgba(255, 255, 255, 0.72);
  font-size: 0.92rem;
  line-height: 1.65;
}

.sheet {
  position: relative;
  z-index: 2;
  background: linear-gradient(180deg, #f7fafd 0%, #eef3f9 100%);
  color: var(--ink);
  border-radius: 28px 28px 0 0;
  padding: 12px 20px calc(22px + var(--safe-bottom));
  box-shadow: 0 -24px 60px rgba(0, 0, 0, 0.45);
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.sheet-handle {
  width: 44px;
  height: 5px;
  border-radius: 999px;
  background: rgba(0, 51, 102, 0.14);
  margin: 4px auto 16px;
}

.sheet-head h1 {
  margin: 0;
  font-size: 1.35rem;
  color: var(--brand-blue-deep);
  letter-spacing: -0.02em;
}

.sheet-head p {
  margin: 6px 0 0;
  color: var(--ink-muted);
  font-size: 0.9rem;
}

.field {
  margin-top: 18px;
}

.company-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
  margin-top: 2px;
  max-height: 168px;
  overflow: auto;
  padding: 2px;
  -webkit-overflow-scrolling: touch;
}

.company-tile {
  border: 1px solid rgba(0, 51, 102, 0.1);
  background: #fff;
  border-radius: 14px;
  padding: 10px 12px;
  text-align: start;
  cursor: pointer;
  display: grid;
  gap: 2px;
  transition: border-color 0.18s ease, background 0.18s ease, box-shadow 0.18s ease;
}

.company-tile strong {
  font-size: 0.88rem;
  color: var(--ink);
}

.company-tile small {
  font-size: 0.72rem;
  color: var(--ink-muted);
}

.company-tile.on {
  border-color: rgba(0, 174, 239, 0.55);
  background: linear-gradient(135deg, rgba(0, 174, 239, 0.12), #fff 55%);
  box-shadow: 0 0 0 3px rgba(0, 174, 239, 0.12);
}

.company-tile.on strong {
  color: var(--brand-blue-mid);
}

.input-wrap {
  display: flex;
  align-items: center;
  gap: 10px;
  background: #fff;
  border: 1px solid rgba(0, 51, 102, 0.1);
  border-radius: 14px;
  padding: 0 14px;
  transition: border-color 0.18s ease, box-shadow 0.18s ease;
}

.input-wrap:focus-within {
  border-color: rgba(0, 174, 239, 0.55);
  box-shadow: 0 0 0 4px rgba(0, 174, 239, 0.12);
}

.input-icon {
  color: var(--brand-blue-mid);
  display: grid;
  place-items: center;
  flex-shrink: 0;
}

.input-wrap input {
  width: 100%;
  border: none;
  background: transparent;
  padding: 14px 0;
  color: var(--ink);
  outline: none;
  font-family: var(--font-num);
  letter-spacing: 0.02em;
}

.submit {
  width: 100%;
  margin-top: 18px;
  min-height: 52px;
  font-size: 1.05rem;
  border-radius: 16px;
}

.foot {
  margin: 14px 0 0;
  text-align: center;
  color: var(--ink-muted);
  font-size: 0.78rem;
  line-height: 1.5;
}

@media (max-height: 740px) {
  .login {
    grid-template-rows: minmax(200px, 32dvh) 1fr;
  }

  .logo-wrap {
    width: min(36vw, 110px);
  }

  .tagline {
    font-size: 0.84rem;
  }

  .company-grid {
    max-height: 120px;
  }
}
</style>
