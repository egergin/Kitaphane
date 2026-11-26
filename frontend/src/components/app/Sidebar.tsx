import type { ComponentType } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, NavLink, useLocation } from 'react-router'
import { Logo } from '@/components/Logo'
import { cn } from '@/lib/utils'
import { GridIcon, HistoryIcon, HouseIcon } from './icons'

type NavItem = {
  to: string
  labelKey: 'nav.home' | 'nav.readBooks' | 'nav.profile'
  icon: ComponentType<{ className?: string }>
  /** Extra paths that keep the item active (book detail screens belong to "home" in the design). */
  matches?: (pathname: string) => boolean
}

const NAV_ITEMS: NavItem[] = [
  { to: '/', labelKey: 'nav.home', icon: HouseIcon, matches: (path) => path === '/' || path.startsWith('/books') },
  { to: '/read-books', labelKey: 'nav.readBooks', icon: HistoryIcon },
  { to: '/profile', labelKey: 'nav.profile', icon: GridIcon },
]

/**
 * Left menu of the Penpot app screens: 120px wide bordered column with the logo icon at the top
 * and the navigation icons centered vertically. On small screens it becomes a bottom bar.
 */
export function Sidebar() {
  const { t } = useTranslation()
  const { pathname } = useLocation()

  return (
    <aside
      className={cn(
        'fixed inset-x-0 bottom-0 z-30 flex h-14 items-center justify-center border-t border-border bg-background',
        // Penpot layout (logo on top, icons centered vertically) at standard web sizes: 72px column, 1px border.
        'md:inset-y-0 md:left-0 md:right-auto md:h-auto md:w-[4.5rem] md:flex-col md:justify-start md:border-t-0 md:border-r md:py-4',
      )}
    >
      <Link to="/" className="hidden md:flex md:size-10 md:items-center md:justify-center" aria-label={t('app.name')}>
        <Logo iconOnly className="[&>svg]:size-8" />
      </Link>

      <nav aria-label={t('nav.label')} className="flex gap-8 md:my-auto md:flex-col md:gap-4">
        {NAV_ITEMS.map(({ to, labelKey, icon: Icon, matches }) => {
          const label = t(labelKey)
          return (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              aria-label={label}
              title={label}
              aria-current={matches?.(pathname) ? 'page' : undefined}
              className={({ isActive }) => {
                const active = isActive || matches?.(pathname)
                return cn(
                  'flex size-10 items-center justify-center rounded-lg transition-colors outline-none focus-visible:ring-3 focus-visible:ring-ring/50',
                  active ? 'bg-destructive text-white' : 'text-foreground hover:bg-muted',
                )
              }}
            >
              <Icon className="size-6" />
            </NavLink>
          )
        })}
      </nav>
    </aside>
  )
}
