import { useState } from 'react'
import { errorMessage } from '../../api/http'
import type { JobPosting, MatchResult } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Icon } from '../../components/Icon'
import { Spinner } from '../../components/Spinner'
import { CvViewDialog } from '../cvs/CvViewDialog'
import { formatDate, formatScore } from '../../utils/format'
import { MatchDetailDialog } from './MatchDetailDialog'
import { SimilarityChart } from './SimilarityChart'
import { useMatchResults } from './useMatchResults'

interface MatchPanelProps {
  job: JobPosting
  refreshKey: number
  onDeleteJob: () => Promise<void>
}

export function MatchPanel({ job, refreshKey, onDeleteJob }: MatchPanelProps) {
  const { results, total, hasMore, loadingMore, error, loadMore, removeCv } = useMatchResults(job.id, refreshKey)
  const [deletingJob, setDeletingJob] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [previewing, setPreviewing] = useState<MatchResult | null>(null)
  const [explaining, setExplaining] = useState<MatchResult | null>(null)

  async function handleDeleteJob() {
    if (!window.confirm(`“${job.title}” ilanı silinsin mi? Yüklediğiniz CV’ler silinmez.`)) return

    setDeletingJob(true)
    setDeleteError(null)
    try {
      await onDeleteJob()
    } catch (err) {
      setDeleteError(errorMessage(err))
      setDeletingJob(false)
    }
  }

  function handleDeleteCv(result: MatchResult) {
    if (window.confirm(`“${result.fileName}” kalıcı olarak silinsin mi? CV tüm ilanların sonuçlarından kalkar.`)) {
      void removeCv(result.profileId)
    }
  }

  return (
    <div className="panel">
      <header className="panel-header">
        <div>
          <h1>{job.title}</h1>
          <p className="muted">
            {formatDate(job.createdAt)} · {job.keywords.length} anahtar kelime
          </p>
          <div className="chips">
            {job.keywords.map((k) => (
              <span key={k} className="chip">
                {k}
              </span>
            ))}
          </div>
        </div>
        <div className="panel-actions">
          <button type="button" className="button" onClick={handleDeleteJob} disabled={deletingJob}>
            {deletingJob ? <Spinner /> : <Icon name="trash" />}
            İlanı sil
          </button>
        </div>
      </header>

      {(error ?? deleteError) && <Alert tone="error">{error ?? deleteError}</Alert>}

      <PanelBody
        results={results}
        total={total}
        hasMore={hasMore}
        loadingMore={loadingMore}
        onLoadMore={loadMore}
        onPreviewCv={setPreviewing}
        onExplainCv={setExplaining}
        onDeleteCv={handleDeleteCv}
      />

      <CvViewDialog
        cv={previewing && { id: previewing.profileId, fileName: previewing.fileName }}
        onClose={() => setPreviewing(null)}
      />
      <MatchDetailDialog job={job} result={explaining} onClose={() => setExplaining(null)} />
    </div>
  )
}

interface PanelBodyProps {
  results: MatchResult[] | null
  total: number
  hasMore: boolean
  loadingMore: boolean
  onLoadMore: () => void
  onPreviewCv: (result: MatchResult) => void
  onExplainCv: (result: MatchResult) => void
  onDeleteCv: (result: MatchResult) => void
}

function PanelBody({ results, total, hasMore, loadingMore, onLoadMore, onPreviewCv, onExplainCv, onDeleteCv }: PanelBodyProps) {
  if (results === null) {
    return (
      <div className="card empty-state">
        <Spinner />
        <p className="muted">CV’ler ilana göre puanlanıyor…</p>
      </div>
    )
  }

  if (results.length === 0) {
    return (
      <div className="card empty-state">
        <span className="empty-icon">
          <Icon name="sparkles" size={24} />
        </span>
        <h2>Henüz sonuç yok</h2>
        <p className="muted">CV yüklediğinizde adaylar bu ilana yakınlığına göre burada sıralanır.</p>
      </div>
    )
  }

  return (
    <>
      <div className="stats">
        <Stat label="Değerlendirilen CV" value={String(total)} />
        <Stat label="En yüksek skor" value={formatScore(results[0].score)} />
      </div>
      <section className="card">
        <h2 className="card-title">Yakınlık grafiği</h2>
        <SimilarityChart results={results} onPreview={onPreviewCv} onExplain={onExplainCv} onDelete={onDeleteCv} />
        {hasMore && (
          <div className="chart-more">
            <button type="button" className="button" onClick={onLoadMore} disabled={loadingMore}>
              {loadingMore && <Spinner />}
              Daha fazla göster ({results.length} / {total})
            </button>
          </div>
        )}
      </section>
    </>
  )
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="card stat">
      <span className="muted">{label}</span>
      <strong>{value}</strong>
    </div>
  )
}
