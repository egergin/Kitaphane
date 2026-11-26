import { BookOpenIcon, Loader2Icon, SearchIcon } from 'lucide-react'
import { type FormEvent, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { OutlinedField } from '@/components/form/OutlinedField'
import { Button } from '@/components/ui/button'
import { isValidIsbn, normalizeIsbn } from '@/features/books/isbn'
import { getErrorMessage } from '@/lib/errors'
import { cn } from '@/lib/utils'
import { type GoogleBooksSearch as Search, useGoogleBooksSearch } from './api'
import type { GoogleBookResult, GoogleBooksSearchMode } from './types'

type GoogleBooksSearchProps = {
  onSelect: (result: GoogleBookResult) => void
}

/**
 * Search Google Books by title/author or ISBN. Selecting a result only fills the form;
 * the book is saved when the user confirms the form.
 */
export function GoogleBooksSearch({ onSelect }: GoogleBooksSearchProps) {
  const { t } = useTranslation()
  const [mode, setMode] = useState<GoogleBooksSearchMode>('text')
  const [term, setTerm] = useState('')
  const [inputError, setInputError] = useState<string | undefined>()
  const [search, setSearch] = useState<Search | null>(null)
  const results = useGoogleBooksSearch(search)

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const value = term.trim()

    if (!value) {
      setInputError(t('googleBooks.validation.required'))
      return
    }
    if (mode === 'isbn' && !isValidIsbn(normalizeIsbn(value))) {
      setInputError(t('books.validation.isbn'))
      return
    }

    setInputError(undefined)
    setSearch({ mode, term: mode === 'isbn' ? normalizeIsbn(value) : value })
  }

  const changeMode = (next: GoogleBooksSearchMode) => {
    setMode(next)
    setInputError(undefined)
  }

  return (
    <section aria-labelledby="google-books-title" className="flex flex-col gap-3 rounded-lg border border-border bg-white p-4">
      <div className="flex flex-col gap-1 lg:flex-row lg:items-baseline lg:gap-3">
        <h2 id="google-books-title" className="text-base font-bold">
          {t('googleBooks.title')}
        </h2>
        <p className="text-sm text-muted-foreground">{t('googleBooks.subtitle')}</p>
      </div>

      <div className="flex flex-col gap-3 lg:flex-row lg:items-start">
        <div
          role="radiogroup"
          aria-label={t('googleBooks.mode')}
          className="flex w-fit shrink-0 rounded-lg border border-foreground/70 p-0.5 lg:mt-4"
        >
          {(['text', 'isbn'] as const).map((option) => (
            <button
              key={option}
              type="button"
              role="radio"
              aria-checked={mode === option}
              onClick={() => changeMode(option)}
              className={cn(
                'rounded-md px-3 py-1 text-xs font-semibold transition-colors',
                mode === option ? 'bg-foreground text-white' : 'hover:bg-muted',
              )}
            >
              {t(`googleBooks.modes.${option}`)}
            </button>
          ))}
        </div>

        <form onSubmit={submit} noValidate className="flex flex-1 flex-col gap-3 sm:flex-row sm:items-start">
          <OutlinedField
            className="flex-1"
            label={mode === 'isbn' ? t('googleBooks.isbnLabel') : t('googleBooks.textLabel')}
            inputMode={mode === 'isbn' ? 'numeric' : 'search'}
            value={term}
            onChange={(event) => setTerm(event.target.value)}
            error={inputError}
          />
          <Button type="submit" className="h-10 rounded-lg px-4 text-sm font-semibold sm:mt-[0.6875rem]" disabled={results.isFetching}>
            {results.isFetching ? <Loader2Icon className="animate-spin" aria-hidden /> : <SearchIcon aria-hidden />}
            {t('googleBooks.search')}
          </Button>
        </form>
      </div>

      <div aria-live="polite">
        {search && results.isError && <p className="text-sm text-destructive">{getErrorMessage(t, results.error)}</p>}
        {search && results.isSuccess && results.data.length === 0 && (
          <p className="text-sm text-muted-foreground">{t('googleBooks.noResults')}</p>
        )}
        {results.isSuccess && results.data.length > 0 && (
          <ul className="flex max-h-[26.25rem] flex-col divide-y overflow-y-auto">
            {results.data.map((result) => (
              <li key={result.googleBooksId} className="flex items-center gap-4 py-3">
                <ResultCover src={result.coverImage} />
                <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                  <span className="truncate text-sm font-semibold">{result.title}</span>
                  <span className="truncate text-sm">{result.author ?? t('googleBooks.unknownAuthor')}</span>
                  <span className="text-xs text-muted-foreground">
                    {[
                      result.pageCount ? t('googleBooks.pages', { count: result.pageCount }) : null,
                      result.isbn ? `ISBN ${result.isbn}` : null,
                    ]
                      .filter(Boolean)
                      .join(' · ')}
                  </span>
                </div>
                <Button type="button" variant="outline" size="sm" className="rounded-lg border-primary text-primary" onClick={() => onSelect(result)}>
                  {t('googleBooks.select')}
                </Button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  )
}

function ResultCover({ src }: { src: string | null }) {
  const [failed, setFailed] = useState(false)

  return (
    <div className="flex h-[3.75rem] w-10 shrink-0 items-center justify-center overflow-hidden rounded-md bg-muted">
      {src && !failed ? (
        <img src={src} alt="" loading="lazy" referrerPolicy="no-referrer" onError={() => setFailed(true)} className="size-full object-cover" />
      ) : (
        <BookOpenIcon className="size-5 text-muted-foreground" aria-hidden />
      )}
    </div>
  )
}
