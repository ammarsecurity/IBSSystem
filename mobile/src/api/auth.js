import api from './client'

export function login(payload) {
  return api.post('/api/Auth/login', payload)
}

export function getCompanies() {
  return api.get('/api/Auth/companies')
}
