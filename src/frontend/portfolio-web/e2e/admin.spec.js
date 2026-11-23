import { expect, login, test } from './fixtures.js'

test.describe('admin panel', () => {
  test('requires signing in', async ({ page }) => {
    await page.goto('/admin/projects')

    await expect(page).toHaveURL(/\/admin\/login\?returnTo=%2Fadmin%2Fprojects/)
  })

  test('publishes a project, shows it on the site and deletes it', async ({ page }) => {
    const slug = `e2e-${Date.now().toString(36)}`
    await login(page)

    // Create.
    await page.getByRole('link', { name: 'Nuevo proyecto' }).click()
    const english = page.getByRole('group', { name: 'Inglés' })
    const spanish = page.getByRole('group', { name: 'Español' })
    await english.getByLabel('Título').fill('E2E Project')
    await english.getByLabel('Resumen').fill('Created by the end-to-end tests.')
    await english.getByRole('textbox', { name: 'Caso de estudio' }).fill('## Problem\nTesting.')
    await spanish.getByLabel('Título').fill('Proyecto E2E')
    await spanish.getByLabel('Resumen').fill('Creado por las pruebas de extremo a extremo.')
    await page.getByLabel('Slug (URL)').fill(slug)
    await page.getByLabel('Estado').selectOption('Published')
    await page.getByLabel('React').check()
    await page.getByRole('button', { name: 'Guardar' }).click()
    await expect(page.getByText('Cambios guardados.')).toBeVisible()

    // Visible on the public site, in both languages.
    await page.goto(`/en/projects/${slug}`)
    await expect(page.getByRole('heading', { level: 1, name: 'E2E Project' })).toBeVisible()
    await page.goto(`/es/projects/${slug}`)
    await expect(page.getByRole('heading', { level: 1, name: 'Proyecto E2E' })).toBeVisible()

    // Delete.
    await page.goto('/admin/projects')
    page.once('dialog', (dialog) => dialog.accept())
    await page.getByRole('button', { name: `Eliminar ${slug}` }).click()
    await expect(page.getByRole('link', { name: 'Proyecto E2E' })).toHaveCount(0)

    const response = await page.goto(`/en/projects/${slug}`)
    expect(response.status()).toBe(404)
  })

  test('shows contact messages and marks them as read', async ({ page, request }) => {
    const subject = `E2E ${Date.now()}`
    await request.post('/api/contact?lang=en', {
      data: {
        name: 'Visitor',
        email: 'visitor@example.com',
        subject,
        message: 'Hello from the e2e tests!',
      },
    })
    await login(page)

    await page.getByRole('link', { name: /Mensajes/ }).click()
    await page.locator('summary', { hasText: subject }).click()

    await expect(page.getByRole('link', { name: 'Responder' })).toHaveAttribute(
      'href',
      /^mailto:visitor@example\.com/,
    )
    await expect(page.getByRole('button', { name: 'Marcar como no leído' })).toBeVisible()
  })

  test('signs out', async ({ page }) => {
    await login(page)

    await page.getByRole('button', { name: 'Salir' }).click()
    await expect(page).toHaveURL(/\/admin\/login/)

    await page.goto('/admin/projects')
    await expect(page).toHaveURL(/\/admin\/login/)
  })
})
