import { Loader2Icon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

type SubmitButtonProps = {
  children: ReactNode
  loading?: boolean
  /** "lg": full-width auth form button (Penpot "Buttoon"); "md": standard in-app form button. */
  size?: 'lg' | 'md'
}

export function SubmitButton({ children, loading, size = 'lg' }: SubmitButtonProps) {
  return (
    <Button
      type="submit"
      disabled={loading}
      className={cn(
        'w-full',
        size === 'lg' ? 'h-[50px] rounded-[10px] text-xl font-bold' : 'h-10 rounded-lg text-sm font-semibold',
      )}
    >
      {loading && <Loader2Icon className={size === 'lg' ? 'size-5 animate-spin' : 'size-4 animate-spin'} aria-hidden />}
      {children}
    </Button>
  )
}
