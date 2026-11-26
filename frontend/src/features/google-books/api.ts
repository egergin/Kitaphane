import { useQuery } from '@tanstack/react-query'
import { http } from '@/lib/http'
import type { GoogleBookResult, GoogleBooksSearchMode } from './types'

export const googleBooksApi = {
  search: (query: string) => http.get<GoogleBookResult[]>(`/google-books/search?query=${encodeURIComponent(query)}`),
  byIsbn: (isbn: string) => http.get<GoogleBookResult[]>(`/google-books/isbn/${encodeURIComponent(isbn)}`),
}

export type GoogleBooksSearch = { mode: GoogleBooksSearchMode; term: string }

/** Runs only after the user submits a search (search is null until then). */
export function useGoogleBooksSearch(search: GoogleBooksSearch | null) {
  return useQuery({
    queryKey: ['google-books', search?.mode, search?.term],
    queryFn: () => (search!.mode === 'isbn' ? googleBooksApi.byIsbn(search!.term) : googleBooksApi.search(search!.term)),
    enabled: search !== null,
    staleTime: 10 * 60 * 1000,
    retry: false,
  })
}
