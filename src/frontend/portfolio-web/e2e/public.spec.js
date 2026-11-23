import { expect, test } from './fixtures.js'

test.describe('public site', () => {
  test('redirects / to the browser language and shows the home page', async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveURL(/\/en$/)
    await expect(page.getByRole('heading', { level: 1 })).toHaveText("Hi, I'm Your Name.")
    await expect(page.getByRole('heading', { name: 'Featured projects' })).toBeVisible()
    await expect(page.locator('html')).toHaveAttribute('lang', 'en')
  })

  test('switches language and keeps the current page', async ({ page, isMobile }) => {
    await page.goto('/en/projects')
    if (isMobile) await page.getByRole('button', { name: 'Open menu' }).click()

    await page.getByRole('link', { name: 'ES Español' }).first().click()

    await expect(page).toHaveURL(/\/es\/projects$/)
    await expect(page.getByRole('heading', { level: 1, name: 'Proyectos' })).toBeVisible()
    await expect(page).toHaveTitle('Proyectos · Portafolio')
    await expect(page.locator('html')).toHaveAttribute('lang', 'es')
  })

  test('filters projects and opens a case study', async ({ page }) => {
    await page.goto('/en/projects')

    await page.getByLabel('Technology').selectOption('React')
    await expect(page).toHaveURL(/tech=React/)
    await page.getByRole('button', { name: 'Mobile' }).click()
    await expect(page.getByText('No projects match these filters')).toBeVisible()
    await page.getByRole('button', { name: 'Clear filters' }).click()

    await page.getByRole('link', { name: 'Developer Portfolio' }).click()

    await expect(page).toHaveURL(/\/en\/projects\/portfolio$/)
    await expect(page.getByRole('heading', { level: 2, name: 'Problem' })).toBeVisible()
    await expect(page).toHaveTitle('Developer Portfolio · Portfolio')
  })

  test('sends the contact form', async ({ page }) => {
    await page.goto('/es/contact')

    await page.getByRole('button', { name: 'Enviar mensaje' }).click()
    await expect(page.getByText('Este campo es obligatorio.').first()).toBeVisible()

    await page.getByLabel('Nombre').fill('Prueba E2E')
    await page.getByLabel('Correo electrónico').fill('e2e@example.com')
    await page.getByLabel('Mensaje').fill('Mensaje enviado por las pruebas de extremo a extremo.')
    await page.getByRole('button', { name: 'Enviar mensaje' }).click()

    await expect(page.getByRole('heading', { name: 'Mensaje enviado' })).toBeVisible()
  })

  test('remembers the dark theme across reloads', async ({ page, isMobile }) => {
    await page.goto('/en/about')
    if (isMobile) await page.getByRole('button', { name: 'Open menu' }).click()

    await page.getByRole('button', { name: 'Dark' }).first().click()
    await expect(page.locator('html')).toHaveClass(/dark/)

    await page.reload()
    await expect(page.locator('html')).toHaveClass(/dark/)
  })

  test('unknown pages show the 404 page with a 404 status', async ({ page }) => {
    const response = await page.goto('/en/projects/does-not-exist')

    expect(response.status()).toBe(404)
    await expect(page.getByRole('heading', { name: 'Project not found' })).toBeVisible()
  })
})

test.describe('server-rendered metadata', () => {
  test('pages ship their Open Graph tags in the HTML (no JavaScript needed)', async ({
    request,
  }) => {
    const response = await request.get('/es/projects/portfolio')
    const html = await response.text()

    expect(response.status()).toBe(200)
    expect(html).toContain('<html lang="es"')
    expect(html).toContain('property="og:title" content="Portafolio de desarrollador · Your Name"')
    expect(response.headers()['content-security-policy']).toContain("script-src 'self' 'sha256-")
  })

  test('robots.txt and sitemap.xml are served', async ({ request }) => {
    expect(await (await request.get('/robots.txt')).text()).toContain('Disallow: /admin')
    expect(await (await request.get('/sitemap.xml')).text()).toContain(
      '/es/projects/portfolio</loc>',
    )
  })

  test('removes the server tags once React takes over (no duplicates)', async ({ page }) => {
    await page.goto('/en/about')
    await expect(page).toHaveTitle('About me · Portfolio')

    expect(await page.locator('head [data-ssr]').count()).toBe(0)
    expect(await page.locator('head meta[name="description"]').count()).toBe(1)
  })
})
