import { useTranslation } from 'react-i18next'
import { ErrorState } from '@/components/ErrorState'
import { Skeleton } from '@/components/ui/skeleton'
import { useStatisticsOverview } from './hooks'
import { StatCard } from './StatCard'

/** Book counts by status. Accent colors match the status badges. */
export function BookStatistics() {
  const { t } = useTranslation()
  const overview = useStatisticsOverview()

  if (overview.isError) return <ErrorState error={overview.error} onRetry={() => void overview.refetch()} />

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
      {overview.isPending ? (
        Array.from({ length: 4 }, (_, index) => <Skeleton key={index} className="h-[6.5rem] rounded-lg bg-white/70" />)
      ) : (
        <>
          <StatCard label={t('profile.stats.total')} value={overview.data.totalBooks} accentClassName="bg-foreground" />
          <StatCard label={t('profile.stats.completed')} value={overview.data.completedBooks} accentClassName="bg-[#10B981]" />
          <StatCard label={t('profile.stats.reading')} value={overview.data.readingBooks} accentClassName="bg-[#2563EB]" />
          <StatCard label={t('profile.stats.toRead')} value={overview.data.toReadBooks} accentClassName="bg-[#F59E0B]" />
        </>
      )}
    </div>
  )
}
