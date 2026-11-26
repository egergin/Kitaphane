import { Link, Outlet } from 'react-router'
import illustration from '@/assets/auth-illustration.webp'
import { LanguageSwitcher } from '@/components/LanguageSwitcher'
import { Logo } from '@/components/Logo'

/** Split layout of the Penpot auth screens: book illustration on the left, form on the right. */
export function AuthLayout() {
  return (
    <div className="relative grid min-h-svh bg-white lg:grid-cols-[52%_1fr]">
      <aside className="relative hidden overflow-hidden bg-background lg:block" aria-hidden>
        <img src={illustration} alt="" className="absolute inset-0 size-full object-cover object-left-top" />
      </aside>

      <Link to="/login" className="absolute top-6 left-6 z-10 lg:top-[30px] lg:left-[30px]">
        <Logo />
      </Link>

      <div className="absolute top-6 right-6 z-10 lg:top-[30px] lg:right-[30px]">
        <LanguageSwitcher />
      </div>

      <main className="flex items-center justify-center px-6 pt-28 pb-12 lg:pt-12">
        <div className="w-full max-w-[400px]">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
