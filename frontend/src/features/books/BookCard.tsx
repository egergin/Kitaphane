import { Link } from 'react-router'
import { BookCover } from './BookCover'
import { ReadingProgress } from './ReadingProgress'
import type { BookListItem } from './types'

/** Home page card: cover, title, author and reading percentage only. */
export function BookCard({ book }: { book: BookListItem }) {
  return (
    <Link
      to={`/books/${book.id}`}
      className="group flex flex-col gap-3 rounded-lg outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
    >
      <BookCover src={book.coverImage} title={book.title} className="transition-transform group-hover:-translate-y-1" />
      <div className="flex flex-col">
        <h2 className="truncate text-[0.9375rem] leading-5 font-bold" title={book.title}>
          {book.title}
        </h2>
        <p className="mt-0.5 truncate text-sm leading-5 text-foreground/80" title={book.author}>
          {book.author}
        </p>
        <div className="mt-1.5">
          <ReadingProgress currentPage={book.currentPage} pageCount={book.pageCount} />
        </div>
      </div>
    </Link>
  )
}

export function BookCardSkeleton() {
  return (
    <div className="flex animate-pulse flex-col gap-3" aria-hidden>
      <div className="aspect-[219/321] w-full rounded-lg bg-muted" />
      <div className="flex flex-col gap-1.5">
        <div className="h-4 w-4/5 rounded bg-muted" />
        <div className="h-4 w-3/5 rounded bg-muted" />
        <div className="h-4 w-full rounded bg-muted" />
      </div>
    </div>
  )
}
