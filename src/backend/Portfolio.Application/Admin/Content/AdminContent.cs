using FluentValidation;
using Portfolio.Application.Common;
using Portfolio.Domain.Experiences;
using Portfolio.Domain.Profiles;
using Portfolio.Domain.Technologies;

namespace Portfolio.Application.Admin.Content;

// ---------- Technologies ----------

public sealed record AdminTechnologyDto(
    int Id, string Name, string? IconKey, TechnologyCategory Category, int DisplayOrder, int ProjectCount);

public sealed record SaveTechnologyRequest(string Name, string? IconKey, TechnologyCategory Category, int DisplayOrder);

public interface IAdminTechnologyService
{
    Task<IReadOnlyList<AdminTechnologyDto>> ListAsync(CancellationToken cancellationToken);
    Task<Result<AdminTechnologyDto>> CreateAsync(SaveTechnologyRequest request, CancellationToken cancellationToken);
    Task<Result<AdminTechnologyDto>> UpdateAsync(int id, SaveTechnologyRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

public sealed class SaveTechnologyRequestValidator : AbstractValidator<SaveTechnologyRequest>
{
    public SaveTechnologyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Technology.NameMaxLength);
        RuleFor(x => x.IconKey).MaximumLength(Technology.IconKeyMaxLength);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 10_000);
    }
}

// ---------- Experience ----------

public sealed record ExperienceTranslationInput(string Language, string Role, string? Location, string? Description);

public sealed record AdminExperienceDto(
    int Id,
    string Company,
    string? CompanyUrl,
    ExperienceType Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    int DisplayOrder,
    IReadOnlyList<ExperienceTranslationInput> Translations);

public sealed record SaveExperienceRequest(
    string Company,
    string? CompanyUrl,
    ExperienceType Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    int DisplayOrder,
    IReadOnlyList<ExperienceTranslationInput> Translations);

public interface IAdminExperienceService
{
    Task<IReadOnlyList<AdminExperienceDto>> ListAsync(CancellationToken cancellationToken);
    Task<AdminExperienceDto> CreateAsync(SaveExperienceRequest request, CancellationToken cancellationToken);
    Task<Result<AdminExperienceDto>> UpdateAsync(int id, SaveExperienceRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

public sealed class SaveExperienceRequestValidator : AbstractValidator<SaveExperienceRequest>
{
    public SaveExperienceRequestValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(Experience.CompanyMaxLength);
        RuleFor(x => x.CompanyUrl).OptionalHttpUrl(Experience.UrlMaxLength);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate is not null)
            .WithMessage("'End Date' must be on or after 'Start Date'.");
        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 10_000);

        RuleFor(x => x.Translations).NotNull().CoversAllLanguages(t => t.Language);
        RuleForEach(x => x.Translations).ChildRules(translation =>
        {
            translation.RuleFor(t => t.Language).SupportedLanguage();
            translation.RuleFor(t => t.Role).NotEmpty().MaximumLength(ExperienceTranslation.RoleMaxLength);
            translation.RuleFor(t => t.Location).MaximumLength(ExperienceTranslation.LocationMaxLength);
        });
    }
}

// ---------- Profile ----------

public sealed record ProfileTranslationInput(string Language, string Headline, string? Bio);

public sealed record SocialLinkInput(string Platform, string Url);

public sealed record AdminProfileDto(
    string FullName,
    string? Email,
    string? Location,
    string? AvatarUrl,
    string? CvUrlEn,
    string? CvUrlEs,
    IReadOnlyList<ProfileTranslationInput> Translations,
    IReadOnlyList<SocialLinkInput> SocialLinks);

public sealed record SaveProfileRequest(
    string FullName,
    string? Email,
    string? Location,
    string? AvatarUrl,
    string? CvUrlEn,
    string? CvUrlEs,
    IReadOnlyList<ProfileTranslationInput> Translations,
    IReadOnlyList<SocialLinkInput> SocialLinks);

public interface IAdminProfileService
{
    Task<AdminProfileDto?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Creates the profile on first save, updates it afterwards.</summary>
    Task<AdminProfileDto> SaveAsync(SaveProfileRequest request, CancellationToken cancellationToken);
}

public sealed class SaveProfileRequestValidator : AbstractValidator<SaveProfileRequest>
{
    public const int MaxSocialLinks = 10;

    public SaveProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(Profile.FullNameMaxLength);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(Profile.EmailMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Location).MaximumLength(Profile.LocationMaxLength);
        RuleFor(x => x.AvatarUrl).OptionalAssetUrl(Profile.UrlMaxLength);
        RuleFor(x => x.CvUrlEn).OptionalAssetUrl(Profile.UrlMaxLength);
        RuleFor(x => x.CvUrlEs).OptionalAssetUrl(Profile.UrlMaxLength);

        RuleFor(x => x.Translations).NotNull().CoversAllLanguages(t => t.Language);
        RuleForEach(x => x.Translations).ChildRules(translation =>
        {
            translation.RuleFor(t => t.Language).SupportedLanguage();
            translation.RuleFor(t => t.Headline).NotEmpty().MaximumLength(ProfileTranslation.HeadlineMaxLength);
        });

        RuleFor(x => x.SocialLinks).NotNull().Must(links => links.Count <= MaxSocialLinks)
            .WithMessage($"At most {MaxSocialLinks} social links are allowed.");
        RuleForEach(x => x.SocialLinks).ChildRules(link =>
        {
            link.RuleFor(l => l.Platform).NotEmpty().MaximumLength(SocialLink.PlatformMaxLength);
            link.RuleFor(l => l.Url)
                .NotEmpty()
                .MaximumLength(Profile.UrlMaxLength)
                .Must(url => ValidationRules.IsHttpUrl(url) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                .WithMessage("'Url' must be an http(s) or mailto: link.");
        });
    }
}

// ---------- Contact messages ----------

public sealed record AdminContactMessageDto(
    int Id,
    string Name,
    string Email,
    string? Subject,
    string Message,
    string Language,
    DateTimeOffset CreatedAt,
    bool IsRead);

public sealed record UpdateContactMessageRequest(bool IsRead);

public interface IAdminMessageService
{
    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<AdminContactMessageDto>> ListAsync(bool unreadOnly, CancellationToken cancellationToken);
    Task<Result> SetReadAsync(int id, bool isRead, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

// ---------- Uploads ----------

public sealed record StoredFile(string Url, string ContentType, long Size);

public interface IFileStorage
{
    /// <summary>
    /// Validates the file signature against the allowed types and stores it under a random name.
    /// Returns null when the content is not an allowed type.
    /// </summary>
    Task<StoredFile?> SaveAsync(Stream content, CancellationToken cancellationToken);
}
