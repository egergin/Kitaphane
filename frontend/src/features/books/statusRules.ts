import type { BookStatus } from './types'

type RuleFields = {
  status: BookStatus
  pageCount: number
  currentPage: number
  startDate: string | null
  finishDate: string | null
}

/** Today in the user's local time zone as yyyy-MM-dd. */
export function localToday(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

/**
 * Client-side preview of the backend status rules (docs/api/api-design.md §13), so the form shows
 * what will be saved and dates use the user's local "today". The backend remains the source of truth.
 */
export function applyStatusRules<T extends RuleFields>(values: T, previousStatus: BookStatus | null, today = localToday()): T {
  const next = { ...values }

  if (next.status === 'READING' && previousStatus !== 'READING' && !next.startDate) {
    next.startDate = today
  }

  if (next.status === 'COMPLETED') {
    next.currentPage = next.pageCount
    next.finishDate ??= today
  } else {
    next.finishDate = null
  }

  return next
}
