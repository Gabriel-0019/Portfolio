import { ArrowLeft, ExternalLink } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import Container from '../components/ui/Container.jsx'
import Markdown from '../components/ui/Markdown.jsx'
import Seo from '../components/ui/Seo.jsx'
import SocialIcon from '../components/ui/SocialIcon.jsx'
import { EmptyState, ErrorState, LoadingState } from '../components/ui/States.jsx'
import { buttonClasses, cardClasses } from '../components/ui/styles.js'
import { TechBadgeList } from '../components/ui/TechBadge.jsx'
import { ProjectCover } from '../features/projects/ProjectCard.jsx'
import { ApiError } from '../lib/api.js'
import { formatPeriod } from '../lib/format.js'
import { useLanguage, useProject } from '../lib/queries.js'
import { useLocalizedPath } from '../lib/useLocalizedPath.js'

function BackLink() {
  const { t } = useTranslation()
  const to = useLocalizedPath()
  return (
    <Link
      to={to('/projects')}
      className="inline-flex items-center gap-1 text-sm font-medium text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-white"
    >
      <ArrowLeft className="h-4 w-4" aria-hidden="true" />
      {t('project.back')}
    </Link>
  )
}

function Fact({ label, children }) {
  return (
    <div>
      <dt className="text-xs font-semibold tracking-wide text-slate-500 uppercase dark:text-slate-400">
        {label}
      </dt>
      <dd className="mt-2 text-sm text-slate-900 dark:text-slate-100">{children}</dd>
    </div>
  )
}

export default function ProjectDetailPage() {
  const { t } = useTranslation()
  const lang = useLanguage()
  const { slug } = useParams()
  const { data: project, isPending, isError, error, refetch } = useProject(slug)

  if (isPending) {
    return (
      <Container className="py-12">
        <LoadingState />
      </Container>
    )
  }

  if (isError) {
    const notFound = error instanceof ApiError && error.status === 404
    return (
      <Container className="py-12">
        <BackLink />
        <div className="mt-8">
          {notFound ? (
            <>
              <Seo title={t('project.notFoundTitle')} />
              <EmptyState title={t('project.notFoundTitle')} body={t('project.notFoundBody')} />
            </>
          ) : (
            <ErrorState onRetry={refetch} />
          )}
        </div>
      </Container>
    )
  }

  const period = project.startDate
    ? formatPeriod(project.startDate, project.endDate, lang, t('common.present'))
    : null

  return (
    <article>
      <Seo title={project.title} description={project.summary} />

      <header className="border-b border-slate-200 bg-slate-50 dark:border-slate-800 dark:bg-slate-900/40">
        <Container className="py-10 sm:py-14">
          <BackLink />
          <p className="mt-6 text-sm font-semibold tracking-widest text-indigo-600 uppercase dark:text-indigo-400">
            {t(`projects.categories.${project.category}`, project.category)}
          </p>
          <h1 className="mt-2 max-w-3xl text-3xl font-semibold tracking-tight text-slate-900 sm:text-5xl dark:text-white">
            {project.title}
          </h1>
          <p className="mt-4 max-w-3xl text-lg text-slate-600 dark:text-slate-400">
            {project.summary}
          </p>
          {(project.repoUrl || project.demoUrl) && (
            <div className="mt-8 flex flex-wrap gap-3">
              {project.demoUrl && (
                <a
                  href={project.demoUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className={buttonClasses()}
                >
                  <ExternalLink className="h-4 w-4" aria-hidden="true" />
                  {t('project.demo')}
                </a>
              )}
              {project.repoUrl && (
                <a
                  href={project.repoUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className={buttonClasses({ variant: 'secondary' })}
                >
                  <SocialIcon platform="github" className="h-4 w-4" />
                  {t('project.repo')}
                </a>
              )}
            </div>
          )}
        </Container>
      </header>

      <Container className="grid gap-10 py-12 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <div className="min-w-0">
          {project.coverImageUrl && (
            <div className={`${cardClasses} mb-10 overflow-hidden`}>
              <ProjectCover project={project} />
            </div>
          )}
          <Markdown>{project.content}</Markdown>

          {project.images.length > 0 && (
            <section aria-labelledby="gallery-heading" className="mt-12">
              <h2
                id="gallery-heading"
                className="text-xl font-semibold tracking-tight text-slate-900 dark:text-white"
              >
                {t('project.gallery')}
              </h2>
              <ul className="mt-6 grid gap-4 sm:grid-cols-2">
                {project.images.map((image) => (
                  <li key={image.url} className={`${cardClasses} overflow-hidden`}>
                    <img src={image.url} alt={image.alt} loading="lazy" className="w-full" />
                  </li>
                ))}
              </ul>
            </section>
          )}
        </div>

        <aside className="lg:sticky lg:top-24 lg:self-start">
          <dl className={`${cardClasses} flex flex-col gap-6 p-6`}>
            <Fact label={t('project.technologies')}>
              <TechBadgeList technologies={project.technologies} />
            </Fact>
            <Fact label={t('project.category')}>
              {t(`projects.categories.${project.category}`, project.category)}
            </Fact>
            {period && <Fact label={t('project.period')}>{period}</Fact>}
          </dl>
        </aside>
      </Container>
    </article>
  )
}
