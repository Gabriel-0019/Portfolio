using Portfolio.Domain.Common;

namespace Portfolio.Domain.Projects;

public sealed class ProjectImage : Entity
{
    public const int AltMaxLength = 250;

    public int ProjectId { get; set; }
    public required string Url { get; set; }
    public required string AltEn { get; set; }
    public required string AltEs { get; set; }
    public int DisplayOrder { get; set; }

    public string AltFor(string language) =>
        string.Equals(language, Languages.Spanish, StringComparison.OrdinalIgnoreCase) ? AltEs : AltEn;
}
