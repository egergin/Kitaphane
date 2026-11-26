import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ErrorState } from '@/components/ErrorState'
import { Skeleton } from '@/components/ui/skeleton'
import { formatDate } from '@/features/books/dates'
import { cn } from '@/lib/utils'
import { useReadingStatistics } from './hooks'
import { StatCard } from './StatCard'
import { READING_PERIODS, type ReadingPeriod } from './types'

/** Reading statistics for the last 1 month / 6 months / 1 year (computed by the API). */
export function ReadingStatistics() {
  const { t } = useTranslation()
  const [period, setPeriod] = useState<ReadingPeriod>('1m')
  const stats = useReadingStatistics(period)

  return (
    <section aria-labelledby="reading-stats-title" className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-col">
          <h2 id="reading-stats-title" className="text-lg font-bold">
            {t('profile.reading.title')}
          </h2>
          {stats.data && (
            <p className="text-sm text-muted-foreground">
              {formatDate(stats.data.periodStart)} – {formatDate(stats.data.periodEnd)}
            </p>
          )}
        </div>

        <div role="radiogroup" aria-label={t('profile.reading.period')} className="flex rounded-lg border border-foreground/70 bg-white p-0.5">
          {READING_PERIODS.map((option) => (
            <button
              key={option}
              type="button"
              role="radio"
              aria-checked={period === option}
              onClick={() => setPeriod(option)}
              className={cn(
                'rounded-md px-3 py-1 text-sm font-semibold transition-colors',
                period === option ? 'bg-foreground text-white' : 'hover:bg-muted',
              )}
            >
              {t(`profile.reading.periods.${option}`)}
            </button>
          ))}
        </div>
      </div>

      {stats.isError ? (
        <ErrorState error={stats.error} onRetry={() => void stats.refetch()} />
      ) : stats.isPending ? (
        <div className="grid gap-4 sm:grid-cols-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-[5.5rem] rounded-lg bg-white/70" />
          ))}
        </div>
      ) : (
        <div className={cn('grid gap-4 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]', stats.isPlaceholderData && 'opacity-60')}>
          <div className="grid content-start gap-4 sm:grid-cols-3 lg:grid-cols-1 xl:grid-cols-3">
            <StatCard label={t('profile.reading.completedBooks')} value={stats.data.completedBooks} />
            <StatCard label={t('profile.reading.pagesRead')} value={stats.data.pagesRead} />
            <StatCard
              label={t('profile.reading.averageLength')}
              value={stats.data.averageBookLength}
              unit={t('profile.reading.pagesUnit')}
            />
          </div>

          <div className="flex flex-col gap-3 rounded-lg border border-border bg-white p-4">
            <h3 className="text-sm font-bold">{t('profile.reading.byAuthor')}</h3>
            {stats.data.booksByAuthor.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t('profile.reading.noBooks')}</p>
            ) : (
              <ul className="flex max-h-[11rem] flex-col divide-y overflow-y-auto pr-1">
                {stats.data.booksByAuthor.map(({ author, count }) => (
                  <li key={author} className="flex items-center justify-between gap-3 py-2 text-sm">
                    <span className="min-w-0 truncate font-semibold">{author}</span>
                    <span className="shrink-0 text-muted-foreground tabular-nums">
                      {t('profile.reading.bookCount', { count })}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </section>
  )
}
