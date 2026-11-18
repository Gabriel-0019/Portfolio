namespace Portfolio.Domain.Common;

/// <summary>
/// Languages supported by the portfolio. Every public piece of content must exist in all of them.
/// </summary>
public static class Languages
{
    public const string English = "en";
    public const string Spanish = "es";
    public const string Default = English;

    /// <summary>Maximum length of a language code column.</summary>
    public const int CodeMaxLength = 5;

    public static readonly string[] Supported = [English, Spanish];

    public static bool IsSupported(string? language) =>
        !string.IsNullOrWhiteSpace(language)
        && Supported.Contains(language.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the normalized code when supported, otherwise <see cref="Default"/>.</summary>
    public static string Normalize(string? language) =>
        IsSupported(language) ? language!.Trim().ToLowerInvariant() : Default;
}
