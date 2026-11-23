# Guía de despliegue

> Estado (2026-11-17): la aplicación está **lista para desplegar en cualquier proveedor**. Falta elegir
> hosting, base de datos de producción y dominio; cuando se decida, se agrega el paso de deploy al CI.

---

## 1. Qué se despliega

Un solo paquete: la API .NET 10 sirve también el sitio React compilado (`wwwroot/`).

```bash
dotnet publish src/backend/Portfolio.Api -c Release -o publish
```

`publish` ejecuta `npm ci` + `npm run build` del frontend y copia `dist/` a `publish/wwwroot/`
(usa `-p:BuildFrontend=false` si `dist/` ya está compilado). El paquete:

- Incluye `appsettings.json` y **excluye** `appsettings.Development.json` (cuenta admin local de desarrollo).
- Incluye `web.config` (por si el hosting usa IIS).
- Es *framework-dependent*: el servidor necesita el runtime **ASP.NET Core 10**. Para un servidor Linux
  concreto se puede añadir `-r linux-x64 --self-contained false` y reducir el tamaño.

El workflow de GitHub Actions (`.github/workflows/ci.yml`) genera este paquete como artefacto
`portfolio-site` en cada push a `main`, después de pasar todas las pruebas.

---

## 2. Configuración de producción (variables de entorno)

ASP.NET Core lee la configuración de variables de entorno usando `__` como separador
(`ConnectionStrings__Portfolio` = `ConnectionStrings:Portfolio`). **Ningún secreto va en el repositorio.**

| Variable | Obligatoria | Ejemplo / valor | Para qué |
|----------|:-----------:|-----------------|----------|
| `ASPNETCORE_ENVIRONMENT` | ✅ | `Production` | Activa HSTS, redirección HTTPS, oculta la documentación de la API |
| `ConnectionStrings__Portfolio` | ✅ | `Host=ep-xxx-pooler.….neon.tech;Database=portfolio;Username=…;Password=…;SSL Mode=Require` | PostgreSQL. Con Neon: la cadena **con *pooling*** (`-pooler` en el host) |
| `ConnectionStrings__PortfolioMigrations` | Con Neon | `Host=ep-xxx.….neon.tech;Database=portfolio;Username=…;Password=…;SSL Mode=Require` | Conexión **directa** (sin `-pooler`) solo para aplicar migraciones: el *pooler* de Neon (PgBouncer) no las soporta. Sin ella se usa `ConnectionStrings__Portfolio` |
| `Database__MigrateOnStartup` | Recomendado | `true` | Aplica migraciones al arrancar (crea tablas y datos semilla la primera vez) |
| `Admin__Email` | Primera vez | `tu@correo.com` | Crea la cuenta admin si no existe |
| `Admin__Password` | Primera vez | contraseña larga (12+) | Igual. Puede quitarse después: nunca se modifica una cuenta existente |
| `Storage__RootPath` | ✅ | `/home/data/uploads`, `/var/lib/portfolio/uploads` | Carpeta **persistente** para imágenes y CV subidos |
| `Site__BaseUrl` | Recomendado | `https://tunombre.dev` | URLs canónicas, Open Graph y sitemap con tu dominio |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Detrás de proxy | `true` | Ver el esquema `https` real detrás de App Service, Caddy, nginx… (cookie `Secure`, IP real para el límite de peticiones) |
| `ASPNETCORE_URLS` | Según hosting | `http://+:8080` | Puerto en el que escucha |

### Avisos que la app escribe al arrancar en producción

- *Storage:RootPath is not set…*: las subidas se guardarían dentro de la carpeta de la app y **se perderían
  en cada despliegue**. Configura `Storage__RootPath`.
- *The frontend was not found…*: el paquete no tiene `wwwroot/` (se publicó sin frontend).
- *No XML encryptor configured…* (Data Protection): las claves que firman la cookie de sesión se guardan en
  la tabla `DataProtectionKeys` sin cifrar. Es lo habitual cuando el acceso a la base de datos ya está
  protegido; si el proveedor ofrece un certificado o Key Vault se pueden cifrar.
- *Failed to determine the https port…*: solo aparece si la app no ve HTTPS. Detrás de un proxy, activa
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.

### Comprobaciones tras desplegar

| URL | Esperado |
|-----|----------|
| `/health/live` | `Healthy` (el proceso responde) |
| `/health/ready` | `Healthy` (además, la base de datos responde) — úsala como *health check* del hosting |
| `/` | Redirección a `/en` o `/es` |
| `/es/projects/portfolio` → ver código fuente | `<meta property="og:title" …>` con el título del proyecto |
| `/robots.txt`, `/sitemap.xml` | Con tu dominio si configuraste `Site__BaseUrl` |
| `/admin` | Login; entra con la cuenta configurada y **cambia el perfil de ejemplo** |

Verificado localmente con el paquete publicado en modo `Production` (base de datos nueva): migraciones y
datos semilla al arrancar, cuenta admin creada desde variables, páginas con metadatos, redirección por
idioma, assets, health checks, y la **sesión del admin sigue válida tras reiniciar el proceso**
(claves de Data Protection en la base de datos). Esa verificación se hizo con SQL Server; tras pasar a
PostgreSQL, las mismas comprobaciones están cubiertas por las pruebas de integración y e2e contra PostgreSQL.

### Base de datos en Neon

1. En [console.neon.tech](https://console.neon.tech) crea un proyecto (región cercana a donde irá la API) con
   la base `portfolio`. Usa la misma versión principal de PostgreSQL que en local (17).
2. Copia las dos cadenas en formato .NET: con *pooling* → `ConnectionStrings__Portfolio`; directa →
   `ConnectionStrings__PortfolioMigrations`. Ambas con `SSL Mode=Require`. **Solo en las variables del hosting.**
3. Con `Database__MigrateOnStartup=true`, la API crea las tablas en el primer arranque.
4. Para llevar tu contenido actual: ejecuta la herramienta de migración contra Neon desde tu equipo, con la
   cadena **directa**, o haz `pg_dump` de tu base local y `pg_restore` en Neon.
5. Plan gratuito: la base se suspende tras unos minutos sin uso y tarda un poco en despertar; la API
   reintenta automáticamente los errores transitorios de conexión.

---

## 3. Opciones de hosting

Las tres funcionan con el mismo paquete; cambia cómo se sube y dónde viven las subidas. La base de datos es
PostgreSQL en **Neon** (sección anterior) en cualquiera de ellas.

### A. Azure App Service (Linux) — la más integrada

- **App Service** plan B1 (~13 USD/mes; el plan gratuito F1 no permite dominio propio con HTTPS).
  Runtime: .NET 10. Variables en *Configuración → Variables de entorno*.
- Base de datos: Neon, en una región de Azure cercana.
- Subidas: `Storage__RootPath=/home/data/uploads` (`/home` es almacenamiento persistente en App Service Linux).
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`. *Health check path*: `/health/ready`.
- Deploy desde GitHub Actions con `azure/webapps-deploy` usando el artefacto `portfolio-site`.
- Dominio propio + certificado gratuito administrado por App Service.

### B. VPS Linux (Hetzner, DigitalOcean…, ~5–10 USD/mes)

- Instalar el runtime ASP.NET Core 10. Base de datos: Neon (o PostgreSQL en el propio servidor).
- La app como servicio `systemd` escuchando en `127.0.0.1:8080`, detrás de **Caddy** (HTTPS automático con
  Let's Encrypt) o nginx. `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.
- Subidas en `/var/lib/portfolio/uploads`; copias de seguridad de esa carpeta y de la base de datos.
- Deploy: copiar el artefacto por SSH (`rsync`) y reiniciar el servicio (automatizable desde GitHub Actions).
- Alternativa: contenerizarlo con Docker (no incluido todavía; se agrega si se elige esta opción).

### C. Hosting compartido Windows (IIS)

- El más barato; el paquete ya trae `web.config`. Requiere que el proveedor tenga **.NET 10** y permita
  conexiones salientes a Neon (puerto 5432).
- Subir la carpeta por Web Deploy o FTP; variables desde su panel o en `web.config` (`<environmentVariables>`).
- Subidas en una carpeta fuera de `wwwroot` con permisos de escritura para la app.
- Menos automatizable y con límites de recursos; comprobar que soporten HTTPS con dominio propio.

---

## 4. Subir el código a GitHub

Crea un repositorio **vacío** en GitHub (sin README ni .gitignore) y luego, desde la carpeta del proyecto:

```bash
git remote add origin https://github.com/<tu-usuario>/<tu-repo>.git
```

```bash
git push -u origin main
```

El primer push ejecuta el CI (pruebas .NET con PostgreSQL en contenedor, lint y pruebas del frontend,
pruebas e2e y de accesibilidad con Chromium) y genera el paquete `portfolio-site`.

> Antes de publicar el repositorio (si será público): `appsettings.Development.json` contiene una
> contraseña **solo de desarrollo** para la cuenta admin local. No da acceso a nada fuera de tu equipo, pero puedes
> moverla a *user secrets* (ver README) si prefieres que no aparezca.

---

## 5. Checklist del primer despliegue

1. Elegir hosting y crear la base de datos en Neon (ver "Base de datos en Neon").
2. Configurar las variables de la sección 2 (con `Admin__Email`/`Admin__Password` y `Database__MigrateOnStartup=true`).
3. Desplegar el artefacto `portfolio-site`.
4. Comprobar las URLs de la sección 2.
5. Entrar a `/admin`, completar perfil, experiencia, CV y redes; revisar el proyecto "portfolio".
6. Configurar el dominio y `Site__BaseUrl`; volver a comprobar `/sitemap.xml`.
7. Quitar `Admin__Password` de las variables (la cuenta ya existe).
8. Programar copias de seguridad de la base de datos y de la carpeta de subidas.
9. Añadir el paso de deploy al workflow de CI para el proveedor elegido.
