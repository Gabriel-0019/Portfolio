import { defineConfig, devices } from '@playwright/test'
import { randomBytes } from 'node:crypto'
import { tmpdir } from 'node:os'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { serverConnectionString, withDatabase } from './e2e/database.js'

const here = path.dirname(fileURLToPath(import.meta.url))

// Generated once in the main process; worker processes inherit them through the environment.
process.env.E2E_RUN_ID ??= Date.now().toString(36)
process.env.E2E_ADMIN_EMAIL ??= 'admin@e2e.localhost'
process.env.E2E_ADMIN_PASSWORD ??= `E2e-${randomBytes(12).toString('base64url')}-1a`
process.env.E2E_DATABASE ??= `portfolio_e2e_${process.env.E2E_RUN_ID}`

const port = Number(process.env.E2E_PORT ?? 5090)
const baseURL = `http://127.0.0.1:${port}`
// Locally: the Microsoft Edge installed on Windows. CI (Linux): "chromium" after `playwright install chromium`.
const channel = process.env.E2E_BROWSER_CHANNEL ?? 'msedge'

/**
 * End-to-end tests against the production shape of the app: the ASP.NET Core API serving the
 * built React app (run `npm run e2e`, which builds first). Uses its own PostgreSQL database, dropped
 * afterwards, and the Microsoft Edge already installed on Windows (no browser download).
 */
export default defineConfig({
  testDir: './e2e',
  // Tests share one database and the admin tests change data: run them one at a time.
  workers: 1,
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  globalTeardown: './e2e/global-teardown.js',
  use: {
    baseURL,
    locale: 'en-US',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    // channel goes after the device spread: device descriptors may set their own (Desktop Edge = msedge).
    { name: 'desktop', use: { ...devices['Desktop Edge'], channel } },
    {
      name: 'mobile',
      use: { ...devices['Pixel 7'], channel },
      // The admin panel is desktop-first; the public site is tested on both.
      testIgnore: /admin\.spec\.js/,
    },
  ],
  webServer: {
    // A separate artifacts path, so it never fights with a dev API that has the normal bin folder locked.
    command:
      'dotnet run --project ../../backend/Portfolio.Api --no-launch-profile --artifacts-path ../../../artifacts/e2e',
    url: `${baseURL}/api/status`,
    timeout: 240_000,
    reuseExistingServer: false,
    stdout: 'ignore',
    stderr: 'pipe',
    env: {
      ASPNETCORE_ENVIRONMENT: 'E2E',
      ASPNETCORE_URLS: baseURL,
      // E2E_PG (CI) or the API's user secrets locally; always a throw-away database for this run.
      ConnectionStrings__Portfolio: withDatabase(
        serverConnectionString(),
        process.env.E2E_DATABASE,
      ),
      Database__MigrateOnStartup: 'true',
      Admin__Email: process.env.E2E_ADMIN_EMAIL,
      Admin__Password: process.env.E2E_ADMIN_PASSWORD,
      Spa__RootPath: path.join(here, 'dist'),
      Storage__RootPath: path.join(tmpdir(), `portfolio-e2e-uploads-${process.env.E2E_RUN_ID}`),
      RateLimiting__Contact__PermitLimit: '1000',
      RateLimiting__Login__PermitLimit: '1000',
    },
  },
})
