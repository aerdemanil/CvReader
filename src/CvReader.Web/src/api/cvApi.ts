import { request } from './http'
import type { CvDetail, CvFilter, CvPage, CvUploadResult } from './types'

export const CV_PAGE_SIZE = 50

const cvPath = (id: string) => `/cv/${encodeURIComponent(id)}`

export const cvApi = {
  upload: (files: File[], folderId: string | null) => {
    const form = new FormData()
    for (const file of files) form.append('files', file)
    if (folderId) form.append('folderId', folderId)
    return request<CvUploadResult[]>('/cv/bulk', { method: 'POST', body: form })
  },

  list: (filter: CvFilter, search: string, page: number, signal?: AbortSignal) => {
    const query = new URLSearchParams({ page: String(page), pageSize: String(CV_PAGE_SIZE) })
    if (filter.kind === 'folder') query.set('folderId', filter.id)
    if (filter.kind === 'unfiled') query.set('unfiled', 'true')
    if (search) query.set('search', search)
    return request<CvPage>(`/cv?${query}`, { signal })
  },

  get: (id: string, signal?: AbortSignal) => request<CvDetail>(cvPath(id), { signal }),

  // folderId null ise CV'ler klasörden çıkarılır.
  move: (profileIds: string[], folderId: string | null) =>
    request<void>('/cv/folder', { method: 'PUT', json: { profileIds, folderId } }),

  remove: (id: string) => request<void>(cvPath(id), { method: 'DELETE' }),
}
