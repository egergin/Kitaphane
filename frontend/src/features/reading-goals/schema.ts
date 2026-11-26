import { z } from 'zod'
import { READING_GOAL_TYPES, type ReadingGoal, type ReadingGoalInput } from './types'

/** Mirrors the API rules: positive target (max 1,000,000), both dates, end after start. */
export const readingGoalSchema = z
  .object({
    type: z.enum(READING_GOAL_TYPES),
    target: z
      .string()
      .trim()
      .min(1, 'readingGoals.validation.target')
      .regex(/^\d+$/, 'readingGoals.validation.target')
      .refine((value) => Number(value) >= 1 && Number(value) <= 1_000_000, 'readingGoals.validation.target'),
    startDate: z.string().min(1, 'readingGoals.validation.date'),
    endDate: z.string().min(1, 'readingGoals.validation.date'),
  })
  .refine((values) => !values.startDate || !values.endDate || values.endDate > values.startDate, {
    path: ['endDate'],
    message: 'readingGoals.validation.endAfterStart',
  })

export type ReadingGoalFormValues = z.infer<typeof readingGoalSchema>

/** A new goal defaults to a book count for the current calendar year. */
export function newGoalValues(today = new Date()): ReadingGoalFormValues {
  const year = today.getFullYear()
  return { type: 'BOOK_COUNT', target: '', startDate: `${year}-01-01`, endDate: `${year}-12-31` }
}

export function toFormValues(goal: ReadingGoal): ReadingGoalFormValues {
  return { type: goal.type, target: String(goal.target), startDate: goal.startDate, endDate: goal.endDate }
}

export function toInput(values: ReadingGoalFormValues): ReadingGoalInput {
  return { type: values.type, target: Number(values.target), startDate: values.startDate, endDate: values.endDate }
}
