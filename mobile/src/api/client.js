import axios from 'axios'

const STORAGE_KEY = 'ibs_auth'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5026',
  timeout: 30000,
  // Prevent axios from masking redirect issues in non-browser adapters
  maxRedirects: 0,
  headers: {
    Accept: 'application/json',
  },
})

export function setAuthToken(token) {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`
  } else {
    delete api.defaults.headers.common.Authorization
  }
}

export function clearAuthToken() {
  delete api.defaults.headers.common.Authorization
}

function getStoredToken() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw)?.accessToken || null : null
  } catch {
    return null
  }
}

const initialToken = getStoredToken()
if (initialToken) setAuthToken(initialToken)

api.interceptors.request.use((config) => {
  const token = getStoredToken()
  if (token) {
    const value = `Bearer ${token}`
    // Always force-set so redirects/HMR cannot drop it
    if (!config.headers) config.headers = {}
    if (typeof config.headers.set === 'function') {
      config.headers.set('Authorization', value)
      config.headers.set('Accept', 'application/json')
    } else {
      config.headers.Authorization = value
      config.headers.Accept = 'application/json'
    }
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status
    const url = String(error.config?.url || '')

    // 307/308 means HTTPS redirect still enabled — do not wipe the session
    if (status === 307 || status === 308) {
      console.error('API redirected (HTTPS). Use http://localhost:5026 and restart backend.')
      return Promise.reject(error)
    }

    if (status === 401 && !url.includes('/Auth/login')) {
      // Only logout if token exists and server explicitly rejected it on same origin
      const requestUrl = error.config?.baseURL || ''
      const isHttpsRedirectTarget = String(error.request?.responseURL || '').includes('7101')
      if (!isHttpsRedirectTarget) {
        clearAuthToken()
        localStorage.removeItem(STORAGE_KEY)
        const { default: router } = await import('../router')
        if (router.currentRoute.value.name !== 'login') {
          await router.push({ name: 'login' })
        }
      }
    }

    return Promise.reject(error)
  },
)

export default api
