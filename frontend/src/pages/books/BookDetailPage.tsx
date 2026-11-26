import { PencilIcon } from 'lucide-react'
import { type CSSProperties, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { PageLoader } from '@/components/PageLoader'
import { PenpotBackButton } from '@/components/PenpotBackButton'
import { BookCover } from '@/features/books/BookCover'
import { BookNotes } from '@/features/books/BookNotes'
import { formatDate } from '@/features/books/dates'
import { useBook } from '@/features/books/hooks'
import { RatingStars } from '@/features/books/RatingStars'
import { StatusBadge } from '@/features/books/StatusBadge'
import { useOffsetFromRight } from '@/lib/useAlignWithElement'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex min-w-0 flex-col">
      <dt className="text-[0.9rem] leading-[1.08rem] font-bold">{label}</dt>
      <dd className="mt-[0.135rem] truncate text-[0.9rem] leading-[1.08rem] font-medium">{value}</dd>
    </div>
  )
}

/*
 * Penpot "Kitap Bilgileri 1 / 2" (1920px design), shown at 90% of the design's sizes:
 * values are Penpot px / 20 × 0.9 in rem (the app's 125% scale makes 1rem = 20px). Design values:
 *   back 50px arrow + 24/600 · 50px below: cover 312×457 (radius 10, shadow) · 50px gap ·
 *   title 32/700 · rating row 44px · 20px below: status 206×50 · 50px below: fields
 *   (row 1 Yazar / ISBN, row 2 Toplam Sayfa / Okunan Sayfa / Başlangıç / Bitiş; columns in the ratio
 *   285 / 288 / 302 / 105, rows 22px apart; label 20/700, value 20/500, 3px apart) · 52px below: notes.
 *   The fields and notes stretch to the left edge of the header's profile picture, as in the design.
 * The screen is display only; everything (rating and status included) is changed from the edit form.
 */
export function BookDetailPage() {
  const { t } = useTranslation()
  const { id = '' } = useParams()
  const book = useBook(id)
  const infoRef = useRef<HTMLDivElement>(null)
  // As in Penpot, the info column (fields and notes) ends where the header's profile picture starts.
  const avatarGap = useOffsetFromRight(infoRef, '[data-user-avatar]', 'left')

  if (book.isPending) return <PageLoader />
  if (book.isError) {
    return (
      <section className="flex flex-col gap-[2.25rem]">
        <PenpotBackButton />
        <ErrorState error={book.error} />
      </section>
    )
  }

  const data = book.data

  return (
    <section className="flex flex-col">
      <div className="flex items-center justify-between gap-4">
        <PenpotBackButton />
        <Link
          to={`/books/${data.id}/edit`}
          className="inline-flex h-[1.8rem] items-center gap-[0.36rem] rounded-[0.45rem] border border-foreground/70 px-[0.63rem] text-[0.675rem] font-semibold transition-colors hover:bg-white/60 focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
        >
          <PencilIcon className="size-[0.81rem]" aria-hidden />
          {t('books.detail.edit')}
        </Link>
      </div>

      <div className="mt-[2.25rem] flex flex-col gap-[2.25rem] md:flex-row">
        <div className="w-[9rem] shrink-0 self-start md:w-[14.04rem]">
          <BookCover
            src={data.coverImage}
            title={data.title}
            className="rounded-[0.45rem] shadow-[-0.675rem_0.45rem_1.35rem_rgba(0,0,0,0.25)]"
          />
        </div>

        <div
          ref={infoRef}
          className="flex min-w-0 flex-1 flex-col lg:pr-[var(--avatar-gap,0px)]"
          style={avatarGap !== null && avatarGap > 0 ? ({ '--avatar-gap': `${avatarGap}px` } as CSSProperties) : undefined}
        >
          <h1 className="text-[1.44rem] leading-[1.728rem] font-bold break-words">{data.title}</h1>
          <RatingStars rating={data.rating} className="mt-[0.045rem] text-[0.855rem] md:text-[1.08rem]" />

          <div className="mt-[0.9rem]">
            <StatusBadge status={data.status} />
          </div>

          <dl className="mt-[2.25rem] grid grid-cols-2 gap-x-[0.9rem] gap-y-[0.99rem] lg:grid-cols-[minmax(0,285fr)_minmax(0,288fr)_minmax(0,302fr)_minmax(max-content,105fr)] lg:gap-x-0">
            <Field label={t('books.fields.author')} value={data.author} />
            <div className="lg:col-span-3">
              <Field label={t('books.fields.isbn')} value={data.isbn ?? '—'} />
            </div>
            <Field label={t('books.fields.pageCount')} value={String(data.pageCount)} />
            <Field label={t('books.fields.currentPage')} value={String(data.currentPage)} />
            <Field label={t('books.fields.startDate')} value={formatDate(data.startDate)} />
            <Field label={t('books.fields.finishDate')} value={formatDate(data.finishDate)} />
          </dl>

          {data.notes && (
            <div className="mt-[2.34rem]">
              <BookNotes notes={data.notes} />
            </div>
          )}
        </div>
      </div>
    </section>
  )
}
