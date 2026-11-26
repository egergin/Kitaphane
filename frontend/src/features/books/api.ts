import { http } from '@/lib/http'
import type { PagedResult } from '@/lib/paging'
import type { BookDetail, BookInput, BookListItem, BookStatus, CompletedBook } from './types'

export const booksApi = {
  list: (page: number, pageSize: number, search = '') => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search) params.set('search', search)
    return http.get<PagedResult<BookListItem>>(`/books?${params}`)
  },
  completed: (page: number, pageSize: number, search = '') => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search) params.set('search', search)
    return http.get<PagedResult<CompletedBook>>(`/books/completed?${params}`)
  },
  updateStatus: (id: string, status: BookStatus) =>
    http.put<BookDetail>(`/books/${encodeURIComponent(id)}/status`, { status }),
  get: (id: string) => http.get<BookDetail>(`/books/${encodeURIComponent(id)}`),
  create: (book: BookInput) => http.post<BookDetail>('/books', book),
  update: (id: string, book: BookInput) => http.put<BookDetail>(`/books/${encodeURIComponent(id)}`, book),
  remove: (id: string) => http.delete<void>(`/books/${encodeURIComponent(id)}`),
}
