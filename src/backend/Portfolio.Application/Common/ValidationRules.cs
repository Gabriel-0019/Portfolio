using FluentValidation;
using Portfolio.Domain.Common;

namespace Portfolio.Application.Common;

public static class ValidationRules
{
    /// <summary>Prefix of files served from the local upload store.</summary>
    public const string UploadsPrefix = "/uploads/";

    /// <summary>Absolute http(s) URL.</summary>
    public static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>Absolute http(s) URL or a file uploaded to this site.</summary>
    public static bool IsHttpOrUploadUrl(string? value) =>
        IsHttpUrl(value)
        || (value is not null
            && value.StartsWith(UploadsPrefix, StringComparison.Ordinal)
            && !value.Contains("..", StringComparison.Ordinal));

    public static IRuleBuilderOptions<T, string?> OptionalHttpUrl<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule.MaximumLength(maxLength)
            .Must(value => string.IsNullOrWhiteSpace(value) || IsHttpUrl(value))
            .WithMessage("'{PropertyName}' must be an absolute http(s) URL.");

    public static IRuleBuilderOptions<T, string?> OptionalAssetUrl<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule.MaximumLength(maxLength)
            .Must(value => string.IsNullOrWhiteSpace(value) || IsHttpOrUploadUrl(value))
            .WithMessage("'{PropertyName}' must be an http(s) URL or an uploaded file.");

    public static IRuleBuilderOptions<T, string> SupportedLanguage<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(Languages.IsSupported)
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", Languages.Supported)}.");

    /// <summary>Each supported language appears exactly once.</summary>
    public static IRuleBuilderOptions<T, IReadOnlyList<TItem>> CoversAllLanguages<T, TItem>(
        this IRuleBuilder<T, IReadOnlyList<TItem>> rule, Func<TItem, string> language) =>
        rule.Must(items => Languages.Supported.All(l =>
                items.Count(i => string.Equals(language(i), l, StringComparison.OrdinalIgnoreCase)) == 1))
            .WithMessage($"Exactly one translation is required for each language: {string.Join(", ", Languages.Supported)}.");

    /// <summary>No language appears more than once.</summary>
    public static IRuleBuilderOptions<T, IReadOnlyList<TItem>> UniqueLanguages<T, TItem>(
        this IRuleBuilder<T, IReadOnlyList<TItem>> rule, Func<TItem, string> language) =>
        rule.Must(items => items
                .GroupBy(i => language(i)?.Trim().ToLowerInvariant())
                .All(g => g.Count() == 1))
            .WithMessage("Each language can only appear once.");
}
