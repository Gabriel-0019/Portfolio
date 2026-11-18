import { LoaderCircle, Pencil, Plus, Save, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States.jsx'
import { buttonClasses, cardClasses } from '../../components/ui/styles.js'
import { formatPeriod } from '../../lib/format.js'
import { adminApi } from '../api.js'
import { emptyToNull, EXPERIENCE_TYPES, LANGUAGES } from '../constants.js'
import { useAdminExperience, useAdminMutation } from '../queries.js'
import AdminPageHeader from '../ui/AdminPageHeader.jsx'
import { Badge, ErrorSummary, Field, inputClasses, Section } from '../ui/Fields.jsx'
import MarkdownField from '../ui/MarkdownField.jsx'

const emptyTranslation = { role: '', location: '', description: '' }
const EMPTY = {
  company: '',
  companyUrl: '',
  type: 'Work',
  startDate: '',
  endDate: '',
  displayOrder: 0,
  translations: { en: { ...emptyTranslation }, es: { ...emptyTranslation } },
}

function toForm(item) {
  if (!item) return EMPTY
  return {
    company: item.company,
    companyUrl: item.companyUrl ?? '',
    type: item.type,
    startDate: item.startDate,
    endDate: item.endDate ?? '',
    displayOrder: item.displayOrder,
    translations: Object.fromEntries(
      LANGUAGES.map(({ code }) => {
        const t = item.translations.find((x) => x.language === code)
        return [
          code,
          t
            ? { role: t.role, location: t.location ?? '', description: t.description ?? '' }
            : { ...emptyTranslation },
        ]
      }),
    ),
  }
}

function toRequest(values) {
  return {
    company: values.company.trim(),
    companyUrl: emptyToNull(values.companyUrl.trim()),
    type: values.type,
    startDate: values.startDate,
    endDate: emptyToNull(values.endDate),
    displayOrder: Number(values.displayOrder) || 0,
    translations: LANGUAGES.map(({ code }) => ({
      language: code,
      role: values.translations[code].role.trim(),
      location: emptyToNull(values.translations[code].location.trim()),
      description: values.translations[code].description.trim(),
    })),
  }
}

function ExperienceForm({ item, onDone }) {
  const isNew = !item
  const { register, control, handleSubmit } = useForm({ defaultValues: toForm(item) })
  const translations = useWatch({ control, name: 'translations' })
  const save = useAdminMutation(
    (body) => (isNew ? adminApi.createExperience(body) : adminApi.updateExperience(item.id, body)),
    { onSuccess: onDone },
  )

  return (
    <Section title={isNew ? 'Nueva entrada' : `Editar ${item.company}`}>
      <form
        onSubmit={handleSubmit((values) => save.mutate(toRequest(values)))}
        className="flex flex-col gap-5"
      >
        {save.isError && <ErrorSummary error={save.error} />}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <Field id="exp-company" label="Empresa o institución">
            <input
              id="exp-company"
              required
              maxLength={120}
              className={inputClasses}
              {...register('company')}
            />
          </Field>
          <Field id="exp-url" label="Sitio web" hint="Opcional.">
            <input id="exp-url" type="url" className={inputClasses} {...register('companyUrl')} />
          </Field>
          <Field id="exp-type" label="Tipo">
            <select id="exp-type" className={inputClasses} {...register('type')}>
              {EXPERIENCE_TYPES.map(({ value, label }) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </Field>
          <Field id="exp-start" label="Inicio">
            <input
              id="exp-start"
              type="date"
              required
              className={inputClasses}
              {...register('startDate')}
            />
          </Field>
          <Field id="exp-end" label="Fin" hint="Vacío = actualidad.">
            <input id="exp-end" type="date" className={inputClasses} {...register('endDate')} />
          </Field>
          <Field id="exp-order" label="Orden" hint="0 = ordenar por fecha.">
            <input
              id="exp-order"
              type="number"
              min="0"
              className={inputClasses}
              {...register('displayOrder')}
            />
          </Field>
        </div>

        <div className="grid gap-6 xl:grid-cols-2">
          {LANGUAGES.map(({ code, label }) => (
            <fieldset key={code} className="flex flex-col gap-4">
              <legend className="mb-1 text-sm font-semibold text-indigo-600 dark:text-indigo-400">
                {label}
              </legend>
              <Field id={`exp-role-${code}`} label="Puesto o título">
                <input
                  id={`exp-role-${code}`}
                  required
                  maxLength={120}
                  className={inputClasses}
                  {...register(`translations.${code}.role`)}
                />
              </Field>
              <Field id={`exp-location-${code}`} label="Ubicación" hint="Opcional, p. ej. Remoto.">
                <input
                  id={`exp-location-${code}`}
                  maxLength={120}
                  className={inputClasses}
                  {...register(`translations.${code}.location`)}
                />
              </Field>
              <MarkdownField
                label="Descripción"
                rows={6}
                value={translations[code]?.description}
                {...register(`translations.${code}.description`)}
              />
            </fieldset>
          ))}
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

export default function ExperiencePage() {
  const { data: items, isPending, isError, refetch } = useAdminExperience()
  const [editing, setEditing] = useState(null)
  const remove = useAdminMutation((id) => adminApi.deleteExperience(id))

  return (
    <>
      <AdminPageHeader
        title="Experiencia"
        description="Trabajos, formación y certificaciones que aparecen en Sobre mí."
        actions={
          <button type="button" onClick={() => setEditing('new')} className={buttonClasses()}>
            <Plus className="h-4 w-4" aria-hidden="true" />
            Nueva entrada
          </button>
        }
      />

      {editing && (
        <div className="mb-6">
          <ExperienceForm
            key={editing === 'new' ? 'new' : editing.id}
            item={editing === 'new' ? null : editing}
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
      ) : items.length === 0 ? (
        <EmptyState title="Sin experiencia registrada" />
      ) : (
        <ul className="flex flex-col gap-3">
          {items.map((item) => {
            const es = item.translations.find((t) => t.language === 'es')
            return (
              <li
                key={item.id}
                className={`${cardClasses} flex flex-wrap items-center justify-between gap-3 p-4`}
              >
                <div>
                  <p className="font-medium text-slate-900 dark:text-white">
                    {es?.role} ·{' '}
                    <span className="font-normal text-slate-600 dark:text-slate-300">
                      {item.company}
                    </span>
                  </p>
                  <p className="mt-1 flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400">
                    <Badge>{EXPERIENCE_TYPES.find((t) => t.value === item.type)?.label}</Badge>
                    {formatPeriod(item.startDate, item.endDate, 'es', 'Actualidad')}
                  </p>
                </div>
                <div className="flex gap-1">
                  <button
                    type="button"
                    onClick={() => setEditing(item)}
                    className={buttonClasses({ variant: 'ghost', size: 'sm' })}
                    aria-label={`Editar ${item.company}`}
                  >
                    <Pencil className="h-4 w-4" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      if (window.confirm(`¿Eliminar "${item.company}"?`)) remove.mutate(item.id)
                    }}
                    className={buttonClasses({
                      variant: 'ghost',
                      size: 'sm',
                      className: 'text-rose-600 dark:text-rose-400',
                    })}
                    aria-label={`Eliminar ${item.company}`}
                  >
                    <Trash2 className="h-4 w-4" aria-hidden="true" />
                  </button>
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </>
  )
}
