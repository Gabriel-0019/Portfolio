# Portfolio

Personal software developer portfolio — bilingual (English / Spanish).
Portafolio personal de desarrollador de software — bilingüe (inglés / español).

| Layer | Stack |
|-------|-------|
| Backend | .NET 10 · ASP.NET Core Web API · EF Core 10 · FluentValidation |
| Frontend | React · JavaScript · Vite · Tailwind CSS |
| Database | SQL Server (LocalDB in development) |

See [Docs/PLAN.md](Docs/PLAN.md) for the full project plan.

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

## Tests and quality

```bash
dotnet test Portfolio.slnx
npm --prefix src/frontend/portfolio-web test
npm --prefix src/frontend/portfolio-web run lint
npm --prefix src/frontend/portfolio-web run format:check
```

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
