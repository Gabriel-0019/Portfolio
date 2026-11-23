using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Projects;
using Portfolio.Application.Technologies;
using Portfolio.Domain.Common;
using Portfolio.Domain.Projects;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Queries;

internal sealed class ProjectQueries(PortfolioDbContext db) : IProjectQueries
{
    public async Task<IReadOnlyList<ProjectSummaryDto>> GetPublishedAsync(
        ProjectFilter filter, string language, CancellationToken cancellationToken)
    {
        var query = PubliclyVisible();

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            if (!TryParseCategory(filter.Category, out var category))
            {
                return [];
            }

            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(filter.Technology))
        {
            var technology = filter.Technology.Trim();
            query = query.Where(p => p.Technologies.Any(t => t.Name == technology));
        }

        if (filter.Featured is bool featured)
        {
            query = query.Where(p => p.IsFeatured == featured);
        }

        var projects = await query
            .Include(p => p.Translations.Where(t => t.Language == language || t.Language == Languages.Default))
            .Include(p => p.Technologies)
            .OrderByDescending(p => p.IsFeatured)
            .ThenBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.StartDate)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return projects.Select(p => ToSummary(p, language)).ToList();
    }

    public async Task<ProjectDetailDto?> GetPublishedBySlugAsync(
        string slug, string language, CancellationToken cancellationToken)
    {
        var project = await PubliclyVisible()
            .Where(p => p.Slug == slug)
            .Include(p => p.Translations.Where(t => t.Language == language || t.Language == Languages.Default))
            .Include(p => p.Technologies)
            .Include(p => p.Images)
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return null;
        }

        var translation = project.Translations.ForLanguage(language)!;

        return new ProjectDetailDto(
            project.Slug,
            translation.Title,
            translation.Summary,
            translation.Content,
            project.Category.ToString(),
            project.IsFeatured,
            project.CoverImageUrl,
            project.RepoUrl,
            project.DemoUrl,
            project.StartDate,
            project.EndDate,
            ToTechnologyDtos(project),
            project.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProjectImageDto(i.Url, i.AltFor(language)))
                .ToList());
    }

    public async Task<IReadOnlyList<PublishedProjectRef>> GetPublishedRefsAsync(CancellationToken cancellationToken) =>
        await PubliclyVisible()
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Slug)
            .Select(p => new PublishedProjectRef(p.Slug, p.UpdatedAt))
            .ToListAsync(cancellationToken);

    /// <summary>Published projects translated into every supported language.</summary>
    private IQueryable<Project> PubliclyVisible()
    {
        var supported = Languages.Supported;
        var required = supported.Length;

        return db.Projects.Where(p =>
            p.Status == ProjectStatus.Published
            && p.Translations.Count(t => supported.Contains(t.Language)) == required);
    }

    private static bool TryParseCategory(string value, out ProjectCategory category) =>
        Enum.TryParse(value.Trim(), ignoreCase: true, out category)
        && Enum.IsDefined(category)
        && !int.TryParse(value, out _);

    private static ProjectSummaryDto ToSummary(Project project, string language)
    {
        var translation = project.Translations.ForLanguage(language)!;

        return new ProjectSummaryDto(
            project.Slug,
            translation.Title,
            translation.Summary,
            project.Category.ToString(),
            project.IsFeatured,
            project.CoverImageUrl,
            project.RepoUrl,
            project.DemoUrl,
            project.StartDate,
            project.EndDate,
            ToTechnologyDtos(project));
    }

    private static List<TechnologyDto> ToTechnologyDtos(Project project) =>
        project.Technologies
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .Select(t => new TechnologyDto(t.Name, t.IconKey, t.Category.ToString()))
            .ToList();
}
