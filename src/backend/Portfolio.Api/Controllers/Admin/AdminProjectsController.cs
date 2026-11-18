using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Admin.Projects;

namespace Portfolio.Api.Controllers.Admin;

/// <summary>Create, edit and delete projects (any status).</summary>
[Route("api/admin/projects")]
public sealed class AdminProjectsController(IAdminProjectService projects) : AdminControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminProjectListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminProjectListItemDto>>> List(CancellationToken cancellationToken) =>
        Ok(await projects.ListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProjectDto>> Get(int id, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(id, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    [ProducesResponseType<AdminProjectDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Create(SaveProjectRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var result = await projects.CreateAsync(request, cancellationToken);
        return FromResult(result, project => CreatedAtAction(nameof(Get), new { id = project.Id }, project));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update(int id, SaveProjectRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var result = await projects.UpdateAsync(id, request, cancellationToken);
        return FromResult(result, project => Ok(project));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await projects.DeleteAsync(id, cancellationToken));
}
