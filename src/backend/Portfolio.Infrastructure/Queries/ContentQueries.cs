using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Experiences;
using Portfolio.Application.Profiles;
using Portfolio.Application.Technologies;
using Portfolio.Domain.Common;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Queries;

internal sealed class ProfileQueries(PortfolioDbContext db) : IProfileQueries
{
    public async Task<ProfileDto?> GetAsync(string language, CancellationToken cancellationToken)
    {
        var profile = await db.Profiles
            .Include(p => p.Translations)
            .Include(p => p.SocialLinks)
            .OrderBy(p => p.Id)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var translation = profile.Translations.ForLanguage(language);

        return new ProfileDto(
            profile.FullName,
            translation?.Headline ?? string.Empty,
            translation?.Bio ?? string.Empty,
            profile.Email,
            profile.Location,
            profile.AvatarUrl,
            profile.CvUrlFor(language),
            profile.SocialLinks
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new SocialLinkDto(s.Platform, s.Url))
                .ToList());
    }
}

internal sealed class ExperienceQueries(PortfolioDbContext db) : IExperienceQueries
{
    public async Task<IReadOnlyList<ExperienceDto>> GetAllAsync(string language, CancellationToken cancellationToken)
    {
        var experiences = await db.Experiences
            .Include(e => e.Translations)
            .OrderBy(e => e.DisplayOrder)
            .ThenByDescending(e => e.StartDate)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return experiences
            .Select(e =>
            {
                var translation = e.Translations.ForLanguage(language);
                return new ExperienceDto(
                    e.Company,
                    e.CompanyUrl,
                    e.Type.ToString(),
                    translation?.Role ?? string.Empty,
                    translation?.Location,
                    translation?.Description ?? string.Empty,
                    e.StartDate,
                    e.EndDate,
                    e.IsCurrent);
            })
            .ToList();
    }
}

internal sealed class TechnologyQueries(PortfolioDbContext db) : ITechnologyQueries
{
    public async Task<IReadOnlyList<TechnologyDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var technologies = await db.Technologies
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return technologies
            .Select(t => new TechnologyDto(t.Name, t.IconKey, t.Category.ToString()))
            .ToList();
    }
}
