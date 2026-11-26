import { cn } from '@/lib/utils'

type StatCardProps = { label: string; value: number; accentClassName?: string; unit?: string }

/** A single number with its label; the optional accent is a thin bar matching the status colors. */
export function StatCard({ label, value, accentClassName, unit }: StatCardProps) {
  return (
    <div className="flex flex-col gap-1 overflow-hidden rounded-lg border border-border bg-white p-4">
      {accentClassName && <span className={cn('mb-1 h-1 w-8 rounded-full', accentClassName)} aria-hidden />}
      <span className="text-3xl leading-9 font-bold tabular-nums">
        {value.toLocaleString('tr-TR')}
        {unit && <span className="ml-1 text-base font-semibold text-muted-foreground">{unit}</span>}
      </span>
      <span className="text-sm text-muted-foreground">{label}</span>
    </div>
  )
}
