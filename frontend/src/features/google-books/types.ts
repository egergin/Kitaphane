export type GoogleBookResult = {
  googleBooksId: string
  title: string
  author: string | null
  pageCount: number | null
  isbn: string | null
  coverImage: string | null
}

export type GoogleBooksSearchMode = 'text' | 'isbn'
