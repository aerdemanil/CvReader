import { useCallback, useEffect, useState } from 'react'
import { cvApi, CV_PAGE_SIZE } from '../../api/cvApi'
import { foldersApi } from '../../api/foldersApi'
import { errorMessage } from '../../api/http'
import type { CvFilter, CvSummary, FolderList } from '../../api/types'

const SEARCH_DELAY_MS = 250

// Klasörler ile seçili bölümün CV listesini birlikte yönetir.
// refreshKey değiştiğinde (ör. yeni CV yüklendiğinde) ikisi de baştan okunur.
export function useCvLibrary(refreshKey: number) {
  const [folderList, setFolderList] = useState<FolderList | null>(null)
  const [filter, setFilter] = useState<CvFilter>({ kind: 'all' })
  const [search, setSearch] = useState('')
  const [cvs, setCvs] = useState<CvSummary[] | null>(null)
  const [total, setTotal] = useState(0)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reloadCount, setReloadCount] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    foldersApi
      .list(controller.signal)
      .then(setFolderList)
      .catch((err) => {
        if (!controller.signal.aborted) setError(errorMessage(err))
      })
    return () => controller.abort()
  }, [refreshKey, reloadCount])

  useEffect(() => {
    const controller = new AbortController()
    // Arama kutusuna her tuş vuruşunda istek gitmesin.
    const timer = setTimeout(() => {
      cvApi
        .list(filter, search.trim(), 1, controller.signal)
        .then((page) => {
          setCvs(page.items)
          setTotal(page.total)
        })
        .catch((err) => {
          if (controller.signal.aborted) return
          setError(errorMessage(err))
          setCvs((current) => current ?? [])
        })
    }, SEARCH_DELAY_MS)

    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [filter, search, refreshKey, reloadCount])

  const loadedCount = cvs?.length ?? 0

  const loadMore = useCallback(async () => {
    setLoadingMore(true)
    setError(null)
    try {
      const page = await cvApi.list(filter, search.trim(), loadedCount / CV_PAGE_SIZE + 1)
      setCvs((current) => [...(current ?? []), ...page.items])
      setTotal(page.total)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setLoadingMore(false)
    }
  }, [filter, search, loadedCount])

  // Değişiklikten sonra sayılar ve sayfa sınırları kaydığı için her şey baştan okunur.
  const change = useCallback(async (action: () => Promise<unknown>) => {
    setError(null)
    try {
      await action()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setReloadCount((count) => count + 1)
    }
  }, [])

  // Hata formda gösterilsin diye çağırana bırakılır.
  const createFolder = useCallback(async (name: string) => {
    const folder = await foldersApi.create(name)
    setReloadCount((count) => count + 1)
    setFilter({ kind: 'folder', id: folder.id })
  }, [])

  const deleteFolder = useCallback(
    async (id: string) => {
      await change(() => foldersApi.remove(id))
      setFilter({ kind: 'all' })
    },
    [change],
  )

  const moveCvs = useCallback((ids: string[], folderId: string | null) => change(() => cvApi.move(ids, folderId)), [change])

  const deleteCvs = useCallback((ids: string[]) => change(() => Promise.all(ids.map((id) => cvApi.remove(id)))), [change])

  return {
    folderList,
    filter,
    setFilter,
    search,
    setSearch,
    cvs,
    total,
    hasMore: loadedCount < total,
    loadingMore,
    error,
    loadMore,
    createFolder,
    deleteFolder,
    moveCvs,
    deleteCvs,
  }
}
