using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Portfolio.Api.Controllers;
using Portfolio.Application.Admin.Content;
using Portfolio.Application.Admin.Projects;
using Portfolio.Application.Contact;
using Portfolio.Application.Profiles;
using Portfolio.Application.Projects;
using Portfolio.Domain.Experiences;
using Portfolio.Domain.Projects;
using Portfolio.Domain.Technologies;
using Portfolio.IntegrationTests.Infrastructure;

namespace Portfolio.IntegrationTests;

internal static class Json
{
    /// <summary>Matches the API: camelCase with enums as strings.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}

[Collection(ApiCollection.Name)]
public sealed class AuthEndpointTests(PortfolioApiFactory factory)
{
    [Theory]
    [InlineData("GET", "/api/admin/projects")]
    [InlineData("GET", "/api/admin/messages")]
    [InlineData("GET", "/api/admin/profile")]
    [InlineData("DELETE", "/api/admin/technologies/1")]
    [InlineData("GET", "/api/auth/me")]
    public async Task AdminEndpoints_RequireAuthentication(string method, string url)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(PortfolioApiFactory.AdminEmail, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_SetsHttpOnlyStrictCookie_AndMeReturnsTheAdmin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var login = await client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(PortfolioApiFactory.AdminEmail, factory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith("portfolio.auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal(PortfolioApiFactory.AdminEmail, me!.Email);

        await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
}

[Collection(ApiCollection.Name)]
public sealed class AdminProjectsEndpointTests(PortfolioApiFactory factory)
{
    private static SaveProjectRequest Request(
        string slug,
        ProjectStatus status = ProjectStatus.Draft,
        IReadOnlyList<int>? technologyIds = null,
        params string[] languages) => new(
        slug,
        ProjectCategory.Backend,
        status,
        IsFeatured: false,
        DisplayOrder: 5,
        CoverImageUrl: null,
        RepoUrl: "https://github.com/example/repo",
        DemoUrl: null,
        StartDate: new DateOnly(2026, 1, 1),
        EndDate: null,
        TechnologyIds: technologyIds ?? [],
        Translations: (languages.Length == 0 ? ["en", "es"] : languages)
            .Select(l => new ProjectTranslationInput(l, $"Title {l}", $"Summary {l}", $"## Content {l}"))
            .ToList(),
        Images: [new ProjectImageInput("/uploads/example.png", "Alt en", "Alt es")]);

    [Fact]
    public async Task ProjectLifecycle_DraftIsHidden_PublishedIsPublic_DeleteRemovesIt()
    {
        var admin = await factory.CreateAdminClientAsync();
        var anonymous = factory.CreateClient();
        var slug = $"api-{Guid.NewGuid():N}"[..20];
        var technologies = await admin.GetFromJsonAsync<List<AdminTechnologyDto>>("/api/admin/technologies", Json.Options);
        var reactId = technologies!.Single(t => t.Name == "React").Id;

        // Create as draft.
        var created = await admin.PostAsJsonAsync("/api/admin/projects", Request(slug, technologyIds: [reactId]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var project = await created.Content.ReadFromJsonAsync<AdminProjectDto>(Json.Options);
        Assert.False(project!.IsPubliclyVisible);
        Assert.Equal([reactId], project.TechnologyIds);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/projects/{slug}")).StatusCode);

        // Publish.
        var updated = await admin.PutAsJsonAsync(
            $"/api/admin/projects/{project.Id}", Request(slug, ProjectStatus.Published, [reactId]), Json.Options);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var detail = await anonymous.GetFromJsonAsync<ProjectDetailDto>($"/api/projects/{slug}?lang=es");
        Assert.Equal("Title es", detail!.Title);
        Assert.Equal("Alt es", Assert.Single(detail.Images).Alt);

        // Delete.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/projects/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/admin/projects/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/projects/{slug}")).StatusCode);
    }

    [Fact]
    public async Task Update_ReplacingOneTranslation_KeepsTheOther()
    {
        var admin = await factory.CreateAdminClientAsync();
        var slug = $"tr-{Guid.NewGuid():N}"[..20];
        var created = await admin.PostAsJsonAsync("/api/admin/projects", Request(slug), Json.Options);
        var id = (await created.Content.ReadFromJsonAsync<AdminProjectDto>(Json.Options))!.Id;

        var response = await admin.PutAsJsonAsync($"/api/admin/projects/{id}", Request(slug, languages: "es"), Json.Options);

        var project = await response.Content.ReadFromJsonAsync<AdminProjectDto>(Json.Options);
        Assert.Equal("es", Assert.Single(project!.Translations).Language);
    }

    [Fact]
    public async Task Publishing_WithoutEveryLanguage_IsRejected()
    {
        var admin = await factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/admin/projects", Request("only-english", ProjectStatus.Published, languages: "en"), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Translations", problem!.Errors.Keys);
    }

    [Theory]
    [InlineData("Not A Slug")]
    [InlineData("double--hyphen")]
    [InlineData("")]
    public async Task InvalidSlug_IsRejected(string slug)
    {
        var admin = await factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/projects", Request(slug), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateSlug_Returns409()
    {
        var admin = await factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/projects", Request("portfolio"), Json.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UnknownTechnology_Returns400()
    {
        var admin = await factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/admin/projects", Request($"t-{Guid.NewGuid():N}"[..12], technologyIds: [999_999]), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_IncludesTitlesInBothLanguages()
    {
        var admin = await factory.CreateAdminClientAsync();

        var projects = await admin.GetFromJsonAsync<List<AdminProjectListItemDto>>("/api/admin/projects", Json.Options);

        var portfolio = Assert.Single(projects!, p => p.Slug == "portfolio");
        Assert.Equal("Developer Portfolio", portfolio.TitleEn);
        Assert.Equal("Portafolio de desarrollador", portfolio.TitleEs);
        Assert.True(portfolio.IsPubliclyVisible);
    }
}

[Collection(ApiCollection.Name)]
public sealed class AdminContentEndpointTests(PortfolioApiFactory factory)
{
    [Fact]
    public async Task Technologies_CreateUpdateDelete_AndRejectDuplicates()
    {
        var admin = await factory.CreateAdminClientAsync();
        var name = $"Tech {Guid.NewGuid():N}"[..20];

        var created = await admin.PostAsJsonAsync(
            "/api/admin/technologies", new SaveTechnologyRequest(name, "x", TechnologyCategory.Tool, 50), Json.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var technology = await created.Content.ReadFromJsonAsync<AdminTechnologyDto>(Json.Options);

        var duplicate = await admin.PostAsJsonAsync(
            "/api/admin/technologies", new SaveTechnologyRequest(name, null, TechnologyCategory.Tool, 0), Json.Options);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var updated = await admin.PutAsJsonAsync(
            $"/api/admin/technologies/{technology!.Id}",
            new SaveTechnologyRequest(name, null, TechnologyCategory.DevOps, 51),
            Json.Options);
        Assert.Equal(TechnologyCategory.DevOps, (await updated.Content.ReadFromJsonAsync<AdminTechnologyDto>(Json.Options))!.Category);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/technologies/{technology.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/technologies/{technology.Id}")).StatusCode);
    }

    [Fact]
    public async Task Experience_CreateUpdateDelete()
    {
        var admin = await factory.CreateAdminClientAsync();
        var request = new SaveExperienceRequest(
            "Acme",
            "https://acme.example",
            ExperienceType.Education,
            new DateOnly(2020, 9, 1),
            new DateOnly(2024, 6, 30),
            DisplayOrder: 9,
            [
                new ExperienceTranslationInput("en", "Student", "Madrid", "Studied."),
                new ExperienceTranslationInput("es", "Estudiante", "Madrid", "Estudié."),
            ]);

        var created = await admin.PostAsJsonAsync("/api/admin/experience", request, Json.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var experience = await created.Content.ReadFromJsonAsync<AdminExperienceDto>(Json.Options);

        var updated = await admin.PutAsJsonAsync(
            $"/api/admin/experience/{experience!.Id}", request with { Company = "Acme University" }, Json.Options);
        Assert.Equal("Acme University", (await updated.Content.ReadFromJsonAsync<AdminExperienceDto>(Json.Options))!.Company);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/experience/{experience.Id}")).StatusCode);
    }

    [Fact]
    public async Task Experience_RequiresBothLanguages_AndValidDates()
    {
        var admin = await factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/admin/experience",
            new SaveExperienceRequest(
                "Acme", null, ExperienceType.Work, new DateOnly(2024, 1, 1), new DateOnly(2023, 1, 1), 0,
                [new ExperienceTranslationInput("en", "Dev", null, null)]),
            Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Translations", problem!.Errors.Keys);
        Assert.Contains("EndDate", problem.Errors.Keys);
    }

    [Fact]
    public async Task Profile_SaveIsReflectedOnThePublicSite()
    {
        var admin = await factory.CreateAdminClientAsync();
        var original = await admin.GetFromJsonAsync<AdminProfileDto>("/api/admin/profile", Json.Options);

        try
        {
            var request = new SaveProfileRequest(
                "Ada Lovelace",
                "ada@example.com",
                "London",
                null,
                "/uploads/cv-en.pdf",
                null,
                [
                    new ProfileTranslationInput("en", "Engineer", "Bio en"),
                    new ProfileTranslationInput("es", "Ingeniera", "Bio es"),
                ],
                [new SocialLinkInput("GitHub", "https://github.com/ada")]);

            var saved = await admin.PutAsJsonAsync("/api/admin/profile", request, Json.Options);
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

            var publicProfile = await factory.CreateClient().GetFromJsonAsync<ProfileDto>("/api/profile?lang=es");
            Assert.Equal("Ada Lovelace", publicProfile!.FullName);
            Assert.Equal("Ingeniera", publicProfile.Headline);
            Assert.Equal("/uploads/cv-en.pdf", publicProfile.CvUrl);
            Assert.Equal("github", Assert.Single(publicProfile.SocialLinks).Platform);
        }
        finally
        {
            // Restore so other tests still see the seeded profile.
            await admin.PutAsJsonAsync(
                "/api/admin/profile",
                new SaveProfileRequest(
                    original!.FullName, original.Email, original.Location, original.AvatarUrl,
                    original.CvUrlEn, original.CvUrlEs, original.Translations, original.SocialLinks),
                Json.Options);
        }
    }

    [Fact]
    public async Task Profile_RejectsUnsafeLinks()
    {
        var admin = await factory.CreateAdminClientAsync();

        var response = await admin.PutAsJsonAsync(
            "/api/admin/profile",
            new SaveProfileRequest(
                "Name", null, null, "javascript:alert(1)", null, null,
                [new ProfileTranslationInput("en", "H", null), new ProfileTranslationInput("es", "H", null)],
                [new SocialLinkInput("web", "javascript:alert(1)")]),
            Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Messages_ListMarkReadAndDelete()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await factory.CreateClient().PostAsJsonAsync(
            "/api/contact", new SubmitContactMessageRequest("Ana", email, "Hi", "Hello there from the tests."));
        var admin = await factory.CreateAdminClientAsync();

        var unread = await admin.GetFromJsonAsync<List<AdminContactMessageDto>>("/api/admin/messages?unreadOnly=true", Json.Options);
        var message = Assert.Single(unread!, m => m.Email == email);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"/api/admin/messages/{message.Id}", new { isRead = true })).StatusCode);
        var stillUnread = await admin.GetFromJsonAsync<List<AdminContactMessageDto>>("/api/admin/messages?unreadOnly=true", Json.Options);
        Assert.DoesNotContain(stillUnread!, m => m.Id == message.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/messages/{message.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/messages/{message.Id}")).StatusCode);
    }
}

[Collection(ApiCollection.Name)]
public sealed class AdminUploadsEndpointTests(PortfolioApiFactory factory)
{
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    private static MultipartFormDataContent Form(byte[] bytes, string fileName, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    [Fact]
    public async Task UploadingAPng_StoresItAndServesIt()
    {
        var admin = await factory.CreateAdminClientAsync();

        var response = await admin.PostAsync("/api/admin/uploads", Form(TinyPng, "photo.png", "image/png"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var stored = await response.Content.ReadFromJsonAsync<StoredFile>();
        Assert.StartsWith("/uploads/", stored!.Url);
        Assert.EndsWith(".png", stored.Url);

        var served = await factory.CreateClient().GetAsync(stored.Url);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/png", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TinyPng, await served.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UploadingHtmlDisguisedAsAnImage_IsRejected()
    {
        var admin = await factory.CreateAdminClientAsync();
        var html = "<html><script>alert(1)</script></html>"u8.ToArray();

        var response = await admin.PostAsync("/api/admin/uploads", Form(html, "evil.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Uploading_RequiresAuthentication()
    {
        var response = await factory.CreateClient().PostAsync("/api/admin/uploads", Form(TinyPng, "a.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
