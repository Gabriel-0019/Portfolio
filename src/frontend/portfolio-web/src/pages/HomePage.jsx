import { ArrowRight, Mail } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Container from '../components/ui/Container.jsx'
import Seo from '../components/ui/Seo.jsx'
import { SocialLinks } from '../components/ui/SocialIcon.jsx'
import { EmptyState, ErrorState, LoadingState } from '../components/ui/States.jsx'
import { buttonClasses } from '../components/ui/styles.js'
import { TechBadgeList } from '../components/ui/TechBadge.jsx'
import ProjectGrid from '../features/projects/ProjectGrid.jsx'
import { useProfile, useProjects, useTechnologies } from '../lib/queries.js'
import { useLocalizedPath } from '../lib/useLocalizedPath.js'

const FEATURED_LIMIT = 3

function SectionHeading({ title, subtitle, action }) {
  return (
    <div className="mb-8 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900 dark:text-white">
          {title}
        </h2>
        {subtitle && <p className="mt-1 text-slate-600 dark:text-slate-400">{subtitle}</p>}
      </div>
      {action}
    </div>
  )
}

function Hero() {
  const { t } = useTranslation()
  const to = useLocalizedPath()
  const { data: profile } = useProfile()

  return (
    <section className="relative overflow-hidden border-b border-slate-200 dark:border-slate-800">
      <div
        aria-hidden="true"
        className="absolute inset-0 -z-10 bg-[radial-gradient(60%_50%_at_50%_0%,rgba(99,102,241,0.12),transparent)] dark:bg-[radial-gradient(60%_50%_at_50%_0%,rgba(99,102,241,0.18),transparent)]"
      />
      <Container className="py-20 sm:py-28">
        <p className="text-sm font-semibold tracking-widest text-indigo-600 uppercase dark:text-indigo-400">
          {profile?.headline || t('home.eyebrow')}
        </p>
        <h1 className="mt-4 max-w-3xl text-4xl font-semibold tracking-tight text-slate-900 sm:text-6xl dark:text-white">
          {profile ? t('home.greeting', { name: profile.fullName }) : t('home.eyebrow')}
        </h1>
        {profile?.bio && (
          <p className="mt-6 max-w-2xl text-lg leading-relaxed text-slate-600 dark:text-slate-400">
            {profile.bio}
          </p>
        )}
        <div className="mt-10 flex flex-wrap items-center gap-3">
          <Link to={to('/projects')} className={buttonClasses({ size: 'lg' })}>
            {t('home.viewProjects')}
            <ArrowRight className="h-4 w-4" aria-hidden="true" />
          </Link>
          <Link to={to('/contact')} className={buttonClasses({ variant: 'secondary', size: 'lg' })}>
            <Mail className="h-4 w-4" aria-hidden="true" />
            {t('home.contactMe')}
          </Link>
          <SocialLinks links={profile?.socialLinks} className="sm:ml-2" />
        </div>
      </Container>
    </section>
  )
}

function FeaturedProjects() {
  const { t } = useTranslation()
  const to = useLocalizedPath()
  const { data: projects, isPending, isError, refetch } = useProjects({ featured: true })

  let content
  if (isPending) content = <LoadingState />
  else if (isError) content = <ErrorState onRetry={refetch} />
  else if (projects.length === 0)
    content = <EmptyState title={t('projects.emptyTitle')} body={t('projects.emptyBody')} />
  else content = <ProjectGrid projects={projects.slice(0, FEATURED_LIMIT)} />

  return (
    <section aria-labelledby="featured-heading" className="py-16 sm:py-20">
      <Container>
        <SectionHeading
          title={<span id="featured-heading">{t('home.featuredTitle')}</span>}
          subtitle={t('home.featuredSubtitle')}
          action={
            <Link
              to={to('/projects')}
              className="inline-flex items-center gap-1 text-sm font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400"
            >
              {t('home.allProjects')}
              <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </Link>
          }
        />
        {content}
      </Container>
    </section>
  )
}

function TechStack() {
  const { t } = useTranslation()
  const { data: technologies } = useTechnologies()
  if (!technologies?.length) return null

  return (
    <section
      aria-labelledby="stack-heading"
      className="border-y border-slate-200 bg-slate-50 py-16 dark:border-slate-800 dark:bg-slate-900/40"
    >
      <Container>
        <h2
          id="stack-heading"
          className="text-2xl font-semibold tracking-tight text-slate-900 dark:text-white"
        >
          {t('home.stackTitle')}
        </h2>
        <TechBadgeList technologies={technologies} size="md" className="mt-6" />
      </Container>
    </section>
  )
}

function ContactCta() {
  const { t } = useTranslation()
  const to = useLocalizedPath()

  return (
    <section className="py-16 sm:py-20">
      <Container>
        <div className="rounded-3xl bg-slate-900 px-6 py-12 text-center sm:px-12 dark:bg-slate-800">
          <h2 className="text-2xl font-semibold tracking-tight text-white sm:text-3xl">
            {t('home.ctaTitle')}
          </h2>
          <p className="mx-auto mt-3 max-w-xl text-slate-300">{t('home.ctaBody')}</p>
          <Link to={to('/contact')} className={buttonClasses({ size: 'lg', className: 'mt-8' })}>
            {t('home.contactMe')}
          </Link>
        </div>
      </Container>
    </section>
  )
}

export default function HomePage() {
  const { t } = useTranslation()
  const { data: profile } = useProfile()

  return (
    <>
      <Seo
        description={
          profile
            ? t('seo.homeDescription', { name: profile.fullName, headline: profile.headline })
            : undefined
        }
      />
      <Hero />
      <FeaturedProjects />
      <TechStack />
      <ContactCta />
    </>
  )
}
