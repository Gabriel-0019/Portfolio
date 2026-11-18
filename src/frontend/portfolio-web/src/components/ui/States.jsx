import { CircleAlert, FolderOpen, LoaderCircle } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { buttonClasses } from './styles.js'

export function LoadingState({ label }) {
  const { t } = useTranslation()
  return (
    <div
      role="status"
      className="flex items-center justify-center gap-3 py-16 text-slate-500 dark:text-slate-400"
    >
      <LoaderCircle
        className="h-5 w-5 animate-spin motion-reduce:animate-none"
        aria-hidden="true"
      />
      <span>{label ?? t('common.loading')}</span>
    </div>
  )
}

export function ErrorState({ title, body, onRetry }) {
  const { t } = useTranslation()
  return (
    <div role="alert" className="flex flex-col items-center gap-3 py-16 text-center">
      <CircleAlert className="h-8 w-8 text-rose-500" aria-hidden="true" />
      <h2 className="text-lg font-semibold">{title ?? t('common.errorTitle')}</h2>
      <p className="max-w-md text-slate-600 dark:text-slate-400">{body ?? t('common.errorBody')}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className={buttonClasses({ variant: 'secondary', className: 'mt-2' })}
        >
          {t('common.retry')}
        </button>
      )}
    </div>
  )
}

export function EmptyState({ title, body, children, icon: Icon = FolderOpen }) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-2xl border border-dashed border-slate-300 px-6 py-16 text-center dark:border-slate-700">
      <Icon className="h-8 w-8 text-slate-400" aria-hidden="true" />
      <h2 className="text-lg font-semibold">{title}</h2>
      {body && <p className="max-w-md text-slate-600 dark:text-slate-400">{body}</p>}
      {children}
    </div>
  )
}
