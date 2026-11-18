using Portfolio.Application.Admin.Projects;
using Portfolio.Application.Common;
using Portfolio.Domain.Projects;

namespace Portfolio.UnitTests.Application;

public sealed class ValidationRulesTests
{
    [Theory]
    [InlineData("https://example.com/a.png", true)]
    [InlineData("http://example.com", true)]
    [InlineData("/uploads/abc.png", true)]
    [InlineData("/uploads/../appsettings.json", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html;base64,AAAA", false)]
    [InlineData("//evil.example/x.png", false)]
    [InlineData("/other/x.png", false)]
    public void IsHttpOrUploadUrl(string value, bool expected) =>
        Assert.Equal(expected, ValidationRules.IsHttpOrUploadUrl(value));
}

public sealed class SaveProjectRequestValidatorTests
{
    private readonly SaveProjectRequestValidator _validator = new();

    private static SaveProjectRequest Valid(ProjectStatus status = ProjectStatus.Published, params string[] languages) => new(
        "my-project",
        ProjectCategory.Web,
        status,
        false,
        0,
        null,
        null,
        null,
        null,
        null,
        [],
        (languages.Length == 0 ? ["en", "es"] : languages)
            .Select(l => new ProjectTranslationInput(l, "Title", "Summary", null))
            .ToList(),
        []);

    [Fact]
    public void PublishedWithBothLanguages_IsValid() =>
        Assert.True(_validator.Validate(Valid()).IsValid);

    [Fact]
    public void DraftWithOneLanguage_IsValid() =>
        Assert.True(_validator.Validate(Valid(ProjectStatus.Draft, "en")).IsValid);

    [Fact]
    public void PublishedWithOneLanguage_IsInvalid() =>
        Assert.False(_validator.Validate(Valid(ProjectStatus.Published, "en")).IsValid);

    [Fact]
    public void RepeatedLanguage_IsInvalid() =>
        Assert.False(_validator.Validate(Valid(ProjectStatus.Draft, "en", "en")).IsValid);

    [Fact]
    public void UnsupportedLanguage_IsInvalid() =>
        Assert.False(_validator.Validate(Valid(ProjectStatus.Draft, "fr")).IsValid);

    [Fact]
    public void EndDateBeforeStartDate_IsInvalid()
    {
        var request = Valid() with { StartDate = new DateOnly(2025, 1, 1), EndDate = new DateOnly(2024, 1, 1) };
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void RepoUrlMustBeHttp()
    {
        var request = Valid() with { RepoUrl = "javascript:alert(1)" };
        Assert.False(_validator.Validate(request).IsValid);
    }
}
