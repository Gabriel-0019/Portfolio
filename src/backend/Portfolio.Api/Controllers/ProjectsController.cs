using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Localization;
using Portfolio.Application.Projects;

namespace Portfolio.Api.Controllers;

/// <summary>Public, read-only access to published projects.</summary>
[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(IProjectQueries queries) : ControllerBase
{
    /// <summary>Lists publicly visible projects in the requested language.</summary>
    /// <param name="category">Optional category (Web, Mobile, Backend, Desktop, Data, Other).</param>
    /// <param name="tech">Optional technology name, e.g. "React".</param>
    /// <param name="featured">Optional featured flag.</param>
    /// <param name="lang">Content language: "en" (default) or "es". Falls back to Accept-Language.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProjectSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? tech,
        [FromQuery] bool? featured,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var projects = await queries.GetPublishedAsync(
            new ProjectFilter(category, tech, featured), HttpContext.GetLanguage(), cancellationToken);

        return Ok(projects);
    }

    /// <summary>Gets the case study of a publicly visible project.</summary>
    /// <param name="slug">Project slug, e.g. "portfolio".</param>
    /// <param name="lang">Content language: "en" (default) or "es". Falls back to Accept-Language.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("{slug}")]
    [ProducesResponseType<ProjectDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailDto>> GetBySlug(
        string slug,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var project = await queries.GetPublishedBySlugAsync(slug, HttpContext.GetLanguage(), cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }
}
