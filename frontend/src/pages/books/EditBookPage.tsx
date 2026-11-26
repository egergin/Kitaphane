import { Loader2Icon, Trash2Icon } from 'lucide-react'
import { type CSSProperties, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { toast } from 'sonner'
import { PenpotBackButton } from '@/components/PenpotBackButton'
import { ErrorState } from '@/components/ErrorState'
import { PageLoader } from '@/components/PageLoader'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { BookForm } from '@/features/books/BookForm'
import { useBook, useDeleteBook, useUpdateBook } from '@/features/books/hooks'
import { toFormValues } from '@/features/books/schema'
import { getErrorMessage } from '@/lib/errors'
import { useOffsetFromRight } from '@/lib/useAlignWithElement'

/**
 * Edit form for a book, opened from the book detail screen's "Düzenle" button.
 * Saving returns to the detail screen; deleting returns to the home page.
 * Like the detail screen, the content spans to the left edge of the header avatar and is shown at
 * 90% size (the back button keeps its shared size).
 */
export function EditBookPage() {
  const { t } = useTranslation()
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const book = useBook(id)
  const updateBook = useUpdateBook(id)
  const deleteBook = useDeleteBook(id)
  const contentRef = useRef<HTMLDivElement>(null)
  const avatarGap = useOffsetFromRight(contentRef, '[data-user-avatar]', 'left')

  if (book.isPending) return <PageLoader />
  if (book.isError) {
    return (
      <section className="flex flex-col gap-6">
        <PenpotBackButton fallback={`/books/${id}`} />
        <ErrorState error={book.error} />
      </section>
    )
  }

  const remove = async () => {
    try {
      await deleteBook.mutateAsync()
      toast.success(t('books.delete.done'))
      navigate('/', { replace: true })
    } catch (error) {
      toast.error(getErrorMessage(t, error))
    }
  }

  return (
    <section className="flex flex-col gap-6">
      <PenpotBackButton fallback={`/books/${id}`} />
      <div
        ref={contentRef}
        className="lg:pr-[var(--avatar-gap,0px)]"
        style={
          avatarGap !== null && avatarGap > 0 ? ({ '--avatar-gap': `${avatarGap}px` } as CSSProperties) : undefined
        }
      >
        <div className="flex flex-col gap-6 [zoom:0.9]">
          <h1 className="text-2xl leading-8 font-semibold break-words">{t('books.detail.editTitle')}</h1>
          <BookForm
            key={book.data.updatedAt}
            defaultValues={toFormValues(book.data)}
            savedStatus={book.data.status}
            submitLabel={t('books.form.update')}
            onSubmit={async (input) => {
              await updateBook.mutateAsync(input)
              toast.success(t('books.form.updated'))
              navigate(`/books/${id}`, { replace: true })
            }}
            actions={
              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button
                    type="button"
                    variant="outline"
                    className="h-10 rounded-lg border-destructive/60 text-sm text-destructive hover:bg-destructive/10"
                  >
                    <Trash2Icon aria-hidden />
                    {t('books.delete.button')}
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>{t('books.delete.title')}</AlertDialogTitle>
                    <AlertDialogDescription>
                      {t('books.delete.description', {
                        title: book.data.title,
                      })}
                    </AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                    <AlertDialogAction
                      variant="destructive"
                      disabled={deleteBook.isPending}
                      onClick={(event) => {
                        event.preventDefault()
                        void remove()
                      }}
                    >
                      {deleteBook.isPending && <Loader2Icon className="animate-spin" aria-hidden />}
                      {t('books.delete.confirm')}
                    </AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            }
          />
        </div>
      </div>
    </section>
  )
}
