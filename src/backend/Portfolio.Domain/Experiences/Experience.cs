using Portfolio.Domain.Common;

namespace Portfolio.Domain.Experiences;

public sealed class Experience : Entity
{
    public const int CompanyMaxLength = 120;
    public const int UrlMaxLength = 500;

    public required string Company { get; set; }
    public string? CompanyUrl { get; set; }
    public ExperienceType Type { get; set; } = ExperienceType.Work;
    public DateOnly StartDate { get; set; }

    /// <summary>Null means the position is current.</summary>
    public DateOnly? EndDate { get; set; }

    public int DisplayOrder { get; set; }

    public List<ExperienceTranslation> Translations { get; set; } = [];

    public bool IsCurrent => EndDate is null;
}

public sealed class ExperienceTranslation : Entity, ITranslation
{
    public const int RoleMaxLength = 120;
    public const int LocationMaxLength = 120;

    public int ExperienceId { get; set; }
    public required string Language { get; set; }
    public required string Role { get; set; }
    public string? Location { get; set; }

    /// <summary>Responsibilities and achievements in Markdown.</summary>
    public string Description { get; set; } = string.Empty;
}

public enum ExperienceType
{
    Work,
    Education,
    Certification,
}
