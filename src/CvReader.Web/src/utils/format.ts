const dateFormatter = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' })

export function formatDate(iso: string): string {
  return dateFormatter.format(new Date(iso))
}

export function formatScore(score: number): string {
  return score.toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })
}
