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

  if (data && typeof data === 'object') {
    const nested =
      data.message ||
      data.Message ||
      data.msg ||
      null
    if (typeof nested === 'string' && nested.trim()) return nested.trim()
  }

  if (typeof payload.error === 'string' && payload.error.trim()) return payload.error.trim()

  return fallback
}

export function getApiPurpose(payload) {
  const data = payload?.data
  if (data && typeof data === 'object') {
    return data.purpose || data.Purpose || null
  }
  return null
}

export function isApiError(payload) {
  if (!payload) return true
  if (payload.error === true) return true
  if (typeof payload.error === 'string' && payload.error.length) return true
  if (payload.isSuccess === false) return true
  return false
}
