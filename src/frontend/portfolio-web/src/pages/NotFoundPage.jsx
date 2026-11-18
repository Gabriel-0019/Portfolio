import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Container from '../components/ui/Container.jsx'
import Seo from '../components/ui/Seo.jsx'
import { buttonClasses } from '../components/ui/styles.js'
import { useLocalizedPath } from '../lib/useLocalizedPath.js'

export default function NotFoundPage() {
  const { t } = useTranslation()
  const to = useLocalizedPath()

  return (
    <Container className="flex flex-col items-center gap-4 py-24 text-center">
      <Seo title={t('notFound.title')} />
      <p className="text-sm font-semibold text-indigo-600 dark:text-indigo-400">404</p>
      <h1 className="text-3xl font-semibold tracking-tight text-slate-900 sm:text-4xl dark:text-white">
        {t('notFound.title')}
      </h1>
      <p className="text-slate-600 dark:text-slate-400">{t('notFound.body')}</p>
      <Link to={to('/')} className={buttonClasses({ className: 'mt-4' })}>
        {t('notFound.home')}
      </Link>
    </Container>
  )
}
