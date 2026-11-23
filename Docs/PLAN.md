# Plan del Proyecto: Portafolio de Desarrollador

> Estado: **Plan aprobado en lo general** — quedan las decisiones de la sección 11.
> Última actualización: 2026-11-17

---

## 1. Objetivo

Crear un sitio web personal, **bilingüe (inglés / español)** y de **estilo profesional**, que:

1. Presente quién soy como desarrollador de software (perfil, stack, experiencia).
2. Muestre los proyectos que voy construyendo, cada uno como un **caso de estudio** (problema → solución → arquitectura → resultados).
3. Facilite el contacto (formulario, redes, CV descargable).
4. Permita **agregar proyectos nuevos sin modificar código ni volver a desplegar**, ya que hoy no hay proyectos publicados y se irán sumando con el tiempo.

### Contexto
- Hoy no existen proyectos que mostrar. El sitio debe verse bien **vacío** (estado "Próximamente") y crecer con el tiempo.
- El propio portafolio será **el primer proyecto** de la galería.

### Público objetivo
- Reclutadores (escanean en menos de un minuto).
- Líderes técnicos (quieren ver arquitectura, código, decisiones).
- Clientes potenciales (quieren ver resultados y contacto).

---

## 2. Decisiones tomadas

| Tema | Decisión |
|------|----------|
| Idiomas | Inglés y español (UI y contenido) |
| Estilo visual | Profesional, sobrio |
| Backend | .NET 10 (ASP.NET Core Web API) |
| Frontend | React + JavaScript + Vite |
| Base de datos | SQL Server |
| Publicación | Se decide más adelante |

---

## 3. Arquitectura

```
┌──────────────────────────┐        HTTPS/JSON        ┌────────────────────────────┐       EF Core       ┌──────────────┐
│  React SPA (Vite + JS)   │  ─────────────────────►  │  ASP.NET Core 10 Web API   │  ─────────────────► │  SQL Server  │
│  Sitio público + Admin   │  ◄─────────────────────  │  /api/...                  │                     │  (LocalDB en │
└──────────────────────────┘                          └────────────────────────────┘                     │   desarrollo)│
                                                                                                         └──────────────┘
```

### ¿Por qué base de datos?
Como los proyectos se agregarán con el tiempo, guardarlos en SQL Server y gestionarlos desde un **panel de administración** permite publicar un proyecto nuevo sin tocar código. Además, la BD guarda los mensajes del formulario de contacto. Esto también convierte al portafolio en una demostración full-stack (.NET + React + SQL Server).

### Backend — .NET 10
- **ASP.NET Core Web API** con controladores (o Minimal APIs agrupadas por módulo).
- **Entity Framework Core 10** + proveedor SQL Server, migraciones code-first.
- Arquitectura por capas:
  - `Portfolio.Domain` — entidades y reglas.
  - `Portfolio.Application` — casos de uso, DTOs, validaciones (FluentValidation).
  - `Portfolio.Infrastructure` — EF Core, repositorios, almacenamiento de imágenes, correo.
  - `Portfolio.Api` — endpoints, autenticación, configuración.
- **OpenAPI** (documentación de la API) con Scalar como interfaz.
- **Autenticación** del panel admin: ASP.NET Core Identity con un único usuario administrador + cookie segura o JWT.
- **Localización**: el contenido viene de la BD por idioma (`?lang=en|es` o cabecera `Accept-Language`).
- Imágenes: en desarrollo se guardan en disco (`wwwroot/uploads`); al publicar se podrá cambiar a almacenamiento en la nube sin cambiar la lógica (interfaz `IFileStorage`).
- Protección del formulario de contacto: rate limiting nativo de ASP.NET Core + campo honeypot.
- Tests: **xUnit** + pruebas de integración con `WebApplicationFactory`.

### Frontend — React
- **React + JavaScript + Vite** (archivos `.jsx` / `.js`, sin TypeScript).
- **oxlint + Prettier** para mantener la calidad y consistencia del código (oxlint es el linter que trae la plantilla actual de Vite: compatible con las reglas de ESLint y mucho más rápido).
- **Tailwind CSS 4** + plugin `@tailwindcss/typography` (estilo del Markdown); componentes propios en `components/ui` (se descartó shadcn/ui porque está pensado para TypeScript).
- **React Router 8** (data router con *loaders* y páginas cargadas bajo demanda); **TanStack Query** para llamadas a la API y caché.
- **react-i18next** para textos de la interfaz (archivos `en.json` / `es.json`).
- **React Hook Form + Zod** para formularios; el esquema replica las reglas del servidor y los mensajes están traducidos.
- **react-markdown** para casos de estudio y bio (no renderiza HTML crudo), **lucide-react** para íconos, fuente **Inter**.
- Idioma en la URL (`/en/...`, `/es/...`): `/` y rutas sin prefijo redirigen al idioma guardado o al del navegador. El selector mantiene la página actual.
- Modo claro / oscuro / sistema, guardado en el navegador y aplicado antes del primer pintado (sin parpadeo).
- SEO: `<title>`, descripción, Open Graph y `hreflang` por página (React 19 los sube al `<head>`). En producción la API además los inserta en el HTML de cada página (sección 10.1), así que las vistas previas en LinkedIn/WhatsApp funcionan aunque esos bots no ejecuten JavaScript.
- Tests: **Vitest** + React Testing Library (con la API simulada); **Playwright** para pruebas end-to-end (fase 4).

### Base de datos — SQL Server
- Desarrollo: **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`, ya instalado).
- Producción: se decide junto con la publicación (Azure SQL, SQL Server en VPS, etc.).

---

## 4. Modelo de datos (implementado en la migración `InitialCreate`)

```
Project                     ProjectTranslation            Technology
───────                     ──────────────────            ──────────
Id                          Id                            Id
Slug (único)                ProjectId  ──► Project        Name (único, ej. ".NET", "React")
Category                    Language   ("en" | "es")      IconKey
Status (Draft/Published/    Title                         Category (Language/Frontend/Backend/
        Archived)           Summary                                 Database/DevOps/Tool)
IsFeatured                  Content (Markdown: problema,  DisplayOrder
DisplayOrder                  solución, arquitectura,
CoverImageUrl                 retos, resultados)          ProjectTechnologies (N:N)
RepoUrl                     (único: ProjectId+Language)   ──────────────────
DemoUrl                                                   ProjectsId, TechnologiesId
StartDate / EndDate         ProjectImage
CreatedAt / UpdatedAt       ────────────                  ContactMessage
                            Id, ProjectId, Url,           ──────────────
                            AltEn, AltEs, DisplayOrder    Id, Name, Email, Subject?, Message,
                                                          Language, CreatedAt, IsRead

Profile (1 registro): FullName, Email, Location, AvatarUrl, CvUrlEn, CvUrlEs
  + ProfileTranslation (Headline, Bio por idioma) + SocialLink (Platform, Url, DisplayOrder)
Experience: Company, CompanyUrl, Type (Work/Education/Certification), StartDate, EndDate (null = actual)
  + ExperienceTranslation (Role, Location, Description por idioma)
```

Enums guardados como texto en la BD para que sean legibles. Las traducciones se borran en cascada con su entidad.

Regla: un proyecto solo aparece en el sitio público si está **Published** y tiene traducción en **ambos idiomas**. Si falta la traducción de perfil o experiencia en el idioma pedido, se usa la de inglés.

### Datos semilla
Se cargan automáticamente al aplicar migraciones (`UseSeeding` / `UseAsyncSeeding` de EF Core), **solo si la tabla está vacía**, así nunca sobrescriben contenido editado después:
- Perfil de ejemplo ("Your Name", titular y bio en EN/ES, enlaces a GitHub, LinkedIn y email).
- Catálogo de 11 tecnologías (C#, JavaScript, SQL, .NET, ASP.NET Core, EF Core, React, Vite, Tailwind CSS, SQL Server, Git).
- Una experiencia de ejemplo.
- Este portafolio como primer proyecto (publicado y destacado, caso de estudio en EN/ES).

---

## 5. Funcionalidades

### 5.1 Sitio público (MVP)
| # | Funcionalidad |
|---|---------------|
| 1 | **Inicio**: hero con nombre, rol, frase y botones "Ver proyectos" / "Contactar"; proyectos destacados |
| 2 | **Sobre mí**: foto, resumen, stack tecnológico, experiencia en línea de tiempo |
| 3 | **Proyectos**: galería en tarjetas con filtro por tecnología/categoría; estado vacío "Próximamente" |
| 4 | **Detalle de proyecto**: caso de estudio en Markdown, galería de imágenes, tecnologías, enlaces a demo y repo |
| 5 | **Contacto**: formulario (guarda en BD y opcionalmente envía correo), enlaces a redes, **CV descargable** en ambos idiomas |
| 6 | Selector de idioma EN/ES, modo claro/oscuro, diseño responsive |
| 7 | SEO: título y descripción por página, Open Graph para compartir en LinkedIn |

### 5.2 Panel de administración (implementado en fase 3)
- Login de administrador (una sola cuenta) con opción "Mantener la sesión iniciada".
- Proyectos: lista con estado y aviso si un publicado no es visible por falta de traducciones; editor con datos generales, contenido **EN / ES** lado a lado, vista previa del Markdown, portada y galería con subida de imágenes, selección de tecnologías por categoría, borrador/publicado/archivado, orden y destacados. El slug se sugiere a partir del título.
- Tecnologías, experiencia (con descripción en Markdown) y perfil (foto, CV en PDF por idioma, redes).
- Bandeja de mensajes: contador de no leídos, marcar leído/no leído, responder por correo, eliminar.
- La interfaz del panel está en español (es privada); el contenido que se edita es bilingüe.

### 5.3 Fase 2 (ideas)
- Integración con la API de GitHub (estrellas, lenguajes, último commit de cada repo).
- Blog / notas técnicas bilingües.
- Analítica respetuosa con la privacidad.
- Paleta de comandos (Ctrl+K).

---

## 6. Rutas

**Públicas**
```
/{lang}                    Inicio
/{lang}/projects           Galería
/{lang}/projects/{slug}    Detalle de proyecto
/{lang}/about              Sobre mí
/{lang}/contact            Contacto
```

**Admin**
```
/admin/login
/admin/projects, /admin/projects/new, /admin/projects/{id}
/admin/technologies, /admin/experience, /admin/profile, /admin/messages
```

**API**

Implementados (fase 1):
```
GET    /api/status                                   Estado de la API
GET    /api/projects?lang=&tech=&category=&featured= Proyectos públicos
GET    /api/projects/{slug}?lang=                    Caso de estudio (404 si no es público)
GET    /api/profile?lang=                            Perfil
GET    /api/experience?lang=                         Experiencia (más reciente primero)
GET    /api/technologies                             Catálogo de tecnologías
POST   /api/contact?lang=                            Formulario (202; 400 con errores; 429 si excede el límite)
```
Implementados (fase 3) — todo `/api/admin/*` requiere la cookie de sesión (401 sin ella):
```
POST   /api/auth/login        Inicia sesión (200; 401 credenciales; 423 cuenta bloqueada; 429 límite)
POST   /api/auth/logout       Cierra sesión
GET    /api/auth/me           Usuario actual (401 si no hay sesión)
GET|POST          /api/admin/projects          Lista (todos los estados) / crear
GET|PUT|DELETE    /api/admin/projects/{id}     Detalle / guardar / eliminar
GET|POST          /api/admin/technologies      Lista con nº de proyectos / crear
PUT|DELETE        /api/admin/technologies/{id}
GET|POST          /api/admin/experience
PUT|DELETE        /api/admin/experience/{id}
GET|PUT           /api/admin/profile           (PUT crea el perfil si no existe)
GET               /api/admin/messages?unreadOnly=
PATCH|DELETE      /api/admin/messages/{id}     { isRead }
POST              /api/admin/uploads           multipart "file" → { url, contentType, size }
GET               /uploads/{archivo}           Archivos subidos (públicos)
```

**Seguridad del panel:**
- ASP.NET Core Identity con una sola cuenta, creada al arrancar desde `Admin:Email` / `Admin:Password` si no existe. No hay registro público.
- Cookie `portfolio.auth` **HttpOnly** + **SameSite=Strict** (el navegador no la envía desde otros sitios, lo que bloquea CSRF); 8 h con renovación deslizante.
- Contraseña de 12+ caracteres; bloqueo de 15 min tras 5 intentos fallidos; límite de 10 intentos de login cada 5 min por IP.
- Subidas: el tipo se decide por la firma del archivo (PNG, JPEG, GIF, WebP, PDF), nunca por el nombre; SVG/HTML rechazados; máx. 10 MB; nombres aleatorios; se sirven con `X-Content-Type-Options: nosniff`.
- Las URLs guardadas solo pueden ser `http(s)://`, `mailto:` (redes) o `/uploads/…`; se rechaza `javascript:` y similares.
- `?returnTo=` del login solo acepta rutas `/admin/…` (evita redirecciones abiertas).

**Producción (fase 5):** cookie `Secure` vía HTTPS + cabeceras reenviadas del proxy, contraseña admin desde variables de entorno del hosting, claves de Data Protection en SQL Server (la sesión sobrevive reinicios), carpeta de subidas persistente configurable con aviso si falta. Ver [DEPLOY.md](DEPLOY.md). Pendiente a futuro: limpieza de archivos subidos que ya no se usan.

**Idioma de la API:** `?lang=es` → cabecera `Accept-Language` (acepta regionales como `es-MX`) → inglés por defecto. La respuesta incluye `Content-Language`.

**Formulario de contacto:** validación con FluentValidation, campo honeypot `website` (si viene lleno se responde 202 sin guardar) y límite de 5 envíos cada 10 minutos por IP (configurable en `RateLimiting:Contact`).

---

## 7. Estructura del repositorio

```
Portfolio/
├── Docs/                          # Plan y documentación
├── Portfolio.slnx                 # Solución .NET (en la raíz para compilar y probar todo junto)
├── global.json                    # Fija el SDK de .NET 10
├── dotnet-tools.json              # Herramientas locales (dotnet-ef)
├── src/
│   ├── backend/
│   │   ├── Portfolio.Api/
│   │   ├── Portfolio.Application/
│   │   ├── Portfolio.Domain/
│   │   └── Portfolio.Infrastructure/
│   └── frontend/
│       └── portfolio-web/         # React + Vite
│           ├── src/
│           │   ├── app/           # App, rutas, tema, QueryClient
│           │   ├── pages/         # Home, Projects, ProjectDetail, About, Contact, NotFound
│           │   ├── features/      # projects, profile, contact (y admin en fase 3)
│           │   ├── components/    # layout (Header, Footer...) y ui compartida
│           │   ├── i18n/          # configuración + locales/en.json, es.json
│           │   ├── lib/           # cliente API, hooks de consultas, formato
│           │   ├── admin/         # panel de administración
│           │   └── test/          # setup y utilidades de pruebas
│           ├── e2e/               # Playwright (e2e + accesibilidad con axe)
│           ├── playwright.config.js
│           └── package.json
├── tests/
│   ├── Portfolio.UnitTests/
│   └── Portfolio.IntegrationTests/
├── .editorconfig
├── .gitignore
└── README.md
```

En desarrollo, Vite hace de proxy de `/api` hacia la API .NET para evitar problemas de CORS.

---

## 8. Estilo visual (profesional)

- Paleta neutra (grises/blanco o grafito) con **un solo color de acento** (azul o índigo).
- Tipografía sans-serif limpia (ej. Inter) y monoespaciada solo para fragmentos de código.
- Mucho espacio en blanco, cuadrícula consistente, animaciones mínimas.
- Contraste accesible AA en modo claro y oscuro.

---

## 9. Fases y entregables

| Fase | Contenido | Entregable |
|------|-----------|------------|
| **0. Preparación** ✅ | Inicializar git, solución .NET, app React, `.editorconfig`, `.gitignore`, README | Repo listo; API y React arrancan y se comunican (completada 2026-11-17) |
| **1. Backend base** ✅ | Entidades, DbContext, migraciones en LocalDB, datos semilla, endpoints públicos, OpenAPI | API pública funcional con datos de ejemplo (completada 2026-11-17) |
| **2. Frontend público** ✅ | Layout, i18n, tema, Inicio, Sobre mí, Proyectos, Detalle, Contacto | Sitio público navegable en EN/ES (completada 2026-11-17) |
| **3. Admin** ✅ | Identity, login, CRUD de proyectos/experiencia/perfil, subida de imágenes, mensajes | Contenido editable sin tocar código (completada 2026-11-17) |
| **4. Calidad** ✅ | Tests unitarios/integración/e2e, accesibilidad, rendimiento, SEO | Versión 1.0 lista para publicar (completada 2026-11-17) |
| **5. Publicación** 🟡 | Elegir hosting, CI/CD, dominio, BD de producción | **Preparación completada (2026-11-17):** paquete único, configuración por variables, health checks, CI en GitHub Actions, guía [DEPLOY.md](DEPLOY.md). **Pendiente:** elegir hosting/dominio, crear el repo en GitHub y el paso de deploy |
| **6. Primer proyecto** | Registrar el propio portafolio como caso de estudio; luego el siguiente proyecto | Galería con contenido real |

---

## 10. Criterios de calidad

- Funciona de 360 px a escritorio; navegable con teclado; contraste AA.
- Agregar un proyecto = usar el panel admin (sin código ni despliegue).
- Toda la UI y todo el contenido disponible en EN y ES.
- Validación en cliente y servidor; errores de la API con formato `ProblemDetails`.
- Secretos fuera del repositorio (User Secrets en desarrollo).

### 10.1 Producción: un solo despliegue (fase 4)

La API sirve el frontend compilado (`dist/`, por defecto desde `wwwroot`, configurable con `Spa:RootPath`):

- **Assets** de `/assets/*` con caché de 1 año (`immutable`, llevan hash en el nombre); HTML con `no-cache`.
- **Cada página** se responde con `index.html` más, en el marcador `<!--app-head-->`:
  - `<title>`, descripción, `canonical`, `hreflang` (en/es/x-default), Open Graph y Twitter Card en el idioma de la URL (`og:image` = portada del proyecto o foto del perfil).
  - Un bloque JSON (`type="application/json"`) con los datos de la primera vista (perfil, proyectos destacados, proyecto…). React los usa directamente sin llamar a la API → sin saltos de diseño.
  - `<html lang>` correcto y **código 404 real** para proyectos/páginas que no existen (con `noindex`).
  - El cliente borra esas etiquetas al arrancar (`data-ssr`) y gestiona las suyas, sin duplicados.
- `/` y rutas sin idioma → **redirección 302** al idioma del visitante (`Accept-Language`).
- `/robots.txt` (bloquea `/admin` y `/api/`) y `/sitemap.xml` dinámico con todas las páginas en ambos idiomas y sus alternativas.
- **Cabeceras de seguridad** en todas las respuestas (`nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, COOP) y **CSP estricta** en el HTML: `script-src 'self'` + hash SHA-256 del script de tema calculado al arrancar (sin `unsafe-inline`), `style-src 'self'`, `frame-ancestors 'none'`.
- HSTS + redirección a HTTPS fuera de desarrollo; compresión Brotli/Gzip.
- `Site:BaseUrl` fija el dominio de las URLs canónicas (si no, se usa el de la petición).

### 10.2 Resultados de calidad (2026-11-17)

| Verificación | Resultado |
|--------------|-----------|
| Pruebas .NET (xUnit) | 38 unitarias + 71 de integración ✅ |
| Pruebas frontend (Vitest) | 38 ✅ |
| E2E (Playwright, Edge, escritorio + móvil Pixel 7) | 51 ✅ (5 omitidas a propósito: panel admin en móvil) |
| Accesibilidad (axe, WCAG 2.2 AA, claro y oscuro) | 0 violaciones en páginas públicas y panel |
| Lighthouse (móvil simulado, producción local) | Rendimiento 90–94 · Accesibilidad 100 · Buenas prácticas 100 · SEO 100 · CLS ≤ 0,03 |

Hallazgos corregidos en esta fase: nombre accesible del selector EN/ES (WCAG 2.5.3), contraste del contador "+N" en modo oscuro, el slug del editor se mezclaba con el texto sugerido al escribir, y el salto de diseño del hero al cargar el perfil (CLS 0,137 → 0).

Mejoras posibles a futuro (rendimiento 90+ ya es bueno para un SPA): precargar el chunk de la página actual desde el servidor (`modulepreload` con el manifest de Vite) o renderizado completo en servidor.

---

## 11. Decisiones

**Tomadas (2026-11-17)**
- Panel de administración incluido en el MVP.
- Datos personales: textos de ejemplo por ahora, se reemplazan desde el panel admin.
- Control de versiones: repositorio git local (rama `main`); el usuario creará el repositorio en GitHub (CI ya preparado).
- Publicación: preparar todo lo independiente del proveedor; hosting y dominio se eligen más adelante.

**Pendientes**
- **Hosting y BD de producción**: opciones comparadas en [DEPLOY.md](DEPLOY.md) (Azure, VPS, hosting compartido).
- **Dominio**: todavía no hay; se configura con `Site__BaseUrl`.
- **Correo**: ¿el formulario solo guarda en BD o también envía un email de aviso? (Depende del hosting/proveedor de correo.)

## 11.1 Notas técnicas

- `npm audit` reporta 6 vulnerabilidades altas en dependencias **solo de desarrollo** (picomatch/tinyglobby vía Vite y Vitest). No afectan al sitio compilado; se revisan al actualizar Vite/Vitest. No usar `npm audit fix --force` (instala versiones incompatibles).

## 12. Entorno verificado (2026-11-17)

| Herramienta | Estado |
|-------------|--------|
| .NET SDK 10.0.401 | Instalado |
| Node.js 26 / npm 11 | Instalado |
| SQL Server LocalDB (`MSSQLLocalDB`) | Instalado |
| sqlcmd | Instalado |
| Git 2.55 | Instalado |
| `dotnet-ef` 10.0.15 | Instalado como herramienta local (`dotnet tool restore`) |
| Docker | No instalado (no es necesario) |
