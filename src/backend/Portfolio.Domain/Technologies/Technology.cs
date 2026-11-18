using Portfolio.Domain.Common;
using Portfolio.Domain.Projects;

namespace Portfolio.Domain.Technologies;

public sealed class Technology : Entity
{
    public const int NameMaxLength = 60;
    public const int IconKeyMaxLength = 60;

    public required string Name { get; set; }

    /// <summary>Icon identifier used by the frontend (e.g. a Simple Icons slug such as "dotnet").</summary>
    public string? IconKey { get; set; }

    public TechnologyCategory Category { get; set; }
    public int DisplayOrder { get; set; }

    public List<Project> Projects { get; set; } = [];
}

public enum TechnologyCategory
{
    Language,
    Frontend,
    Backend,
    Database,
    DevOps,
    Tool,
}
