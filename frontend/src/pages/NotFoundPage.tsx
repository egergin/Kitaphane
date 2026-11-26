import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Logo } from '@/components/Logo'
import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-4 px-6 text-center">
      <Logo />
      <h1 className="text-2xl font-semibold">{t('notFound.title')}</h1>
      <p className="text-muted-foreground">{t('notFound.description')}</p>
      <Button asChild variant="outline">
        <Link to="/">{t('common.backToHome')}</Link>
      </Button>
    </div>
  )
}
