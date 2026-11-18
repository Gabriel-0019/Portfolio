import { Download, MapPin } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import Container from '../components/ui/Container.jsx'
import Markdown from '../components/ui/Markdown.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Seo from '../components/ui/Seo.jsx'
import { SocialLinks } from '../components/ui/SocialIcon.jsx'
import { EmptyState, ErrorState, LoadingState } from '../components/ui/States.jsx'
import { buttonClasses, cardClasses } from '../components/ui/styles.js'
import ExperienceTimeline from '../features/profile/ExperienceTimeline.jsx'
import TechStackGroups from '../features/profile/TechStackGroups.jsx'
import { initials } from '../lib/format.js'
import { useExperience, useProfile, useTechnologies } from '../lib/queries.js'

function ProfileCard({ profile }) {
  const { t } = useTranslation()

  return (
    <div className={`${cardClasses} flex flex-col items-center gap-4 p-6 text-center`}>
      {profile.avatarUrl ? (
        <img
          src={profile.avatarUrl}
          alt={profile.fullName}
          className="h-32 w-32 rounded-full object-cover"
        />
      ) : (
        <div
          aria-hidden="true"
          className="flex h-32 w-32 items-center justify-center rounded-full bg-indigo-100 text-3xl font-semibold text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300"
        >
          {initials(profile.fullName)}
        </div>
      )}
      <div>
        <p className="text-lg font-semibold text-slate-900 dark:text-white">{profile.fullName}</p>
        <p className="text-sm text-slate-600 dark:text-slate-400">{profile.headline}</p>
        {profile.location && (
          <p className="mt-2 inline-flex items-center gap-1 text-sm text-slate-500 dark:text-slate-400">
            <MapPin className="h-4 w-4" aria-hidden="true" />
            {profile.location}
          </p>
        )}
      </div>
      {profile.cvUrl && (
        <a href={profile.cvUrl} download className={buttonClasses({ className: 'w-full' })}>
          <Download className="h-4 w-4" aria-hidden="true" />
          {t('about.downloadCv')}
        </a>
      )}
      <SocialLinks links={profile.socialLinks} className="justify-center" />
    </div>
  )
}

function Section({ id, title, children }) {
  return (
    <section aria-labelledby={id} className="mt-12 first:mt-0">
      <h2
        id={id}
        className="mb-6 text-2xl font-semibold tracking-tight text-slate-900 dark:text-white"
      >
        {title}
      </h2>
      {children}
    </section>
  )
}

export default function AboutPage() {
  const { t } = useTranslation()
  const profile = useProfile()
  const experience = useExperience()
  const technologies = useTechnologies()

  let body
  if (profile.isPending) {
    body = <LoadingState />
  } else if (profile.isError) {
    body = <ErrorState onRetry={profile.refetch} />
  } else {
    body = (
      <div className="grid gap-10 lg:grid-cols-[18rem_minmax(0,1fr)]">
        <aside className="lg:sticky lg:top-24 lg:self-start">
          <ProfileCard profile={profile.data} />
        </aside>

        <div className="min-w-0">
          {profile.data.bio && <Markdown className="prose-lg">{profile.data.bio}</Markdown>}

          <Section id="experience-heading" title={t('about.experience')}>
            {experience.isPending ? (
              <LoadingState />
            ) : experience.isError ? (
              <ErrorState onRetry={experience.refetch} />
            ) : experience.data.length === 0 ? (
              <EmptyState title={t('about.emptyExperience')} />
            ) : (
              <ExperienceTimeline items={experience.data} />
            )}
          </Section>

          {technologies.data?.length > 0 && (
            <Section id="stack-heading" title={t('about.stack')}>
              <TechStackGroups technologies={technologies.data} />
            </Section>
          )}
        </div>
      </div>
    )
  }

  return (
    <>
      <Seo
        title={t('about.title')}
        description={
          profile.data ? t('seo.aboutDescription', { name: profile.data.fullName }) : undefined
        }
      />
      <PageHeader title={t('about.title')} subtitle={t('about.subtitle')} />
      <Container className="py-12">{body}</Container>
    </>
  )
}
