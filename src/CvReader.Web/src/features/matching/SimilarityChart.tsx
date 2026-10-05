import type { MatchResult } from '../../api/types'
import { Icon } from '../../components/Icon'
import { Menu } from '../../components/Menu'
import { formatScore } from '../../utils/format'

const ticks = [0, 25, 50, 75, 100]

interface SimilarityChartProps {
  results: MatchResult[]
  onPreview: (result: MatchResult) => void
  onExplain: (result: MatchResult) => void
  onDelete: (result: MatchResult) => void
}

// Sonuçlar sunucudan skora göre azalan sırada gelir.
export function SimilarityChart({ results, onPreview, onExplain, onDelete }: SimilarityChartProps) {
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
            <Menu
              label={`${r.fileName} için seçenekler`}
              items={[
                { label: 'CV önizleme', icon: 'file', onSelect: () => onPreview(r) },
                { label: 'Eşleşen kelimeler', icon: 'sparkles', onSelect: () => onExplain(r) },
              ]}
            />
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
        <span />
      </div>
      <figcaption className="muted">Skor, ilanın anahtar kelimelerinin ya da anlamca yakınlarının CV’de ne ölçüde geçtiğini gösterir (0–100).</figcaption>
    </figure>
  )
}
