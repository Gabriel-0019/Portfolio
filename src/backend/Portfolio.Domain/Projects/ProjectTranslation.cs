using Portfolio.Domain.Common;

namespace Portfolio.Domain.Projects;

public sealed class ProjectTranslation : Entity, ITranslation
{
    public const int TitleMaxLength = 150;
    public const int SummaryMaxLength = 300;

    public int ProjectId { get; set; }
    public required string Language { get; set; }
    public required string Title { get; set; }
    public required string Summary { get; set; }

    /// <summary>Case study body in Markdown (problem, solution, architecture, challenges, results).</summary>
    public string Content { get; set; } = string.Empty;
}
