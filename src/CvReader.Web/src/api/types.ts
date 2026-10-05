// Backend DTO'larının ön yüzdeki karşılıkları.

export interface Session {
  email: string
}

export interface CvUploadResult {
  fileName: string
  profileId: string | null
  error: string | null
}

export interface JobPosting {
  id: string
  title: string
  keywords: string[]
  createdAt: string
}

export interface CreateJobRequest {
  title: string
  keywords: string[]
}

export interface MatchResult {
  profileId: string
  fileName: string
  score: number
}

// CV'de bulunan bir kelime ve ilanın anahtar kelimesine yakınlık puanı (0-100).
export interface TermMatch {
  term: string
  score: number
}

export interface KeywordMatch {
  keyword: string
  score: number
  terms: TermMatch[]
}

export interface MatchDetail extends MatchResult {
  keywords: KeywordMatch[]
}

// total: kullanıcının tüm CV'lerinin sayısı; items: istenen sayfa.
export interface MatchPage {
  total: number
  items: MatchResult[]
}

export interface Folder {
  id: string
  name: string
  cvCount: number
}

export interface FolderList {
  folders: Folder[]
  unfiledCount: number
}

export interface CvSummary {
  id: string
  fileName: string
  pageCount: number
  createdAt: string
  folderId: string | null
}

export interface CvPage {
  total: number
  items: CvSummary[]
}

export interface CvDetail extends CvSummary {
  text: string
}

// CV listesinin hangi bölümü gösteriliyor: hepsi, klasörsüzler ya da tek bir klasör.
export type CvFilter = { kind: 'all' } | { kind: 'unfiled' } | { kind: 'folder'; id: string }
