import type { TFunction } from 'i18next'
import { ApiError } from '@/lib/http'
import tr from '@/locales/tr.json'

type ErrorCode = keyof typeof tr.errors

function isKnownCode(code: string): code is ErrorCode {
  return Object.hasOwn(tr.errors, code)
}

/** Turkish user-facing message for any thrown error, chosen by the API error code. */
export function getErrorMessage(t: TFunction, error: unknown): string {
  if (error instanceof ApiError && isKnownCode(error.code)) {
    return t(`errors.${error.code}`)
  }
  return t('errors.default')
}
