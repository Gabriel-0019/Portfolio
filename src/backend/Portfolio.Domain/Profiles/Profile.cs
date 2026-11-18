using Portfolio.Domain.Common;

namespace Portfolio.Domain.Profiles;

/// <summary>The portfolio owner. There is a single profile row.</summary>
public sealed class Profile : Entity
{
    public const int FullNameMaxLength = 120;
    public const int EmailMaxLength = 254;
    public const int LocationMaxLength = 120;
    public const int UrlMaxLength = 500;

    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? Location { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CvUrlEn { get; set; }
    public string? CvUrlEs { get; set; }

    public List<ProfileTranslation> Translations { get; set; } = [];
    public List<SocialLink> SocialLinks { get; set; } = [];

    public string? CvUrlFor(string language) =>
        string.Equals(language, Languages.Spanish, StringComparison.OrdinalIgnoreCase)
            ? CvUrlEs ?? CvUrlEn
            : CvUrlEn ?? CvUrlEs;
}

public sealed class ProfileTranslation : Entity, ITranslation
{
    public const int HeadlineMaxLength = 150;

    public int ProfileId { get; set; }
    public required string Language { get; set; }
    public required string Headline { get; set; }

    /// <summary>Short biography in Markdown.</summary>
    public string Bio { get; set; } = string.Empty;
}

public sealed class SocialLink : Entity
{
    public const int PlatformMaxLength = 40;

    public int ProfileId { get; set; }

    /// <summary>Platform key used by the frontend for the icon (github, linkedin, email, website...).</summary>
    public required string Platform { get; set; }

    public required string Url { get; set; }
    public int DisplayOrder { get; set; }
}
