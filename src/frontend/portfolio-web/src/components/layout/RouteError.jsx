import { useTranslation } from 'react-i18next'
import { Link, useRouteError } from 'react-router'
import { buttonClasses } from '../ui/styles.js'

/** Last-resort screen for unexpected rendering errors. */
export default function RouteError() {
  const { t } = useTranslation()
  const error = useRouteError()

  if (import.meta.env.DEV) {
    console.error(error)
  }

  return (
    <main className="flex min-h-svh flex-col items-center justify-center gap-4 px-4 text-center">
      <h1 className="text-2xl font-semibold">{t('common.errorTitle')}</h1>
      <p className="max-w-md text-slate-600 dark:text-slate-400">{t('common.errorBody')}</p>
      <Link to="/" className={buttonClasses()}>
        {t('notFound.home')}
      </Link>
    </main>
  )
}
