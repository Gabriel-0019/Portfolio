import AxeBuilder from '@axe-core/playwright'
import { expect, login, test } from './fixtures.js'

const WCAG = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']

const publicPages = [
  { path: '/en', ready: { level: 1 } },
  { path: '/es/projects', ready: { level: 1, name: 'Proyectos' } },
  { path: '/en/projects/portfolio', ready: { level: 2, name: 'Problem' } },
  { path: '/es/about', ready: { level: 2, name: 'Experiencia' } },
  { path: '/en/contact', ready: { level: 1, name: 'Contact' } },
  { path: '/en/does-not-exist', ready: { level: 1, name: 'Page not found' } },
]

/** Summarizes axe violations so a failure message points straight at the problem. */
function summarize(violations) {
  return violations.map((v) => ({
    rule: v.id,
    impact: v.impact,
    help: v.help,
    targets: v.nodes.slice(0, 5).map((n) => n.target.join(' ')),
  }))
}

for (const colorScheme of ['light', 'dark']) {
  test.describe(`accessibility (${colorScheme})`, () => {
    test.use({ colorScheme })

    for (const { path, ready } of publicPages) {
      test(`${path} has no WCAG 2.2 AA violations`, async ({ page }) => {
        await page.goto(path)
        await expect(page.getByRole('heading', ready).first()).toBeVisible()

        const results = await new AxeBuilder({ page }).withTags(WCAG).analyze()

        expect(summarize(results.violations)).toEqual([])
      })
    }
  })
}

test.describe('accessibility (admin)', () => {
  test.skip(({ isMobile }) => isMobile, 'The admin panel is checked on desktop only.')

  test('login page', async ({ page }) => {
    await page.goto('/admin/login')
    await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible()

    const results = await new AxeBuilder({ page }).withTags(WCAG).analyze()
    expect(summarize(results.violations)).toEqual([])
  })

  for (const path of [
    '/admin/projects',
    '/admin/projects/new',
    '/admin/profile',
    '/admin/messages',
  ]) {
    test(path, async ({ page }) => {
      await login(page)
      await page.goto(path)
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

      const results = await new AxeBuilder({ page }).withTags(WCAG).analyze()
      expect(summarize(results.violations)).toEqual([])
    })
  }
})
