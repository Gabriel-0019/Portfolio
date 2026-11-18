import { CircleAlert, CircleCheck } from 'lucide-react'
import { ApiError } from '../../lib/api.js'

export const inputClasses =
  'block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm placeholder:text-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100'

export function Field({ id, label, hint, error, children, className = '' }) {
  return (
    <div className={className}>
      <label
        htmlFor={id}
        className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300"
      >
        {label}
      </label>
      {children}
      {hint && !error && <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{hint}</p>}
      {error && <p className="mt-1 text-sm text-rose-600 dark:text-rose-400">{error}</p>}
    </div>
  )
}

export function Section({ title, description, children, actions }) {
  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6 dark:border-slate-800 dark:bg-slate-900">
      <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-base font-semibold text-slate-900 dark:text-white">{title}</h2>
          {description && (
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{description}</p>
          )}
        </div>
        {actions}
      </div>
      {children}
    </section>
  )
}

/** Turns "Translations[0].Title" into something readable. */
function prettifyKey(key) {
  return key
    .replace(/\[(\d+)\]/g, (_, index) => ` ${Number(index) + 1}`)
    .replace(/\./g, ' › ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
}

/** Shows an API error: ProblemDetails detail/title and each validation message. */
export function ErrorSummary({ error }) {
  if (!error) return null

  let title = 'No se pudo completar la operación.'
  let items = []

  if (error instanceof ApiError) {
    if (error.status === 401) title = 'La sesión expiró. Vuelve a iniciar sesión.'
    else if (error.status === 409)
      title = error.problem?.detail ?? 'Ya existe un registro con esos datos.'
    else if (error.status === 413) title = 'El archivo es demasiado grande (máximo 10 MB).'
    else if (error.problem?.detail) title = error.problem.detail
    else if (error.problem?.errors) title = 'Revisa los campos marcados:'

    items = Object.entries(error.problem?.errors ?? {}).flatMap(([key, messages]) =>
      messages.map((message) => (key ? `${prettifyKey(key)}: ${message}` : message)),
    )
  }

  return (
    <div
      role="alert"
      className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700 dark:bg-rose-950/40 dark:text-rose-300"
    >
      <p className="flex items-start gap-2 font-medium">
        <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
        {title}
      </p>
      {items.length > 0 && (
        <ul className="mt-2 list-disc space-y-0.5 pl-10">
          {items.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      )}
    </div>
  )
}

export function SuccessNotice({ children }) {
  return (
    <p
      role="status"
      className="flex items-center gap-2 rounded-lg bg-emerald-50 p-3 text-sm font-medium text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300"
    >
      <CircleCheck className="h-4 w-4" aria-hidden="true" />
      {children}
    </p>
  )
}

const BADGE = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  Published: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300',
  Archived: 'bg-amber-50 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300',
  info: 'bg-indigo-50 text-indigo-700 dark:bg-indigo-950/60 dark:text-indigo-300',
}

export function Badge({ tone = 'info', children }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${BADGE[tone] ?? BADGE.info}`}
    >
      {children}
    </span>
  )
}
