using Portfolio.Domain.Common;
using Portfolio.Domain.Technologies;

namespace Portfolio.Domain.Projects;

public sealed class Project : Entity
{
    public const int SlugMaxLength = 100;
    public const int UrlMaxLength = 500;

    public required string Slug { get; set; }
    public ProjectCategory Category { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? RepoUrl { get; set; }
    public string? DemoUrl { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<ProjectTranslation> Translations { get; set; } = [];
    public List<ProjectImage> Images { get; set; } = [];
    public List<Technology> Technologies { get; set; } = [];

    /// <summary>
    /// A project is shown on the public site only when it is published and translated
    /// into every supported language.
    /// </summary>
    public bool IsPubliclyVisible =>
        Status == ProjectStatus.Published
        && Languages.Supported.All(language =>
            Translations.Any(t => string.Equals(t.Language, language, StringComparison.OrdinalIgnoreCase)));
}
