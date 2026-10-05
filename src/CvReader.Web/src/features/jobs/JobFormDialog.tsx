import { useState, type FormEvent } from 'react'
import { errorMessage } from '../../api/http'
import type { CreateJobRequest, JobPosting } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Dialog } from '../../components/Dialog'
import { Spinner } from '../../components/Spinner'
import { KeywordInput, MAX_KEYWORDS } from './KeywordInput'

interface JobFormDialogProps {
  open: boolean
  onClose: () => void
  onSubmit: (request: CreateJobRequest) => Promise<JobPosting>
  onCreated: (job: JobPosting) => void
}

export function JobFormDialog({ open, onClose, onSubmit, onCreated }: JobFormDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Yeni ilan"
      description="Aradığınız pozisyonu ve önemli becerileri girin; CV’ler bunlara yakınlığına göre sıralanır."
    >
      <JobForm onClose={onClose} onSubmit={onSubmit} onCreated={onCreated} />
    </Dialog>
  )
}

function JobForm({ onClose, onSubmit, onCreated }: Omit<JobFormDialogProps, 'open'>) {
  const [title, setTitle] = useState('')
  const [keywords, setKeywords] = useState<string[]>([])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (keywords.length === 0) {
      setError('En az bir anahtar kelime ekleyin.')
      return
    }

    setSubmitting(true)
    setError(null)
    try {
      onCreated(await onSubmit({ title: title.trim(), keywords }))
    } catch (err) {
      setError(errorMessage(err))
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div className="dialog-body form">
        <label className="field">
          <span>İlan başlığı</span>
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            maxLength={200}
            placeholder="ör. Kıdemli Backend Geliştirici"
            autoFocus
          />
        </label>
        <div className="field">
          <span>Anahtar kelimeler</span>
          <KeywordInput value={keywords} onChange={setKeywords} />
          <small className="muted">
            Enter veya virgül ile ekleyin · {keywords.length}/{MAX_KEYWORDS}
          </small>
        </div>
        {error && <Alert tone="error">{error}</Alert>}
      </div>
      <footer className="dialog-footer">
        <button type="button" className="button" onClick={onClose} disabled={submitting}>
          Vazgeç
        </button>
        <button type="submit" className="button button-primary" disabled={submitting}>
          {submitting && <Spinner />}
          Oluştur
        </button>
      </footer>
    </form>
  )
}
