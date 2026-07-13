import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { login as loginApi } from '../api/auth'
import { clearAuthToken, setAuthToken } from '../api/client'

const STORAGE_KEY = 'ibs_auth'

function loadStored() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const stored = loadStored()
  const token = ref(stored?.accessToken || null)
  const userId = ref(stored?.userId || null)
  const fullName = ref(stored?.fullName || '')
  const mobile = ref(stored?.mobile || '')
  const expiresAt = ref(stored?.expiresAt || null)
  const loading = ref(false)
  const error = ref('')

  if (token.value) setAuthToken(token.value)

  const isAuthenticated = computed(() => Boolean(token.value))

  function persist() {
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        accessToken: token.value,
        userId: userId.value,
        fullName: fullName.value,
        mobile: mobile.value,
        expiresAt: expiresAt.value,
      }),
    )
  }

  async function login(credentials) {
    loading.value = true
    error.value = ''
    try {
      const { data } = await loginApi(credentials)
      if (!data?.isSuccess || !data?.accessToken) {
        error.value = data?.error || 'فشل تسجيل الدخول'
        return false
      }
      token.value = data.accessToken
      userId.value = data.userId
      fullName.value = data.fullName || ''
      mobile.value = data.mobile || credentials.mobile
      expiresAt.value = data.expiresAt
      setAuthToken(data.accessToken)
      persist()
      return true
    } catch (err) {
      error.value =
        err.response?.data?.error ||
        err.response?.data?.message ||
        'تعذر الاتصال بالخادم'
      return false
    } finally {
      loading.value = false
    }
  }

  function logout() {
    token.value = null
    userId.value = null
    fullName.value = ''
    mobile.value = ''
    expiresAt.value = null
    clearAuthToken()
    localStorage.removeItem(STORAGE_KEY)
  }

  return {
    token,
    userId,
    fullName,
    mobile,
    expiresAt,
    loading,
    error,
    isAuthenticated,
    login,
    logout,
  }
})
