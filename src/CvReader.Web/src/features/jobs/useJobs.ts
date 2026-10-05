import { useCallback, useEffect, useState } from 'react'
import { errorMessage } from '../../api/http'
import { jobsApi } from '../../api/jobsApi'
import type { CreateJobRequest, JobPosting } from '../../api/types'

export function useJobs() {
  const [jobs, setJobs] = useState<JobPosting[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    jobsApi
      .list()
      .then(setJobs)
      .catch((err) => setError(errorMessage(err)))
      .finally(() => setLoading(false))
  }, [])

  const create = useCallback(async (request: CreateJobRequest) => {
    const job = await jobsApi.create(request)
    setJobs((current) => [job, ...current])
    return job
  }, [])

  const remove = useCallback(async (id: string) => {
    await jobsApi.remove(id)
    setJobs((current) => current.filter((job) => job.id !== id))
  }, [])

  return { jobs, loading, error, create, remove }
}
