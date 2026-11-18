using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Admin.Projects;
using Portfolio.Application.Common;
using Portfolio.Domain.Common;
using Portfolio.Domain.Projects;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Admin;

internal sealed class AdminProjectService(PortfolioDbContext db, TimeProvider timeProvider) : IAdminProjectService
{
    public async Task<IReadOnlyList<AdminProjectListItemDto>> ListAsync(CancellationToken cancellationToken)
    {
        var projects = await db.Projects
            .Include(p => p.Translations)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Slug)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return projects
            .Select(p => new AdminProjectListItemDto(
                p.Id,
                p.Slug,
                TitleFor(p, Languages.English),
                TitleFor(p, Languages.Spanish),
                p.Category,
                p.Status,
                p.IsFeatured,
                p.DisplayOrder,
                p.IsPubliclyVisible,
                p.UpdatedAt))
            .ToList();
    }

    public async Task<AdminProjectDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var project = await LoadAsync(id, tracking: false, cancellationToken);
        return project is null ? null : ToDto(project);
    }

    public async Task<Result<AdminProjectDto>> CreateAsync(SaveProjectRequest request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim();
        if (await db.Projects.AnyAsync(p => p.Slug == slug, cancellationToken))
        {
            return Result<AdminProjectDto>.Conflict($"A project with slug '{slug}' already exists.");
        }

        var technologies = await LoadTechnologiesAsync(request.TechnologyIds, cancellationToken);
        if (technologies is null)
        {
            return Result<AdminProjectDto>.Invalid("One or more technologies do not exist.");
        }

        var now = timeProvider.GetUtcNow();
        var project = new Project { Slug = slug, CreatedAt = now };
        Apply(project, request, technologies, now);

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return Result<AdminProjectDto>.Ok(ToDto(project));
    }

    public async Task<Result<AdminProjectDto>> UpdateAsync(
        int id, SaveProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await LoadAsync(id, tracking: true, cancellationToken);
        if (project is null)
        {
            return Result<AdminProjectDto>.NotFound();
        }

        var slug = request.Slug.Trim();
        if (await db.Projects.AnyAsync(p => p.Slug == slug && p.Id != id, cancellationToken))
        {
            return Result<AdminProjectDto>.Conflict($"A project with slug '{slug}' already exists.");
        }

        var technologies = await LoadTechnologiesAsync(request.TechnologyIds, cancellationToken);
        if (technologies is null)
        {
            return Result<AdminProjectDto>.Invalid("One or more technologies do not exist.");
        }

        project.Slug = slug;
        Apply(project, request, technologies, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return Result<AdminProjectDto>.Ok(ToDto(project));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await db.Projects.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? Result.Missing : Result.Success;
    }

    private Task<Project?> LoadAsync(int id, bool tracking, CancellationToken cancellationToken)
    {
        var query = db.Projects
            .Include(p => p.Translations)
            .Include(p => p.Technologies)
            .Include(p => p.Images)
            .AsSplitQuery();

        return (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>Returns null when any id does not exist.</summary>
    private async Task<List<Domain.Technologies.Technology>?> LoadTechnologiesAsync(
        IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var technologies = await db.Technologies.Where(t => ids.Contains(t.Id)).ToListAsync(cancellationToken);
        return technologies.Count == ids.Distinct().Count() ? technologies : null;
    }

    private static void Apply(
        Project project,
        SaveProjectRequest request,
        List<Domain.Technologies.Technology> technologies,
        DateTimeOffset now)
    {
        project.Category = request.Category;
        project.Status = request.Status;
        project.IsFeatured = request.IsFeatured;
        project.DisplayOrder = request.DisplayOrder;
        project.CoverImageUrl = Clean(request.CoverImageUrl);
        project.RepoUrl = Clean(request.RepoUrl);
        project.DemoUrl = Clean(request.DemoUrl);
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.UpdatedAt = now;

        // Translations are updated in place per language so the (ProjectId, Language) unique index
        // never sees a delete and an insert for the same language in one save.
        var requested = request.Translations.ToDictionary(t => Languages.Normalize(t.Language));
        project.Translations.RemoveAll(t => !requested.ContainsKey(t.Language));
        foreach (var (language, input) in requested)
        {
            var translation = project.Translations.SingleOrDefault(t => t.Language == language);
            if (translation is null)
            {
                translation = new ProjectTranslation { Language = language, Title = string.Empty, Summary = string.Empty };
                project.Translations.Add(translation);
            }

            translation.Title = input.Title.Trim();
            translation.Summary = input.Summary.Trim();
            translation.Content = input.Content?.Trim() ?? string.Empty;
        }

        project.Images.Clear();
        project.Images.AddRange(request.Images.Select((image, index) => new ProjectImage
        {
            Url = image.Url.Trim(),
            AltEn = image.AltEn.Trim(),
            AltEs = image.AltEs.Trim(),
            DisplayOrder = index,
        }));

        project.Technologies.Clear();
        project.Technologies.AddRange(technologies);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? TitleFor(Project project, string language) =>
        project.Translations.SingleOrDefault(t => t.Language == language)?.Title;

    private static AdminProjectDto ToDto(Project project) => new(
        project.Id,
        project.Slug,
        project.Category,
        project.Status,
        project.IsFeatured,
        project.DisplayOrder,
        project.CoverImageUrl,
        project.RepoUrl,
        project.DemoUrl,
        project.StartDate,
        project.EndDate,
        project.Technologies.OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name).Select(t => t.Id).ToList(),
        project.Translations
            .OrderBy(t => Array.IndexOf(Languages.Supported, t.Language))
            .Select(t => new ProjectTranslationInput(t.Language, t.Title, t.Summary, t.Content))
            .ToList(),
        project.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new ProjectImageInput(i.Url, i.AltEn, i.AltEs))
            .ToList(),
        project.IsPubliclyVisible,
        project.CreatedAt,
        project.UpdatedAt);
}
