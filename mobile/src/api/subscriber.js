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
  return api.post('/api/Subscriber/payment/confirm', payload)
}
