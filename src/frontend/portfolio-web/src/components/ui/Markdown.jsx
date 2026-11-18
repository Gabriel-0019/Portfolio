import ReactMarkdown from 'react-markdown'

const components = {
  a: ({ href, children, ...props }) => {
    const external = /^https?:\/\//.test(href ?? '')
    return (
      <a
        href={href}
        {...props}
        {...(external ? { target: '_blank', rel: 'noopener noreferrer' } : {})}
      >
        {children}
      </a>
    )
  },
}

/** Renders trusted Markdown content from the API. Raw HTML is not rendered. */
export default function Markdown({ children, className = '' }) {
  return (
    <div
      className={`prose prose-slate max-w-none dark:prose-invert prose-headings:font-semibold prose-headings:tracking-tight prose-a:text-indigo-600 dark:prose-a:text-indigo-400 ${className}`}
    >
      <ReactMarkdown components={components}>{children ?? ''}</ReactMarkdown>
    </div>
  )
}
