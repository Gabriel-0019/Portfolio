import {
  ArrowDown,
  ArrowLeft,
  ArrowUp,
  ExternalLink,
  LoaderCircle,
  Plus,
  Save,
  Trash2,
} from 'lucide-react'
import { useState } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States.jsx'
import { buttonClasses } from '../../components/ui/styles.js'
import { ApiError } from '../../lib/api.js'
import { adminApi } from '../api.js'
import {
  CATEGORY_LABELS,
  LANGUAGES,
  PROJECT_CATEGORIES,
  PROJECT_STATUSES,
  TECH_CATEGORIES,
} from '../constants.js'
import { EMPTY_PROJECT_FORM, formToRequest, projectToForm, slugify } from '../projectForm.js'
import { useAdminMutation, useAdminProject, useAdminTechnologies } from '../queries.js'
import AdminPageHeader from '../ui/AdminPageHeader.jsx'
import { ErrorSummary, Field, inputClasses, Section, SuccessNotice } from '../ui/Fields.jsx'
import MarkdownField from '../ui/MarkdownField.jsx'
import UploadField from '../ui/UploadField.jsx'

function TechnologyPicker({ technologies, register }) {
  const groups = TECH_CATEGORIES.map((category) => ({
    ...category,
    items: technologies.filter((t) => t.category === category.value),
  })).filter((g) => g.items.length > 0)

  if (technologies.length === 0) {
    return (
      <p className="text-sm text-slate-500">
        No hay tecnologías.{' '}
        <Link to="/admin/technologies" className="text-indigo-600 underline">
          Crea alguna
        </Link>
        .
      </p>
    )
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {groups.map((group) => (
        <fieldset key={group.value}>
          <legend className="mb-2 text-xs font-semibold tracking-wide text-slate-500 uppercase dark:text-slate-400">
            {group.label}
          </legend>
          <div className="flex flex-col gap-1.5">
            {group.items.map((tech) => (
              <label
                key={tech.id}
                className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300"
              >
                <input
                  type="checkbox"
                  value={String(tech.id)}
                  className="h-4 w-4 rounded border-slate-300"
                  {...register('technologyIds')}
                />
                {tech.name}
              </label>
            ))}
          </div>
        </fieldset>
      ))}
    </div>
  )
}

function ProjectForm({ project, technologies }) {
  const navigate = useNavigate()
  const location = useLocation()
  const isNew = !project
  const [saved, setSaved] = useState(Boolean(location.state?.created))

  const { register, control, handleSubmit, reset } = useForm({
    defaultValues: project ? projectToForm(project) : EMPTY_PROJECT_FORM,
  })
  const images = useFieldArray({ control, name: 'images' })

  const save = useAdminMutation(
    (body) => (isNew ? adminApi.createProject(body) : adminApi.updateProject(project.id, body)),
    {
      onSuccess: (result) => {
        if (isNew) {
          navigate(`/admin/projects/${result.id}`, { replace: true, state: { created: true } })
        } else {
          reset(projectToForm(result))
          setSaved(true)
        }
      },
    },
  )

  const remove = useAdminMutation(() => adminApi.deleteProject(project.id), {
    onSuccess: () => navigate('/admin/projects', { replace: true }),
  })

  const status = useWatch({ control, name: 'status' })
  const translations = useWatch({ control, name: 'translations' })

  // Shown as the slug placeholder and used when the field is left empty.
  const suggestedSlug = slugify(translations.en?.title || translations.es?.title || '')

  function onSubmit(values) {
    setSaved(false)
    save.mutate(formToRequest({ ...values, slug: values.slug.trim() || suggestedSlug }))
  }

  const title = isNew
    ? 'Nuevo proyecto'
    : (project.translations.find((t) => t.language === 'es')?.title ?? project.slug)

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-6">
      <AdminPageHeader
        title={title}
        description={
          isNew
            ? 'Puedes guardarlo como borrador y publicarlo cuando esté listo.'
            : `/${project.slug}`
        }
        actions={
          <>
            <Link to="/admin/projects" className={buttonClasses({ variant: 'ghost' })}>
              <ArrowLeft className="h-4 w-4" aria-hidden="true" />
              Volver
            </Link>
            {project?.isPubliclyVisible && (
              <a
                href={`/es/projects/${project.slug}`}
                target="_blank"
                rel="noopener noreferrer"
                className={buttonClasses({ variant: 'secondary' })}
              >
                <ExternalLink className="h-4 w-4" aria-hidden="true" />
                Ver en el sitio
              </a>
            )}
            <button type="submit" disabled={save.isPending} className={buttonClasses()}>
              {save.isPending ? (
                <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" />
              ) : (
                <Save className="h-4 w-4" aria-hidden="true" />
              )}
              Guardar
            </button>
          </>
        }
      />

      {save.isError && <ErrorSummary error={save.error} />}
      {saved && !save.isPending && <SuccessNotice>Cambios guardados.</SuccessNotice>}
      {status === 'Published' &&
        LANGUAGES.some(({ code }) => !translations[code]?.title?.trim()) && (
          <p
            role="status"
            className="rounded-lg bg-amber-50 p-3 text-sm text-amber-800 dark:bg-amber-950/40 dark:text-amber-300"
          >
            Para publicar, completa el título y el resumen en inglés y en español.
          </p>
        )}

      <Section title="Datos generales">
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Field
            id="slug"
            label="Slug (URL)"
            hint="Minúsculas y guiones. Si lo dejas vacío se usa el sugerido a partir del título."
            className="sm:col-span-2"
          >
            <input
              id="slug"
              className={inputClasses}
              placeholder={suggestedSlug || 'mi-proyecto'}
              {...register('slug')}
            />
          </Field>
          <Field id="category" label="Categoría">
            <select id="category" className={inputClasses} {...register('category')}>
              {PROJECT_CATEGORIES.map((value) => (
                <option key={value} value={value}>
                  {CATEGORY_LABELS[value]}
                </option>
              ))}
            </select>
          </Field>
          <Field id="status" label="Estado">
            <select id="status" className={inputClasses} {...register('status')}>
              {PROJECT_STATUSES.map(({ value, label }) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </Field>
          <Field id="startDate" label="Inicio">
            <input id="startDate" type="date" className={inputClasses} {...register('startDate')} />
          </Field>
          <Field id="endDate" label="Fin" hint="Vacío si sigue en curso.">
            <input id="endDate" type="date" className={inputClasses} {...register('endDate')} />
          </Field>
          <Field id="displayOrder" label="Orden" hint="Menor = aparece antes.">
            <input
              id="displayOrder"
              type="number"
              min="0"
              className={inputClasses}
              {...register('displayOrder')}
            />
          </Field>
          <div className="flex items-end pb-2">
            <label className="flex items-center gap-2 text-sm font-medium text-slate-700 dark:text-slate-300">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300"
                {...register('isFeatured')}
              />
              Destacado en el inicio
            </label>
          </div>
          <Field id="repoUrl" label="Repositorio" className="sm:col-span-2">
            <input
              id="repoUrl"
              type="url"
              placeholder="https://github.com/…"
              className={inputClasses}
              {...register('repoUrl')}
            />
          </Field>
          <Field id="demoUrl" label="Demo" className="sm:col-span-2">
            <input
              id="demoUrl"
              type="url"
              placeholder="https://…"
              className={inputClasses}
              {...register('demoUrl')}
            />
          </Field>
          <div className="sm:col-span-2 lg:col-span-4">
            <Controller
              control={control}
              name="coverImageUrl"
              render={({ field }) => (
                <UploadField
                  label="Imagen de portada"
                  value={field.value}
                  onChange={field.onChange}
                  hint="Recomendado 1600×900 (16:9). PNG, JPEG o WebP."
                />
              )}
            />
          </div>
        </div>
      </Section>

      <Section
        title="Contenido"
        description="El caso de estudio en cada idioma. Título y resumen son obligatorios para publicar."
      >
        <div className="grid gap-6 xl:grid-cols-2">
          {LANGUAGES.map(({ code, label }) => (
            <fieldset key={code} className="flex flex-col gap-4">
              <legend className="mb-1 text-sm font-semibold text-indigo-600 dark:text-indigo-400">
                {label}
              </legend>
              <Field id={`title-${code}`} label="Título">
                <input
                  id={`title-${code}`}
                  className={inputClasses}
                  maxLength={150}
                  {...register(`translations.${code}.title`)}
                />
              </Field>
              <Field
                id={`summary-${code}`}
                label="Resumen"
                hint="Una o dos frases para la tarjeta del proyecto."
              >
                <textarea
                  id={`summary-${code}`}
                  rows={3}
                  maxLength={300}
                  className={inputClasses}
                  {...register(`translations.${code}.summary`)}
                />
              </Field>
              <MarkdownField
                label="Caso de estudio"
                rows={14}
                value={translations[code]?.content}
                hint="Sugerencia: ## Problema, ## Solución, ## Arquitectura, ## Retos, ## Resultados."
                {...register(`translations.${code}.content`)}
              />
            </fieldset>
          ))}
        </div>
      </Section>

      <Section title="Tecnologías">
        <TechnologyPicker technologies={technologies} register={register} />
      </Section>

      <Section
        title="Galería"
        description="Capturas que se muestran al final del caso de estudio, en este orden."
        actions={
          <button
            type="button"
            onClick={() => images.append({ url: '', altEn: '', altEs: '' })}
            className={buttonClasses({ variant: 'secondary', size: 'sm' })}
          >
            <Plus className="h-4 w-4" aria-hidden="true" />
            Agregar imagen
          </button>
        }
      >
        {images.fields.length === 0 ? (
          <p className="text-sm text-slate-500 dark:text-slate-400">Sin imágenes.</p>
        ) : (
          <ol className="flex flex-col gap-4">
            {images.fields.map((item, index) => (
              <li
                key={item.id}
                className="rounded-xl border border-slate-200 p-4 dark:border-slate-700"
              >
                <div className="grid gap-4 lg:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,1fr)_auto]">
                  <Controller
                    control={control}
                    name={`images.${index}.url`}
                    render={({ field }) => (
                      <UploadField
                        label={`Imagen ${index + 1}`}
                        value={field.value}
                        onChange={field.onChange}
                      />
                    )}
                  />
                  <Field id={`alt-en-${item.id}`} label="Texto alternativo (EN)">
                    <input
                      id={`alt-en-${item.id}`}
                      className={inputClasses}
                      {...register(`images.${index}.altEn`)}
                    />
                  </Field>
                  <Field id={`alt-es-${item.id}`} label="Texto alternativo (ES)">
                    <input
                      id={`alt-es-${item.id}`}
                      className={inputClasses}
                      {...register(`images.${index}.altEs`)}
                    />
                  </Field>
                  <div className="flex items-end gap-1">
                    <button
                      type="button"
                      disabled={index === 0}
                      onClick={() => images.move(index, index - 1)}
                      className={buttonClasses({ variant: 'ghost', size: 'sm' })}
                      aria-label={`Subir imagen ${index + 1}`}
                    >
                      <ArrowUp className="h-4 w-4" aria-hidden="true" />
                    </button>
                    <button
                      type="button"
                      disabled={index === images.fields.length - 1}
                      onClick={() => images.move(index, index + 1)}
                      className={buttonClasses({ variant: 'ghost', size: 'sm' })}
                      aria-label={`Bajar imagen ${index + 1}`}
                    >
                      <ArrowDown className="h-4 w-4" aria-hidden="true" />
                    </button>
                    <button
                      type="button"
                      onClick={() => images.remove(index)}
                      className={buttonClasses({
                        variant: 'ghost',
                        size: 'sm',
                        className: 'text-rose-600',
                      })}
                      aria-label={`Quitar imagen ${index + 1}`}
                    >
                      <Trash2 className="h-4 w-4" aria-hidden="true" />
                    </button>
                  </div>
                </div>
              </li>
            ))}
          </ol>
        )}
      </Section>

      {!isNew && (
        <Section title="Zona de peligro">
          {remove.isError && (
            <div className="mb-3">
              <ErrorSummary error={remove.error} />
            </div>
          )}
          <button
            type="button"
            onClick={() => {
              if (window.confirm('¿Eliminar este proyecto? Esta acción no se puede deshacer.'))
                remove.mutate()
            }}
            className={buttonClasses({
              variant: 'secondary',
              className: 'text-rose-600 dark:text-rose-400',
            })}
          >
            <Trash2 className="h-4 w-4" aria-hidden="true" />
            Eliminar proyecto
          </button>
        </Section>
      )}
    </form>
  )
}

export default function ProjectEditPage() {
  const { id } = useParams()
  const technologies = useAdminTechnologies()
  const project = useAdminProject(id)

  if (technologies.isPending || (id && project.isPending)) return <LoadingState />

  if (id && project.isError) {
    if (project.error instanceof ApiError && project.error.status === 404) {
      return <EmptyState title="Proyecto no encontrado" body="Puede que se haya eliminado." />
    }
    return <ErrorState onRetry={project.refetch} />
  }
  if (technologies.isError) return <ErrorState onRetry={technologies.refetch} />

  // key: remount the form (fresh default values) when switching between projects.
  return (
    <ProjectForm
      key={id ?? 'new'}
      project={id ? project.data : null}
      technologies={technologies.data}
    />
  )
}
