import { useTranslation } from 'react-i18next'
import Container from '../components/ui/Container.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Seo from '../components/ui/Seo.jsx'
import { SocialLinks } from '../components/ui/SocialIcon.jsx'
import { cardClasses } from '../components/ui/styles.js'
import ContactForm from '../features/contact/ContactForm.jsx'
import { useProfile } from '../lib/queries.js'

export default function ContactPage() {
  const { t } = useTranslation()
  const { data: profile } = useProfile()

  return (
    <>
      <Seo
        title={t('contact.title')}
        description={profile ? t('seo.contactDescription', { name: profile.fullName }) : undefined}
      />
      <PageHeader title={t('contact.title')} subtitle={t('contact.subtitle')} />
      <Container className="grid gap-10 py-12 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <div className={`${cardClasses} relative p-6 sm:p-8`}>
          <ContactForm />
        </div>
        {profile?.socialLinks?.length > 0 && (
          <aside>
            <h2 className="text-sm font-semibold tracking-wide text-slate-500 uppercase dark:text-slate-400">
              {t('contact.directTitle')}
            </h2>
            <SocialLinks links={profile.socialLinks} withLabels className="mt-4 flex-col" />
          </aside>
        )}
      </Container>
    </>
  )
}
