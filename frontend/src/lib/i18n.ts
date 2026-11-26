import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import en from '@/locales/en.json'
import tr from '@/locales/tr.json'

export const SUPPORTED_LANGUAGES = ['tr', 'en'] as const
export type Language = (typeof SUPPORTED_LANGUAGES)[number]

const STORAGE_KEY = 'kitaphane.language'
const DEFAULT_LANGUAGE: Language = 'tr'

function isLanguage(value: string | null | undefined): value is Language {
  return SUPPORTED_LANGUAGES.includes(value as Language)
}

/** Saved choice first, then the browser language, then Turkish. */
function detectLanguage(): Language {
  const saved = localStorage.getItem(STORAGE_KEY)
  if (isLanguage(saved)) {
    return saved
  }
  const browser = navigator.language?.slice(0, 2).toLowerCase()
  return isLanguage(browser) ? browser : DEFAULT_LANGUAGE
}

export function currentLanguage(): Language {
  return isLanguage(i18n.resolvedLanguage) ? i18n.resolvedLanguage : DEFAULT_LANGUAGE
}

export async function changeLanguage(language: Language) {
  localStorage.setItem(STORAGE_KEY, language)
  await i18n.changeLanguage(language)
}

i18n.on('languageChanged', (language) => {
  document.documentElement.lang = language
})

void i18n.use(initReactI18next).init({
  lng: detectLanguage(),
  fallbackLng: DEFAULT_LANGUAGE,
  supportedLngs: SUPPORTED_LANGUAGES,
  resources: {
    tr: { translation: tr },
    en: { translation: en },
  },
  interpolation: {
    escapeValue: false, // React already escapes output.
  },
})

export default i18n
