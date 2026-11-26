/** Mirrors Kitaphane.Domain.Books.Isbn: removes hyphens/spaces, upper-cases X. */
export function normalizeIsbn(value: string): string {
  return value.replace(/[-\s]/g, '').toUpperCase()
}

/** ISBN-10 or ISBN-13 with a valid check digit. */
export function isValidIsbn(normalized: string): boolean {
  if (/^\d{9}[\dX]$/.test(normalized)) {
    const sum = [...normalized].reduce(
      (total, char, index) => total + (char === 'X' ? 10 : Number(char)) * (10 - index),
      0,
    )
    return sum % 11 === 0
  }
  if (/^\d{13}$/.test(normalized)) {
    const sum = [...normalized].reduce((total, char, index) => total + Number(char) * (index % 2 === 0 ? 1 : 3), 0)
    return sum % 10 === 0
  }
  return false
}
