import { Capacitor } from '@capacitor/core'
import {
  clearPendingPayment,
  readPendingPayment,
  savePendingPayment,
} from './pendingPayment'

let installed = false
let handling = false

function notificationPath(pending, extraQuery = {}) {
  const params = new URLSearchParams()
  if (pending.paymentId) params.set('paymentId', pending.paymentId)
  if (pending.requestId) params.set('requestId', pending.requestId)
  if (pending.company) params.set('company', pending.company)
  Object.entries(extraQuery).forEach(([key, value]) => {
    if (value != null && value !== '') params.set(key, String(value))
  })
  const qs = params.toString()
  return qs ? `/payment/notification?${qs}` : '/payment/notification'
}

function parseDeepLink(url) {
  try {
    const parsed = new URL(url)
    const path = `${parsed.pathname || ''}${parsed.hash || ''}`
    if (!/payment\/notification/i.test(path) && !/payment\/notification/i.test(url)) {
      return null
    }
    return {
      paymentId: parsed.searchParams.get('paymentId') || '',
      requestId: parsed.searchParams.get('requestId') || '',
      status: parsed.searchParams.get('status') || '',
    }
  } catch {
    return null
  }
}

export async function resumePendingPayment(router, { force = false } = {}) {
  if (!router || handling) return false

  const pending = readPendingPayment()
  if (!pending?.paymentId) return false

  const current = router.currentRoute?.value
  if (!force && current?.path?.includes('/payment/notification')) return false

  handling = true
  try {
    await router.replace(notificationPath(pending, { status: pending.status }))
    return true
  } catch {
    return false
  } finally {
    handling = false
  }
}

export async function installPaymentReturnHandler(router) {
  if (installed || !router) return
  installed = true

  if (!Capacitor.isNativePlatform()) return

  try {
    const { App } = await import('@capacitor/app')
    const { Browser } = await import('@capacitor/browser')

    await App.addListener('appUrlOpen', async ({ url }) => {
      const deep = parseDeepLink(url)
      if (deep?.paymentId) {
        const existing = readPendingPayment() || {}
        savePendingPayment({
          paymentId: deep.paymentId || existing.paymentId,
          requestId: deep.requestId || existing.requestId,
          purpose: existing.purpose || 'Refill',
        })
      }
      try {
        await Browser.close()
      } catch {
        // already closed
      }
      await resumePendingPayment(router, { force: true })
    })

    await App.addListener('appStateChange', async ({ isActive }) => {
      if (!isActive) return
      await resumePendingPayment(router)
    })

    await Browser.addListener('browserFinished', async () => {
      await resumePendingPayment(router, { force: true })
    })
  } catch {
    // plugins unavailable
  }
}

export { clearPendingPayment }
