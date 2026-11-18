/** Shown while the first route loads (language + page chunk). Text-free: i18n may not be ready. */
export default function InitialLoading() {
  return (
    <div className="flex min-h-svh items-center justify-center" role="status" aria-label="Loading">
      <span className="h-8 w-8 animate-spin rounded-full border-2 border-slate-300 border-t-indigo-600 motion-reduce:animate-none" />
    </div>
  )
}
