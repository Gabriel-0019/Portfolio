namespace Portfolio.Application.Profiles;

public sealed record ProfileDto(
    string FullName,
    string Headline,
    string Bio,
    string? Email,
    string? Location,
    string? AvatarUrl,
    string? CvUrl,
    IReadOnlyList<SocialLinkDto> SocialLinks);

public sealed record SocialLinkDto(string Platform, string Url);

public interface IProfileQueries
{
    /// <summary>Returns null when no profile has been created yet.</summary>
    Task<ProfileDto?> GetAsync(string language, CancellationToken cancellationToken);
}
