import { FileText, LoaderCircle, Upload, X } from 'lucide-react'
import { useId, useRef } from 'react'
import { buttonClasses } from '../../components/ui/styles.js'
import { useAdminMutation } from '../queries.js'
import { adminApi } from '../api.js'
import { ErrorSummary, inputClasses } from './Fields.jsx'

const ACCEPT = {
  image: 'image/png,image/jpeg,image/gif,image/webp',
  pdf: 'application/pdf',
}

/**
 * URL input with an upload button. The uploaded file's URL is written into the field,
 * so an external URL can also be pasted by hand.
 */
export default function UploadField({ label, value, onChange, kind = 'image', hint }) {
  const id = useId()
  const fileInput = useRef(null)
  const upload = useAdminMutation((file) => adminApi.upload(file))

  async function handleFile(event) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    const stored = await upload.mutateAsync(file).catch(() => null)
    if (stored) onChange(stored.url)
  }

  return (
    <div>
      <label
        htmlFor={id}
        className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300"
      >
        {label}
      </label>
      <div className="flex flex-wrap items-start gap-3">
        {value && kind === 'image' && (
          <img
            src={value}
            alt=""
            className="h-16 w-24 rounded-md border border-slate-200 object-cover dark:border-slate-700"
          />
        )}
        {value && kind === 'pdf' && (
          <a
            href={value}
            target="_blank"
            rel="noopener noreferrer"
            className="flex h-16 w-24 items-center justify-center rounded-md border border-slate-200 text-slate-500 hover:text-indigo-600 dark:border-slate-700"
            aria-label="Abrir archivo"
          >
            <FileText className="h-6 w-6" aria-hidden="true" />
          </a>
        )}
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          <input
            id={id}
            type="text"
            value={value ?? ''}
            onChange={(event) => onChange(event.target.value)}
            placeholder="https://… o sube un archivo"
            className={inputClasses}
          />
          <div className="flex gap-2">
            <button
              type="button"
              disabled={upload.isPending}
              onClick={() => fileInput.current?.click()}
              className={buttonClasses({ variant: 'secondary', size: 'sm' })}
            >
              {upload.isPending ? (
                <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" />
              ) : (
                <Upload className="h-4 w-4" aria-hidden="true" />
              )}
              {upload.isPending ? 'Subiendo…' : 'Subir archivo'}
            </button>
            {value && (
              <button
                type="button"
                onClick={() => onChange('')}
                className={buttonClasses({ variant: 'ghost', size: 'sm' })}
              >
                <X className="h-4 w-4" aria-hidden="true" />
                Quitar
              </button>
            )}
          </div>
          <input
            ref={fileInput}
            type="file"
            accept={ACCEPT[kind]}
            onChange={handleFile}
            className="hidden"
            data-testid={`${id}-file`}
          />
        </div>
      </div>
      {hint && <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{hint}</p>}
      {upload.isError && (
        <div className="mt-2">
          <ErrorSummary error={upload.error} />
        </div>
      )}
    </div>
  )
}
