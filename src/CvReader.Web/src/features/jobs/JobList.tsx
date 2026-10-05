import type { JobPosting } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Spinner } from '../../components/Spinner'
import { formatDate } from '../../utils/format'

interface JobListProps {
  jobs: JobPosting[]
  loading: boolean
  error: string | null
  selectedId: string | null
  onSelect: (job: JobPosting) => void
}

export function JobList({ jobs, loading, error, selectedId, onSelect }: JobListProps) {
  if (loading) {
    return (
      <div className="sidebar-state">
        <Spinner />
      </div>
    )
  }
  if (error) return <Alert tone="error">{error}</Alert>
  if (jobs.length === 0) return <p className="sidebar-state muted">Henüz ilan yok. İlk ilanınızı oluşturun.</p>

  return (
    <ul className="job-list">
      {jobs.map((job) => (
        <li key={job.id}>
          <button
            type="button"
            className={`job-item${job.id === selectedId ? ' is-selected' : ''}`}
            aria-current={job.id === selectedId}
            onClick={() => onSelect(job)}
          >
            <span className="job-title">{job.title}</span>
            <span className="job-meta">
              {job.keywords.length} anahtar kelime · {formatDate(job.createdAt)}
            </span>
          </button>
        </li>
      ))}
    </ul>
  )
}
