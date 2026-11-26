import { type ComponentProps, useState } from 'react'
import { cn } from '@/lib/utils'

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toLocaleUpperCase('tr'))
    .join('')
}

type UserAvatarProps = ComponentProps<'span'> & {
  name: string
  image: string | null
}

/** Round profile picture; falls back to the name's initials when there is no image or it fails to load. */
export function UserAvatar({ name, image, className, ...props }: UserAvatarProps) {
  // Remember which URL failed, so a new URL is tried again.
  const [failedImage, setFailedImage] = useState<string | null>(null)

  return (
    <span
      className={cn(
        'flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-foreground font-bold text-white',
        className,
      )}
      {...props}
    >
      {image && failedImage !== image ? (
        <img
          src={image}
          alt=""
          referrerPolicy="no-referrer"
          onError={() => setFailedImage(image)}
          className="size-full object-cover"
        />
      ) : (
        initials(name)
      )}
    </span>
  )
}
