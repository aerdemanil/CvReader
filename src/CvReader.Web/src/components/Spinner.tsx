export function Spinner({ label }: { label?: string }) {
  return <span className="spinner" role="status" aria-label={label ?? 'Yükleniyor'} />
}

export function FullPageSpinner() {
  return (
    <div className="full-page-center">
      <Spinner />
    </div>
  )
}
