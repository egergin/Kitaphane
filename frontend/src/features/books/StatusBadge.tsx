import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import type { BookStatus } from './types'

/** Colors from Penpot "Kitap Bilgileri 2". */
const STATUS_COLORS: Record<BookStatus, string> = {
  READING: 'bg-[#2563EB]',
  TO_READ: 'bg-[#F59E0B]',
  COMPLETED: 'bg-[#10B981]',
}

/**
 * The book's current status as the Penpot status button (206×50, radius 10, 20/700 white uppercase),
 * shown at 90%. Display only: the status is changed from the edit form.
 */
export function StatusBadge({ status }: { status: BookStatus }) {
  const { t } = useTranslation()

  return (
    <span
      className={cn(
        'inline-flex h-[2.25rem] w-[9.27rem] items-center justify-center rounded-[0.45rem] text-[0.9rem] leading-[1.08rem] font-bold text-white uppercase',
        STATUS_COLORS[status],
      )}
    >
      <span className="sr-only">{t('books.fields.status')}: </span>
      {t(`books.status.${status}`).toLocaleUpperCase('tr')}
    </span>
  )
}
