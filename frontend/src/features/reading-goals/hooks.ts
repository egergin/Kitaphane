import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { readingGoalsApi } from './api'
import type { ReadingGoalInput } from './types'

// Progress is computed from books, so the list is refetched whenever the profile page is opened.
export const readingGoalKeys = { all: ['reading-goals'] as const }

export function useReadingGoals() {
  return useQuery({ queryKey: readingGoalKeys.all, queryFn: readingGoalsApi.list })
}

export function useSaveReadingGoal() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, input }: { id: string | null; input: ReadingGoalInput }) =>
      id ? readingGoalsApi.update(id, input) : readingGoalsApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: readingGoalKeys.all }),
  })
}

export function useDeleteReadingGoal() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => readingGoalsApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: readingGoalKeys.all }),
  })
}
