import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { FormAlert } from '@/components/form/FormAlert'
import { OutlinedField, OutlinedSelect } from '@/components/form/OutlinedField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { goalTitle } from './format'
import { useSaveReadingGoal } from './hooks'
import { newGoalValues, readingGoalSchema, type ReadingGoalFormValues, toFormValues, toInput } from './schema'
import { READING_GOAL_TYPES, type ReadingGoal } from './types'

const FIELDS = ['type', 'target', 'startDate', 'endDate'] as const

type ReadingGoalDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** The goal being edited; null creates a new goal. */
  goal: ReadingGoal | null
}

export function ReadingGoalDialog({ open, onOpenChange, goal }: ReadingGoalDialogProps) {
  const { t } = useTranslation()

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="gap-5 p-6 sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="text-lg font-bold">
            {goal ? t('readingGoals.editTitle') : t('readingGoals.newTitle')}
          </DialogTitle>
          <DialogDescription>{t('readingGoals.formDescription')}</DialogDescription>
        </DialogHeader>
        {/* Re-created each time it opens so it starts from the goal's saved values. */}
        {open && <GoalForm goal={goal} onDone={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  )
}

function GoalForm({ goal, onDone }: { goal: ReadingGoal | null; onDone: () => void }) {
  const { t, i18n } = useTranslation()
  const saveGoal = useSaveReadingGoal()
  const form = useForm<ReadingGoalFormValues>({
    resolver: zodResolver(readingGoalSchema),
    defaultValues: goal ? toFormValues(goal) : newGoalValues(),
  })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, FIELDS)
  const { errors, isSubmitting } = form.formState
  const values = useWatch({ control: form.control })

  const parsed = readingGoalSchema.safeParse(values)
  const preview = parsed.success ? goalTitle(toInput(parsed.data), t, i18n.language) : null

  const onSubmit = form.handleSubmit(async (formValues) => {
    setFormError(null)
    try {
      await saveGoal.mutateAsync({ id: goal?.id ?? null, input: toInput(formValues) })
      toast.success(goal ? t('readingGoals.updated') : t('readingGoals.created'))
      onDone()
    } catch (error) {
      handleError(error)
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-5">
      <FormAlert message={formError} />

      <div className="grid gap-5 sm:grid-cols-2">
        <OutlinedSelect label={t('readingGoals.fields.type')} error={fieldError(errors.type?.message)} {...form.register('type')}>
          {READING_GOAL_TYPES.map((type) => (
            <option key={type} value={type}>
              {t(`readingGoals.types.${type}`)}
            </option>
          ))}
        </OutlinedSelect>
        <OutlinedField
          label={t('readingGoals.fields.target')}
          inputMode="numeric"
          autoFocus
          error={fieldError(errors.target?.message)}
          {...form.register('target')}
        />
        <OutlinedField
          label={t('readingGoals.fields.startDate')}
          type="date"
          error={fieldError(errors.startDate?.message)}
          {...form.register('startDate')}
        />
        <OutlinedField
          label={t('readingGoals.fields.endDate')}
          type="date"
          error={fieldError(errors.endDate?.message)}
          {...form.register('endDate')}
        />
      </div>

      <p className="min-h-5 text-sm font-semibold" aria-live="polite">
        {preview}
      </p>

      <DialogFooter className="gap-3 sm:items-center">
        <Button type="button" variant="outline" className="h-10 rounded-lg" onClick={onDone}>
          {t('common.cancel')}
        </Button>
        <div className="sm:w-36">
          <SubmitButton size="md" loading={isSubmitting}>
            {t('readingGoals.save')}
          </SubmitButton>
        </div>
      </DialogFooter>
    </form>
  )
}
