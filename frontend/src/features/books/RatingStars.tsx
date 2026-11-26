import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'

const MAX_RATING = 10

// Star paths exported from the Penpot "Okunan Kitaplar" screen (24×24 "Button Review").
const STAR_PATH =
  'M22.423 9.118a1.55 1.55 0 0 0-1.337-1.056l-5.531-.446-2.136-5.164a1.53 1.53 0 0 0-2.838 0L8.448 7.615l-5.534.448a1.55 1.55 0 0 0-.879 2.705l4.219 3.64-1.285 5.444a1.55 1.55 0 0 0 2.297 1.67L12 18.608l4.737 2.914a1.55 1.55 0 0 0 2.294-1.67l-1.29-5.445 4.219-3.64a1.55 1.55 0 0 0 .463-1.649Z'
const STAR_OUTLINE_PATH =
  'M22.423 9.118a1.55 1.55 0 0 0-1.337-1.056l-5.531-.446-2.136-5.164a1.53 1.53 0 0 0-2.838 0L8.448 7.615l-5.534.448a1.55 1.55 0 0 0-.879 2.705l4.219 3.64-1.285 5.444a1.55 1.55 0 0 0 2.297 1.67L12 18.608l4.737 2.914a1.55 1.55 0 0 0 2.294-1.67l-1.29-5.445 4.219-3.64a1.55 1.55 0 0 0 .463-1.649Zm-1.438.513-4.566 3.937a.75.75 0 0 0-.24.742l1.395 5.887a.04.04 0 0 1-.017.045c-.017.013-.021.01-.036 0l-5.13-3.154a.75.75 0 0 0-.785 0l-5.13 3.156c-.014.008-.018.011-.035 0a.04.04 0 0 1-.017-.045l1.395-5.888a.75.75 0 0 0-.24-.741L3.015 9.633c-.011-.01-.021-.018-.012-.047.009-.03.017-.026.031-.028l5.992-.484a.75.75 0 0 0 .63-.463l2.308-5.588c.008-.016.01-.023.033-.023s.025.007.033.023l2.314 5.588a.75.75 0 0 0 .633.461l5.992.484c.014 0 .023 0 .031.027.008.028 0 .038-.015.047Z'

function Star({ filled }: { filled: boolean }) {
  return (
    <svg viewBox="0 0 24 24" className="size-[1em] shrink-0" aria-hidden>
      <path d={filled ? STAR_PATH : STAR_OUTLINE_PATH} fill={filled ? 'var(--rating)' : 'var(--foreground)'} />
    </svg>
  )
}

type RatingStarsProps = {
  rating: number | null
  /** Sets the size: everything is in em of this font size (Penpot: 24px number and stars, 10px apart). */
  className?: string
}

/**
 * Read-only rating from the Penpot book screens: the number followed by 10 stars.
 * Sized in em, so the caller's font size scales the whole row. The rating is changed from the edit form.
 */
export function RatingStars({ rating, className }: RatingStarsProps) {
  const { t } = useTranslation()

  if (rating === null) {
    return (
      <p className={cn('flex h-[1.8333em] items-center text-muted-foreground', className)}>
        <span className="text-[0.75em]">{t('books.form.noRating')}</span>
      </p>
    )
  }

  return (
    <div
      className={cn('flex h-[1.8333em] items-center gap-[0.4167em]', className)}
      role="img"
      aria-label={t('books.ratingLabel', { rating, max: MAX_RATING })}
    >
      <span className="leading-[1.2] font-medium">{rating}</span>
      {Array.from({ length: MAX_RATING }, (_, index) => (
        <Star key={index} filled={index < rating} />
      ))}
    </div>
  )
}
