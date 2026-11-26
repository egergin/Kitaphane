import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { booksApi } from './api'
import type { BookInput, BookStatus } from './types'

export const bookKeys = {
  all: ['books'] as const,
  list: (pageSize: number, search: string) => ['books', 'list', pageSize, search] as const,
  completed: (pageSize: number, search: string) => ['books', 'completed', pageSize, search] as const,
  detail: (id: string) => ['books', 'detail', id] as const,
}

/** Home page list: further pages are appended while scrolling. */
export function useBookList(pageSize: number, search = '') {
  return useInfiniteQuery({
    queryKey: bookKeys.list(pageSize, search),
    queryFn: ({ pageParam }) => booksApi.list(pageParam, pageSize, search),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasNext ? lastPage.page + 1 : undefined),
  })
}

/** "Okuduğum Kitaplar" list, loaded batch by batch while scrolling. */
export function useCompletedBooks(pageSize: number, search = '') {
  return useInfiniteQuery({
    queryKey: bookKeys.completed(pageSize, search),
    queryFn: ({ pageParam }) => booksApi.completed(pageParam, pageSize, search),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasNext ? lastPage.page + 1 : undefined),
  })
}

/** Status-only change (e.g. taking a book off the "read" list). Refreshes every book list. */
export function useUpdateBookStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: BookStatus }) => booksApi.updateStatus(id, status),
    onSuccess: (book) => {
      queryClient.setQueryData(bookKeys.detail(book.id), book)
      return queryClient.invalidateQueries({ queryKey: bookKeys.all })
    },
  })
}

export function useBook(id: string) {
  return useQuery({ queryKey: bookKeys.detail(id), queryFn: () => booksApi.get(id) })
}

export function useCreateBook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (book: BookInput) => booksApi.create(book),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: bookKeys.all }),
  })
}

export function useUpdateBook(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (book: BookInput) => booksApi.update(id, book),
    onSuccess: (book) => {
      queryClient.setQueryData(bookKeys.detail(id), book)
      return queryClient.invalidateQueries({ queryKey: bookKeys.all })
    },
  })
}

export function useDeleteBook(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => booksApi.remove(id),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: bookKeys.detail(id) })
      return queryClient.invalidateQueries({ queryKey: bookKeys.all })
    },
  })
}
