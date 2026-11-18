import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { rememberLanguage, switchLanguageInPath } from '../../i18n/index.js'
import { useLanguage } from '../../lib/queries.js'

const LANGUAGES = [
  { code: 'en', label: 'EN', name: 'English' },
  { code: 'es', label: 'ES', name: 'Español' },
]

export default function LanguageSwitcher() {
  const { t } = useTranslation()
  const current = useLanguage()
  const { pathname, search } = useLocation()

  return (
    <div
      role="group"
      aria-label={t('nav.language')}
      className="inline-flex rounded-lg border border-slate-200 p-0.5 dark:border-slate-700"
    >
      {LANGUAGES.map(({ code, label, name }) => {
        const active = code === current
        return (
          <Link
            key={code}
            to={switchLanguageInPath(pathname, code) + search}
            lang={code}
            hrefLang={code}
            aria-current={active ? 'true' : undefined}
            aria-label={name}
            onClick={() => rememberLanguage(code)}
            preventScrollReset
            className={[
              'rounded-md px-2.5 py-1 text-xs font-semibold transition-colors',
              active
                ? 'bg-slate-900 text-white dark:bg-white dark:text-slate-900'
                : 'text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-white',
            ].join(' ')}
          >
            {label}
          </Link>
        )
      })}
    </div>
  )
}
