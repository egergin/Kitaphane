export const BOOK_STATUSES = ['TO_READ', 'READING', 'COMPLETED'] as const
export type BookStatus = (typeof BOOK_STATUSES)[number]

export type BookListItem = {
  id: string
  title: string
  author: string
  coverImage: string | null
  currentPage: number
  pageCount: number
}

export type BookDetail = {
  id: string
  title: string
  author: string
  isbn: string | null
  pageCount: number
  currentPage: number
  status: BookStatus
  coverImage: string | null
  rating: number | null
  notes: string | null
  /** yyyy-MM-dd */
  startDate: string | null
  /** yyyy-MM-dd */
  finishDate: string | null
  createdAt: string
  updatedAt: string
}

/** Row of the "Okuduğum Kitaplar" list. */
export type CompletedBook = {
  id: string
  title: string
  author: string
  coverImage: string | null
  isbn: string | null
  pageCount: number
  rating: number | null
  /** yyyy-MM-dd */
  finishDate: string | null
}

/** Body of POST /api/books and PUT /api/books/{id}. */
export type BookInput = Omit<BookDetail, 'id' | 'createdAt' | 'updatedAt'>
