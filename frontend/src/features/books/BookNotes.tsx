import { useLayoutEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'

/** Notes block of Penpot "Kitap Bilgileri" at 90%: full width of the info column, 18/500; 6 lines with "...View more", or all with "Hide". */
export function BookNotes({ notes }: { notes: string }) {
  const { t } = useTranslation()
  const textRef = useRef<HTMLParagraphElement>(null)
  const [expanded, setExpanded] = useState(false)
  const [overflowing, setOverflowing] = useState(false)

  useLayoutEffect(() => {
    const element = textRef.current
    if (!element || expanded) return
    const measure = () => setOverflowing(element.scrollHeight > element.clientHeight + 1)
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(element)
    return () => observer.disconnect()
  }, [notes, expanded])

  return (
    <div className="flex w-full flex-col items-start">
      <p
        ref={textRef}
        className={cn('text-[0.81rem] leading-[0.972rem] font-medium whitespace-pre-line', !expanded && 'line-clamp-6')}
      >
        {notes}
      </p>
      {(overflowing || expanded) && (
        <button
          type="button"
          onClick={() => setExpanded((value) => !value)}
          aria-expanded={expanded}
          className="mt-[0.855rem] text-[0.81rem] leading-[0.972rem] font-bold text-primary hover:underline focus-visible:underline focus-visible:outline-none"
        >
          {expanded ? t('books.detail.hide') : t('books.detail.viewMore')}
        </button>
      )}
    </div>
  )
}
