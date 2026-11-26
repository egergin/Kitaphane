import { HistoryBackButton } from '@/components/HistoryBackButton'

type AuthHeadingProps = {
  title: string
  subtitle?: string
  /** Back button above the title; hidden on the login screen, which is the start of the flow. */
  showBack?: boolean
}

/** Title block of the auth screens, with an optional back button above the title. */
export function AuthHeading({ title, subtitle, showBack = true }: AuthHeadingProps) {
  return (
    <div className="flex flex-col gap-1">
      {showBack && <HistoryBackButton className="mb-2" />}
      <h1 className="text-[32px] leading-tight font-semibold">{title}</h1>
      {subtitle && <p className="text-base">{subtitle}</p>}
    </div>
  )
}
