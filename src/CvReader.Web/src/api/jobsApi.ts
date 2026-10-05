import { request } from './http'
import type { CreateJobRequest, JobPosting, MatchDetail, MatchPage } from './types'

export const RESULTS_PAGE_SIZE = 50

const jobPath = (id: string) => `/jobs/${encodeURIComponent(id)}`

export const jobsApi = {
  list: () => request<JobPosting[]>('/jobs'),

  create: (job: CreateJobRequest) => request<JobPosting>('/jobs', { method: 'POST', json: job }),

  remove: (id: string) => request<void>(jobPath(id), { method: 'DELETE' }),

  getResults: (id: string, page: number, signal?: AbortSignal) =>
    request<MatchPage>(`${jobPath(id)}/results?page=${page}&pageSize=${RESULTS_PAGE_SIZE}`, { signal }),

  getMatchDetail: (id: string, profileId: string, signal?: AbortSignal) =>
    request<MatchDetail>(`${jobPath(id)}/results/${encodeURIComponent(profileId)}`, { signal }),
}
