import { PlusIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ErrorState } from '@/components/ErrorState'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useReadingGoals } from './hooks'
import { ReadingGoalCard } from './ReadingGoalCard'
import { ReadingGoalDialog } from './ReadingGoalDialog'
import type { ReadingGoal } from './types'

/** "Okuma Hedefleri" section of the profile page (FR-10): list, create, edit and delete goals. */
export function ReadingGoals() {
  const { t } = useTranslation()
  const goals = useReadingGoals()
  const [dialog, setDialog] = useState<{ open: boolean; goal: ReadingGoal | null }>({ open: false, goal: null })

  const openNew = () => setDialog({ open: true, goal: null })

  return (
    <section aria-labelledby="reading-goals-title" className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 id="reading-goals-title" className="text-lg font-bold">
          {t('readingGoals.sectionTitle')}
        </h2>
        <Button type="button" onClick={openNew} className="h-10 rounded-lg px-4 text-sm font-semibold">
          <PlusIcon aria-hidden />
          {t('readingGoals.add')}
        </Button>
      </div>

      {goals.isError ? (
        <ErrorState error={goals.error} onRetry={() => void goals.refetch()} />
      ) : goals.isPending ? (
        <div className="grid gap-4 lg:grid-cols-2 2xl:grid-cols-3">
          {Array.from({ length: 2 }, (_, index) => (
            <Skeleton key={index} className="h-[8.5rem] rounded-lg bg-white/70" />
          ))}
        </div>
      ) : goals.data.length === 0 ? (
        <div className="flex flex-col items-start gap-1 rounded-lg border border-dashed border-foreground/30 p-6">
          <p className="font-semibold">{t('readingGoals.empty.title')}</p>
          <p className="text-sm text-muted-foreground">{t('readingGoals.empty.description')}</p>
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2 2xl:grid-cols-3">
          {goals.data.map((goal) => (
            <ReadingGoalCard key={goal.id} goal={goal} onEdit={() => setDialog({ open: true, goal })} />
          ))}
        </div>
      )}

      <ReadingGoalDialog
        open={dialog.open}
        goal={dialog.goal}
        onOpenChange={(open) => setDialog((current) => ({ ...current, open }))}
      />
    </section>
  )
}
