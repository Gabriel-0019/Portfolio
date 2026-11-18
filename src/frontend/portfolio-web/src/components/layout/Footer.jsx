import { useTranslation } from 'react-i18next'
import { useProfile } from '../../lib/queries.js'
import Container from '../ui/Container.jsx'
import { SocialLinks } from '../ui/SocialIcon.jsx'

const CURRENT_YEAR = new Date().getFullYear()

export default function Footer() {
  const { t } = useTranslation()
  const { data: profile } = useProfile()
  const name = profile?.fullName ?? t('common.siteName')

  return (
    <footer className="border-t border-slate-200 dark:border-slate-800">
      <Container className="flex flex-col items-center justify-between gap-4 py-8 text-sm text-slate-500 sm:flex-row dark:text-slate-400">
        <div className="text-center sm:text-left">
          <p>{t('footer.rights', { year: CURRENT_YEAR, name })}</p>
          <p className="mt-1">{t('footer.builtWith')}</p>
        </div>
        <SocialLinks links={profile?.socialLinks} />
      </Container>
    </footer>
  )
}
