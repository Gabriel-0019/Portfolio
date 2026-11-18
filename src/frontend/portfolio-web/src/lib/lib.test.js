import { describe, expect, it, vi } from 'vitest'
import { detectLanguage, rememberLanguage, switchLanguageInPath } from '../i18n/index.js'
import { formatPeriod, initials, parseDateOnly } from './format.js'

describe('switchLanguageInPath', () => {
  it.each([
    ['/en/projects', 'es', '/es/projects'],
    ['/es/projects/portfolio', 'en', '/en/projects/portfolio'],
    ['/en', 'es', '/es'],
    ['/projects', 'es', '/es/projects'],
    ['/', 'en', '/en'],
  ])('%s -> %s', (path, lang, expected) => {
    expect(switchLanguageInPath(path, lang)).toBe(expected)
  })
})

describe('detectLanguage', () => {
  it('prefers the remembered choice', () => {
    rememberLanguage('es')
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['en-US'])
    expect(detectLanguage()).toBe('es')
  })

  it('uses the first supported browser language', () => {
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['fr-FR', 'es-MX', 'en'])
    expect(detectLanguage()).toBe('es')
  })

  it('falls back to English', () => {
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(['de-DE'])
    expect(detectLanguage()).toBe('en')
  })
})

describe('format', () => {
  it('parses date-only strings as local dates', () => {
    const date = parseDateOnly('2026-11-17')
    expect([date.getFullYear(), date.getMonth(), date.getDate()]).toEqual([2026, 10, 17])
  })

  it('formats open periods with the "present" label', () => {
    expect(formatPeriod('2024-01-01', null, 'en', 'Present')).toBe('Jan 2024 – Present')
  })

  it('builds initials from the first two names', () => {
    expect(initials('ana maría lópez')).toBe('AM')
  })
})
