using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Localization;
using Portfolio.Application.Experiences;
using Portfolio.Application.Profiles;
using Portfolio.Application.Technologies;

namespace Portfolio.Api.Controllers;

/// <summary>Public profile, experience and technology catalog.</summary>
[ApiController]
[Route("api")]
public sealed class ContentController : ControllerBase
{
    /// <summary>Gets the portfolio owner's profile in the requested language.</summary>
    /// <param name="lang">Content language: "en" (default) or "es". Falls back to Accept-Language.</param>
    [HttpGet("profile")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> GetProfile(
        [FromServices] IProfileQueries queries,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var profile = await queries.GetAsync(HttpContext.GetLanguage(), cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Lists work, education and certification entries, most recent first.</summary>
    /// <param name="lang">Content language: "en" (default) or "es". Falls back to Accept-Language.</param>
    [HttpGet("experience")]
    [ProducesResponseType<IReadOnlyList<ExperienceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExperienceDto>>> GetExperience(
        [FromServices] IExperienceQueries queries,
        [FromQuery] string? lang,
        CancellationToken cancellationToken) =>
        Ok(await queries.GetAllAsync(HttpContext.GetLanguage(), cancellationToken));

    /// <summary>Lists the technology catalog used to tag projects.</summary>
    [HttpGet("technologies")]
    [ProducesResponseType<IReadOnlyList<TechnologyDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TechnologyDto>>> GetTechnologies(
        [FromServices] ITechnologyQueries queries,
        CancellationToken cancellationToken) =>
        Ok(await queries.GetAllAsync(cancellationToken));
}
