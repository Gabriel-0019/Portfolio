using Portfolio.Application.Technologies;

namespace Portfolio.Application.Projects;

public sealed record ProjectSummaryDto(
    string Slug,
    string Title,
    string Summary,
    string Category,
    bool IsFeatured,
    string? CoverImageUrl,
    string? RepoUrl,
    string? DemoUrl,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<TechnologyDto> Technologies);

public sealed record ProjectDetailDto(
    string Slug,
    string Title,
    string Summary,
    string Content,
    string Category,
    bool IsFeatured,
    string? CoverImageUrl,
    string? RepoUrl,
    string? DemoUrl,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<TechnologyDto> Technologies,
    IReadOnlyList<ProjectImageDto> Images);

public sealed record ProjectImageDto(string Url, string Alt);

/// <summary>Optional filters for the public project list. Values are matched case-insensitively.</summary>
public sealed record ProjectFilter(string? Category = null, string? Technology = null, bool? Featured = null);
