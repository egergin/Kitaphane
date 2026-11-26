import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { statisticsApi } from './api'
import type { ReadingPeriod } from './types'

// Statistics are computed from books, so they are refetched whenever the profile page is opened.
export const statisticsKeys = {
  all: ['statistics'] as const,
  overview: () => [...statisticsKeys.all, 'overview'] as const,
  reading: (period: ReadingPeriod) => [...statisticsKeys.all, 'reading', period] as const,
}

export function useStatisticsOverview() {
  return useQuery({ queryKey: statisticsKeys.overview(), queryFn: statisticsApi.overview })
}

export function useReadingStatistics(period: ReadingPeriod) {
  return useQuery({
    queryKey: statisticsKeys.reading(period),
    queryFn: () => statisticsApi.reading(period),
    placeholderData: keepPreviousData,
  })
}
