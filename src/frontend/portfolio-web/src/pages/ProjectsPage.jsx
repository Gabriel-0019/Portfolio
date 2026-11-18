import { SearchX } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import Container from '../components/ui/Container.jsx'
import PageHeader from '../components/ui/PageHeader.jsx'
import Seo from '../components/ui/Seo.jsx'
import { EmptyState, ErrorState, LoadingState } from '../components/ui/States.jsx'
import { buttonClasses } from '../components/ui/styles.js'
import { CATEGORIES } from '../features/projects/categories.js'
import ProjectFilters from '../features/projects/ProjectFilters.jsx'
import ProjectGrid from '../features/projects/ProjectGrid.jsx'
import { useProfile, useProjects } from '../lib/queries.js'

export default function ProjectsPage() {
  const { t } = useTranslation()
  const { data: profile } = useProfile()
  const [searchParams, setSearchParams] = useSearchParams()

  const rawCategory = searchParams.get('category')
  const category = CATEGORIES.includes(rawCategory) ? rawCategory : null
  const tech = searchParams.get('tech') || null
  const hasFilters = Boolean(category || tech)

  const {
    data: projects,
    isPending,
    isError,
    refetch,
  } = useProjects({
    ...(category ? { category } : {}),
    ...(tech ? { tech } : {}),
  })

  function updateFilters(changes) {
    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current)
        for (const [key, value] of Object.entries(changes)) {
          if (value) next.set(key, value)
          else next.delete(key)
        }
        return next
      },
      { replace: true, preventScrollReset: true },
    )
  }

  let content
  if (isPending) {
    content = <LoadingState />
  } else if (isError) {
    content = <ErrorState onRetry={refetch} />
  } else if (projects.length === 0 && hasFilters) {
    content = (
      <EmptyState
        icon={SearchX}
        title={t('projects.noResultsTitle')}
        body={t('projects.noResultsBody')}
      >
        <button
          type="button"
          className={buttonClasses({ variant: 'secondary', className: 'mt-2' })}
          onClick={() => updateFilters({ category: null, tech: null })}
        >
          {t('projects.filters.clear')}
        </button>
      </EmptyState>
    )
  } else if (projects.length === 0) {
    content = <EmptyState title={t('projects.emptyTitle')} body={t('projects.emptyBody')} />
  } else {
    content = (
      <>
        <p className="mb-4 text-sm text-slate-500 dark:text-slate-400" aria-live="polite">
          {t('projects.count', { count: projects.length })}
        </p>
        <ProjectGrid projects={projects} headingLevel="h2" />
      </>
    )
  }

  return (
    <>
      <Seo
        title={t('projects.title')}
        description={profile ? t('seo.projectsDescription', { name: profile.fullName }) : undefined}
      />
      <PageHeader title={t('projects.title')} subtitle={t('projects.subtitle')} />
      <Container className="py-10 sm:py-12">
        <ProjectFilters category={category} tech={tech} onChange={updateFilters} />
        <div className="mt-8">{content}</div>
      </Container>
    </>
  )
}
