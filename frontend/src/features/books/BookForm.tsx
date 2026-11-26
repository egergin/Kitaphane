import { zodResolver } from '@hookform/resolvers/zod'
import { type ReactNode, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { FormAlert } from '@/components/form/FormAlert'
import { OutlinedField, OutlinedSelect, OutlinedTextarea } from '@/components/form/OutlinedField'
import { SubmitButton } from '@/components/form/SubmitButton'
import { useFormErrors } from '@/components/form/useFormErrors'
import { ApiError } from '@/lib/http'
import { BookCover } from './BookCover'
import { bookFormSchema, type BookFormValues, toBookInput } from './schema'
import { applyStatusRules } from './statusRules'
import { BOOK_STATUSES, type BookInput, type BookStatus } from './types'

const FIELDS = [
  'title', 'author', 'isbn', 'pageCount', 'currentPage', 'status',
  'coverImage', 'rating', 'notes', 'startDate', 'finishDate',
] as const

type BookFormProps = {
  defaultValues: BookFormValues
  /** Status saved on the server (null for a new book); used for the status rule preview. */
  savedStatus: BookStatus | null
  submitLabel: string
  onSubmit: (book: BookInput) => Promise<void>
  /** Extra actions rendered next to the submit button (e.g. delete). */
  actions?: ReactNode
}

export function BookForm({ defaultValues, savedStatus, submitLabel, onSubmit, actions }: BookFormProps) {
  const { t } = useTranslation()
  const form = useForm<BookFormValues>({ resolver: zodResolver(bookFormSchema), defaultValues })
  const { formError, fieldError, handleError, setFormError } = useFormErrors(form.setError, FIELDS)
  const { errors, isSubmitting } = form.formState
  const [previousStatus, setPreviousStatus] = useState<BookStatus | null>(savedStatus)

  const [status, coverImage, title] = useWatch({ control: form.control, name: ['status', 'coverImage', 'title'] })

  /** Preview of the backend status rules so the form shows what will be saved. */
  const onStatusChange = (next: BookStatus) => {
    const values = form.getValues()
    const pageCount = Number(values.pageCount) || 0
    const ruled = applyStatusRules(
      {
        status: next,
        pageCount,
        currentPage: Number(values.currentPage) || 0,
        startDate: values.startDate || null,
        finishDate: values.finishDate || null,
      },
      previousStatus,
    )
    setPreviousStatus(next)

    if (next === 'COMPLETED' && pageCount > 0) form.setValue('currentPage', String(ruled.currentPage))
    form.setValue('startDate', ruled.startDate ?? '')
    form.setValue('finishDate', ruled.finishDate ?? '')
  }

  const submit = form.handleSubmit(async (values) => {
    setFormError(null)
    const input = toBookInput(values)
    const ruled = applyStatusRules(input, savedStatus)
    try {
      await onSubmit(ruled)
    } catch (error) {
      if (error instanceof ApiError && error.code === 'BOOK_ISBN_EXISTS') {
        form.setError('isbn', { message: t('errors.BOOK_ISBN_EXISTS') })
        return
      }
      handleError(error)
    }
  })

  return (
    <form onSubmit={submit} noValidate className="grid gap-8 md:grid-cols-[9.375rem_1fr]">
      <div className="mx-auto w-[7.5rem] md:mx-0 md:w-full">
        <BookCover src={coverImage.trim() || null} title={title || t('books.form.untitled')} />
      </div>

      <div className="flex flex-col gap-5">
        <FormAlert message={formError} />

        {/* On wide screens related fields share a row so the whole form fits on one screen. */}
        <div className="grid gap-5 lg:grid-cols-2">
          <OutlinedField label={t('books.fields.title')} error={fieldError(errors.title?.message)} {...form.register('title')} />
          <OutlinedField label={t('books.fields.author')} error={fieldError(errors.author?.message)} {...form.register('author')} />
        </div>

        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          <OutlinedField
            label={t('books.fields.isbn')}
            inputMode="numeric"
            error={fieldError(errors.isbn?.message)}
            {...form.register('isbn')}
          />
          <OutlinedSelect
            label={t('books.fields.status')}
            error={fieldError(errors.status?.message)}
            {...form.register('status', { onChange: (event) => onStatusChange(event.target.value as BookStatus) })}
          >
            {BOOK_STATUSES.map((value) => (
              <option key={value} value={value}>
                {t(`books.status.${value}`)}
              </option>
            ))}
          </OutlinedSelect>
          <OutlinedField
            label={t('books.fields.pageCount')}
            inputMode="numeric"
            error={fieldError(errors.pageCount?.message)}
            {...form.register('pageCount', {
              onChange: (event) => {
                if (form.getValues('status') === 'COMPLETED') form.setValue('currentPage', event.target.value)
              },
            })}
          />
          <OutlinedField
            label={t('books.fields.currentPage')}
            inputMode="numeric"
            readOnly={status === 'COMPLETED'}
            error={fieldError(errors.currentPage?.message)}
            {...form.register('currentPage')}
          />
        </div>

        <div className="grid gap-5 sm:grid-cols-[minmax(0,0.7fr)_minmax(0,1fr)_minmax(0,1fr)] 2xl:grid-cols-[minmax(0,0.7fr)_minmax(0,1fr)_minmax(0,1fr)_minmax(0,1.5fr)]">
          <OutlinedSelect label={t('books.fields.rating')} error={fieldError(errors.rating?.message)} {...form.register('rating')}>
            <option value="">{t('books.form.noRating')}</option>
            {Array.from({ length: 10 }, (_, index) => index + 1).map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </OutlinedSelect>
          <OutlinedField
            label={t('books.fields.startDate')}
            type="date"
            error={fieldError(errors.startDate?.message)}
            {...form.register('startDate')}
          />
          <OutlinedField
            label={t('books.fields.finishDate')}
            type="date"
            readOnly={status !== 'COMPLETED'}
            error={fieldError(errors.finishDate?.message)}
            {...form.register('finishDate')}
          />
          <OutlinedField
            className="sm:col-span-3 2xl:col-span-1"
            label={t('books.fields.coverImage')}
            type="url"
            placeholder="https://"
            error={fieldError(errors.coverImage?.message)}
            {...form.register('coverImage')}
          />
        </div>
        <OutlinedTextarea label={t('books.fields.notes')} error={fieldError(errors.notes?.message)} {...form.register('notes')} />

        <div className="flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
          {actions ?? <span />}
          <div className="sm:w-44">
            <SubmitButton size="md" loading={isSubmitting}>{submitLabel}</SubmitButton>
          </div>
        </div>
      </div>
    </form>
  )
}
