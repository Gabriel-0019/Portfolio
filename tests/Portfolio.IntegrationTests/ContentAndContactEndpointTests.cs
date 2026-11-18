using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Contact;
using Portfolio.Application.Experiences;
using Portfolio.Application.Profiles;
using Portfolio.Application.Technologies;
using Portfolio.IntegrationTests.Infrastructure;

namespace Portfolio.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ContentEndpointTests(PortfolioApiFactory factory)
{
    [Theory]
    [InlineData("en", "Software Developer · .NET & React")]
    [InlineData("es", "Desarrollador de software · .NET y React")]
    public async Task GetProfile_ReturnsHeadlineInRequestedLanguage(string lang, string expectedHeadline)
    {
        var client = factory.CreateClient();

        var profile = await client.GetFromJsonAsync<ProfileDto>($"/api/profile?lang={lang}");

        Assert.Equal(expectedHeadline, profile!.Headline);
        Assert.NotEmpty(profile.SocialLinks);
    }

    [Fact]
    public async Task GetExperience_ReturnsTranslatedEntries()
    {
        var client = factory.CreateClient();

        var experience = await client.GetFromJsonAsync<List<ExperienceDto>>("/api/experience?lang=es");

        var entry = Assert.Single(experience!);
        Assert.Equal("Desarrollador de software", entry.Role);
        Assert.True(entry.IsCurrent);
    }

    [Fact]
    public async Task GetTechnologies_ReturnsCatalogInDisplayOrder()
    {
        var client = factory.CreateClient();

        var technologies = await client.GetFromJsonAsync<List<TechnologyDto>>("/api/technologies");

        Assert.Equal(11, technologies!.Count);
        Assert.Equal("C#", technologies[0].Name);
    }
}

[Collection(ApiCollection.Name)]
public sealed class ContactEndpointTests(PortfolioApiFactory factory)
{
    [Fact]
    public async Task PostContact_WithValidMessage_StoresItAndReturns202()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync(
            "/api/contact?lang=es",
            new SubmitContactMessageRequest("Ana", email, "Hola", "Me interesa tu trabajo, hablemos."));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await factory.WithDbAsync(async db =>
        {
            var stored = await db.ContactMessages.SingleAsync(m => m.Email == email);
            Assert.Equal("es", stored.Language);
            Assert.False(stored.IsRead);
        });
    }

    [Fact]
    public async Task PostContact_WithInvalidData_ReturnsValidationProblem()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/contact",
            new SubmitContactMessageRequest("", "not-an-email", null, "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Name", problem!.Errors.Keys);
        Assert.Contains("Email", problem.Errors.Keys);
        Assert.Contains("Message", problem.Errors.Keys);
    }

    [Fact]
    public async Task PostContact_WithHoneypotFilled_ReturnsAcceptedButDoesNotStore()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync(
            "/api/contact",
            new SubmitContactMessageRequest("Bot", email, null, "Buy cheap stuff now!!!", Website: "http://spam.example"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await factory.WithDbAsync(async db =>
            Assert.False(await db.ContactMessages.AnyAsync(m => m.Email == email)));
    }
}
