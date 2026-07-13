/**
 * Normalize API Response shape: { error, message, data }
 */
export function getApiMessage(payload, fallback = '') {
  if (!payload) return fallback

  if (typeof payload === 'string') return payload

  const message = payload.message
  if (typeof message === 'string' && message.trim()) return message.trim()

  const data = payload.data
  if (typeof data === 'string' && data.trim()) {
    const text = data.trim()
    // Payment APIs sometimes put a redirect URL in data
    if (/^https?:\/\//i.test(text)) return fallback
    return text
  }

  if (typeof payload.error === 'string' && payload.error.trim()) return payload.error.trim()

  return fallback
}

export function isApiError(payload) {
  if (!payload) return true
  if (payload.error === true) return true
  if (typeof payload.error === 'string' && payload.error.length) return true
  if (payload.isSuccess === false) return true
  return false
}
