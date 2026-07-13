import api from './client'

export function login(payload) {
  return api.post('/api/Auth/login', payload)
}
