import { CircleAlertIcon } from 'lucide-react'

export function FormAlert({ message }: { message: string | null }) {
  if (!message) return null

  return (
    <div
      role="alert"
      className="flex items-start gap-2 rounded-[10px] border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm"
    >
      <CircleAlertIcon className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden />
      <span>{message}</span>
    </div>
  )
}
