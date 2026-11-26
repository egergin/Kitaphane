/** (CurrentPage / PageCount) * 100, rounded and clamped to 0–100; 0 when PageCount is 0. */
export function readingPercentage(currentPage: number, pageCount: number): number {
  if (pageCount <= 0) return 0
  return Math.min(100, Math.max(0, Math.round((currentPage / pageCount) * 100)))
}

/** Locale-aware percent text: "%60" in Turkish, "60%" in English. */
export function formatPercent(percentage: number, language: string): string {
  return new Intl.NumberFormat(language, { style: 'percent', maximumFractionDigits: 0 }).format(percentage / 100)
}
