import { useTranslation } from 'react-i18next'
import { formatPercent, readingPercentage } from './progress'

type ReadingProgressProps = {
  currentPage: number
  pageCount: number
}

/** "%60 ████░░" row of the book card: percentage label followed by a slim progress bar. */
export function ReadingProgress({ currentPage, pageCount }: ReadingProgressProps) {
  const { t, i18n } = useTranslation()
  const percentage = readingPercentage(currentPage, pageCount)
  const label = formatPercent(percentage, i18n.language)

  return (
    <div className="flex h-5 items-center gap-2">
      <span className="w-9 shrink-0 text-xs leading-4 font-semibold tabular-nums">{label}</span>
      <div
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percentage}
        aria-label={t('books.progressLabel', { percentage: label })}
        className="h-1.5 flex-1 overflow-hidden rounded-full bg-progress-track"
      >
        <div className="h-full rounded-full bg-progress transition-[width]" style={{ width: `${percentage}%` }} />
      </div>
    </div>
  )
}
