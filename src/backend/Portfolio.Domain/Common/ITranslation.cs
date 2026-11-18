namespace Portfolio.Domain.Common;

/// <summary>Language-specific content attached to an entity.</summary>
public interface ITranslation
{
    string Language { get; }
}

public static class TranslationExtensions
{
    /// <summary>
    /// Picks the translation for <paramref name="language"/>, falling back to the default language
    /// and then to any available translation.
    /// </summary>
    public static T? ForLanguage<T>(this IEnumerable<T> translations, string language)
        where T : class, ITranslation
    {
        T? fallback = null;
        T? any = null;

        foreach (var translation in translations)
        {
            if (string.Equals(translation.Language, language, StringComparison.OrdinalIgnoreCase))
            {
                return translation;
            }

            if (string.Equals(translation.Language, Languages.Default, StringComparison.OrdinalIgnoreCase))
            {
                fallback = translation;
            }

            any ??= translation;
        }

        return fallback ?? any;
    }
}
