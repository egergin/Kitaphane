export const READING_GOAL_TYPES = ['BOOK_COUNT', 'PAGE_COUNT'] as const
export type ReadingGoalType = (typeof READING_GOAL_TYPES)[number]

export type ReadingGoal = {
  id: string
  type: ReadingGoalType
  target: number
  startDate: string
  endDate: string
  /** Real progress; may be above the target. */
  current: number
  /** current / target, rounded down; not capped at 100. */
  percentage: number
}

export type ReadingGoalInput = {
  type: ReadingGoalType
  target: number
  startDate: string
  endDate: string
}
