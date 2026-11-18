import { LoaderCircle, Pencil, Plus, Save, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { ErrorState, LoadingState } from '../../components/ui/States.jsx'
import { buttonClasses } from '../../components/ui/styles.js'
import { adminApi } from '../api.js'
import { emptyToNull, TECH_CATEGORIES } from '../constants.js'
import { useAdminMutation, useAdminTechnologies } from '../queries.js'
import AdminPageHeader from '../ui/AdminPageHeader.jsx'
import { ErrorSummary, Field, inputClasses, Section } from '../ui/Fields.jsx'

const EMPTY = { name: '', iconKey: '', category: 'Language', displayOrder: 0 }
const categoryLabel = (value) => TECH_CATEGORIES.find((c) => c.value === value)?.label ?? value

function TechnologyForm({ technology, onDone }) {
  const isNew = !technology
  const { register, handleSubmit } = useForm({
    defaultValues: technology
      ? {
          name: technology.name,
          iconKey: technology.iconKey ?? '',
          category: technology.category,
          displayOrder: technology.displayOrder,
        }
      : EMPTY,
  })

  const save = useAdminMutation(
    (body) =>
      isNew ? adminApi.createTechnology(body) : adminApi.updateTechnology(technology.id, body),
    { onSuccess: onDone },
  )

  function onSubmit(values) {
    save.mutate({
      name: values.name.trim(),
      iconKey: emptyToNull(values.iconKey.trim()),
      category: values.category,
      displayOrder: Number(values.displayOrder) || 0,
    })
  }

  return (
    <Section title={isNew ? 'Nueva tecnología' : `Editar ${technology.name}`}>
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        {save.isError && <ErrorSummary error={save.error} />}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Field id="tech-name" label="Nombre">
            <input
              id="tech-name"
              required
              maxLength={60}
              className={inputClasses}
              {...register('name')}
            />
          </Field>
          <Field id="tech-category" label="Categoría">
            <select id="tech-category" className={inputClasses} {...register('category')}>
              {TECH_CATEGORIES.map(({ value, label }) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </Field>
          <Field id="tech-icon" label="Clave de ícono" hint="Opcional, p. ej. react.">
            <input
              id="tech-icon"
              maxLength={60}
              className={inputClasses}
              {...register('iconKey')}
            />
          </Field>
          <Field id="tech-order" label="Orden">
            <input
              id="tech-order"
              type="number"
              min="0"
              className={inputClasses}
              {...register('displayOrder')}
            />
          </Field>
        </div>
        <div className="flex gap-2">
          <button type="submit" disabled={save.isPending} className={buttonClasses()}>
            {save.isPending ? (
              <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : (
              <Save className="h-4 w-4" aria-hidden="true" />
            )}
            Guardar
          </button>
          <button type="button" onClick={onDone} className={buttonClasses({ variant: 'ghost' })}>
            <X className="h-4 w-4" aria-hidden="true" />
            Cancelar
          </button>
        </div>
      </form>
    </Section>
  )
}

export default function TechnologiesPage() {
  const { data: technologies, isPending, isError, refetch } = useAdminTechnologies()
  const [editing, setEditing] = useState(null) // null | 'new' | technology
  const remove = useAdminMutation((id) => adminApi.deleteTechnology(id))

  function confirmDelete(technology) {
    const usage =
      technology.projectCount > 0 ? ` Se quitará de ${technology.projectCount} proyecto(s).` : ''
    if (window.confirm(`¿Eliminar "${technology.name}"?${usage}`)) remove.mutate(technology.id)
  }

  return (
    <>
      <AdminPageHeader
        title="Tecnologías"
        description="Catálogo para etiquetar proyectos y mostrar tu stack."
        actions={
          <button type="button" onClick={() => setEditing('new')} className={buttonClasses()}>
            <Plus className="h-4 w-4" aria-hidden="true" />
            Nueva tecnología
          </button>
        }
      />

      {editing && (
        <div className="mb-6">
          <TechnologyForm
            key={editing === 'new' ? 'new' : editing.id}
            technology={editing === 'new' ? null : editing}
            onDone={() => setEditing(null)}
          />
        </div>
      )}
      {remove.isError && (
        <div className="mb-4">
          <ErrorSummary error={remove.error} />
        </div>
      )}

      {isPending ? (
        <LoadingState />
      ) : isError ? (
        <ErrorState onRetry={refetch} />
      ) : (
        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-200 text-xs tracking-wide text-slate-500 uppercase dark:border-slate-800 dark:text-slate-400">
              <tr>
                <th scope="col" className="px-4 py-3">
                  Nombre
                </th>
                <th scope="col" className="px-4 py-3">
                  Categoría
                </th>
                <th scope="col" className="px-4 py-3 text-right">
                  Proyectos
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
              {technologies.map((tech) => (
                <tr key={tech.id}>
                  <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">
                    {tech.name}
                  </td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                    {categoryLabel(tech.category)}
                  </td>
                  <td className="px-4 py-3 text-right tabular-nums">{tech.projectCount}</td>
                  <td className="px-4 py-3 text-right tabular-nums">{tech.displayOrder}</td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-1">
                      <button
                        type="button"
                        onClick={() => setEditing(tech)}
                        className={buttonClasses({ variant: 'ghost', size: 'sm' })}
                        aria-label={`Editar ${tech.name}`}
                      >
                        <Pencil className="h-4 w-4" aria-hidden="true" />
                      </button>
                      <button
                        type="button"
                        onClick={() => confirmDelete(tech)}
                        className={buttonClasses({
                          variant: 'ghost',
                          size: 'sm',
                          className: 'text-rose-600 dark:text-rose-400',
                        })}
                        aria-label={`Eliminar ${tech.name}`}
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
      )}
    </>
  )
}
