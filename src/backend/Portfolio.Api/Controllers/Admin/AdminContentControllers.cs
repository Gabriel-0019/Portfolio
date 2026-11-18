using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Admin.Content;
using Portfolio.Infrastructure.Storage;

namespace Portfolio.Api.Controllers.Admin;

[Route("api/admin/technologies")]
public sealed class AdminTechnologiesController(IAdminTechnologyService technologies) : AdminControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminTechnologyDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminTechnologyDto>>> List(CancellationToken cancellationToken) =>
        Ok(await technologies.ListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType<AdminTechnologyDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Create(SaveTechnologyRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var result = await technologies.CreateAsync(request, cancellationToken);
        return FromResult(result, technology => StatusCode(StatusCodes.Status201Created, technology));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminTechnologyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update(int id, SaveTechnologyRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        return FromResult(await technologies.UpdateAsync(id, request, cancellationToken), t => Ok(t));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await technologies.DeleteAsync(id, cancellationToken));
}

[Route("api/admin/experience")]
public sealed class AdminExperienceController(IAdminExperienceService experience) : AdminControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminExperienceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminExperienceDto>>> List(CancellationToken cancellationToken) =>
        Ok(await experience.ListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType<AdminExperienceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create(SaveExperienceRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        return StatusCode(StatusCodes.Status201Created, await experience.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminExperienceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(int id, SaveExperienceRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        return FromResult(await experience.UpdateAsync(id, request, cancellationToken), e => Ok(e));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await experience.DeleteAsync(id, cancellationToken));
}

[Route("api/admin/profile")]
public sealed class AdminProfileController(IAdminProfileService profile) : AdminControllerBase
{
    [HttpGet]
    [ProducesResponseType<AdminProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProfileDto>> Get(CancellationToken cancellationToken)
    {
        var current = await profile.GetAsync(cancellationToken);
        return current is null ? NotFound() : Ok(current);
    }

    [HttpPut]
    [ProducesResponseType<AdminProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Save(SaveProfileRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        return Ok(await profile.SaveAsync(request, cancellationToken));
    }
}

[Route("api/admin/messages")]
public sealed class AdminMessagesController(IAdminMessageService messages) : AdminControllerBase
{
    /// <summary>Contact messages, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminContactMessageDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminContactMessageDto>>> List(
        [FromQuery] bool unreadOnly, CancellationToken cancellationToken) =>
        Ok(await messages.ListAsync(unreadOnly, cancellationToken));

    /// <summary>Marks a message as read or unread.</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(
        int id, UpdateContactMessageRequest request, CancellationToken cancellationToken) =>
        FromResult(await messages.SetReadAsync(id, request.IsRead, cancellationToken));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await messages.DeleteAsync(id, cancellationToken));
}

[Route("api/admin/uploads")]
public sealed class AdminUploadsController(IFileStorage storage) : AdminControllerBase
{
    /// <summary>Multipart overhead allowed on top of the file size limit.</summary>
    private const long RequestLimit = 11 * 1024 * 1024;

    /// <summary>Uploads an image (PNG, JPEG, GIF, WebP) or a PDF. Returns its public URL.</summary>
    [HttpPost]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [ProducesResponseType<StoredFile>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Problem("Select a file to upload.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid file");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var stored = await storage.SaveAsync(stream, cancellationToken);
            if (stored is null)
            {
                return Problem(
                    "Only PNG, JPEG, GIF, WebP images or PDF documents are allowed.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid file");
            }

            return StatusCode(StatusCodes.Status201Created, stored);
        }
        catch (FileTooLargeException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status413PayloadTooLarge, title: "File too large");
        }
    }
}
