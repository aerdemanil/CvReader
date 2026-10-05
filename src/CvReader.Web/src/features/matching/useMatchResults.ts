import { useCallback, useEffect, useState } from 'react'
import { cvApi } from '../../api/cvApi'
import { errorMessage } from '../../api/http'
import { jobsApi, RESULTS_PAGE_SIZE } from '../../api/jobsApi'
import type { MatchResult } from '../../api/types'

// Skorlar sunucuda her okumada hesaplanır. refreshKey değiştiğinde (ör. yeni CV yüklendiğinde) liste baştan okunur.
export function useMatchResults(jobId: string, refreshKey: number) {
  const [results, setResults] = useState<MatchResult[] | null>(null)
  const [total, setTotal] = useState(0)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reloadCount, setReloadCount] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    jobsApi
      .getResults(jobId, 1, controller.signal)
      .then((page) => {
        setResults(page.items)
        setTotal(page.total)
        setError(null)
      })
      .catch((err) => {
        if (controller.signal.aborted) return
        setError(errorMessage(err))
        setResults((current) => current ?? [])
      })
    return () => controller.abort()
  }, [jobId, refreshKey, reloadCount])

  const loadedCount = results?.length ?? 0

  const loadMore = useCallback(async () => {
    setLoadingMore(true)
    setError(null)
    try {
      const page = await jobsApi.getResults(jobId, loadedCount / RESULTS_PAGE_SIZE + 1)
      setResults((current) => [...(current ?? []), ...page.items])
      setTotal(page.total)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setLoadingMore(false)
    }
  }, [jobId, loadedCount])

  // Silme sonrası sayfa sınırları kaydığı için liste baştan okunur.
  const removeCv = useCallback(async (profileId: string) => {
    setError(null)
    try {
      await cvApi.remove(profileId)
      setReloadCount((count) => count + 1)
    } catch (err) {
      setError(errorMessage(err))
    }
  }, [])

  return { results, total, hasMore: loadedCount < total, loadingMore, error, loadMore, removeCv }
}
