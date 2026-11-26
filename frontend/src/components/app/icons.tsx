/**
 * Icons taken from the Penpot "Ana Sayfa" design (Menu and Header boards).
 * They use currentColor so the active/inactive colors come from CSS.
 */
type IconProps = { className?: string }

export function HouseIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 40 40" fill="currentColor" className={className} aria-hidden>
      <path d="M34.267 16.981 21.767 4.481a2.5 2.5 0 0 0-3.534 0l-12.5 12.5A2.49 2.49 0 0 0 5 18.75v15A1.25 1.25 0 0 0 6.25 35h27.5A1.25 1.25 0 0 0 35 33.75v-15a2.49 2.49 0 0 0-.733-1.769ZM32.5 32.5h-25V18.75L20 6.25l12.5 12.5Z" />
    </svg>
  )
}

export function HistoryIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 40 40" fill="currentColor" className={className} aria-hidden>
      <path d="M21.25 12.5v6.792l5.644 3.386a1.25 1.25 0 1 1-1.288 2.144l-6.25-3.75A1.25 1.25 0 0 1 18.75 20v-7.5a1.25 1.25 0 0 1 2.5 0ZM20 5a14.9 14.9 0 0 0-10.613 4.398C8.251 10.549 7.242 11.654 6.25 12.813V10a1.25 1.25 0 0 0-2.5 0v6.25A1.25 1.25 0 0 0 5 17.5h6.25a1.25 1.25 0 0 0 0-2.5H7.656c1.117-1.316 2.23-2.555 3.499-3.839a12.5 12.5 0 1 1 .259 17.93 1.25 1.25 0 0 0-1.719 1.818A15 15 0 1 0 20 5Z" />
    </svg>
  )
}

export function GridIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 40 40" fill="none" stroke="currentColor" strokeWidth="2.5" className={className} aria-hidden>
      <path d="M8.333 16.666h3.333c3.334 0 5-1.666 5-5V8.333c0-3.333-1.666-5-5-5H8.333c-3.333 0-5 1.667-5 5v3.333c0 3.334 1.667 5 5 5Z" />
      <path d="M28.333 16.666h3.333c3.334 0 5-1.666 5-5V8.333c0-3.333-1.666-5-5-5h-3.333c-3.333 0-5 1.667-5 5v3.333c0 3.334 1.667 5 5 5Z" />
      <path d="M28.333 36.666h3.333c3.334 0 5-1.666 5-5v-3.333c0-3.333-1.666-5-5-5h-3.333c-3.333 0-5 1.667-5 5v3.333c0 3.334 1.667 5 5 5Z" />
      <path d="M8.333 36.666h3.333c3.334 0 5-1.666 5-5v-3.333c0-3.333-1.666-5-5-5H8.333c-3.333 0-5 1.667-5 5v3.333c0 3.334 1.667 5 5 5Z" />
    </svg>
  )
}

export function SearchIcon({ className }: IconProps) {
  return (
    <svg viewBox="0 0 30 30" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" className={className} aria-hidden>
      <circle cx="14.375" cy="14.375" r="10.375" />
      <path d="M27.5 27.5 25 25" />
    </svg>
  )
}
