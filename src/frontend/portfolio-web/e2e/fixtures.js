import { test as base, expect } from '@playwright/test'

/**
 * `test` that fails when the page logs a console error, throws, or violates the CSP
 * (CSP violations are reported as console errors).
 */
export const test = base.extend({
  page: async ({ page }, use) => {
    const problems = []
    page.on('pageerror', (error) => problems.push(`pageerror: ${error.message}`))
    page.on('console', (message) => {
      if (message.type() !== 'error') return
      const text = message.text()
      // Expected: 401 from /api/auth/me on admin pages before login, 404 page loads.
      if (/status of (401|404)/.test(text)) return
      problems.push(`console: ${text}`)
    })

    await use(page)

    expect(problems, 'browser errors').toEqual([])
  },
})

export { expect }

export const admin = {
  email: process.env.E2E_ADMIN_EMAIL,
  password: process.env.E2E_ADMIN_PASSWORD,
}

export async function login(page) {
  await page.goto('/admin/login')
  await page.getByLabel('Correo electrónico').fill(admin.email)
  await page.getByLabel('Contraseña').fill(admin.password)
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page.getByRole('button', { name: 'Salir' })).toBeVisible()
}
