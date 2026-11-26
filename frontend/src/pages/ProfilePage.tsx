import { PencilIcon } from 'lucide-react'
import { type CSSProperties, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { UserAvatar } from '@/components/UserAvatar'
import { useAuth } from '@/features/auth/AuthContext'
import { ReadingGoals } from '@/features/reading-goals/ReadingGoals'
import { BookStatistics } from '@/features/statistics/BookStatistics'
import { ReadingStatistics } from '@/features/statistics/ReadingStatistics'
import { useOffsetFromRight } from '@/lib/useAlignWithElement'

/**
 * Profile: user information, book statistics, reading goals and reading statistics (FR-07..FR-10). "Düzenle" opens the
 * account settings screen (/profile/edit). Penpot has no profile design, so the page uses the app's
 * card style. Like the book screens it is shown at 90% size and spans to the left edge of the
 * header avatar.
 */
export function ProfilePage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const contentRef = useRef<HTMLDivElement>(null)
  const avatarGap = useOffsetFromRight(contentRef, '[data-user-avatar]', 'left')

  if (!user) return null

  return (
    <div
      ref={contentRef}
      className="lg:pr-[var(--avatar-gap,0px)]"
      style={avatarGap !== null && avatarGap > 0 ? ({ '--avatar-gap': `${avatarGap}px` } as CSSProperties) : undefined}
    >
      <div className="flex flex-col gap-8 [zoom:0.9]">
        {/* No visible title (user request); kept for screen readers. */}
        <h1 className="sr-only">{t('nav.profile')}</h1>

        <section
          aria-label={t('profile.userInfo')}
          className="flex items-center gap-3 rounded-lg border border-border bg-white p-4 sm:gap-5 sm:p-6"
        >
          <UserAvatar name={user.name} image={user.profileImage} className="size-14 text-lg sm:size-20 sm:text-2xl" />
          <div className="flex min-w-0 flex-1 flex-col gap-1">
            <p className="truncate text-lg font-bold sm:text-xl">{user.name}</p>
            <p className="truncate text-sm text-muted-foreground">{user.email}</p>
          </div>
          <Link
            to="/profile/edit"
            className="inline-flex size-10 shrink-0 items-center justify-center gap-2 rounded-lg border border-foreground/70 sm:w-auto sm:px-3 text-sm font-semibold transition-colors hover:bg-muted/60 focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
          >
            <PencilIcon className="size-4" aria-hidden />
            <span className="sr-only sm:not-sr-only">{t('profile.edit')}</span>
          </Link>
        </section>

        <section aria-labelledby="book-stats-title" className="flex flex-col gap-4">
          <h2 id="book-stats-title" className="text-lg font-bold">
            {t('profile.stats.title')}
          </h2>
          <BookStatistics />
        </section>

        <ReadingGoals />

        <ReadingStatistics />
      </div>
    </div>
  )
}
