import { useId, useState } from 'react'
import Markdown from '../../components/ui/Markdown.jsx'
import { inputClasses } from './Fields.jsx'

/** Markdown textarea with a Write / Preview toggle. Works with react-hook-form's register(). */
export default function MarkdownField({ label, value, rows = 10, hint, ...inputProps }) {
  const id = useId()
  const [preview, setPreview] = useState(false)

  const tab = (active) =>
    `rounded-md px-2.5 py-1 text-xs font-medium ${
      active
        ? 'bg-slate-900 text-white dark:bg-white dark:text-slate-900'
        : 'text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-white'
    }`

  return (
    <div>
      <div className="mb-1.5 flex items-center justify-between gap-2">
        <label htmlFor={id} className="text-sm font-medium text-slate-700 dark:text-slate-300">
          {label}
        </label>
        <div className="flex gap-1" role="group" aria-label={`${label}: modo`}>
          <button
            type="button"
            className={tab(!preview)}
            aria-pressed={!preview}
            onClick={() => setPreview(false)}
          >
            Escribir
          </button>
          <button
            type="button"
            className={tab(preview)}
            aria-pressed={preview}
            onClick={() => setPreview(true)}
          >
            Vista previa
          </button>
        </div>
      </div>
      <textarea
        id={id}
        rows={rows}
        className={`${inputClasses} font-mono ${preview ? 'hidden' : ''}`}
        {...inputProps}
      />
      {preview && (
        <div className="min-h-32 rounded-lg border border-slate-200 p-4 dark:border-slate-700">
          {value?.trim() ? (
            <Markdown className="prose-sm">{value}</Markdown>
          ) : (
            <p className="text-sm text-slate-400">Nada que mostrar.</p>
          )}
        </div>
      )}
      <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
        {hint ?? 'Admite Markdown: ## títulos, **negrita**, listas con -, [enlaces](https://…).'}
      </p>
    </div>
  )
}
