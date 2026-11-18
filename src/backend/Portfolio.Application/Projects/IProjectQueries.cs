namespace Portfolio.Application.Projects;

public interface IProjectQueries
{
    /// <summary>Publicly visible projects ordered by featured first, then display order.</summary>
    Task<IReadOnlyList<ProjectSummaryDto>> GetPublishedAsync(
        ProjectFilter filter, string language, CancellationToken cancellationToken);

    /// <summary>Returns null when the slug does not exist or the project is not publicly visible.</summary>
    Task<ProjectDetailDto?> GetPublishedBySlugAsync(
        string slug, string language, CancellationToken cancellationToken);
}
