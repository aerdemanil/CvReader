import { request } from './http'
import type { Folder, FolderList } from './types'

export const foldersApi = {
  list: (signal?: AbortSignal) => request<FolderList>('/folders', { signal }),

  create: (name: string) => request<Folder>('/folders', { method: 'POST', json: { name } }),

  remove: (id: string) => request<void>(`/folders/${encodeURIComponent(id)}`, { method: 'DELETE' }),
}
