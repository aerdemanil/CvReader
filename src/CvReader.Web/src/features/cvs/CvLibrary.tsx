import { useState, type FormEvent } from 'react'
import { ApiError, errorMessage } from '../../api/http'
import type { CvFilter, CvSummary, Folder } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Icon } from '../../components/Icon'
import { Spinner } from '../../components/Spinner'
import { formatDate } from '../../utils/format'
import { CvViewDialog } from './CvViewDialog'
import { useCvLibrary } from './useCvLibrary'

const UNFILED = 'unfiled'

function sameFilter(a: CvFilter, b: CvFilter): boolean {
  return a.kind === b.kind && (a.kind !== 'folder' || (b.kind === 'folder' && a.id === b.id))
}

export function CvLibrary({ refreshKey }: { refreshKey: number }) {
  const library = useCvLibrary(refreshKey)
  const { folderList, filter, cvs } = library
  const [selectedIds, setSelectedIds] = useState<ReadonlySet<string>>(new Set())
  const [viewing, setViewing] = useState<CvSummary | null>(null)

  const folders = folderList?.folders ?? []
  const folderNames = new Map(folders.map((f) => [f.id, f.name]))
  const currentFolder = filter.kind === 'folder' ? folders.find((f) => f.id === filter.id) : undefined
  const allCount = folders.reduce((sum, f) => sum + f.cvCount, 0) + (folderList?.unfiledCount ?? 0)

  // Listeden çıkmış (silinmiş, taşınmış, filtrelenmiş) CV'ler seçili sayılmaz.
  const selected = (cvs ?? []).filter((cv) => selectedIds.has(cv.id)).map((cv) => cv.id)
  const allSelected = cvs !== null && cvs.length > 0 && selected.length === cvs.length

  function toggle(id: string) {
    const next = new Set(selectedIds)
    if (!next.delete(id)) next.add(id)
    setSelectedIds(next)
  }

  function toggleAll() {
    setSelectedIds(allSelected ? new Set() : new Set(cvs?.map((cv) => cv.id)))
  }

  function selectFilter(next: CvFilter) {
    library.setFilter(next)
    setSelectedIds(new Set())
  }

  function handleMove(target: string) {
    void library.moveCvs(selected, target === UNFILED ? null : target)
    setSelectedIds(new Set())
  }

  function handleDelete(ids: string[], label: string) {
    if (window.confirm(`${label} kalıcı olarak silinsin mi? Tüm ilanların sonuçlarından da kalkar.`)) {
      void library.deleteCvs(ids)
    }
  }

  function handleDeleteFolder(folder: Folder) {
    if (window.confirm(`“${folder.name}” klasörü silinsin mi? İçindeki CV’ler silinmez, klasörsüz kalır.`)) {
      void library.deleteFolder(folder.id)
    }
  }

  const title = filter.kind === 'all' ? 'Tüm CV’ler' : filter.kind === 'unfiled' ? 'Klasörsüz' : (currentFolder?.name ?? 'Klasör')

  return (
    <div className="workspace">
      <aside className="sidebar">
        <div className="sidebar-header">
          <h2>Klasörler</h2>
        </div>
        {folderList === null ? (
          <div className="sidebar-state">
            <Spinner />
          </div>
        ) : (
          <ul className="job-list">
            <FolderItem label="Tüm CV’ler" count={allCount} selected={filter.kind === 'all'} onSelect={() => selectFilter({ kind: 'all' })} />
            <FolderItem
              label="Klasörsüz"
              count={folderList.unfiledCount}
              selected={filter.kind === 'unfiled'}
              onSelect={() => selectFilter({ kind: 'unfiled' })}
            />
            {folders.map((folder) => {
              const folderFilter: CvFilter = { kind: 'folder', id: folder.id }
              return (
                <FolderItem
                  key={folder.id}
                  label={folder.name}
                  count={folder.cvCount}
                  selected={sameFilter(filter, folderFilter)}
                  onSelect={() => selectFilter(folderFilter)}
                />
              )
            })}
          </ul>
        )}
        <NewFolderForm onCreate={library.createFolder} />
      </aside>

      <main className="content">
        <div className="panel">
          <header className="panel-header">
            <div>
              <h1>{title}</h1>
              <p className="muted">{cvs === null ? 'Yükleniyor…' : `${library.total} CV`}</p>
            </div>
            <div className="panel-actions">
              <input
                className="input"
                type="search"
                value={library.search}
                onChange={(e) => library.setSearch(e.target.value)}
                maxLength={100}
                placeholder="İsim, e-posta ya da dosya adı ara"
                aria-label="İsim, e-posta ya da dosya adı ara"
              />
              {currentFolder && (
                <button type="button" className="button" onClick={() => handleDeleteFolder(currentFolder)}>
                  <Icon name="trash" />
                  Klasörü sil
                </button>
              )}
            </div>
          </header>

          {library.error && <Alert tone="error">{library.error}</Alert>}

          {cvs === null ? (
            <div className="card empty-state">
              <Spinner />
            </div>
          ) : cvs.length === 0 ? (
            <div className="card empty-state">
              <span className="empty-icon">
                <Icon name="file" size={24} />
              </span>
              <h2>{library.search ? 'Aramayla eşleşen CV yok' : 'Burada henüz CV yok'}</h2>
              <p className="muted">
                {library.search ? 'Farklı bir isim, e-posta ya da dosya adı deneyin.' : 'Üstteki “CV yükle” düğmesiyle PDF ekleyebilir, yüklerken klasör seçebilirsiniz.'}
              </p>
            </div>
          ) : (
            <section className="card">
              <div className="cv-toolbar">
                <label className="cv-check">
                  <input type="checkbox" checked={allSelected} onChange={toggleAll} />
                  {selected.length > 0 ? `${selected.length} seçili` : 'Tümünü seç'}
                </label>
                {selected.length > 0 && (
                  <>
                    <select className="input" value="" onChange={(e) => handleMove(e.target.value)} aria-label="Seçili CV’leri klasöre taşı">
                      <option value="" disabled>
                        Klasöre taşı…
                      </option>
                      <option value={UNFILED}>Klasörsüz</option>
                      {folders.map((f) => (
                        <option key={f.id} value={f.id}>
                          {f.name}
                        </option>
                      ))}
                    </select>
                    <button type="button" className="button" onClick={() => handleDelete(selected, `Seçili ${selected.length} CV`)}>
                      <Icon name="trash" />
                      Sil
                    </button>
                  </>
                )}
              </div>

              <ul className="cv-rows">
                {cvs.map((cv) => {
                  // Aday adı henüz çıkarılmadıysa ya da bulunamadıysa dosya adı gösterilir.
                  const name = cv.fullName ?? cv.fileName
                  return (
                    <li key={cv.id} className="cv-row">
                      <input
                        type="checkbox"
                        checked={selectedIds.has(cv.id)}
                        onChange={() => toggle(cv.id)}
                        aria-label={`${name} CV’sini seç`}
                      />
                      <button type="button" className="link-button" onClick={() => setViewing(cv)} title="CV metnini görüntüle">
                        {name}
                      </button>
                      <span className="file-note cv-contact" title={cv.phone ?? undefined}>
                        {cv.email ?? cv.phone}
                      </span>
                      <span className="cv-folder">{cv.folderId && <span className="chip">{folderNames.get(cv.folderId)}</span>}</span>
                      <span className="file-note">{cv.pageCount} sayfa</span>
                      <span className="file-note">{formatDate(cv.createdAt)}</span>
                      <button
                        type="button"
                        className="icon-button icon-button-danger"
                        aria-label={`${name} CV’sini sil`}
                        title="CV’yi sil"
                        onClick={() => handleDelete([cv.id], `“${name}”`)}
                      >
                        <Icon name="trash" size={16} />
                      </button>
                    </li>
                  )
                })}
              </ul>

              {library.hasMore && (
                <div className="chart-more">
                  <button type="button" className="button" onClick={library.loadMore} disabled={library.loadingMore}>
                    {library.loadingMore && <Spinner />}
                    Daha fazla göster ({cvs.length} / {library.total})
                  </button>
                </div>
              )}
            </section>
          )}
        </div>
      </main>

      <CvViewDialog cv={viewing} onClose={() => setViewing(null)} />
    </div>
  )
}

interface FolderItemProps {
  label: string
  count: number
  selected: boolean
  onSelect: () => void
}

function FolderItem({ label, count, selected, onSelect }: FolderItemProps) {
  return (
    <li>
      <button type="button" className={`job-item${selected ? ' is-selected' : ''}`} aria-current={selected} onClick={onSelect}>
        <span className="job-title">{label}</span>
        <span className="job-meta">{count} CV</span>
      </button>
    </li>
  )
}

function NewFolderForm({ onCreate }: { onCreate: (name: string) => Promise<void> }) {
  const [name, setName] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await onCreate(name.trim())
      setName('')
    } catch (err) {
      setError(err instanceof ApiError && err.status === 409 ? 'Bu adda bir klasör zaten var.' : errorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="folder-form" onSubmit={handleSubmit}>
      <input
        className="input"
        value={name}
        onChange={(e) => setName(e.target.value)}
        maxLength={100}
        placeholder="Yeni klasör adı"
        aria-label="Yeni klasör adı"
      />
      <button type="submit" className="button button-small" disabled={submitting || name.trim() === ''}>
        <Icon name="plus" size={16} />
        Ekle
      </button>
      {error && <Alert tone="error">{error}</Alert>}
    </form>
  )
}
