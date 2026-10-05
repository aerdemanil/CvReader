import { FullPageSpinner } from './components/Spinner'
import { useAuth } from './features/auth/AuthContext'
import { AuthProvider } from './features/auth/AuthProvider'
import { LoginPage } from './features/auth/LoginPage'
import { DashboardPage } from './pages/DashboardPage'

export default function App() {
  return (
    <AuthProvider>
      <AuthGate />
    </AuthProvider>
  )
}

function AuthGate() {
  const { session, loading } = useAuth()
  if (loading) return <FullPageSpinner />
  return session ? <DashboardPage /> : <LoginPage />
}
