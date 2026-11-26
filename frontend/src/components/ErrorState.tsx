import { TriangleAlertIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { getErrorMessage } from '@/lib/errors'

type ErrorStateProps = {
  error: unknown
  onRetry?: () => void
}

export function ErrorState({ error, onRetry }: ErrorStateProps) {
  const { t } = useTranslation()

  return (
    <div role="alert" className="flex flex-col items-center gap-3 py-10 text-center">
      <TriangleAlertIcon className="size-6 text-destructive" aria-hidden />
      <p className="text-sm text-muted-foreground">{getErrorMessage(t, error)}</p>
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry}>
          {t('common.retry')}
        </Button>
      )}
    </div>
  )
}
