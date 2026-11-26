import { z } from 'zod'
import { isValidIsbn, normalizeIsbn } from './isbn'
import { BOOK_STATUSES, type BookDetail, type BookInput } from './types'

function isHttpUrl(value: string) {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}

/**
 * Book form values. Number fields are kept as strings while editing and converted on submit.
 * Messages are i18n keys. Mirrors the API rules (docs/api/api-design.md §11).
 */
export const bookFormSchema = z
  .object({
    title: z.string().trim().min(1, 'books.validation.required').max(300, 'books.validation.tooLong'),
    author: z.string().trim().min(1, 'books.validation.required').max(300, 'books.validation.tooLong'),
    isbn: z
      .string()
      .trim()
      .refine((value) => value === '' || isValidIsbn(normalizeIsbn(value)), 'books.validation.isbn'),
    pageCount: z.string().trim().regex(/^[1-9]\d{0,5}$/, 'books.validation.pageCount'),
    currentPage: z.string().trim().regex(/^\d{1,6}$/, 'books.validation.currentPage'),
    status: z.enum(BOOK_STATUSES),
    coverImage: z
      .string()
      .trim()
      .max(2048, 'books.validation.tooLong')
      .refine((value) => value === '' || isHttpUrl(value), 'books.validation.coverImage'),
    rating: z.string(),
    notes: z.string().max(5000, 'books.validation.tooLong'),
    startDate: z.string(),
    finishDate: z.string(),
  })
  .superRefine((values, context) => {
    if (/^\d+$/.test(values.pageCount) && /^\d+$/.test(values.currentPage)
      && Number(values.currentPage) > Number(values.pageCount)) {
      context.addIssue({ code: 'custom', path: ['currentPage'], message: 'books.validation.currentPageMax' })
    }
    if (values.startDate && values.finishDate && values.finishDate < values.startDate) {
      context.addIssue({ code: 'custom', path: ['finishDate'], message: 'books.validation.finishBeforeStart' })
    }
  })

export type BookFormValues = z.infer<typeof bookFormSchema>

export const emptyBookForm: BookFormValues = {
  title: '',
  author: '',
  isbn: '',
  pageCount: '',
  currentPage: '0',
  status: 'TO_READ',
  coverImage: '',
  rating: '',
  notes: '',
  startDate: '',
  finishDate: '',
}

export function toFormValues(book: BookDetail): BookFormValues {
  return {
    title: book.title,
    author: book.author,
    isbn: book.isbn ?? '',
    pageCount: String(book.pageCount),
    currentPage: String(book.currentPage),
    status: book.status,
    coverImage: book.coverImage ?? '',
    rating: book.rating === null ? '' : String(book.rating),
    notes: book.notes ?? '',
    startDate: book.startDate ?? '',
    finishDate: book.finishDate ?? '',
  }
}

export function toBookInput(values: BookFormValues): BookInput {
  const optional = (value: string) => (value.trim() === '' ? null : value.trim())

  return {
    title: values.title.trim(),
    author: values.author.trim(),
    isbn: optional(values.isbn),
    pageCount: Number(values.pageCount),
    currentPage: Number(values.currentPage),
    status: values.status,
    coverImage: optional(values.coverImage),
    rating: values.rating === '' ? null : Number(values.rating),
    notes: optional(values.notes),
    startDate: optional(values.startDate),
    finishDate: optional(values.finishDate),
  }
}
