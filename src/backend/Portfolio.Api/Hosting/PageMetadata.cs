using System.Text;
using System.Text.Json;
using Portfolio.Application.Profiles;
using Portfolio.Application.Projects;
using Portfolio.Domain.Common;

namespace Portfolio.Api.Hosting;

/// <summary>Metadata for one public page. Rendered into the HTML so crawlers and link previews see it.</summary>
public sealed record PageMetadata(
    string Title,
    string? Description,
    string Path,
    string Language,
    int StatusCode = StatusCodes.Status200OK,
    string? ImageUrl = null,
    string OgType = "website",
    bool NoIndex = false);

public sealed record PageResult(PageMetadata Meta, IReadOnlyDictionary<string, object?> Data);

/// <summary>Resolves the metadata for a public route like <c>/es/projects/portfolio</c>.</summary>
public sealed class PageMetadataResolver(IProfileQueries profiles, IProjectQueries projects)
{
    private static readonly Dictionary<string, Dictionary<string, string>> Text = new()
    {
        ["en"] = new()
        {
            ["site"] = "Portfolio",
            ["projects"] = "Projects",
            ["projectsDescription"] = "Software projects and case studies by {0}.",
            ["about"] = "About me",
            ["aboutDescription"] = "Experience, skills and background of {0}.",
            ["contact"] = "Contact",
            ["contactDescription"] = "Get in touch with {0}.",
            ["notFound"] = "Page not found",
        },
        ["es"] = new()
        {
            ["site"] = "Portafolio",
            ["projects"] = "Proyectos",
            ["projectsDescription"] = "Proyectos de software y casos de estudio de {0}.",
            ["about"] = "Sobre mí",
            ["aboutDescription"] = "Experiencia, habilidades y trayectoria de {0}.",
            ["contact"] = "Contacto",
            ["contactDescription"] = "Ponte en contacto con {0}.",
            ["notFound"] = "Página no encontrada",
        },
    };

    /// <summary>
    /// Resolves the page metadata and the API data the page needs first. That data is embedded in the HTML
    /// so the first render does not wait for API calls (faster LCP, no layout shift).
    /// Keys match the client's React Query keys (see src/app/queryClient.js).
    /// </summary>
    public async Task<PageResult> ResolveAsync(string language, string[] segments, CancellationToken cancellationToken)
    {
        var text = Text[language];
        var profile = await profiles.GetAsync(language, cancellationToken);
        var site = profile?.FullName ?? text["site"];
        var path = "/" + string.Join('/', [language, .. segments]);
        var data = new Dictionary<string, object?> { ["profile"] = profile };

        PageResult Page(string? title, string? description, string? image = null) =>
            new(new(title is null ? site : $"{title} · {site}", description, path, language, ImageUrl: image ?? profile?.AvatarUrl), data);

        PageResult NotFound() =>
            new(new($"{text["notFound"]} · {site}", null, path, language, StatusCodes.Status404NotFound, NoIndex: true), data);

        switch (segments)
        {
            case []:
                data["featuredProjects"] = await projects.GetPublishedAsync(new ProjectFilter(Featured: true), language, cancellationToken);
                return Page(
                    profile is null ? null : profile.Headline,
                    profile is null ? null : $"{profile.FullName} — {profile.Headline}. {FirstSentence(profile.Bio)}".Trim());

            case ["projects"]:
                data["projects"] = await projects.GetPublishedAsync(new ProjectFilter(), language, cancellationToken);
                return Page(text["projects"], string.Format(text["projectsDescription"], site));

            case ["projects", var slug]:
                var project = await projects.GetPublishedBySlugAsync(slug, language, cancellationToken);
                if (project is null)
                {
                    return NotFound();
                }

                data["project"] = project;
                var page = Page(project.Title, project.Summary, project.CoverImageUrl);
                return page with { Meta = page.Meta with { OgType = "article" } };

            case ["about"]:
                return Page(text["about"], string.Format(text["aboutDescription"], site));

            case ["contact"]:
                return Page(text["contact"], string.Format(text["contactDescription"], site));

            default:
                return NotFound();
        }
    }

    /// <summary>
    /// JSON island with the initial data. type="application/json" is never executed, so the CSP is unaffected;
    /// System.Text.Json escapes &lt; and &gt;, so the content cannot close the script element.
    /// </summary>
    public static string RenderInitialData(IReadOnlyDictionary<string, object?> data, string language) =>
        $"<script data-ssr id=\"initial-data\" type=\"application/json\">{JsonSerializer.Serialize(new { language, data }, InitialDataJson)}</script>";

    private static readonly JsonSerializerOptions InitialDataJson = new(JsonSerializerDefaults.Web);

    /// <summary>Head tags: title, description, canonical, hreflang alternates, Open Graph and Twitter.</summary>
    public static string RenderHead(PageMetadata page, string baseUrl)
    {
        static string E(string value) => HtmlAttribute(value);
        string Absolute(string url) => url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : baseUrl + url;

        var pathWithoutLanguage = page.Path[(page.Language.Length + 1)..];
        var canonical = Absolute(page.Path);
        var sb = new StringBuilder();

        // data-ssr: the client removes these on startup and renders its own (React 19 metadata).
        sb.Append($"<title data-ssr>{E(page.Title)}</title>");
        if (page.Description is { Length: > 0 } description)
        {
            sb.Append($"<meta data-ssr name=\"description\" content=\"{E(description)}\">");
            sb.Append($"<meta data-ssr property=\"og:description\" content=\"{E(description)}\">");
        }

        if (page.NoIndex)
        {
            sb.Append("<meta data-ssr name=\"robots\" content=\"noindex\">");
        }
        else
        {
            sb.Append($"<link data-ssr rel=\"canonical\" href=\"{E(canonical)}\">");
            foreach (var language in Languages.Supported)
            {
                sb.Append($"<link data-ssr rel=\"alternate\" hreflang=\"{language}\" href=\"{E(Absolute($"/{language}{pathWithoutLanguage}"))}\">");
            }

            sb.Append($"<link data-ssr rel=\"alternate\" hreflang=\"x-default\" href=\"{E(Absolute($"/{Languages.Default}{pathWithoutLanguage}"))}\">");
        }

        sb.Append($"<meta data-ssr property=\"og:type\" content=\"{page.OgType}\">");
        sb.Append($"<meta data-ssr property=\"og:title\" content=\"{E(page.Title)}\">");
        sb.Append($"<meta data-ssr property=\"og:url\" content=\"{E(canonical)}\">");
        sb.Append($"<meta data-ssr property=\"og:locale\" content=\"{(page.Language == Languages.Spanish ? "es_ES" : "en_US")}\">");
        if (page.ImageUrl is { Length: > 0 } image)
        {
            sb.Append($"<meta data-ssr property=\"og:image\" content=\"{E(Absolute(image))}\">");
            sb.Append("<meta data-ssr name=\"twitter:card\" content=\"summary_large_image\">");
        }
        else
        {
            sb.Append("<meta data-ssr name=\"twitter:card\" content=\"summary\">");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes the characters that matter in HTML text and quoted attributes. Unlike WebUtility.HtmlEncode,
    /// accented letters are kept as-is (the page is UTF-8), so titles stay readable in the source.
    /// </summary>
    public static string HtmlAttribute(string value)
    {
        var sb = new StringBuilder(value.Length + 16);
        foreach (var c in value)
        {
            sb.Append(c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&#39;",
                _ => c.ToString(),
            });
        }

        return sb.ToString();
    }

    private static string FirstSentence(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var plain = markdown.Replace("#", string.Empty).Replace("*", string.Empty).Trim();
        var end = plain.IndexOfAny(['.', '\n']);
        var sentence = end > 0 ? plain[..(end + 1)] : plain;
        return sentence.Length > 160 ? sentence[..157] + "…" : sentence;
    }
}
