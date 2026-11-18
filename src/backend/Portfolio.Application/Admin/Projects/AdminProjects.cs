using System.Text.RegularExpressions;
using FluentValidation;
using Portfolio.Application.Common;
using Portfolio.Domain.Projects;

namespace Portfolio.Application.Admin.Projects;

public sealed record AdminProjectListItemDto(
    int Id,
    string Slug,
    string? TitleEn,
    string? TitleEs,
    ProjectCategory Category,
    ProjectStatus Status,
    bool IsFeatured,
    int DisplayOrder,
    bool IsPubliclyVisible,
    DateTimeOffset UpdatedAt);

public sealed record AdminProjectDto(
    int Id,
    string Slug,
    ProjectCategory Category,
    ProjectStatus Status,
    bool IsFeatured,
    int DisplayOrder,
    string? CoverImageUrl,
    string? RepoUrl,
    string? DemoUrl,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<int> TechnologyIds,
    IReadOnlyList<ProjectTranslationInput> Translations,
    IReadOnlyList<ProjectImageInput> Images,
    bool IsPubliclyVisible,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectTranslationInput(string Language, string Title, string Summary, string? Content);

public sealed record ProjectImageInput(string Url, string AltEn, string AltEs);

/// <summary>Create/update payload. Images are stored in the given order.</summary>
public sealed record SaveProjectRequest(
    string Slug,
    ProjectCategory Category,
    ProjectStatus Status,
    bool IsFeatured,
    int DisplayOrder,
    string? CoverImageUrl,
    string? RepoUrl,
    string? DemoUrl,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<int> TechnologyIds,
    IReadOnlyList<ProjectTranslationInput> Translations,
    IReadOnlyList<ProjectImageInput> Images);

public interface IAdminProjectService
{
    Task<IReadOnlyList<AdminProjectListItemDto>> ListAsync(CancellationToken cancellationToken);
    Task<AdminProjectDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<AdminProjectDto>> CreateAsync(SaveProjectRequest request, CancellationToken cancellationToken);
    Task<Result<AdminProjectDto>> UpdateAsync(int id, SaveProjectRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

public sealed partial class SaveProjectRequestValidator : AbstractValidator<SaveProjectRequest>
{
    public const int MaxImages = 20;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    public SaveProjectRequestValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Project.SlugMaxLength)
            .Matches(SlugPattern())
            .WithMessage("'Slug' may only contain lowercase letters, numbers and single hyphens (e.g. my-project).");

        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 10_000);

        RuleFor(x => x.CoverImageUrl).OptionalAssetUrl(Project.UrlMaxLength);
        RuleFor(x => x.RepoUrl).OptionalHttpUrl(Project.UrlMaxLength);
        RuleFor(x => x.DemoUrl).OptionalHttpUrl(Project.UrlMaxLength);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate is not null && x.EndDate is not null)
            .WithMessage("'End Date' must be on or after 'Start Date'.");

        RuleFor(x => x.TechnologyIds)
            .NotNull()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Technologies cannot be repeated.");

        RuleFor(x => x.Translations).NotNull().UniqueLanguages(t => t.Language);
        RuleFor(x => x.Translations)
            .CoversAllLanguages(t => t.Language)
            .When(x => x.Status == ProjectStatus.Published)
            .WithMessage("A published project needs a translation in every language (en, es).");
        RuleForEach(x => x.Translations).ChildRules(translation =>
        {
            translation.RuleFor(t => t.Language).SupportedLanguage();
            translation.RuleFor(t => t.Title).NotEmpty().MaximumLength(ProjectTranslation.TitleMaxLength);
            translation.RuleFor(t => t.Summary).NotEmpty().MaximumLength(ProjectTranslation.SummaryMaxLength);
        });

        RuleFor(x => x.Images).NotNull().Must(images => images.Count <= MaxImages)
            .WithMessage($"A project can have at most {MaxImages} images.");
        RuleForEach(x => x.Images).ChildRules(image =>
        {
            image.RuleFor(i => i.Url).NotEmpty().OptionalAssetUrl(Project.UrlMaxLength);
            image.RuleFor(i => i.AltEn).NotEmpty().MaximumLength(ProjectImage.AltMaxLength);
            image.RuleFor(i => i.AltEs).NotEmpty().MaximumLength(ProjectImage.AltMaxLength);
        });
    }
}
