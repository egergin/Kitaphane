import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'

/** "< Geri" from Penpot "Kitap Bilgileri" at 81% (90% of the 90% screen scale): 50px arrow icon + 24/600 text. Goes to the previous page (home if none). */
export function PenpotBackButton({ fallback = '/' }: { fallback?: string }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()

  return (
    <button
      type="button"
      onClick={() => (location.key !== 'default' ? navigate(-1) : navigate(fallback))}
      className="flex h-[2.025rem] items-center rounded-md pr-[0.2rem] text-[0.972rem] leading-[1.175rem] font-semibold outline-none hover:underline focus-visible:ring-3 focus-visible:ring-ring/50"
    >
      <svg viewBox="0 0 50 50" className="size-[2.025rem]" fill="none" stroke="currentColor" strokeWidth="3" aria-hidden>
        <path d="M31.438 8.5 17.854 22.083c-1.604 1.604-1.604 4.23 0 5.834L31.438 41.5" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
      {t('common.back')}
    </button>
  )
}
