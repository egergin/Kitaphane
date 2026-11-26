import { http } from '@/lib/http'
import type { ReadingGoal, ReadingGoalInput } from './types'

export const readingGoalsApi = {
  list: () => http.get<ReadingGoal[]>('/reading-goals'),
  create: (input: ReadingGoalInput) => http.post<ReadingGoal>('/reading-goals', input),
  update: (id: string, input: ReadingGoalInput) => http.put<ReadingGoal>(`/reading-goals/${id}`, input),
  remove: (id: string) => http.delete<void>(`/reading-goals/${id}`),
}
