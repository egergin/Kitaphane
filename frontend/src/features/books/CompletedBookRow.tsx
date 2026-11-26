import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { BookCover } from './BookCover'
import { formatDate } from './dates'
import { RatingStars } from './RatingStars'
import type { CompletedBook } from './types'

type CompletedBookRowProps = {
  book: CompletedBook
  onRemove: (book: CompletedBook) => void
}

/*
 * Penpot "Okunan Kitaplar" row (1920px design). Values are Penpot px / 20 in rem, so on the app's
 * 125% scale (0.9rem = 20px) they render at the design's pixel sizes:
 *   cover 219×321 (radius 10, shadow -15/10/30 25%) · 50px gap · title 28/700
 *   rating row 44px (5px below the title) · fields 40px below: label 20/700, value 20/500 (5px apart),
 *   columns 100px apart · X: 40px box with a 25px glyph, 141px from the row top, at the row's right edge.
 */

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex min-w-0 flex-col">
      <dt className="text-[0.72rem] leading-[1.08rem] font-bold sm:text-[0.9rem]">{label}</dt>
      <dd className="mt-[0.225rem] truncate text-[0.72rem] leading-[1.08rem] font-medium sm:text-[0.9rem]">{value}</dd>
    </div>
  )
}

/** X icon exported from Penpot (filled path with a 1px outer stroke). */
function CloseIcon() {
  return (
    <svg viewBox="0 0 40 40" className="size-[1.8rem]" aria-hidden>
      <path
        d="M32.134 30.366a1.25 1.25 0 0 1-1.768 1.768L20 21.768 9.634 32.134a1.25 1.25 0 0 1-1.768-1.768L18.233 20 7.866 9.634a1.25 1.25 0 0 1 1.768-1.768L20 18.233 30.366 7.866a1.25 1.25 0 0 1 1.768 1.768L21.767 20Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function CompletedBookRow({ book, onRemove }: CompletedBookRowProps) {
  const { t } = useTranslation()

  return (
    <article className="relative flex gap-[0.9rem] sm:gap-[2.25rem]">
      <Link
        to={`/books/${book.id}`}
        className="w-[5.4rem] shrink-0 rounded-[0.45rem] outline-none focus-visible:ring-3 focus-visible:ring-ring/50 sm:w-[9.855rem]"
        tabIndex={-1}
        aria-hidden
      >
        <BookCover
          src={book.coverImage}
          title={book.title}
          className="rounded-[0.45rem] shadow-[-0.675rem_0.45rem_1.35rem_rgba(0,0,0,0.25)]"
        />
      </Link>

      <div className="flex min-w-0 flex-1 flex-col pr-[2.25rem] sm:pr-[calc(var(--close-center-right,3.4rem)+1.35rem)]">
        <h2 className="text-[0.99rem] leading-[1.26rem] font-bold sm:text-[1.26rem] sm:leading-[1.53rem]">
          <Link to={`/books/${book.id}`} className="hover:underline focus-visible:underline focus-visible:outline-none">
            {book.title}
          </Link>
        </h2>
        <div className="mt-[0.225rem]">
          <RatingStars rating={book.rating} className="text-[1.08rem]" />
        </div>
        <dl className="mt-[0.9rem] grid grid-cols-2 gap-x-[0.9rem] gap-y-[0.675rem] sm:mt-[1.8rem] sm:flex sm:flex-wrap sm:gap-x-[4.5rem] sm:gap-y-[0.9rem]">
          <Field label={t('books.fields.author')} value={book.author} />
          <Field label={t('books.fields.isbn')} value={book.isbn ?? '—'} />
          <Field label={t('books.readList.pageCount')} value={String(book.pageCount)} />
          <Field label={t('books.fields.finishDate')} value={formatDate(book.finishDate)} />
        </dl>
      </div>

      <button
        type="button"
        onClick={() => onRemove(book)}
        aria-label={t('books.readList.remove', { title: book.title })}
        title={t('books.readList.removeShort')}
        className="absolute top-0 right-0 flex size-[1.8rem] items-center justify-center rounded-full text-foreground transition-colors hover:bg-muted focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none sm:top-[6.345rem] sm:right-[calc(var(--close-center-right,3.4rem)-0.9rem)]"
      >
        <CloseIcon />
      </button>
    </article>
  )
}

export function CompletedBookRowSkeleton() {
  return (
    <div className="flex animate-pulse gap-[0.9rem] sm:gap-[2.25rem]" aria-hidden>
      <div className="aspect-[219/321] w-[5.4rem] shrink-0 rounded-[0.45rem] bg-muted sm:w-[9.855rem]" />
      <div className="flex flex-1 flex-col gap-[0.45rem] pt-[0.225rem]">
        <div className="h-[1.53rem] w-2/5 rounded bg-muted" />
        <div className="h-[1.08rem] w-1/4 rounded bg-muted" />
        <div className="mt-[1.8rem] h-[2.385rem] w-3/5 rounded bg-muted" />
      </div>
    </div>
  )
}
