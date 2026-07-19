import api from './client'

export function getFinancialInfo() {
  return api.get('/api/Subscriber/financial-info')
}

export function getSubscriptionInfo() {
  return api.get('/api/Subscriber/subscription-info')
}

export function getPackages() {
  return api.get('/api/Subscriber/packages')
}

export function getAmountDue() {
  return api.get('/api/Subscriber/amount-due')
}

export function getInvoices() {
  return api.get('/api/Subscriber/invoices')
}

export function getReceivables() {
  return api.get('/api/Subscriber/receivables')
}

export function refill(payload) {
  return api.post('/api/Subscriber/refill', payload)
}

export function payback() {
  return api.post('/api/Subscriber/payback')
}

export function createPayment(payload) {
  return api.post('/api/Subscriber/payment', payload)
}

export function confirmPayment(payload) {
  let company = payload?.company || payload?.Company || ''
  // Qi may corrupt query values into "KGD?requestId=..."
  company = String(company).split('?')[0].split('&')[0].trim()

  let requestId = payload?.requestId || payload?.RequestId || ''
  if (!requestId && String(payload?.company || payload?.Company || '').includes('?requestId=')) {
    requestId = String(payload.company || payload.Company)
      .split('?requestId=')[1]
      ?.split('&')[0] || ''
  }

  const body = {
    ...payload,
    company: company || undefined,
    Company: company || undefined,
    requestId: requestId || payload?.requestId,
    RequestId: requestId || payload?.RequestId,
  }

  const headers = {}
  if (company) headers['X-Company'] = company
  return api.post('/api/Subscriber/payment/confirm', body, { headers })
}
