import { Component, type ReactNode } from 'react'

interface ErrorBoundaryState {
  failed: boolean
}

// Bir bileşen render sırasında hata verirse boş sayfa yerine yeniden yükleme seçeneği gösterir.
export class ErrorBoundary extends Component<{ children: ReactNode }, ErrorBoundaryState> {
  state: ErrorBoundaryState = { failed: false }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { failed: true }
  }

  render() {
    if (!this.state.failed) return this.props.children

    return (
      <div className="full-page-center">
        <div className="card empty-state">
          <h2>Bir şeyler ters gitti</h2>
          <p className="muted">Sayfa beklenmeyen bir hatayla karşılaştı.</p>
          <button type="button" className="button button-primary" onClick={() => window.location.reload()}>
            Sayfayı yenile
          </button>
        </div>
      </div>
    )
  }
}
