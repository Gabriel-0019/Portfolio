using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Portfolio.Api.Hosting;
using Portfolio.IntegrationTests.Infrastructure;

namespace Portfolio.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class HostingEndpointTests(PortfolioApiFactory factory)
{
    private HttpClient Client(string? acceptLanguage = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (acceptLanguage is not null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(acceptLanguage));
        }

        return client;
    }

    [Fact]
    public async Task HomePage_RendersMetadata_LanguageAndCsp()
    {
        var response = await Client().GetAsync("/en");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<html lang=\"en\"", html);
        Assert.Contains("<title data-ssr>Software Developer · .NET &amp; React · Your Name</title>", html);
        Assert.DoesNotContain("<title>Portfolio</title>", html);
        Assert.Contains("<link data-ssr rel=\"canonical\" href=\"http://localhost/en\">", html);
        Assert.Contains("hreflang=\"es\" href=\"http://localhost/es\"", html);
        Assert.Equal("no-cache", response.Headers.CacheControl?.ToString());

        var expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(PortfolioApiFactory.SpaInlineScript)));
        var csp = string.Join(';', response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains($"script-src 'self' 'sha256-{expectedHash}'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.DoesNotContain("unsafe-inline", csp.Split(';').Single(d => d.Trim().StartsWith("script-src")));
    }

    [Fact]
    public async Task ProjectPage_RendersOpenGraphInItsLanguage()
    {
        var html = await Client().GetStringAsync("/es/projects/portfolio");

        Assert.Contains("<html lang=\"es\"", html);
        Assert.Contains("<meta data-ssr property=\"og:title\" content=\"Portafolio de desarrollador · Your Name\">", html);
        Assert.Contains("<meta data-ssr property=\"og:type\" content=\"article\">", html);
        Assert.Contains("<meta data-ssr property=\"og:locale\" content=\"es_ES\">", html);
        Assert.Contains("property=\"og:description\" content=\"Portafolio bilingüe", html);
        Assert.Contains("hreflang=\"en\" href=\"http://localhost/en/projects/portfolio\"", html);
    }

    [Theory]
    [InlineData("/en/projects/does-not-exist")]
    [InlineData("/es/no-such-page")]
    public async Task UnknownPages_Return404WithNoIndex(string path)
    {
        var response = await Client().GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("<meta data-ssr name=\"robots\" content=\"noindex\">", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/", "es-MX", "/es")]
    [InlineData("/", "de-DE", "/en")]
    [InlineData("/projects?tech=React", "en", "/en/projects?tech=React")]
    [InlineData("/about", "es", "/es/about")]
    public async Task PathsWithoutLanguage_RedirectToTheVisitorsLanguage(string path, string acceptLanguage, string expected)
    {
        var response = await Client(acceptLanguage).GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AdminPages_GetTheShellWithNoIndex()
    {
        var response = await Client().GetAsync("/admin/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("content=\"noindex\"", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/does-not-exist")]
    [InlineData("/missing-file.js")]
    [InlineData("/uploads/missing.png")]
    public async Task ApiRoutesAndFiles_AreNeverAnsweredWithHtml(string path)
    {
        var response = await Client().GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task HashedAssets_AreCachedForever()
    {
        var response = await Client().GetAsync("/assets/app.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RobotsTxt_BlocksAdminAndPointsToSitemap()
    {
        var robots = await Client().GetStringAsync("/robots.txt");

        Assert.Contains("Disallow: /admin", robots);
        Assert.Contains("Sitemap: http://localhost/sitemap.xml", robots);
    }

    [Fact]
    public async Task Sitemap_ListsEveryPageInBothLanguages()
    {
        var response = await Client().GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();

        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        foreach (var url in new[] { "/en", "/es", "/en/about", "/es/contact", "/en/projects/portfolio", "/es/projects/portfolio" })
        {
            Assert.Contains($"<loc>http://localhost{url}</loc>", xml);
        }

        Assert.Contains("<xhtml:link rel=\"alternate\" hreflang=\"es\" href=\"http://localhost/es/projects/portfolio\"/>", xml);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthChecks_ReportHealthy(string path)
    {
        var response = await Client().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EveryResponse_HasSecurityHeaders()
    {
        var response = await Client().GetAsync("/api/status");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task Pages_EmbedTheirInitialData()
    {
        var home = await Client().GetStringAsync("/es");
        var detail = await Client().GetStringAsync("/en/projects/portfolio");

        Assert.Contains("<script data-ssr id=\"initial-data\" type=\"application/json\">{\"language\":\"es\"", home);
        Assert.Contains("\"fullName\":\"Your Name\"", home);
        Assert.Contains("\"featuredProjects\":[{\"slug\":\"portfolio\"", home);
        Assert.Contains("\"project\":{\"slug\":\"portfolio\",\"title\":\"Developer Portfolio\"", detail);
    }

    [Fact]
    public void RenderInitialData_CannotBreakOutOfTheScriptElement()
    {
        var html = PageMetadataResolver.RenderInitialData(
            new Dictionary<string, object?> { ["x"] = "</script><script>alert(1)</script>" }, "en");

        Assert.Equal(1, html.Split("</script>").Length - 1);
        Assert.Contains("\\u003C/script\\u003E", html);
    }

    [Fact]
    public void RenderHead_EncodesUserContent()
    {
        var head = PageMetadataResolver.RenderHead(
            new PageMetadata("<script>alert(1)</script>", "\"><img src=x>", "/en", "en", ImageUrl: "/uploads/a.png"),
            "https://me.example");

        Assert.DoesNotContain("<script>", head);
        Assert.DoesNotContain("\"><img", head);
        Assert.Contains("&lt;script&gt;", head);
        Assert.Contains("content=\"https://me.example/uploads/a.png\"", head);
    }
}
