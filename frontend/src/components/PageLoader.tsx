import { Loader2Icon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

export function PageLoader() {
  const { t } = useTranslation()

  return (
    <div role="status" className="flex min-h-40 items-center justify-center gap-2 text-muted-foreground">
      <Loader2Icon className="size-5 animate-spin" aria-hidden />
      <span className="text-sm">{t('common.loading')}</span>
    </div>
  )
}
