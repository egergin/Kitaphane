import { PlusIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'

/** "Yeni Kitap Ekle" card; always rendered after the loaded books. */
export function AddBookCard() {
  const { t } = useTranslation()

  return (
    <Link
      to="/books/new"
      className="group flex flex-col gap-3 rounded-lg outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
    >
      <div className="flex aspect-[219/321] w-full items-center justify-center rounded-lg border border-foreground/70 transition-colors group-hover:bg-white/60">
        <span className="flex size-14 items-center justify-center rounded-full border-[0.1875rem] border-foreground">
          <PlusIcon className="size-7" strokeWidth={2.5} aria-hidden />
        </span>
      </div>
      <span className="text-center text-[0.9375rem] leading-5 font-bold">{t('books.addBook')}</span>
    </Link>
  )
}
