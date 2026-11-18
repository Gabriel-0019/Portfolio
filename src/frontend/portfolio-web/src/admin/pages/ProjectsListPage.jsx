import { Eye, Pencil, Plus, Star, Trash2 } from 'lucide-react'
import { Link } from 'react-router'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States.jsx'
import { buttonClasses } from '../../components/ui/styles.js'
import { adminApi } from '../api.js'
import { CATEGORY_LABELS, PROJECT_STATUSES } from '../constants.js'
import { useAdminMutation, useAdminProjects } from '../queries.js'
import { Badge, ErrorSummary } from '../ui/Fields.jsx'
import AdminPageHeader from '../ui/AdminPageHeader.jsx'

const statusLabel = (value) => PROJECT_STATUSES.find((s) => s.value === value)?.label ?? value

export default function ProjectsListPage() {
  const { data: projects, isPending, isError, refetch } = useAdminProjects()
  const remove = useAdminMutation((id) => adminApi.deleteProject(id))

  function confirmDelete(project) {
    const name = project.titleEs ?? project.titleEn ?? project.slug
    if (window.confirm(`¿Eliminar "${name}"? Esta acción no se puede deshacer.`)) {
      remove.mutate(project.id)
    }
  }

  let content
  if (isPending) content = <LoadingState />
  else if (isError) content = <ErrorState onRetry={refetch} />
  else if (projects.length === 0)
    content = (
      <EmptyState
        title="Todavía no hay proyectos"
        body="Crea el primero con el botón Nuevo proyecto."
      />
    )
  else
    content = (
      <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 text-xs tracking-wide text-slate-500 uppercase dark:border-slate-800 dark:text-slate-400">
            <tr>
              <th scope="col" className="px-4 py-3">
                Proyecto
              </th>
              <th scope="col" className="px-4 py-3">
                Categoría
              </th>
              <th scope="col" className="px-4 py-3">
                Estado
              </th>
              <th scope="col" className="px-4 py-3 text-right">
                Orden
              </th>
              <th scope="col" className="px-4 py-3">
                <span className="sr-only">Acciones</span>
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {projects.map((project) => (
              <tr key={project.id}>
                <td className="px-4 py-3">
                  <Link
                    to={`/admin/projects/${project.id}`}
                    className="font-medium text-slate-900 hover:text-indigo-600 dark:text-white dark:hover:text-indigo-400"
                  >
                    {project.titleEs ?? project.titleEn ?? project.slug}
                  </Link>
                  <p className="text-xs text-slate-500 dark:text-slate-400">
                    /{project.slug}
                    {!project.titleEn && ' · falta inglés'}
                    {!project.titleEs && ' · falta español'}
                  </p>
                </td>
                <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                  {CATEGORY_LABELS[project.category]}
                </td>
                <td className="px-4 py-3">
                  <div className="flex flex-wrap items-center gap-1.5">
                    <Badge tone={project.status}>{statusLabel(project.status)}</Badge>
                    {project.isFeatured && (
                      <Badge>
                        <Star className="mr-1 h-3 w-3" aria-hidden="true" />
                        Destacado
                      </Badge>
                    )}
                    {project.status === 'Published' && !project.isPubliclyVisible && (
                      <Badge tone="Archived">No visible: faltan traducciones</Badge>
                    )}
                  </div>
                </td>
                <td className="px-4 py-3 text-right tabular-nums text-slate-600 dark:text-slate-300">
                  {project.displayOrder}
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1">
                    {project.isPubliclyVisible && (
                      <a
                        href={`/es/projects/${project.slug}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className={buttonClasses({ variant: 'ghost', size: 'sm' })}
                        aria-label={`Ver ${project.slug} en el sitio`}
                      >
                        <Eye className="h-4 w-4" aria-hidden="true" />
                      </a>
                    )}
                    <Link
                      to={`/admin/projects/${project.id}`}
                      className={buttonClasses({ variant: 'ghost', size: 'sm' })}
                      aria-label={`Editar ${project.slug}`}
                    >
                      <Pencil className="h-4 w-4" aria-hidden="true" />
                    </Link>
                    <button
                      type="button"
                      onClick={() => confirmDelete(project)}
                      className={buttonClasses({
                        variant: 'ghost',
                        size: 'sm',
                        className: 'text-rose-600 dark:text-rose-400',
                      })}
                      aria-label={`Eliminar ${project.slug}`}
                    >
                      <Trash2 className="h-4 w-4" aria-hidden="true" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    )

  return (
    <>
      <AdminPageHeader
        title="Proyectos"
        description="Solo se muestran en el sitio los proyectos publicados con contenido en inglés y español."
        actions={
          <Link to="/admin/projects/new" className={buttonClasses()}>
            <Plus className="h-4 w-4" aria-hidden="true" />
            Nuevo proyecto
          </Link>
        }
      />
      {remove.isError && (
        <div className="mb-4">
          <ErrorSummary error={remove.error} />
        </div>
      )}
      {content}
    </>
  )
}
