import type { ReactNode } from 'react'

type SettingsCardProps = { title: string; description?: string; children: ReactNode }

/** White card with a heading, used by the profile page sections. */
export function SettingsCard({ title, description, children }: SettingsCardProps) {
  return (
    <section className="flex flex-col gap-5 rounded-lg border border-border bg-white p-6">
      <div className="flex flex-col gap-1">
        <h3 className="text-base font-bold">{title}</h3>
        {description && <p className="text-sm text-muted-foreground">{description}</p>}
      </div>
      {children}
    </section>
  )
}
