import { useEffect, useState } from 'react'
import { errorMessage } from '../../api/http'
import { jobsApi } from '../../api/jobsApi'
import type { JobPosting, MatchDetail, MatchResult } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Dialog } from '../../components/Dialog'
import { Spinner } from '../../components/Spinner'
import { formatScore } from '../../utils/format'

interface MatchDetailDialogProps {
  job: JobPosting
  result: MatchResult | null
  onClose: () => void
}

// Skorun nereden geldiğini gösterir: her anahtar kelime için CV'de ona en yakın bulunan kelimeler.
export function MatchDetailDialog({ job, result, onClose }: MatchDetailDialogProps) {
  return (
    <Dialog
      open={result !== null}
      onClose={onClose}
      title="Eşleşen kelimeler"
      description={result ? `${result.fullName ?? result.fileName} · skor ${formatScore(result.score)}` : undefined}
    >
      {result && <KeywordMatches key={result.profileId} jobId={job.id} profileId={result.profileId} />}
    </Dialog>
  )
}

function KeywordMatches({ jobId, profileId }: { jobId: string; profileId: string }) {
  const [detail, setDetail] = useState<MatchDetail | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    jobsApi
      .getMatchDetail(jobId, profileId, controller.signal)
      .then(setDetail)
      .catch((err) => {
        if (!controller.signal.aborted) setError(errorMessage(err))
      })
    return () => controller.abort()
  }, [jobId, profileId])

  return (
    <div className="dialog-body">
      {error && <Alert tone="error">{error}</Alert>}
      {!detail && !error && <Spinner />}
      {detail && (
        <>
          <ul className="match-keywords">
            {detail.keywords.map((k) => (
              <li key={k.keyword}>
                <div className="match-keyword">
                  <strong>{k.keyword}</strong>
                  <span className="chart-value">{formatScore(k.score)}</span>
                </div>
                {k.terms.length > 0 ? (
                  <div className="chips">
                    {k.terms.map((t) => (
                      <span key={t.term} className="chip">
                        {t.term} · {formatScore(t.score)}
                      </span>
                    ))}
                  </div>
                ) : (
                  <p className="muted">CV’de bu kelimeye yakın bir kelime bulunamadı.</p>
                )}
              </li>
            ))}
          </ul>
          <p className="muted match-note">
            Her anahtar kelime, CV’de ona anlamca en yakın kelimeye göre 0–100 arası puanlanır; CV’nin skoru bu puanların
            ortalamasıdır.
          </p>
        </>
      )}
    </div>
  )
}
