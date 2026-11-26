import { http } from '@/lib/http'
import type { ReadingPeriod, ReadingStatistics, StatisticsOverview } from './types'

export const statisticsApi = {
  overview: () => http.get<StatisticsOverview>('/statistics/overview'),
  reading: (period: ReadingPeriod) => http.get<ReadingStatistics>(`/statistics/reading?period=${period}`),
}
