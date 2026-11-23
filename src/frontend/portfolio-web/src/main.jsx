import '@fontsource-variable/inter'
import './index.css'
import './i18n/index.js'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './app/App.jsx'
import { readInitialData } from './app/queryClient.js'

// Data the server embedded for the first render (profile, projects…). Read it before cleaning up.
const initialData = readInitialData()

// Tags rendered by the server for crawlers and link previews. From here on React renders
// per-page metadata itself, so remove them to avoid duplicates after client-side navigation.
document.head.querySelectorAll('[data-ssr]').forEach((node) => node.remove())

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App initialData={initialData} />
  </StrictMode>,
)
