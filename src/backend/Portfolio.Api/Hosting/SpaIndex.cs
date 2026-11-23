using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Portfolio.Api.Hosting;

public sealed class SpaOptions
{
    public const string SectionName = "Spa";

    /// <summary>Folder with the built frontend (index.html + assets). Empty = &lt;content root&gt;/wwwroot.</summary>
    public string RootPath { get; set; } = string.Empty;
}

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    /// <summary>Public origin used for canonical/Open Graph URLs (e.g. https://me.dev). Empty = request origin.</summary>
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// The built index.html, split around the <c>&lt;!--app-head--&gt;</c> placeholder, plus the
/// CSP hashes of its inline scripts (the theme bootstrap) so the page can run without 'unsafe-inline'.
/// </summary>
public sealed partial class SpaIndex
{
    public const string HeadPlaceholder = "<!--app-head-->";

    private readonly string _html;

    private SpaIndex(string html, IReadOnlyList<string> scriptHashes)
    {
        _html = html;
        ScriptHashes = scriptHashes;
    }

    /// <summary>CSP source expressions like <c>'sha256-…'</c>.</summary>
    public IReadOnlyList<string> ScriptHashes { get; }

    /// <summary>Loads index.html from <paramref name="rootPath"/>, or returns null when the frontend is not built.</summary>
    public static SpaIndex? TryLoad(string rootPath)
    {
        var path = Path.Combine(rootPath, "index.html");
        if (!File.Exists(path))
        {
            return null;
        }

        var html = File.ReadAllText(path);
        if (!html.Contains(HeadPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"'{path}' does not contain the {HeadPlaceholder} placeholder.");
        }

        var hashes = InlineScript()
            .Matches(html)
            .Select(m => $"'sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(m.Groups["body"].Value)))}'")
            .Distinct()
            .ToList();

        return new SpaIndex(html, hashes);
    }

    /// <summary>Returns the page with the head tags inserted and the &lt;html lang&gt; set.</summary>
    public string Render(string headTags, string language)
    {
        var html = _html.Replace(HeadPlaceholder, headTags, StringComparison.Ordinal);
        // The template's default <title> is replaced by the per-page one.
        html = DefaultTitle().Replace(html, string.Empty, 1);
        return HtmlLang().Replace(html, $"<html lang=\"{language}\"", 1);
    }

    [GeneratedRegex(@"<script(?![^>]*\bsrc=)[^>]*>(?<body>[\s\S]*?)</script>", RegexOptions.IgnoreCase)]
    private static partial Regex InlineScript();

    [GeneratedRegex(@"<title>[^<]*</title>\s*", RegexOptions.IgnoreCase)]
    private static partial Regex DefaultTitle();

    [GeneratedRegex(@"<html lang=""[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlLang();
}
