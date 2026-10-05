import { request } from './http'
import type { Session } from './types'

export const authApi = {
  me: () => request<Session>('/auth/me'),

  login: (email: string, password: string) =>
    request<Session>('/auth/login', { method: 'POST', json: { email, password } }),

  register: (email: string, password: string) =>
    request<Session>('/auth/register', { method: 'POST', json: { email, password } }),

  logout: () => request<void>('/auth/logout', { method: 'POST' }),
}
