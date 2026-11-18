using Portfolio.Domain.Common;
using Portfolio.Domain.Projects;

namespace Portfolio.UnitTests.Domain;

public sealed class LanguagesTests
{
    [Theory]
    [InlineData("en", "en")]
    [InlineData("ES", "es")]
    [InlineData(" es ", "es")]
    [InlineData("fr", "en")]
    [InlineData("", "en")]
    [InlineData(null, "en")]
    public void Normalize_ReturnsSupportedCodeOrDefault(string? input, string expected) =>
        Assert.Equal(expected, Languages.Normalize(input));

    [Fact]
    public void ForLanguage_ReturnsRequestedTranslation()
    {
        var translations = Translations("en", "es");

        Assert.Equal("es", translations.ForLanguage("es")!.Language);
    }

    [Fact]
    public void ForLanguage_FallsBackToDefaultLanguage()
    {
        var translations = Translations("es", "en");

        Assert.Equal("en", translations.ForLanguage("fr")!.Language);
    }

    [Fact]
    public void ForLanguage_FallsBackToAnyTranslationWhenDefaultIsMissing()
    {
        var translations = Translations("es");

        Assert.Equal("es", translations.ForLanguage("en")!.Language);
    }

    [Fact]
    public void ForLanguage_ReturnsNullWhenThereAreNoTranslations() =>
        Assert.Null(Translations().ForLanguage("en"));

    private static List<ProjectTranslation> Translations(params string[] languages) =>
        languages.Select(l => new ProjectTranslation { Language = l, Title = l, Summary = l }).ToList();
}
