import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterEach, beforeEach, vi } from 'vitest'
import '../i18n/index.js'

// findBy*/waitFor default to 1 s, too tight when the machine is busy (e.g. a .NET build running).
configure({ asyncUtilTimeout: 3000 })

beforeEach(() => {
  // jsdom does not implement scrolling; ScrollRestoration calls it on navigation.
  window.scrollTo = vi.fn()
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  window.localStorage.clear()
})
