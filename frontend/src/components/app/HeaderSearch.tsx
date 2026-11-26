import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate, useSearchParams } from 'react-router'
import { SearchIcon } from './icons'

export const SEARCH_PARAM = 'q'
const DEBOUNCE_MS = 300

/** Pages that filter their own list with the header search; elsewhere a search opens the home page. */
const SEARCHABLE_PATHS = ['/', '/read-books']

/**
 * "Kitap adı veya yazar ara..." box from the Penpot header. The term lives in the page URL (?q=) so
 * results survive reloads. The home page and "Okuduğum Kitaplar" search their own list; typing on any
 * other page opens the home page with the search.
 */
export function HeaderSearch() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const [searchParams] = useSearchParams()
  const searchable = SEARCHABLE_PATHS.includes(pathname)
  const urlTerm = searchable ? (searchParams.get(SEARCH_PARAM) ?? '') : ''
  const [term, setTerm] = useState(urlTerm)
  const lastPushed = useRef(urlTerm)

  // Keep the box in sync when the URL changes from elsewhere (back button, navigation).
  useEffect(() => {
    if (urlTerm !== lastPushed.current) {
      lastPushed.current = urlTerm
      setTerm(urlTerm)
    }
  }, [urlTerm])

  useEffect(() => {
    const value = term.trim()
    if (value === lastPushed.current) return

    const timer = window.setTimeout(() => {
      lastPushed.current = value
      const path = searchable ? pathname : '/'
      const target = value ? `${path}?${SEARCH_PARAM}=${encodeURIComponent(value)}` : path
      navigate(target, { replace: searchable })
    }, DEBOUNCE_MS)
    return () => window.clearTimeout(timer)
  }, [term, navigate, pathname, searchable])

  return (
    <form
      role="search"
      onSubmit={(event) => event.preventDefault()}
      className="order-last flex h-10 w-full min-w-0 items-center gap-2.5 rounded-lg px-1 md:order-none md:w-auto md:max-w-sm md:flex-1"
    >
      <SearchIcon className="size-5 shrink-0 text-foreground" />
      <input
        type="search"
        value={term}
        onChange={(event) => setTerm(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === 'Escape') setTerm('')
        }}
        maxLength={100}
        placeholder={t('nav.searchPlaceholder')}
        aria-label={t('nav.searchPlaceholder')}
        className="h-8 w-full min-w-0 bg-transparent text-sm font-medium text-foreground outline-none placeholder:text-muted-foreground [&::-webkit-search-cancel-button]:cursor-pointer"
      />
    </form>
  )
}
