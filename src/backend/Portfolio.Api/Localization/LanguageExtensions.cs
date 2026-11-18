using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Portfolio.Domain.Common;

namespace Portfolio.Api.Localization;

public static class LanguageExtensions
{
    /// <summary>Query string key used to pick the content language, e.g. <c>?lang=es</c>.</summary>
    public const string QueryStringKey = "lang";

    /// <summary>
    /// Resolves the request language from <c>?lang=</c>, then <c>Accept-Language</c>,
    /// then the default language.
    /// </summary>
    public static IServiceCollection AddPortfolioLocalization(this IServiceCollection services)
    {
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = Languages.Supported.Select(code => new CultureInfo(code)).ToList();

            options.DefaultRequestCulture = new RequestCulture(Languages.Default);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
            options.ApplyCurrentCultureToResponseHeaders = true;
            options.RequestCultureProviders =
            [
                new QueryStringRequestCultureProvider
                {
                    QueryStringKey = QueryStringKey,
                    UIQueryStringKey = QueryStringKey,
                },
                new AcceptLanguageHeaderRequestCultureProvider(),
            ];
        });

        return services;
    }

    /// <summary>Two-letter code of the resolved content language ("en" or "es").</summary>
    public static string GetLanguage(this HttpContext context)
    {
        var culture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture;
        return Languages.Normalize(culture?.TwoLetterISOLanguageName);
    }
}
