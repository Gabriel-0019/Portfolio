using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Portfolio.Application.Projects;
using Portfolio.Domain.Projects;
using Portfolio.IntegrationTests.Infrastructure;

namespace Portfolio.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ProjectsEndpointTests(PortfolioApiFactory factory)
{
    private const string SeededSlug = "portfolio";

    [Fact]
    public async Task GetProjects_DefaultsToEnglish()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("en", response.Content.Headers.ContentLanguage);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectSummaryDto>>();
        var portfolio = Assert.Single(projects!, p => p.Slug == SeededSlug);
        Assert.Equal("Developer Portfolio", portfolio.Title);
        Assert.True(portfolio.IsFeatured);
        Assert.Contains(portfolio.Technologies, t => t.Name == "React");
    }

    [Fact]
    public async Task GetProjects_WithLangQuery_ReturnsSpanish()
    {
        var client = factory.CreateClient();

        var projects = await client.GetFromJsonAsync<List<ProjectSummaryDto>>("/api/projects?lang=es");

        Assert.Equal("Portafolio de desarrollador", Assert.Single(projects!, p => p.Slug == SeededSlug).Title);
    }

    [Fact]
    public async Task GetProjects_WithRegionalAcceptLanguage_ReturnsSpanish()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("es-MX"));

        var projects = await client.GetFromJsonAsync<List<ProjectSummaryDto>>("/api/projects");

        Assert.Equal("Portafolio de desarrollador", Assert.Single(projects!, p => p.Slug == SeededSlug).Title);
    }

    [Fact]
    public async Task GetProjects_WithUnsupportedLanguage_FallsBackToEnglish()
    {
        var client = factory.CreateClient();

        var projects = await client.GetFromJsonAsync<List<ProjectSummaryDto>>("/api/projects?lang=fr");

        Assert.Equal("Developer Portfolio", Assert.Single(projects!, p => p.Slug == SeededSlug).Title);
    }

    [Theory]
    [InlineData("?tech=React", true)]
    [InlineData("?tech=react", true)]
    [InlineData("?tech=Python", false)]
    [InlineData("?category=web", true)]
    [InlineData("?category=Mobile", false)]
    [InlineData("?category=NotACategory", false)]
    [InlineData("?category=1", false)]
    [InlineData("?featured=true", true)]
    [InlineData("?featured=false", false)]
    public async Task GetProjects_AppliesFilters(string query, bool expectSeededProject)
    {
        var client = factory.CreateClient();

        var projects = await client.GetFromJsonAsync<List<ProjectSummaryDto>>($"/api/projects{query}");

        Assert.Equal(expectSeededProject, projects!.Any(p => p.Slug == SeededSlug));
    }

    [Fact]
    public async Task GetProjectBySlug_ReturnsCaseStudyInRequestedLanguage()
    {
        var client = factory.CreateClient();

        var english = await client.GetFromJsonAsync<ProjectDetailDto>($"/api/projects/{SeededSlug}");
        var spanish = await client.GetFromJsonAsync<ProjectDetailDto>($"/api/projects/{SeededSlug}?lang=es");

        Assert.Contains("## Problem", english!.Content);
        Assert.Contains("## Problema", spanish!.Content);
        Assert.Equal("Web", english.Category);
    }

    [Fact]
    public async Task GetProjectBySlug_WhenMissing_Returns404ProblemDetails()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(ProjectStatus.Draft, new[] { "en", "es" })]
    [InlineData(ProjectStatus.Archived, new[] { "en", "es" })]
    [InlineData(ProjectStatus.Published, new[] { "en" })]
    public async Task HiddenProjects_AreNotExposed(ProjectStatus status, string[] languages)
    {
        var slug = $"hidden-{Guid.NewGuid():N}";
        await factory.WithDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            db.Projects.Add(new Project
            {
                Slug = slug,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now,
                Translations = languages
                    .Select(l => new ProjectTranslation { Language = l, Title = "Hidden", Summary = "Hidden" })
                    .ToList(),
            });
            await db.SaveChangesAsync();
        });
        var client = factory.CreateClient();

        var list = await client.GetFromJsonAsync<List<ProjectSummaryDto>>("/api/projects");
        var detail = await client.GetAsync($"/api/projects/{slug}");

        Assert.DoesNotContain(list!, p => p.Slug == slug);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }
}
