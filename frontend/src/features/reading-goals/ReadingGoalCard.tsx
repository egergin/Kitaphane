import { CircleCheckIcon, Loader2Icon, PencilIcon, Trash2Icon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { formatPercent } from '@/features/books/progress'
import { getErrorMessage } from '@/lib/errors'
import { cn } from '@/lib/utils'
import { goalAmount, goalTitle } from './format'
import { useDeleteReadingGoal } from './hooks'
import type { ReadingGoal } from './types'

type ReadingGoalCardProps = { goal: ReadingGoal; onEdit: () => void }

/** Goal title, progress bar (capped at 100%) and the real progress numbers. */
export function ReadingGoalCard({ goal, onEdit }: ReadingGoalCardProps) {
  const { t, i18n } = useTranslation()
  const deleteGoal = useDeleteReadingGoal()
  const title = goalTitle(goal, t, i18n.language)
  const reached = goal.current >= goal.target
  const barWidth = Math.min(100, goal.percentage)
  const percentLabel = formatPercent(goal.percentage, i18n.language)

  const remove = async () => {
    try {
      await deleteGoal.mutateAsync(goal.id)
      toast.success(t('readingGoals.deleted'))
    } catch (error) {
      toast.error(getErrorMessage(t, error))
    }
  }

  return (
    <article className="flex flex-col gap-3 rounded-lg border border-border bg-white p-4">
      <div className="flex items-start gap-2">
        <div className="flex min-w-0 flex-1 flex-col gap-0.5">
          <h3 className="text-base leading-6 font-bold break-words">{title}</h3>
          <p className="text-xs text-muted-foreground">{t(`readingGoals.types.${goal.type}`)}</p>
        </div>
        <Button type="button" variant="ghost" size="icon-sm" onClick={onEdit} aria-label={t('readingGoals.edit', { title })}>
          <PencilIcon aria-hidden />
        </Button>
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className="text-destructive hover:bg-destructive/10 hover:text-destructive"
              aria-label={t('readingGoals.delete.button', { title })}
            >
              <Trash2Icon aria-hidden />
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t('readingGoals.delete.title')}</AlertDialogTitle>
              <AlertDialogDescription>{t('readingGoals.delete.description', { title })}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
              <AlertDialogAction
                variant="destructive"
                disabled={deleteGoal.isPending}
                onClick={(event) => {
                  event.preventDefault()
                  void remove()
                }}
              >
                {deleteGoal.isPending && <Loader2Icon className="animate-spin" aria-hidden />}
                {t('readingGoals.delete.confirm')}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>

      <div
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={barWidth}
        aria-label={t('readingGoals.progressLabel', { title })}
        className="h-2 overflow-hidden rounded-full bg-progress-track"
      >
        <div
          className={cn('h-full rounded-full transition-[width]', reached ? 'bg-[#10B981]' : 'bg-progress')}
          style={{ width: `${barWidth}%` }}
        />
      </div>

      <div className="flex items-center justify-between gap-3 text-sm">
        <span className="tabular-nums">
          <span className="font-bold">{new Intl.NumberFormat(i18n.language).format(goal.current)}</span>
          {' / '}
          {goalAmount(goal.type, goal.target, t, i18n.language)}
        </span>
        {reached ? (
          <span className="flex items-center gap-1 font-semibold text-[#10B981]">
            <CircleCheckIcon className="size-4" aria-hidden />
            {t('readingGoals.reached')} · {percentLabel}
          </span>
        ) : (
          <span className="font-semibold tabular-nums">{percentLabel}</span>
        )}
      </div>
    </article>
  )
}
