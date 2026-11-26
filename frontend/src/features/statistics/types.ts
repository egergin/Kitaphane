export type StatisticsOverview = {
  totalBooks: number
  completedBooks: number
  readingBooks: number
  toReadBooks: number
}

export const READING_PERIODS = ['1m', '6m', '1y'] as const
export type ReadingPeriod = (typeof READING_PERIODS)[number]

export type ReadingStatistics = {
  period: ReadingPeriod
  periodStart: string
  periodEnd: string
  completedBooks: number
  pagesRead: number
  averageBookLength: number
  booksByAuthor: { author: string; count: number }[]
}
