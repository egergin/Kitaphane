import { BookOpenIcon } from 'lucide-react'
import { useState } from 'react'
import { cn } from '@/lib/utils'

type BookCoverProps = {
  src: string | null
  title: string
  className?: string
}

/** Cover image with the Penpot card shape (2:3-ish, 10px radius, soft shadow) and a placeholder fallback. */
export function BookCover({ src, title, className }: BookCoverProps) {
  const [failed, setFailed] = useState(false)
  const showImage = src && !failed

  return (
    <div
      className={cn(
        'relative aspect-[219/321] w-full overflow-hidden rounded-lg bg-muted shadow-[-0.375rem_0.375rem_1rem_rgba(0,0,0,0.18)]',
        className,
      )}
    >
      {showImage ? (
        <img
          src={src}
          alt=""
          loading="lazy"
          referrerPolicy="no-referrer"
          onError={() => setFailed(true)}
          className="size-full object-cover"
        />
      ) : (
        <div className="flex size-full flex-col items-center justify-center gap-3 p-4 text-center text-muted-foreground">
          <BookOpenIcon className="size-7" aria-hidden />
          <span className="line-clamp-3 text-xs font-semibold">{title}</span>
        </div>
      )}
    </div>
  )
}
