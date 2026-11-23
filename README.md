# Portfolio

Personal software developer portfolio — bilingual (English / Spanish).
Portafolio personal de desarrollador de software — bilingüe (inglés / español).

| Layer | Stack |
|-------|-------|
| Backend | .NET 10 · ASP.NET Core Web API · EF Core 10 · FluentValidation |
| Frontend | React · JavaScript · Vite · Tailwind CSS |
| Database | SQL Server (LocalDB in development) |

See [Docs/PLAN.md](Docs/PLAN.md) for the full project plan and [Docs/DEPLOY.md](Docs/DEPLOY.md) for
deployment (configuration, hosting options, first-deploy checklist).

CI runs on GitHub Actions (`.github/workflows/ci.yml`): .NET tests against a SQL Server container,
frontend lint/tests/build, Playwright end-to-end + accessibility tests, and the deployable package.

## Requirements

- .NET SDK 10 (pinned in `global.json`)
- Node.js 20.19+ / npm
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) — used by the API in development and by the integration tests

## Getting started

```bash
dotnet tool restore
npm --prefix src/frontend/portfolio-web install
```

Run the API (http://localhost:5080, API docs at http://localhost:5080/scalar/v1):

```bash
dotnet run --project src/backend/Portfolio.Api --launch-profile http
```

Run the web app in another terminal (http://localhost:5173):

```bash
npm --prefix src/frontend/portfolio-web run dev
```

In development, Vite proxies every `/api` request to the API, so no CORS setup is needed.

## Frontend

| Route | Page |
|-------|------|
| `/` | Redirects to `/en` or `/es` (saved choice, then browser language) |
| `/{lang}` | Home: hero, featured projects, tech stack |
| `/{lang}/projects` | Project gallery with category / technology filters (`?category=&tech=`) |
| `/{lang}/projects/{slug}` | Case study |
| `/{lang}/about` | Profile, experience timeline, tech stack, CV |
| `/{lang}/contact` | Contact form |

UI texts live in `src/frontend/portfolio-web/src/i18n/locales/{en,es}.json`; content comes from the API
in the URL language. Pages are code-split and loaded on first visit.

## Database

In Development the API applies pending migrations on startup (`Database:MigrateOnStartup`) and
seeds placeholder content when the tables are empty. The connection string lives in
`src/backend/Portfolio.Api/appsettings.Development.json` (`ConnectionStrings:Portfolio`).

Add a migration after changing the model:

```bash
dotnet ef migrations add <Name> -p src/backend/Portfolio.Infrastructure -s src/backend/Portfolio.Api -o Persistence/Migrations
```

Apply migrations manually:

```bash
dotnet ef database update -p src/backend/Portfolio.Infrastructure -s src/backend/Portfolio.Api
```

Integration tests create a temporary `Portfolio_Tests_<guid>` LocalDB database and drop it at the end.

## API

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/status` | API health |
| GET | `/api/projects?lang=&tech=&category=&featured=` | Published projects |
| GET | `/api/projects/{slug}?lang=` | Project case study |
| GET | `/api/profile?lang=` | Owner profile |
| GET | `/api/experience?lang=` | Experience timeline |
| GET | `/api/technologies` | Technology catalog |
| POST | `/api/contact?lang=` | Contact form (rate limited) |

Language: `?lang=en|es`, otherwise the `Accept-Language` header, otherwise English.

Admin endpoints live under `/api/admin/*` (projects, technologies, experience, profile, messages,
uploads) and require the auth cookie from `POST /api/auth/login`. Full list in the API docs
(`/scalar/v1`) and in [Docs/PLAN.md](Docs/PLAN.md).

## Admin panel

Open http://localhost:5173/admin. On startup the API creates the admin account from
`Admin:Email` / `Admin:Password` if it does not exist yet (it never changes an existing account).

- **Development:** a local-only account is configured in
  `src/backend/Portfolio.Api/appsettings.Development.json`. It only works against your LocalDB.
- **Your own credentials:** keep them out of the repo with user secrets, then delete the `Portfolio`
  database's `AspNetUsers` row (or the whole local database) so the account is recreated:

  ```bash
  dotnet user-secrets set "Admin:Email" "you@example.com" --project src/backend/Portfolio.Api
  ```

  ```bash
  dotnet user-secrets set "Admin:Password" "<a long password>" --project src/backend/Portfolio.Api
  ```

Uploaded files are stored in `src/backend/Portfolio.Api/App_Data/uploads` (git-ignored) and served
at `/uploads/...`. Change it with `Storage:RootPath`.

## Tests and quality

```bash
dotnet test Portfolio.slnx
npm --prefix src/frontend/portfolio-web test
npm --prefix src/frontend/portfolio-web run lint
npm --prefix src/frontend/portfolio-web run format:check
```

End-to-end and accessibility tests (Playwright + axe). They build the frontend, start the API serving it
on port 5090 with a throw-away LocalDB database (dropped afterwards) and run in the Microsoft Edge
installed on Windows — no browser download. Other browsers: set `E2E_BROWSER_CHANNEL` (e.g. `chrome`).

```bash
npm --prefix src/frontend/portfolio-web run e2e
```

## Production build

The API serves the built frontend, injecting per-page SEO / Open Graph tags, initial data, a strict CSP,
`robots.txt` and `sitemap.xml` (details in [Docs/PLAN.md](Docs/PLAN.md), section 10.1).

`dotnet publish` builds the React app and copies it into `wwwroot/`, producing one deployable folder
(development settings are excluded):

```bash
dotnet publish src/backend/Portfolio.Api -c Release -o publish
```

Production settings (connection string, admin account, upload folder, domain…) come from environment
variables — see the table in [Docs/DEPLOY.md](Docs/DEPLOY.md). Health checks: `/health/live`, `/health/ready`.

## Repository layout

```
Docs/                         Plan and documentation
src/backend/
  Portfolio.Api/              ASP.NET Core Web API (entry point)
  Portfolio.Application/      Use cases, DTOs, validation
  Portfolio.Domain/           Entities and domain rules
  Portfolio.Infrastructure/   EF Core, storage, email
src/frontend/portfolio-web/   React + Vite app
tests/
  Portfolio.UnitTests/
  Portfolio.IntegrationTests/
Portfolio.slnx                .NET solution
```
