import Container from './Container.jsx'

export default function PageHeader({ title, subtitle, children }) {
  return (
    <section className="border-b border-slate-200 bg-slate-50 dark:border-slate-800 dark:bg-slate-900/40">
      <Container className="py-12 sm:py-16">
        <h1 className="text-3xl font-semibold tracking-tight text-slate-900 sm:text-4xl dark:text-white">
          {title}
        </h1>
        {subtitle && (
          <p className="mt-3 max-w-2xl text-lg text-slate-600 dark:text-slate-400">{subtitle}</p>
        )}
        {children}
      </Container>
    </section>
  )
}
