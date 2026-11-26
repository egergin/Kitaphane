import { CheckIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import enFlag from '@/assets/flags/en.svg'
import trFlag from '@/assets/flags/tr.webp'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { changeLanguage, currentLanguage, type Language, SUPPORTED_LANGUAGES } from '@/lib/i18n'

const LANGUAGES: Record<Language, { name: string; flag: string }> = {
  tr: { name: 'Türkçe', flag: trFlag },
  en: { name: 'English', flag: enFlag },
}

function Flag({ language }: { language: Language }) {
  return <img src={LANGUAGES[language].flag} alt="" className="h-3 w-[1.125rem] rounded-[2px] object-cover" />
}

/** Language pill from the Penpot header: white, 2px primary border, "TR" + flag. */
export function LanguageSwitcher() {
  const { t } = useTranslation()
  const language = currentLanguage()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        data-language-switcher
        className="inline-flex h-9 items-center gap-2 rounded-full border-2 border-primary bg-white px-3.5 text-sm leading-none font-bold text-black uppercase outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
        aria-label={t('common.language')}
      >
        {language}
        <Flag language={language} />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {SUPPORTED_LANGUAGES.map((option) => (
          <DropdownMenuItem key={option} onSelect={() => void changeLanguage(option)}>
            <Flag language={option} />
            <span className="font-bold uppercase">{option}</span>
            {LANGUAGES[option].name}
            {option === language && <CheckIcon className="ml-auto size-4" aria-hidden />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
