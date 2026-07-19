const STORAGE_KEY = 'ibs.pendingPayment'

export function savePendingPayment({ paymentId, requestId, purpose, company } = {}) {
  if (!paymentId) return
  const payload = {
    paymentId: String(paymentId),
    requestId: requestId ? String(requestId) : '',
    purpose: purpose || 'Refill',
    company: company ? String(company) : '',
    savedAt: Date.now(),
  }
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(payload))
  } catch {
    // ignore quota / private mode
  }
}

export function readPendingPayment() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const data = JSON.parse(raw)
    if (!data?.paymentId) return null
    // Drop stale pending payments older than 2 hours.
    if (data.savedAt && Date.now() - Number(data.savedAt) > 2 * 60 * 60 * 1000) {
      clearPendingPayment()
      return null
    }
    return data
  } catch {
    return null
  }
}

export function clearPendingPayment() {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    // ignore
  }
}

export function extractPaymentIds(payload) {
  const data = payload?.data ?? payload ?? {}
  return {
    paymentId:
      data.qiPaymentId ||
      data.QiPaymentId ||
      data.paymentId ||
      data.PaymentId ||
      '',
    requestId: data.requestId || data.RequestId || '',
    purpose: data.purpose || data.Purpose || '',
  }
}
