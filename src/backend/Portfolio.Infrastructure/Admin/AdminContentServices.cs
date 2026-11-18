using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Admin.Content;
using Portfolio.Application.Common;
using Portfolio.Domain.Common;
using Portfolio.Domain.Experiences;
using Portfolio.Domain.Profiles;
using Portfolio.Domain.Technologies;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Admin;

internal static class Text
{
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static int LanguageOrder(string language) => Array.IndexOf(Languages.Supported, language);

    /// <summary>
    /// Updates translations in place per language (insert missing, update existing, remove extra),
    /// so unique (ParentId, Language) indexes are never violated within one save.
    /// </summary>
    public static void SyncTranslations<TEntity, TInput>(
        List<TEntity> current,
        IEnumerable<TInput> requested,
        Func<TInput, string> languageOf,
        Func<string, TEntity> create,
        Action<TEntity, TInput> update)
        where TEntity : class, ITranslation
    {
        var byLanguage = requested.ToDictionary(i => Languages.Normalize(languageOf(i)));
        current.RemoveAll(t => !byLanguage.ContainsKey(t.Language));

        foreach (var (language, input) in byLanguage)
        {
            var entity = current.SingleOrDefault(t => t.Language == language);
            if (entity is null)
            {
                entity = create(language);
                current.Add(entity);
            }

            update(entity, input);
        }
    }
}

internal sealed class AdminTechnologyService(PortfolioDbContext db) : IAdminTechnologyService
{
    public async Task<IReadOnlyList<AdminTechnologyDto>> ListAsync(CancellationToken cancellationToken) =>
        await db.Technologies
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .Select(t => new AdminTechnologyDto(t.Id, t.Name, t.IconKey, t.Category, t.DisplayOrder, t.Projects.Count))
            .ToListAsync(cancellationToken);

    public async Task<Result<AdminTechnologyDto>> CreateAsync(
        SaveTechnologyRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.Technologies.AnyAsync(t => t.Name == name, cancellationToken))
        {
            return Result<AdminTechnologyDto>.Conflict($"The technology '{name}' already exists.");
        }

        var technology = new Technology { Name = name };
        Apply(technology, request);
        db.Technologies.Add(technology);
        await db.SaveChangesAsync(cancellationToken);

        return Result<AdminTechnologyDto>.Ok(ToDto(technology, projectCount: 0));
    }

    public async Task<Result<AdminTechnologyDto>> UpdateAsync(
        int id, SaveTechnologyRequest request, CancellationToken cancellationToken)
    {
        var technology = await db.Technologies.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (technology is null)
        {
            return Result<AdminTechnologyDto>.NotFound();
        }

        var name = request.Name.Trim();
        if (await db.Technologies.AnyAsync(t => t.Name == name && t.Id != id, cancellationToken))
        {
            return Result<AdminTechnologyDto>.Conflict($"The technology '{name}' already exists.");
        }

        technology.Name = name;
        Apply(technology, request);
        await db.SaveChangesAsync(cancellationToken);

        var projectCount = await db.Technologies.Where(t => t.Id == id).Select(t => t.Projects.Count).SingleAsync(cancellationToken);
        return Result<AdminTechnologyDto>.Ok(ToDto(technology, projectCount));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        // Join rows in ProjectTechnologies are removed by the cascade delete.
        var deleted = await db.Technologies.Where(t => t.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? Result.Missing : Result.Success;
    }

    private static void Apply(Technology technology, SaveTechnologyRequest request)
    {
        technology.IconKey = Text.Clean(request.IconKey);
        technology.Category = request.Category;
        technology.DisplayOrder = request.DisplayOrder;
    }

    private static AdminTechnologyDto ToDto(Technology t, int projectCount) =>
        new(t.Id, t.Name, t.IconKey, t.Category, t.DisplayOrder, projectCount);
}

internal sealed class AdminExperienceService(PortfolioDbContext db) : IAdminExperienceService
{
    public async Task<IReadOnlyList<AdminExperienceDto>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await db.Experiences
            .Include(e => e.Translations)
            .OrderBy(e => e.DisplayOrder)
            .ThenByDescending(e => e.StartDate)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return items.Select(ToDto).ToList();
    }

    public async Task<AdminExperienceDto> CreateAsync(SaveExperienceRequest request, CancellationToken cancellationToken)
    {
        var experience = new Experience { Company = string.Empty };
        Apply(experience, request);
        db.Experiences.Add(experience);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(experience);
    }

    public async Task<Result<AdminExperienceDto>> UpdateAsync(
        int id, SaveExperienceRequest request, CancellationToken cancellationToken)
    {
        var experience = await db.Experiences
            .Include(e => e.Translations)
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (experience is null)
        {
            return Result<AdminExperienceDto>.NotFound();
        }

        Apply(experience, request);
        await db.SaveChangesAsync(cancellationToken);
        return Result<AdminExperienceDto>.Ok(ToDto(experience));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await db.Experiences.Where(e => e.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? Result.Missing : Result.Success;
    }

    private static void Apply(Experience experience, SaveExperienceRequest request)
    {
        experience.Company = request.Company.Trim();
        experience.CompanyUrl = Text.Clean(request.CompanyUrl);
        experience.Type = request.Type;
        experience.StartDate = request.StartDate;
        experience.EndDate = request.EndDate;
        experience.DisplayOrder = request.DisplayOrder;

        Text.SyncTranslations(
            experience.Translations,
            request.Translations,
            i => i.Language,
            language => new ExperienceTranslation { Language = language, Role = string.Empty },
            (entity, input) =>
            {
                entity.Role = input.Role.Trim();
                entity.Location = Text.Clean(input.Location);
                entity.Description = input.Description?.Trim() ?? string.Empty;
            });
    }

    private static AdminExperienceDto ToDto(Experience e) => new(
        e.Id,
        e.Company,
        e.CompanyUrl,
        e.Type,
        e.StartDate,
        e.EndDate,
        e.DisplayOrder,
        e.Translations
            .OrderBy(t => Text.LanguageOrder(t.Language))
            .Select(t => new ExperienceTranslationInput(t.Language, t.Role, t.Location, t.Description))
            .ToList());
}

internal sealed class AdminProfileService(PortfolioDbContext db) : IAdminProfileService
{
    public async Task<AdminProfileDto?> GetAsync(CancellationToken cancellationToken)
    {
        var profile = await Query().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return profile is null ? null : ToDto(profile);
    }

    public async Task<AdminProfileDto> SaveAsync(SaveProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await Query().FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            profile = new Profile { FullName = string.Empty };
            db.Profiles.Add(profile);
        }

        profile.FullName = request.FullName.Trim();
        profile.Email = Text.Clean(request.Email);
        profile.Location = Text.Clean(request.Location);
        profile.AvatarUrl = Text.Clean(request.AvatarUrl);
        profile.CvUrlEn = Text.Clean(request.CvUrlEn);
        profile.CvUrlEs = Text.Clean(request.CvUrlEs);

        Text.SyncTranslations(
            profile.Translations,
            request.Translations,
            i => i.Language,
            language => new ProfileTranslation { Language = language, Headline = string.Empty },
            (entity, input) =>
            {
                entity.Headline = input.Headline.Trim();
                entity.Bio = input.Bio?.Trim() ?? string.Empty;
            });

        profile.SocialLinks.Clear();
        profile.SocialLinks.AddRange(request.SocialLinks.Select((link, index) => new SocialLink
        {
            Platform = link.Platform.Trim().ToLowerInvariant(),
            Url = link.Url.Trim(),
            DisplayOrder = index,
        }));

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(profile);
    }

    private IQueryable<Profile> Query() => db.Profiles
        .Include(p => p.Translations)
        .Include(p => p.SocialLinks)
        .OrderBy(p => p.Id)
        .AsSplitQuery();

    private static AdminProfileDto ToDto(Profile p) => new(
        p.FullName,
        p.Email,
        p.Location,
        p.AvatarUrl,
        p.CvUrlEn,
        p.CvUrlEs,
        p.Translations
            .OrderBy(t => Text.LanguageOrder(t.Language))
            .Select(t => new ProfileTranslationInput(t.Language, t.Headline, t.Bio))
            .ToList(),
        p.SocialLinks
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new SocialLinkInput(s.Platform, s.Url))
            .ToList());
}

internal sealed class AdminMessageService(PortfolioDbContext db) : IAdminMessageService
{
    public async Task<IReadOnlyList<AdminContactMessageDto>> ListAsync(bool unreadOnly, CancellationToken cancellationToken)
    {
        var query = db.ContactMessages.AsNoTracking();
        if (unreadOnly)
        {
            query = query.Where(m => !m.IsRead);
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new AdminContactMessageDto(
                m.Id, m.Name, m.Email, m.Subject, m.Message, m.Language, m.CreatedAt, m.IsRead))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> SetReadAsync(int id, bool isRead, CancellationToken cancellationToken)
    {
        var updated = await db.ContactMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.IsRead, isRead), cancellationToken);
        return updated == 0 ? Result.Missing : Result.Success;
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await db.ContactMessages.Where(m => m.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? Result.Missing : Result.Success;
    }
}
