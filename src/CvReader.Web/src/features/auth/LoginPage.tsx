import { useState, type FormEvent } from 'react'
import { ApiError, errorMessage } from '../../api/http'
import { Alert } from '../../components/Alert'
import { Icon, type IconName } from '../../components/Icon'
import { Logo } from '../../components/Logo'
import { Spinner } from '../../components/Spinner'
import { useAuth } from './AuthContext'

type Mode = 'login' | 'register'

const features: { icon: IconName; title: string; text: string }[] = [
  { icon: 'layers', title: 'Toplu yükleme', text: 'Onlarca PDF CV’yi tek seferde sisteme ekleyin.' },
  { icon: 'sparkles', title: 'Anlamsal eşleşme', text: 'Kelime aynı olmasa da anlamca yakın adayları bulur.' },
  { icon: 'shield', title: 'Veriler sizde kalır', text: 'Yapay zekâ modeli yerelde çalışır, CV’ler dışarı gönderilmez.' },
]

function authErrorMessage(error: unknown, mode: Mode): string {
  if (error instanceof ApiError) {
    if (mode === 'login' && error.status === 401) return 'E-posta veya şifre hatalı.'
    if (mode === 'register' && error.status === 409) return 'Bu e-posta adresiyle kayıtlı bir hesap zaten var.'
  }
  return errorMessage(error)
}

export function LoginPage() {
  const { login, register } = useAuth()
  const [mode, setMode] = useState<Mode>('login')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const email = String(form.get('email'))
    const password = String(form.get('password'))

    if (mode === 'register' && password !== form.get('passwordConfirm')) {
      setError('Şifreler eşleşmiyor.')
      return
    }

    setError(null)
    setSubmitting(true)
    try {
      await (mode === 'login' ? login(email, password) : register(email, password))
    } catch (err) {
      setError(authErrorMessage(err, mode))
      setSubmitting(false)
    }
  }

  function switchMode(next: Mode) {
    setMode(next)
    setError(null)
  }

  return (
    <div className="auth-page">
      <section className="auth-hero">
        <Logo />
        <div className="auth-hero-body">
          <h1>Doğru adayı saniyeler içinde bulun.</h1>
          <p>CvReader, yüklediğiniz CV’leri iş ilanınıza anlamca ne kadar yakın olduklarına göre sıralar.</p>
          <ul className="feature-list">
            {features.map((f) => (
              <li key={f.title}>
                <span className="feature-icon">
                  <Icon name={f.icon} />
                </span>
                <div>
                  <strong>{f.title}</strong>
                  <span>{f.text}</span>
                </div>
              </li>
            ))}
          </ul>
        </div>
      </section>

      <section className="auth-panel">
        <div className="auth-card">
          <h2>{mode === 'login' ? 'Tekrar hoş geldiniz' : 'Hesap oluşturun'}</h2>
          <p className="muted">
            {mode === 'login' ? 'Devam etmek için giriş yapın.' : 'Birkaç saniyede hesabınızı açın.'}
          </p>

          <div className="segmented" role="tablist">
            <button type="button" role="tab" aria-selected={mode === 'login'} onClick={() => switchMode('login')}>
              Giriş yap
            </button>
            <button type="button" role="tab" aria-selected={mode === 'register'} onClick={() => switchMode('register')}>
              Kayıt ol
            </button>
          </div>

          <form className="form" onSubmit={handleSubmit} key={mode}>
            <label className="field">
              <span>E-posta</span>
              <input name="email" type="email" required maxLength={256} autoComplete="email" autoFocus />
            </label>
            <label className="field">
              <span>Şifre</span>
              <input
                name="password"
                type="password"
                required
                minLength={mode === 'register' ? 8 : undefined}
                maxLength={72}
                autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
              />
              {mode === 'register' && <small className="muted">En az 8 karakter.</small>}
            </label>
            {mode === 'register' && (
              <label className="field">
                <span>Şifre (tekrar)</span>
                <input name="passwordConfirm" type="password" required maxLength={72} autoComplete="new-password" />
              </label>
            )}

            {error && <Alert tone="error">{error}</Alert>}

            <button type="submit" className="button button-primary button-block" disabled={submitting}>
              {submitting && <Spinner />}
              {mode === 'login' ? 'Giriş yap' : 'Hesap oluştur'}
            </button>
          </form>
        </div>
      </section>
    </div>
  )
}
