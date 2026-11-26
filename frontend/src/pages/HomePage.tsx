import { Loader2Icon } from 'lucide-react'
import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { SEARCH_PARAM } from '@/components/app/HeaderSearch'
import { ErrorState } from '@/components/ErrorState'
import { AddBookCard } from '@/features/books/AddBookCard'
import { BookCard, BookCardSkeleton } from '@/features/books/BookCard'
import { useBookList } from '@/features/books/hooks'
import { BOOKS_PAGE_SIZE } from '@/features/books/pageSize'
import { useInfiniteScroll } from '@/lib/useInfiniteScroll'

/** Book grid: up to 170px (at 100%) wide cards, left aligned (two columns on phones). */
const gridClassName =
  'grid grid-cols-2 gap-x-4 gap-y-8 sm:grid-cols-[repeat(auto-fill,minmax(8.75rem,10.625rem))] sm:gap-x-6 sm:gap-y-8'

export function HomePage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const search = (searchParams.get(SEARCH_PARAM) ?? '').trim()
  const books = useBookList(BOOKS_PAGE_SIZE, search)
  const sentinelRef = useRef<HTMLDivElement>(null)

  const { isFetchingNextPage } = books
  useInfiniteScroll(sentinelRef, books)

  const items = books.data?.pages.flatMap((page) => page.items) ?? []
  const totalCount = books.data?.pages[0]?.totalCount ?? 0

  return (
    <section className="relative flex flex-col gap-6">
      <h1 className="sr-only">{t('home.title')}</h1>

      {search && books.isSuccess && (
        <p className="text-sm text-muted-foreground" aria-live="polite">
          {t('home.searchResults', { count: totalCount, term: search })}
        </p>
      )}

      {books.isPending ? (
        <div className={gridClassName}>
          {Array.from({ length: 6 }, (_, index) => (
            <BookCardSkeleton key={index} />
          ))}
        </div>
      ) : books.isError ? (
        <ErrorState error={books.error} onRetry={() => void books.refetch()} />
      ) : search && totalCount === 0 ? (
        <p className="text-base text-muted-foreground">{t('home.noSearchResults', { term: search })}</p>
      ) : (
        <>
          {totalCount === 0 && <p className="text-base text-muted-foreground">{t('home.empty')}</p>}

          <ul className={gridClassName}>
            {items.map((book) => (
              <li key={book.id}>
                <BookCard book={book} />
              </li>
            ))}
            {!search && (
              <li>
                <AddBookCard />
              </li>
            )}
          </ul>

          <div ref={sentinelRef} aria-hidden className="h-px" />
          {isFetchingNextPage && (
            <div className="flex justify-center py-4 text-muted-foreground" role="status">
              <Loader2Icon className="size-6 animate-spin" aria-hidden />
              <span className="sr-only">{t('common.loading')}</span>
            </div>
          )}
        </>
      )}
    </section>
  )
}
