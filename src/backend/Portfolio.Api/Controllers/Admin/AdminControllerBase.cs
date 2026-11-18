using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Common;

namespace Portfolio.Api.Controllers.Admin;

/// <summary>Base for admin endpoints: requires the auth cookie and maps validation/results to HTTP.</summary>
[ApiController]
[Authorize]
public abstract class AdminControllerBase : ControllerBase
{
    /// <summary>Returns a 400 ValidationProblem when invalid, otherwise null.</summary>
    protected async Task<ActionResult?> ValidateAsync<T>(T request, CancellationToken cancellationToken)
    {
        var validator = HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.IsValid)
        {
            return null;
        }

        foreach (var error in validation.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ValidationProblem(ModelState);
    }

    protected ActionResult FromResult(Result result) => result.Status switch
    {
        ResultStatus.Ok => NoContent(),
        _ => Failure(result.Status, result.Error),
    };

    protected ActionResult FromResult<T>(Result<T> result, Func<T, ActionResult> onSuccess) => result.Status switch
    {
        ResultStatus.Ok => onSuccess(result.Value!),
        _ => Failure(result.Status, result.Error),
    };

    private ActionResult Failure(ResultStatus status, string? error) => status switch
    {
        ResultStatus.NotFound => NotFound(),
        ResultStatus.Conflict => Problem(error, statusCode: StatusCodes.Status409Conflict, title: "Conflict"),
        ResultStatus.Invalid => Problem(error, statusCode: StatusCodes.Status400BadRequest, title: "Invalid request"),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
