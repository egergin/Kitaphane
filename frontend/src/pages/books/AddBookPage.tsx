import { type CSSProperties, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { PenpotBackButton } from '@/components/PenpotBackButton'
import { BookForm } from '@/features/books/BookForm'
import { useCreateBook } from '@/features/books/hooks'
import { type BookFormValues, emptyBookForm } from '@/features/books/schema'
import { GoogleBooksSearch } from '@/features/google-books/GoogleBooksSearch'
import type { GoogleBookResult } from '@/features/google-books/types'
import { useOffsetFromRight } from '@/lib/useAlignWithElement'

function fromGoogleBook(result: GoogleBookResult): BookFormValues {
  return {
    ...emptyBookForm,
    title: result.title,
    author: result.author ?? '',
    pageCount: result.pageCount ? String(result.pageCount) : '',
    isbn: result.isbn ?? '',
    coverImage: result.coverImage ?? '',
  }
}

/**
 * New book form with Google Books search. Like the edit screen, the content spans to the left edge of
 * the header avatar and is shown at 90% size (the back button keeps its shared size).
 */
export function AddBookPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createBook = useCreateBook()
  const formRef = useRef<HTMLDivElement>(null)
  const contentRef = useRef<HTMLDivElement>(null)
  const avatarGap = useOffsetFromRight(contentRef, '[data-user-avatar]', 'left')
  // Selecting a Google Books result re-creates the form with the suggested values.
  const [formState, setFormState] = useState({ key: 0, values: emptyBookForm })

  const selectResult = (result: GoogleBookResult) => {
    setFormState((current) => ({ key: current.key + 1, values: fromGoogleBook(result) }))
    toast.info(t('googleBooks.filled'))
    if (!result.pageCount || !result.author) toast.warning(t('googleBooks.missingFields'))
    requestAnimationFrame(() => formRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' }))
  }

  return (
    <section className="flex flex-col gap-6">
      <PenpotBackButton />
      <div
        ref={contentRef}
        className="lg:pr-[var(--avatar-gap,0px)]"
        style={avatarGap !== null && avatarGap > 0 ? ({ '--avatar-gap': `${avatarGap}px` } as CSSProperties) : undefined}
      >
        <div className="flex flex-col gap-6 [zoom:0.9]">
          <h1 className="text-2xl leading-8 font-semibold">{t('books.addTitle')}</h1>

          <GoogleBooksSearch onSelect={selectResult} />

          <div ref={formRef} className="scroll-mt-6">
            <BookForm
              key={formState.key}
              defaultValues={formState.values}
              savedStatus={null}
              submitLabel={t('books.form.save')}
              onSubmit={async (book) => {
                await createBook.mutateAsync(book)
                toast.success(t('books.form.created'))
                navigate('/', { replace: true })
              }}
            />
          </div>
        </div>
      </div>
    </section>
  )
}
