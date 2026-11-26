import { type Location, Navigate, Outlet, useLocation } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { useAuth } from './AuthContext'

/** Only signed-in users; others are sent to the login page and returned afterwards. */
export function ProtectedRoute() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <PageLoader />
  if (status === 'anonymous') return <Navigate to="/login" replace state={{ from: location }} />
  return <Outlet />
}

/**
 * Login, sign-up and password reset screens. Signed-in users go back to the page that sent them to
 * the login screen (ProtectedRoute passes it as state.from), otherwise to the home page.
 * This redirect also runs right after a successful sign-in, so it must honor `from` itself.
 */
export function GuestRoute() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <PageLoader />
  if (status === 'authenticated') {
    const from = (location.state as { from?: Location } | null)?.from
    return <Navigate to={from ? `${from.pathname}${from.search}` : '/'} replace />
  }
  return <Outlet />
}
