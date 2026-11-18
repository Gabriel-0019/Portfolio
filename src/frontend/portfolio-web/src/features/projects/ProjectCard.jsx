import { ArrowRight, Star } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { formatPeriod } from '../../lib/format.js'
import { useLanguage } from '../../lib/queries.js'
import { useLocalizedPath } from '../../lib/useLocalizedPath.js'
import { cardClasses } from '../../components/ui/styles.js'
import { TechBadgeList } from '../../components/ui/TechBadge.jsx'

const MAX_BADGES = 5

export function ProjectCover({ project, className = '' }) {
  if (project.coverImageUrl) {
    return (
      <img
        src={project.coverImageUrl}
        alt=""
        loading="lazy"
        className={`aspect-[16/9] w-full object-cover ${className}`}
      />
    )
  }

  return (
    <div
      aria-hidden="true"
      className={`flex aspect-[16/9] w-full items-center justify-center bg-gradient-to-br from-indigo-50 via-white to-slate-100 dark:from-indigo-950/60 dark:via-slate-900 dark:to-slate-900 ${className}`}
    >
      <span className="px-6 text-center text-xl font-semibold tracking-tight text-indigo-900/70 dark:text-indigo-200/80">
        {project.title}
      </span>
    </div>
  )
}

export default function ProjectCard({ project, headingLevel = 'h3' }) {
  const { t } = useTranslation()
  const lang = useLanguage()
  const to = useLocalizedPath()
  const Heading = headingLevel
  const extra = project.technologies.length - MAX_BADGES

  return (
    <article
      className={`${cardClasses} group relative flex w-full flex-col overflow-hidden transition-shadow hover:shadow-md has-[a:focus-visible]:outline-2 has-[a:focus-visible]:outline-offset-2 has-[a:focus-visible]:outline-indigo-600 dark:has-[a:focus-visible]:outline-indigo-400`}
    >
      <ProjectCover project={project} />
      <div className="flex flex-1 flex-col gap-3 p-5">
        <div className="flex flex-wrap items-center gap-2 text-xs font-medium text-slate-500 dark:text-slate-400">
          <span>{t(`projects.categories.${project.category}`, project.category)}</span>
          {project.startDate && (
            <>
              <span aria-hidden="true">·</span>
              <span>
                {formatPeriod(project.startDate, project.endDate, lang, t('common.present'))}
              </span>
            </>
          )}
          {project.isFeatured && (
            <span className="ml-auto inline-flex items-center gap-1 rounded-full bg-amber-50 px-2 py-0.5 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300">
              <Star className="h-3 w-3" aria-hidden="true" />
              {t('projects.featured')}
            </span>
          )}
        </div>

        <Heading className="text-lg font-semibold tracking-tight text-slate-900 dark:text-white">
          {/* The ::after overlay makes the whole card clickable while keeping one link per card. */}
          <Link
            to={to(`/projects/${project.slug}`)}
            className="after:absolute after:inset-0 after:content-[''] focus-visible:outline-none"
          >
            {project.title}
          </Link>
        </Heading>

        <p className="flex-1 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
          {project.summary}
        </p>

        <div className="flex items-end justify-between gap-3">
          <TechBadgeList technologies={project.technologies.slice(0, MAX_BADGES)} />
          {extra > 0 && <span className="shrink-0 text-xs text-slate-500">+{extra}</span>}
        </div>

        <span className="inline-flex items-center gap-1 text-sm font-medium text-indigo-600 dark:text-indigo-400">
          {t('project.viewCaseStudy')}
          <ArrowRight
            className="h-4 w-4 transition-transform group-hover:translate-x-0.5 motion-reduce:transition-none"
            aria-hidden="true"
          />
        </span>
      </div>
    </article>
  )
}
