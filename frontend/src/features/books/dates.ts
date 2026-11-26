/** yyyy-MM-dd → dd/MM/yyyy, the date format used in the Penpot designs. */
export function formatDate(value: string | null): string {
  if (!value) return '—'
  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}
