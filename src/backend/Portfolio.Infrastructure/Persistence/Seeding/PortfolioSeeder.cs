using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Common;
using Portfolio.Domain.Experiences;
using Portfolio.Domain.Profiles;
using Portfolio.Domain.Projects;
using Portfolio.Domain.Technologies;

namespace Portfolio.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the initial content: a placeholder profile, the technology catalog, a placeholder
/// experience entry and this portfolio as the first project. Each section is only seeded when
/// its table is empty, so content edited later (e.g. from the admin panel) is never overwritten.
/// </summary>
public static class PortfolioSeeder
{
    public static void Seed(DbContext context)
    {
        var db = (PortfolioDbContext)context;

        if (!db.Technologies.Any())
        {
            db.Technologies.AddRange(SeedData.Technologies());
            db.SaveChanges();
        }

        if (!db.Profiles.Any())
        {
            db.Profiles.Add(SeedData.Profile());
        }

        if (!db.Experiences.Any())
        {
            db.Experiences.Add(SeedData.Experience());
        }

        if (!db.Projects.Any())
        {
            var technologies = db.Technologies.ToList();
            db.Projects.Add(SeedData.PortfolioProject(technologies, DateTimeOffset.UtcNow));
        }

        db.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var db = (PortfolioDbContext)context;

        if (!await db.Technologies.AnyAsync(cancellationToken))
        {
            db.Technologies.AddRange(SeedData.Technologies());
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Profiles.AnyAsync(cancellationToken))
        {
            db.Profiles.Add(SeedData.Profile());
        }

        if (!await db.Experiences.AnyAsync(cancellationToken))
        {
            db.Experiences.Add(SeedData.Experience());
        }

        if (!await db.Projects.AnyAsync(cancellationToken))
        {
            var technologies = await db.Technologies.ToListAsync(cancellationToken);
            db.Projects.Add(SeedData.PortfolioProject(technologies, DateTimeOffset.UtcNow));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    internal static class SeedData
    {
        public const string PortfolioSlug = "portfolio";

        public static IEnumerable<Technology> Technologies()
        {
            (string Name, string IconKey, TechnologyCategory Category)[] items =
            [
                ("C#", "csharp", TechnologyCategory.Language),
                ("JavaScript", "javascript", TechnologyCategory.Language),
                ("SQL", "sql", TechnologyCategory.Language),
                (".NET", "dotnet", TechnologyCategory.Backend),
                ("ASP.NET Core", "dotnet", TechnologyCategory.Backend),
                ("Entity Framework Core", "dotnet", TechnologyCategory.Backend),
                ("React", "react", TechnologyCategory.Frontend),
                ("Vite", "vite", TechnologyCategory.Frontend),
                ("Tailwind CSS", "tailwindcss", TechnologyCategory.Frontend),
                ("PostgreSQL", "postgresql", TechnologyCategory.Database),
                ("Git", "git", TechnologyCategory.Tool),
            ];

            return items.Select((item, index) => new Technology
            {
                Name = item.Name,
                IconKey = item.IconKey,
                Category = item.Category,
                DisplayOrder = index,
            });
        }

        public static Profile Profile() => new()
        {
            FullName = "Your Name",
            Email = "you@example.com",
            Location = "City, Country",
            Translations =
            [
                new ProfileTranslation
                {
                    Language = Languages.English,
                    Headline = "Software Developer · .NET & React",
                    Bio = "Placeholder bio. Replace it from the admin panel with a short introduction: "
                        + "what you build, the technologies you enjoy and what you are looking for.",
                },
                new ProfileTranslation
                {
                    Language = Languages.Spanish,
                    Headline = "Desarrollador de software · .NET y React",
                    Bio = "Biografía de ejemplo. Reemplázala desde el panel de administración con una "
                        + "presentación breve: qué construyes, qué tecnologías disfrutas y qué buscas.",
                },
            ],
            SocialLinks =
            [
                new SocialLink { Platform = "github", Url = "https://github.com/", DisplayOrder = 0 },
                new SocialLink { Platform = "linkedin", Url = "https://www.linkedin.com/", DisplayOrder = 1 },
                new SocialLink { Platform = "email", Url = "mailto:you@example.com", DisplayOrder = 2 },
            ],
        };

        public static Experience Experience() => new()
        {
            Company = "Example Company",
            Type = ExperienceType.Work,
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = null,
            Translations =
            [
                new ExperienceTranslation
                {
                    Language = Languages.English,
                    Role = "Software Developer",
                    Location = "Remote",
                    Description = "Placeholder entry. Replace it from the admin panel with your real experience.",
                },
                new ExperienceTranslation
                {
                    Language = Languages.Spanish,
                    Role = "Desarrollador de software",
                    Location = "Remoto",
                    Description = "Entrada de ejemplo. Reemplázala desde el panel de administración con tu experiencia real.",
                },
            ],
        };

        public static Project PortfolioProject(IReadOnlyCollection<Technology> technologies, DateTimeOffset now)
        {
            string[] stack = ["C#", ".NET", "ASP.NET Core", "Entity Framework Core", "PostgreSQL", "React", "JavaScript", "Vite", "Tailwind CSS"];

            return new Project
            {
                Slug = PortfolioSlug,
                Category = ProjectCategory.Web,
                Status = ProjectStatus.Published,
                IsFeatured = true,
                DisplayOrder = 0,
                StartDate = new DateOnly(2026, 11, 17),
                CreatedAt = now,
                UpdatedAt = now,
                Technologies = technologies.Where(t => stack.Contains(t.Name)).ToList(),
                Translations =
                [
                    new ProjectTranslation
                    {
                        Language = Languages.English,
                        Title = "Developer Portfolio",
                        Summary = "Bilingual portfolio built with .NET 10, React and PostgreSQL, with an admin panel to publish new projects without redeploying.",
                        Content = """
                            ## Problem
                            I needed a place to showcase my projects in English and Spanish that I could keep up to date without touching code every time I finish something new.

                            ## Solution
                            A React single-page app backed by an ASP.NET Core Web API. Content lives in PostgreSQL and is managed from an admin panel, so publishing a project is a form, not a deployment.

                            ## Architecture
                            - **Backend:** .NET 10 Web API split into Domain, Application, Infrastructure and Api layers.
                            - **Data:** Entity Framework Core with code-first migrations on PostgreSQL.
                            - **Frontend:** React + Vite + Tailwind CSS, with the language in the URL.
                            - **i18n:** every piece of content is stored per language; a project is only public when it exists in both.

                            ## Challenges
                            Modeling translations so the public site never shows half-translated content.

                            ## Results
                            A fast, accessible and maintainable portfolio that grows with every new project.
                            """,
                    },
                    new ProjectTranslation
                    {
                        Language = Languages.Spanish,
                        Title = "Portafolio de desarrollador",
                        Summary = "Portafolio bilingüe construido con .NET 10, React y PostgreSQL, con panel de administración para publicar proyectos sin volver a desplegar.",
                        Content = """
                            ## Problema
                            Necesitaba un lugar para mostrar mis proyectos en inglés y español que pudiera mantener actualizado sin tocar código cada vez que termino algo nuevo.

                            ## Solución
                            Una aplicación React de una sola página respaldada por una Web API de ASP.NET Core. El contenido vive en PostgreSQL y se gestiona desde un panel de administración, así que publicar un proyecto es llenar un formulario, no desplegar.

                            ## Arquitectura
                            - **Backend:** Web API en .NET 10 dividida en capas Domain, Application, Infrastructure y Api.
                            - **Datos:** Entity Framework Core con migraciones code-first sobre PostgreSQL.
                            - **Frontend:** React + Vite + Tailwind CSS, con el idioma en la URL.
                            - **i18n:** todo el contenido se guarda por idioma; un proyecto solo es público cuando existe en ambos.

                            ## Retos
                            Modelar las traducciones para que el sitio público nunca muestre contenido traducido a medias.

                            ## Resultados
                            Un portafolio rápido, accesible y fácil de mantener que crece con cada proyecto nuevo.
                            """,
                    },
                ],
            };
        }
    }
}
