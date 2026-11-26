import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { cn } from '@/lib/utils'

/** Chevron icon provided by the user (back-light, 42×42 viewBox); uses the current text color. */
function BackIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 42 42" fill="currentColor" className={className} aria-hidden>
      <polygon fillRule="evenodd" points="31,38.32 13.391,21 31,3.68 28.279,1 8,21.01 28.279,41" />
    </svg>
  )
}

/**
 * Returns to the previously visited page. When the page was opened directly (no in-app history),
 * it falls back to the login screen.
 */
export function HistoryBackButton({ className }: { className?: string }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()

  const goBack = () => {
    const hasInAppHistory = location.key !== 'default'
    if (hasInAppHistory) {
      navigate(-1)
    } else {
      navigate('/login', { replace: true })
    }
  }

  return (
    <button
      type="button"
      onClick={goBack}
      aria-label={t('common.back')}
      title={t('common.back')}
      className={cn(
        '-ml-1 inline-flex size-9 items-center justify-center self-start rounded-full text-foreground transition-colors hover:bg-muted focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none',
        className,
      )}
    >
      <BackIcon className="size-6" />
    </button>
  )
}
