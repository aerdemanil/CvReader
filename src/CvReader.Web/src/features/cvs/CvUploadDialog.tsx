import { useEffect, useState, type ChangeEvent, type DragEvent } from 'react'
import { cvApi } from '../../api/cvApi'
import { foldersApi } from '../../api/foldersApi'
import { errorMessage } from '../../api/http'
import type { CvUploadResult, Folder } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Dialog } from '../../components/Dialog'
import { Icon } from '../../components/Icon'
import { Spinner } from '../../components/Spinner'
import { checkSelection, formatSize, MAX_TOTAL_BYTES } from './fileRules'

interface CvUploadDialogProps {
  open: boolean
  onClose: () => void
  onUploaded: (savedCount: number) => void
}

export function CvUploadDialog({ open, onClose, onUploaded }: CvUploadDialogProps) {
  // Diyaloğun yükleme sırasında kapanmasını engelleyebilmesi için durum burada tutulur.
  const [uploading, setUploading] = useState(false)

  return (
    <Dialog
      open={open}
      busy={uploading}
      onClose={onClose}
      title="CV yükle"
      description="PDF formatındaki CV’leri seçin ya da sürükleyip bırakın."
    >
      <UploadForm onClose={onClose} onUploaded={onUploaded} uploading={uploading} setUploading={setUploading} />
    </Dialog>
  )
}

interface UploadFormProps extends Omit<CvUploadDialogProps, 'open'> {
  uploading: boolean
  setUploading: (uploading: boolean) => void
}

function UploadForm({ onClose, onUploaded, uploading, setUploading }: UploadFormProps) {
  const [files, setFiles] = useState<File[]>([])
  const [rejected, setRejected] = useState<{ file: File; reason: string }[]>([])
  const [dragging, setDragging] = useState(false)
  const [results, setResults] = useState<CvUploadResult[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [folders, setFolders] = useState<Folder[]>([])
  const [folderId, setFolderId] = useState('')

  // Klasör seçimi isteğe bağlıdır; liste okunamazsa yükleme klasörsüz yapılabilir.
  useEffect(() => {
    const controller = new AbortController()
    foldersApi
      .list(controller.signal)
      .then((list) => setFolders(list.folders))
      .catch(() => {})
    return () => controller.abort()
  }, [])

  const totalBytes = files.reduce((sum, f) => sum + f.size, 0)
  const tooLarge = totalBytes > MAX_TOTAL_BYTES

  function addFiles(list: FileList | null) {
    if (!list) return
    const { accepted, rejected: invalid } = checkSelection([...list])
    setFiles((current) => {
      const known = new Set(current.map((f) => `${f.name}:${f.size}`))
      return [...current, ...accepted.filter((f) => !known.has(`${f.name}:${f.size}`))]
    })
    setRejected(invalid)
  }

  function handleDrop(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
    setDragging(false)
    addFiles(event.dataTransfer.files)
  }

  function handleInput(event: ChangeEvent<HTMLInputElement>) {
    addFiles(event.target.files)
    event.target.value = ''
  }

  async function upload() {
    setUploading(true)
    setError(null)
    try {
      const uploaded = await cvApi.upload(files, folderId || null)
      setResults(uploaded)
      onUploaded(uploaded.filter((r) => !r.error).length)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setUploading(false)
    }
  }

  if (results) {
    const saved = results.filter((r) => !r.error).length
    return (
      <>
        <div className="dialog-body">
          <p className="upload-summary">
            <strong>{saved}</strong> / {results.length} CV kaydedildi.
          </p>
          <ul className="file-list">
            {results.map((r, index) => (
              // Aynı adlı iki dosya olabilir; sonuçlar yüklenen dosyalarla aynı sırada gelir ve liste değişmez.
              <li key={index} className={r.error ? 'is-error' : 'is-success'}>
                <Icon name={r.error ? 'close' : 'check'} />
                <span className="file-name">{r.fileName}</span>
                {r.error && <span className="file-note">{r.error}</span>}
              </li>
            ))}
          </ul>
        </div>
        <footer className="dialog-footer">
          <button type="button" className="button button-primary" onClick={onClose}>
            Tamam
          </button>
        </footer>
      </>
    )
  }

  return (
    <>
      <div className="dialog-body">
        <label
          className={`dropzone${dragging ? ' is-dragging' : ''}`}
          onDragOver={(e) => {
            e.preventDefault()
            setDragging(true)
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={handleDrop}
        >
          <input type="file" accept="application/pdf,.pdf" multiple onChange={handleInput} disabled={uploading} />
          <span className="dropzone-icon">
            <Icon name="upload" size={22} />
          </span>
          <strong>Dosyaları buraya bırakın</strong>
          <span className="muted">ya da seçmek için tıklayın · PDF, dosya başına en fazla 10 MB</span>
        </label>

        {rejected.length > 0 && (
          <Alert tone="error">
            {rejected.map((r, index) => (
              <div key={index}>
                <strong>{r.file.name}</strong>: {r.reason}
              </div>
            ))}
          </Alert>
        )}

        {files.length > 0 && (
          <ul className="file-list">
            {files.map((f) => (
              <li key={`${f.name}:${f.size}`}>
                <Icon name="file" />
                <span className="file-name">{f.name}</span>
                <span className="file-note">{formatSize(f.size)}</span>
                <button
                  type="button"
                  className="icon-button"
                  aria-label={`${f.name} dosyasını kaldır`}
                  onClick={() => setFiles((current) => current.filter((x) => x !== f))}
                  disabled={uploading}
                >
                  <Icon name="close" size={16} />
                </button>
              </li>
            ))}
          </ul>
        )}

        {folders.length > 0 && (
          <label className="field">
            <span>Klasör</span>
            <select className="input" value={folderId} onChange={(e) => setFolderId(e.target.value)} disabled={uploading}>
              <option value="">Klasörsüz</option>
              {folders.map((f) => (
                <option key={f.id} value={f.id}>
                  {f.name}
                </option>
              ))}
            </select>
          </label>
        )}

        {tooLarge && <Alert tone="error">Toplam boyut {formatSize(MAX_TOTAL_BYTES)} sınırını aşıyor. Dosyaları birkaç seferde yükleyin.</Alert>}
        {uploading && <Alert tone="info">CV’ler işleniyor. İlk yüklemede model hazırlanırken 30 saniye kadar sürebilir.</Alert>}
        {error && <Alert tone="error">{error}</Alert>}
      </div>

      <footer className="dialog-footer">
        <button type="button" className="button" onClick={onClose} disabled={uploading}>
          Vazgeç
        </button>
        <button
          type="button"
          className="button button-primary"
          onClick={upload}
          disabled={files.length === 0 || tooLarge || uploading}
        >
          {uploading && <Spinner />}
          {files.length > 0 ? `${files.length} CV yükle` : 'Yükle'}
        </button>
      </footer>
    </>
  )
}
