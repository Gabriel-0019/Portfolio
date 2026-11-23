using System.Net;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Portfolio.Api.Localization;
using Portfolio.Application.Projects;
using Portfolio.Domain.Common;

namespace Portfolio.Api.Hosting;

/// <summary>
/// Serves the built React app from the API (single deployable): hashed assets with long caching,
/// and every page as index.html with server-rendered SEO/Open Graph tags, a CSP and the right status code.
/// Also serves robots.txt and sitemap.xml. Does nothing when the frontend has not been built.
/// </summary>
public static class SpaHosting
{
    private static readonly string[] ReservedPrefixes = ["api", "uploads", "openapi", "scalar", "health"];

    public static IServiceCollection AddSpaHosting(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<SpaOptions>()
            .Bind(configuration.GetSection(SpaOptions.SectionName))
            .PostConfigure(options =>
            {
                var root = string.IsNullOrWhiteSpace(options.RootPath) ? "wwwroot" : options.RootPath;
                options.RootPath = Path.GetFullPath(root, environment.ContentRootPath);
            });
        services.AddOptions<SiteOptions>().Bind(configuration.GetSection(SiteOptions.SectionName));
        services.AddSingleton(provider => new SpaIndexHolder(
            SpaIndex.TryLoad(provider.GetRequiredService<IOptions<SpaOptions>>().Value.RootPath)));
        services.AddScoped<PageMetadataResolver>();
        return services;
    }

    /// <summary>Static assets of the built frontend. Call before routing.</summary>
    public static WebApplication UseSpaStaticFiles(this WebApplication app)
    {
        var root = app.Services.GetRequiredService<IOptions<SpaOptions>>().Value.RootPath;
        if (!Directory.Exists(root))
        {
            return app;
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            OnPrepareResponse = context =>
            {
                var headers = context.Context.Response.Headers;
                // Vite puts content-hashed files under /assets: safe to cache forever.
                headers.CacheControl = context.Context.Request.Path.StartsWithSegments("/assets")
                    ? "public, max-age=31536000, immutable"
                    : "public, max-age=3600";
            },
        });

        return app;
    }

    /// <summary>robots.txt, sitemap.xml and the HTML fallback. Call after mapping the API.</summary>
    public static WebApplication MapSpa(this WebApplication app)
    {
        app.MapGet("/robots.txt", (HttpContext context, IOptions<SiteOptions> site) =>
            Results.Text(
                $"User-agent: *\nAllow: /\nDisallow: /admin\nDisallow: /api/\n\nSitemap: {BaseUrl(context, site.Value)}/sitemap.xml\n",
                "text/plain; charset=utf-8"))
            .ExcludeFromDescription();

        app.MapGet("/sitemap.xml", SitemapAsync).ExcludeFromDescription();

        app.MapFallback(PageAsync).ExcludeFromDescription();
        return app;
    }

    private static async Task<IResult> SitemapAsync(
        HttpContext context, IOptions<SiteOptions> site, IProjectQueries projects, CancellationToken cancellationToken)
    {
        var baseUrl = BaseUrl(context, site.Value);
        var refs = await projects.GetPublishedRefsAsync(cancellationToken);

        var pages = new List<(string Path, DateTimeOffset? LastModified)>
        {
            ("", null), ("/projects", refs.Count > 0 ? refs.Max(r => r.UpdatedAt) : null), ("/about", null), ("/contact", null),
        };
        pages.AddRange(refs.Select(r => ($"/projects/{r.Slug}", (DateTimeOffset?)r.UpdatedAt)));

        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        xml.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">\n");
        foreach (var (path, lastModified) in pages)
        {
            foreach (var language in Languages.Supported)
            {
                xml.Append("  <url>\n");
                xml.Append($"    <loc>{WebUtility.HtmlEncode($"{baseUrl}/{language}{path}")}</loc>\n");
                if (lastModified is { } modified)
                {
                    xml.Append($"    <lastmod>{modified.UtcDateTime:yyyy-MM-dd}</lastmod>\n");
                }

                foreach (var alternate in Languages.Supported)
                {
                    xml.Append($"    <xhtml:link rel=\"alternate\" hreflang=\"{alternate}\" href=\"{WebUtility.HtmlEncode($"{baseUrl}/{alternate}{path}")}\"/>\n");
                }

                xml.Append("  </url>\n");
            }
        }

        xml.Append("</urlset>\n");
        return Results.Text(xml.ToString(), "application/xml; charset=utf-8");
    }

    private static async Task<IResult> PageAsync(
        HttpContext context,
        SpaIndexHolder index,
        IOptions<SiteOptions> site,
        PageMetadataResolver resolver,
        CancellationToken cancellationToken)
    {
        var request = context.Request;
        var segments = request.Path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Unknown API routes, missing files and non-GET requests are plain 404s, never the HTML shell.
        if (index.Value is not { } spa
            || !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)
            || segments is [var first, ..] && ReservedPrefixes.Contains(first, StringComparer.OrdinalIgnoreCase)
            || segments is [.., var last] && last.Contains('.'))
        {
            return Results.NotFound();
        }

        // Private admin panel: shell only, never indexed.
        if (segments is ["admin", ..])
        {
            var admin = new PageMetadata("Admin", null, "/es", Languages.Spanish, NoIndex: true);
            return Html(context, spa, PageMetadataResolver.RenderHead(admin, BaseUrl(context, site.Value)), admin);
        }

        // No language prefix: redirect to the visitor's language (?lang=, then Accept-Language).
        if (segments is [] || !Languages.Supported.Contains(segments[0]))
        {
            var target = $"/{context.GetLanguage()}{(segments is [] ? string.Empty : request.Path.Value)}{request.QueryString}";
            context.Response.Headers.Vary = HeaderNames.AcceptLanguage;
            return Results.Redirect(target);
        }

        var result = await resolver.ResolveAsync(segments[0], segments[1..], cancellationToken);
        var head = PageMetadataResolver.RenderHead(result.Meta, BaseUrl(context, site.Value))
            + PageMetadataResolver.RenderInitialData(result.Data, result.Meta.Language);
        return Html(context, spa, head, result.Meta);
    }

    private static IResult Html(HttpContext context, SpaIndex spa, string head, PageMetadata page)
    {
        var headers = context.Response.Headers;
        headers.CacheControl = "no-cache";
        headers.ContentSecurityPolicy = ContentSecurityPolicy(spa);
        return Results.Content(spa.Render(head, page.Language), "text/html; charset=utf-8", statusCode: page.StatusCode);
    }

    private static string ContentSecurityPolicy(SpaIndex spa) => string.Join("; ",
        "default-src 'self'",
        $"script-src 'self' {string.Join(' ', spa.ScriptHashes)}".TrimEnd(),
        "style-src 'self'",
        "img-src 'self' https: data:",
        "font-src 'self'",
        "connect-src 'self'",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        "frame-ancestors 'none'");

    private static string BaseUrl(HttpContext context, SiteOptions site) =>
        string.IsNullOrWhiteSpace(site.BaseUrl)
            ? $"{context.Request.Scheme}://{context.Request.Host}"
            : site.BaseUrl.TrimEnd('/');
}

/// <summary>Holds the loaded index.html (null when the frontend is not built, e.g. during development).</summary>
public sealed record SpaIndexHolder(SpaIndex? Value);
