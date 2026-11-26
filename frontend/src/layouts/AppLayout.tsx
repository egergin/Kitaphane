import { useLayoutEffect } from 'react'
import { Outlet, useLocation } from 'react-router'
import { HeaderSearch } from '@/components/app/HeaderSearch'
import { Sidebar } from '@/components/app/Sidebar'
import { UserMenu } from '@/components/app/UserMenu'
import { LanguageSwitcher } from '@/components/LanguageSwitcher'

/**
 * App shell following the Penpot screens' layout at standard web sizes (rendered at 125%, see useAppScale):
 * 4.5rem left menu, 4rem header (search left, language and user right), content aligned with the header.
 */
/** Enables the 125% scale (html.app-scale, see index.css) while an app screen is shown. */
function useAppScale() {
  useLayoutEffect(() => {
    document.documentElement.classList.add('app-scale')
    return () => document.documentElement.classList.remove('app-scale')
  }, [])
}

export function AppLayout() {
  useAppScale()
  // The book search has no use on the profile screens, so the header shows it everywhere else.
  const showSearch = !useLocation().pathname.startsWith('/profile')

  return (
    <div className="min-h-svh pb-16 md:pb-0">
      <Sidebar />
      <div className="px-4 sm:px-6 md:pr-10 md:pl-[7rem]">
        <header className="flex min-h-16 flex-wrap items-center justify-between gap-x-6 gap-y-3 pt-3 md:pt-2">
          {showSearch && <HeaderSearch />}
          <div className="ml-auto flex items-center gap-4">
            <LanguageSwitcher />
            <UserMenu />
          </div>
        </header>
        <main className="pt-6 pb-12 md:pt-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
