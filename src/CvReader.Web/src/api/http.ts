export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

type UnauthorizedListener = () => void

let unauthorizedListener: UnauthorizedListener | null = null

// Oturum sona erdiğinde (401) uygulamanın giriş ekranına dönebilmesi için.
export function setUnauthorizedListener(listener: UnauthorizedListener | null) {
  unauthorizedListener = listener
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  json?: unknown
  body?: FormData
  signal?: AbortSignal
}

// Token JavaScript'e hiç verilmez; tarayıcı HttpOnly oturum cookie'sini aynı kökene kendisi ekler.
export async function request<T>(path: string, { method = 'GET', json, body, signal }: RequestOptions = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      method,
      credentials: 'same-origin',
      headers: json === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: json === undefined ? body : JSON.stringify(json),
      signal,
    })
  } catch (error) {
    if (signal?.aborted) throw error
    throw new ApiError('Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.', 0)
  }

  if (response.status === 401) unauthorizedListener?.()
  if (!response.ok) throw new ApiError(await readErrorMessage(response), response.status)
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

const statusMessages: Record<number, string> = {
  401: 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.',
  413: 'Dosyalar çok büyük. Daha az dosya seçip tekrar deneyin.',
  429: 'Çok fazla deneme yapıldı. Lütfen bir dakika sonra tekrar deneyin.',
  503: 'Benzerlik servisi (Ollama) şu an yanıt vermiyor. Servisin açık olduğundan emin olun.',
}

async function readErrorMessage(response: Response): Promise<string> {
  const known = statusMessages[response.status]
  if (known) return known

  try {
    const body = (await response.json()) as { errors?: Record<string, string[]> }
    const messages = Object.values(body.errors ?? {}).flat()
    if (messages.length > 0) return messages.join(' ')
  } catch {
    // Gövde JSON değilse genel mesaj kullanılır.
  }
  return 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
}

export function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
}
