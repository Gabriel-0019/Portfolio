using Portfolio.Domain.Projects;

namespace Portfolio.UnitTests.Domain;

public sealed class ProjectTests
{
    [Fact]
    public void IsPubliclyVisible_WhenPublishedAndFullyTranslated_IsTrue() =>
        Assert.True(CreateProject(ProjectStatus.Published, "en", "es").IsPubliclyVisible);

    [Fact]
    public void IsPubliclyVisible_WhenDraft_IsFalse() =>
        Assert.False(CreateProject(ProjectStatus.Draft, "en", "es").IsPubliclyVisible);

    [Fact]
    public void IsPubliclyVisible_WhenAnyLanguageIsMissing_IsFalse() =>
        Assert.False(CreateProject(ProjectStatus.Published, "en").IsPubliclyVisible);

    [Theory]
    [InlineData("en", "English alt")]
    [InlineData("es", "Texto alternativo")]
    [InlineData("fr", "English alt")]
    public void ProjectImage_AltFor_PicksLanguage(string language, string expected)
    {
        var image = new ProjectImage { Url = "/x.png", AltEn = "English alt", AltEs = "Texto alternativo" };

        Assert.Equal(expected, image.AltFor(language));
    }

    private static Project CreateProject(ProjectStatus status, params string[] languages) => new()
    {
        Slug = "test",
        Status = status,
        Translations = languages
            .Select(l => new ProjectTranslation { Language = l, Title = "T", Summary = "S" })
            .ToList(),
    };
}
