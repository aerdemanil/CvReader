// Kullanıcıya erken geri bildirim içindir; asıl doğrulama sunucuda yapılır.
export const MAX_FILE_BYTES = 10 * 1024 * 1024
export const MAX_TOTAL_BYTES = 28 * 1024 * 1024 // Kestrel'in varsayılan ~28,6 MB istek sınırının altında

// PDF görüntüleyicisi kayıtlı olmayan sistemlerde tarayıcı türü boş bırakır; o durumda uzantı yeterlidir.
export function isPdf(file: File): boolean {
  return (file.type === 'application/pdf' || file.type === '') && file.name.toLowerCase().endsWith('.pdf')
}

export function formatSize(bytes: number): string {
  return bytes < 1024 * 1024 ? `${Math.ceil(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

export interface SelectionCheck {
  accepted: File[]
  rejected: { file: File; reason: string }[]
}

export function checkSelection(files: File[]): SelectionCheck {
  const result: SelectionCheck = { accepted: [], rejected: [] }
  for (const file of files) {
    if (!isPdf(file)) result.rejected.push({ file, reason: 'Sadece PDF dosyaları yüklenebilir.' })
    else if (file.size > MAX_FILE_BYTES) result.rejected.push({ file, reason: 'Dosya 10 MB sınırını aşıyor.' })
    else result.accepted.push(file)
  }
  return result
}
