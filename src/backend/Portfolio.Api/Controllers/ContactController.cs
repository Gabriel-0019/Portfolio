using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Api.Localization;
using Portfolio.Application.Contact;

namespace Portfolio.Api.Controllers;

/// <summary>Public contact form.</summary>
[ApiController]
[Route("api/contact")]
public sealed class ContactController(
    IValidator<SubmitContactMessageRequest> validator,
    IContactService contactService) : ControllerBase
{
    public const string RateLimitPolicy = "contact";

    /// <summary>Sends a message to the portfolio owner.</summary>
    /// <param name="request">Name, email, optional subject and message.</param>
    /// <param name="lang">Language of the site the message was sent from.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Submit(
        SubmitContactMessageRequest request,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        await contactService.SubmitAsync(request, HttpContext.GetLanguage(), cancellationToken);
        return Accepted();
    }
}
