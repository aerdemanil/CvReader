import { useEffect, useId, useRef, type ReactNode } from 'react'
import { Icon } from './Icon'

interface DialogProps {
  open: boolean
  title: string
  description?: string
  // Süren bir işlem varken (ör. yükleme) diyalog Esc ya da kapat düğmesiyle kapatılamaz.
  busy?: boolean
  onClose: () => void
  children: ReactNode
}

// Yerleşik <dialog> öğesi: odak yönetimi, Esc ile kapanma ve arka planı kilitleme tarayıcıdan gelir.
export function Dialog({ open, title, description, busy = false, onClose, children }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={ref}
      className="dialog"
      onClose={onClose}
      onCancel={(event) => {
        if (busy) event.preventDefault()
      }}
      aria-labelledby={titleId}
    >
      <header className="dialog-header">
        <div>
          <h2 id={titleId}>{title}</h2>
          {description && <p className="muted">{description}</p>}
        </div>
        <button type="button" className="icon-button" onClick={onClose} disabled={busy} aria-label="Kapat">
          <Icon name="close" />
        </button>
      </header>
      {open && children}
    </dialog>
  )
}
