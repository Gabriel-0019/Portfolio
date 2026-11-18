namespace Portfolio.Application.Technologies;

public sealed record TechnologyDto(string Name, string? IconKey, string Category);

public interface ITechnologyQueries
{
    Task<IReadOnlyList<TechnologyDto>> GetAllAsync(CancellationToken cancellationToken);
}
