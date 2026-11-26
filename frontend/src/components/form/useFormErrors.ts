import { useCallback, useState } from 'react'
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { getErrorMessage } from '@/lib/errors'
import { ApiError } from '@/lib/http'

/**
 * Error helpers for forms:
 * - fieldError: translates zod message keys (raw server messages pass through)
 * - handleError: maps API validation errors to fields, other errors to a form-level message
 */
export function useFormErrors<T extends FieldValues>(setError: UseFormSetError<T>, fields: readonly Path<T>[]) {
  const { t, i18n } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)

  const fieldError = useCallback(
    (message: string | undefined) => (message && i18n.exists(message) ? t(message as never) : message),
    [t, i18n],
  )

  const handleError = useCallback(
    (error: unknown) => {
      if (error instanceof ApiError && error.code === 'VALIDATION_ERROR' && error.errors) {
        let mapped = false
        for (const [field, messages] of Object.entries(error.errors)) {
          if ((fields as readonly string[]).includes(field)) {
            setError(field as Path<T>, { message: messages[0] })
            mapped = true
          }
        }
        if (mapped) {
          setFormError(null)
          return
        }
      }
      setFormError(getErrorMessage(t, error))
    },
    [fields, setError, t],
  )

  return { formError, setFormError, fieldError, handleError }
}
