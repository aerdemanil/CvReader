import { useEffect, useState } from 'react'
import { cvApi } from '../../api/cvApi'
import { errorMessage } from '../../api/http'
import type { CvDetail, CvSummary } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Dialog } from '../../components/Dialog'
import { Spinner } from '../../components/Spinner'
import { formatDate } from '../../utils/format'

interface CvViewDialogProps {
  cv: Pick<CvSummary, 'id' | 'fileName'> | null
  onClose: () => void
}

// PDF'in kendisi saklanmaz; yüklenirken çıkarılan metin gösterilir.
export function CvViewDialog({ cv, onClose }: CvViewDialogProps) {
  return (
    <Dialog open={cv !== null} onClose={onClose} title={cv?.fileName ?? ''}>
      {cv && <CvText key={cv.id} id={cv.id} />}
    </Dialog>
  )
}

function CvText({ id }: { id: string }) {
  const [detail, setDetail] = useState<CvDetail | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    cvApi
      .get(id, controller.signal)
      .then(setDetail)
      .catch((err) => {
        if (!controller.signal.aborted) setError(errorMessage(err))
      })
    return () => controller.abort()
  }, [id])

  return (
    <div className="dialog-body">
      {error && <Alert tone="error">{error}</Alert>}
      {!detail && !error && <Spinner />}
      {detail && (
        <>
          <p className="muted">
            {detail.pageCount} sayfa · {formatDate(detail.createdAt)} tarihinde yüklendi
          </p>
          <pre className="cv-text">{detail.text}</pre>
        </>
      )}
    </div>
  )
}
