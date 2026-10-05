import type { MatchResult } from '../../api/types'
import { Icon } from '../../components/Icon'
import { formatScore } from '../../utils/format'

const ticks = [0, 25, 50, 75, 100]

interface SimilarityChartProps {
  results: MatchResult[]
  onDelete: (result: MatchResult) => void
}

// Sonuçlar sunucudan skora göre azalan sırada gelir.
export function SimilarityChart({ results, onDelete }: SimilarityChartProps) {
  return (
    <figure className="chart">
      <ol className="chart-rows">
        {results.map((r, index) => (
          <li key={r.profileId} className="chart-row" aria-label={`${index + 1}. ${r.fileName}: ${formatScore(r.score)} / 100`}>
            <span className="chart-rank">{index + 1}</span>
            <span className="chart-label" title={r.fileName}>
              {r.fileName}
            </span>
            <span className="chart-track">
              <span className="chart-bar" style={{ width: `${r.score}%` }} />
            </span>
            <span className="chart-value">{formatScore(r.score)}</span>
            <button
              type="button"
              className="icon-button icon-button-danger"
              aria-label={`${r.fileName} CV’sini sil`}
              title="CV’yi sil"
              onClick={() => onDelete(r)}
            >
              <Icon name="trash" size={16} />
            </button>
          </li>
        ))}
      </ol>
      <div className="chart-axis" aria-hidden="true">
        <span />
        <span />
        <span className="chart-ticks">
          {ticks.map((t) => (
            <span key={t}>{t}</span>
          ))}
        </span>
        <span />
        <span />
      </div>
      <figcaption className="muted">Skor, CV’nin ilanın anahtar kelimelerine anlamca ne kadar yakın olduğunu gösterir (0–100).</figcaption>
    </figure>
  )
}
