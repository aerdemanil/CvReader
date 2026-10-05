import { createContext, use } from 'react'
import type { Session } from '../../api/types'

export interface AuthContextValue {
  session: Session | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const context = use(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider.')
  return context
}
