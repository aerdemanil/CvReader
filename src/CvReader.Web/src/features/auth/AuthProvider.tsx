import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi } from '../../api/authApi'
import { setUnauthorizedListener } from '../../api/http'
import type { Session } from '../../api/types'
import { AuthContext, type AuthContextValue } from './AuthContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    setUnauthorizedListener(() => setSession(null))

    // Sayfa yenilendiğinde oturum cookie'si hâlâ geçerliyse kullanıcıyı geri getirir.
    authApi
      .me()
      .then(setSession)
      .catch(() => setSession(null))
      .finally(() => setLoading(false))

    return () => setUnauthorizedListener(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      loading,
      login: async (email, password) => setSession(await authApi.login(email, password)),
      register: async (email, password) => setSession(await authApi.register(email, password)),
      logout: async () => {
        try {
          await authApi.logout()
        } catch {
          // Oturum zaten sona ermişse ya da sunucuya ulaşılamıyorsa da kullanıcı giriş ekranına döner.
        } finally {
          setSession(null)
        }
      },
    }),
    [session, loading],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
