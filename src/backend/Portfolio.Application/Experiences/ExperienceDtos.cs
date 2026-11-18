namespace Portfolio.Application.Experiences;

public sealed record ExperienceDto(
    string Company,
    string? CompanyUrl,
    string Type,
    string Role,
    string? Location,
    string Description,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsCurrent);

public interface IExperienceQueries
{
    /// <summary>Experience entries, most recent first.</summary>
    Task<IReadOnlyList<ExperienceDto>> GetAllAsync(string language, CancellationToken cancellationToken);
}
