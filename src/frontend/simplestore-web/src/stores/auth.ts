import { defineStore } from 'pinia'
import { apiRequest, resetAntiforgeryToken } from '../api/client'
import type { Session } from '../api/types'

const anonymous: Session = {
  isAuthenticated: false,
  email: null,
  storeId: null,
  roles: [],
  hasStore: false,
  mustChangePassword: false,
  isEnabled: false,
}

export const useAuthStore = defineStore('auth', {
  state: () => ({ session: { ...anonymous }, initialized: false }),
  actions: {
    async loadSession(force = false) {
      if (this.initialized && !force) return this.session
      this.session = await apiRequest<Session>('/api/auth/session')
      this.initialized = true
      return this.session
    },
    async login(email: string, password: string) {
      this.session = await apiRequest<Session>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      })
      resetAntiforgeryToken()
      this.initialized = true
    },
    async logout() {
      await apiRequest<void>('/api/auth/logout', { method: 'POST' })
      resetAntiforgeryToken()
      this.session = { ...anonymous }
      this.initialized = true
    },
    async changePassword(currentPassword: string, newPassword: string) {
      this.session = await apiRequest<Session>('/api/auth/change-password', {
        method: 'POST',
        body: JSON.stringify({ currentPassword, newPassword }),
      })
      this.initialized = true
    },
  },
})
