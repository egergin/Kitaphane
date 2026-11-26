import { Loader2Icon } from 'lucide-react'
import { type CSSProperties, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { toast } from 'sonner'
import { SEARCH_PARAM } from '@/components/app/HeaderSearch'
import { ErrorState } from '@/components/ErrorState'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { CompletedBookRow, CompletedBookRowSkeleton } from '@/features/books/CompletedBookRow'
import { useCompletedBooks, useUpdateBookStatus } from '@/features/books/hooks'
import { BOOKS_PAGE_SIZE } from '@/features/books/pageSize'
import type { CompletedBook } from '@/features/books/types'
import { getErrorMessage } from '@/lib/errors'
import { useOffsetFromRight } from '@/lib/useAlignWithElement'
import { useInfiniteScroll } from '@/lib/useInfiniteScroll'

/**
 * Penpot "Okunan Kitaplar": completed books, most recently finished first. The header search (?q=)
 * filters this list by title or author.
 */
export function ReadBooksPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const search = (searchParams.get(SEARCH_PARAM) ?? '').trim()
  const books = useCompletedBooks(BOOKS_PAGE_SIZE, search)
  const updateStatus = useUpdateBookStatus()
  const sentinelRef = useRef<HTMLDivElement>(null)
  const listRef = useRef<HTMLUListElement>(null)
  // X buttons are centered under the header's TR/EN language switcher (its position depends on the user name).
  const closeCenterRight = useOffsetFromRight(listRef, '[data-language-switcher]', 'center')
  const [bookToRemove, setBookToRemove] = useState<CompletedBook | null>(null)

  useInfiniteScroll(sentinelRef, books)

  const items = books.data?.pages.flatMap((page) => page.items) ?? []
  const totalCount = books.data?.pages[0]?.totalCount ?? 0

  const removeFromList = async () => {
    if (!bookToRemove) return
    try {
      await updateStatus.mutateAsync({ id: bookToRemove.id, status: 'READING' })
      toast.success(t('books.readList.removed', { title: bookToRemove.title }))
      setBookToRemove(null)
    } catch (error) {
      toast.error(getErrorMessage(t, error))
    }
  }

  return (
    <section className="flex flex-col">
      {/* Penpot: "OKUDUĞUM KİTAPLAR" 28/700 uppercase, rows start 30px below; rows 50px apart (shown at 90%).
          Rows span the content width so each X sits as far from the right edge as the covers sit from the sidebar. */}
      <h1 className="text-[1.26rem] leading-[1.53rem] font-bold uppercase">{t('nav.readBooks')}</h1>

      {search && books.isSuccess && totalCount > 0 && (
        <p className="mt-[0.45rem] text-sm text-muted-foreground" aria-live="polite">
          {t('books.readList.searchResults', { count: totalCount, term: search })}
        </p>
      )}

      <div className="mt-[1.35rem]">
        {books.isPending ? (
          <div className="flex flex-col gap-[2.25rem]">
            <CompletedBookRowSkeleton />
            <CompletedBookRowSkeleton />
          </div>
        ) : books.isError ? (
          <ErrorState error={books.error} onRetry={() => void books.refetch()} />
        ) : items.length === 0 ? (
          <p className="text-base text-muted-foreground">
            {search ? t('books.readList.noSearchResults', { term: search }) : t('books.readList.empty')}
          </p>
        ) : (
          <>
            <ul
              ref={listRef}
              className="flex flex-col gap-[2.25rem]"
              style={closeCenterRight === null ? undefined : ({ '--close-center-right': `${closeCenterRight}px` } as CSSProperties)}
            >
              {items.map((book) => (
                <li key={book.id}>
                  <CompletedBookRow book={book} onRemove={setBookToRemove} />
                </li>
              ))}
            </ul>
            <div ref={sentinelRef} aria-hidden className="h-px" />
            {books.isFetchingNextPage && (
              <div className="flex justify-center py-4 text-muted-foreground" role="status">
                <Loader2Icon className="size-6 animate-spin" aria-hidden />
                <span className="sr-only">{t('common.loading')}</span>
              </div>
            )}
          </>
        )}
      </div>

      <AlertDialog open={bookToRemove !== null} onOpenChange={(open) => !open && setBookToRemove(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('books.readList.confirmTitle')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('books.readList.confirmDescription', { title: bookToRemove?.title ?? '' })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              disabled={updateStatus.isPending}
              onClick={(event) => {
                event.preventDefault()
                void removeFromList()
              }}
            >
              {updateStatus.isPending && <Loader2Icon className="animate-spin" aria-hidden />}
              {t('books.readList.confirm')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </section>
  )
}
