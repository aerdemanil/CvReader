import { useState } from 'react'
import type { JobPosting } from '../api/types'
import { Icon } from '../components/Icon'
import { Logo } from '../components/Logo'
import { useAuth } from '../features/auth/AuthContext'
import { CvLibrary } from '../features/cvs/CvLibrary'
import { CvUploadDialog } from '../features/cvs/CvUploadDialog'
import { JobFormDialog } from '../features/jobs/JobFormDialog'
import { JobList } from '../features/jobs/JobList'
import { useJobs } from '../features/jobs/useJobs'
import { MatchPanel } from '../features/matching/MatchPanel'

type OpenDialog = 'upload' | 'job' | null
type View = 'jobs' | 'cvs'

export function DashboardPage() {
  const { session, logout } = useAuth()
  const { jobs, loading, error, create, remove } = useJobs()
  const [view, setView] = useState<View>('jobs')
  const [selectedJob, setSelectedJob] = useState<JobPosting | null>(null)
  const [uploadCount, setUploadCount] = useState(0)
  const [openDialog, setOpenDialog] = useState<OpenDialog>(null)

  function handleJobCreated(job: JobPosting) {
    setOpenDialog(null)
    setSelectedJob(job)
  }

  // Yeni CV'ler açık ilanın sonuçlarına ve CV listesine hemen yansısın.
  function handleUploaded(savedCount: number) {
    if (savedCount > 0) setUploadCount((count) => count + 1)
  }

  async function deleteJob(job: JobPosting) {
    await remove(job.id)
    setSelectedJob(null)
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <Logo />
        <nav className="topbar-nav" aria-label="Bölümler">
          <button type="button" aria-current={view === 'jobs'} onClick={() => setView('jobs')}>
            İlanlar
          </button>
          <button type="button" aria-current={view === 'cvs'} onClick={() => setView('cvs')}>
            CV’ler
          </button>
        </nav>
        <div className="topbar-actions">
          <button type="button" className="button button-primary" onClick={() => setOpenDialog('upload')}>
            <Icon name="upload" />
            CV yükle
          </button>
          <span className="user-email" title={session?.email}>
            {session?.email}
          </span>
          <button type="button" className="icon-button" onClick={logout} aria-label="Çıkış yap" title="Çıkış yap">
            <Icon name="logout" />
          </button>
        </div>
      </header>

      {view === 'cvs' ? (
        <CvLibrary refreshKey={uploadCount} />
      ) : (
        <div className="workspace">
          <aside className="sidebar">
            <div className="sidebar-header">
              <h2>İlanlar</h2>
              <button type="button" className="button button-small" onClick={() => setOpenDialog('job')}>
                <Icon name="plus" size={16} />
                Yeni ilan
              </button>
            </div>
            <JobList jobs={jobs} loading={loading} error={error} selectedId={selectedJob?.id ?? null} onSelect={setSelectedJob} />
          </aside>

          <main className="content">
            {selectedJob ? (
              <MatchPanel
                key={selectedJob.id}
                job={selectedJob}
                refreshKey={uploadCount}
                onDeleteJob={() => deleteJob(selectedJob)}
              />
            ) : (
              <div className="card empty-state welcome">
                <span className="empty-icon">
                  <Icon name="briefcase" size={24} />
                </span>
                <h2>Bir ilan seçin ya da oluşturun</h2>
                <p className="muted">CV’leri yükleyin, ilanınızın anahtar kelimelerini girin; CvReader adayları ilana yakınlığına göre sıralasın.</p>
                <div className="welcome-actions">
                  <button type="button" className="button" onClick={() => setOpenDialog('upload')}>
                    <Icon name="upload" />
                    CV yükle
                  </button>
                  <button type="button" className="button button-primary" onClick={() => setOpenDialog('job')}>
                    <Icon name="plus" />
                    Yeni ilan
                  </button>
                </div>
              </div>
            )}
          </main>
        </div>
      )}

      <CvUploadDialog open={openDialog === 'upload'} onClose={() => setOpenDialog(null)} onUploaded={handleUploaded} />
      <JobFormDialog
        open={openDialog === 'job'}
        onClose={() => setOpenDialog(null)}
        onSubmit={create}
        onCreated={handleJobCreated}
      />
    </div>
  )
}
