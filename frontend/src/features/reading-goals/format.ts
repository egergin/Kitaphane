import type { TFunction } from 'i18next'
import type { ReadingGoal } from './types'

/** yyyy-MM-dd → "1 Oca 2026" / "Jan 1, 2026" (parsed as a calendar date, no time zone shift). */
export function formatGoalDate(value: string, language: string): string {
  const [year, month, day] = value.split('-').map(Number)
  return new Intl.DateTimeFormat(language, { day: 'numeric', month: 'short', year: 'numeric' }).format(
    new Date(year, month - 1, day),
  )
}

/** Goal title from its range, target and type, e.g. "1 Oca 2026 – 31 Ara 2026: 30 kitap". */
export function goalTitle(goal: Pick<ReadingGoal, 'type' | 'target' | 'startDate' | 'endDate'>, t: TFunction, language: string) {
  return t('readingGoals.title', {
    start: formatGoalDate(goal.startDate, language),
    end: formatGoalDate(goal.endDate, language),
    amount: goalAmount(goal.type, goal.target, t, language),
  })
}

/** "30 kitap" / "10.000 sayfa". */
export function goalAmount(type: ReadingGoal['type'], value: number, t: TFunction, language: string) {
  const count = new Intl.NumberFormat(language).format(value)
  return type === 'BOOK_COUNT' ? t('readingGoals.books', { count: value, formatted: count }) : t('readingGoals.pages', { count: value, formatted: count })
}
