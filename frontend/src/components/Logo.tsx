import { cn } from '@/lib/utils'

/** Kitaphane logo from the Penpot design: bookmark icon + wordmark. */
export function Logo({ className, iconOnly = false }: { className?: string; iconOnly?: boolean }) {
  return (
    <span className={cn('inline-flex items-center gap-2.5 text-foreground', className)}>
      <svg viewBox="0 0 40 40" className="size-10 shrink-0" fill="none" aria-hidden>
        <path
          d="M15 36.67h10c8.33 0 11.67-3.34 11.67-11.67V15c0-8.33-3.34-11.67-11.67-11.67H15C6.67 3.33 3.33 6.67 3.33 15v10c0 8.33 3.34 11.67 11.67 11.67Z"
          stroke="currentColor"
          strokeWidth="3"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M28.33 4.07V20.7c0 3.28-2.35 4.57-5.23 2.83l-2.2-1.32a1.83 1.83 0 0 0-1.8 0l-2.2 1.32c-2.88 1.72-5.23.45-5.23-2.83V4.07"
          stroke="currentColor"
          strokeWidth="3"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
      {!iconOnly && <span className="text-[32px] leading-none font-semibold">Kitaphane</span>}
    </span>
  )
}
