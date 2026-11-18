using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Portfolio.Api.Controllers;

public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);

public sealed record CurrentUserResponse(string Email);

/// <summary>Cookie-based sign-in for the admin panel.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(SignInManager<IdentityUser> signInManager) : ControllerBase
{
    public const string RateLimitPolicy = "login";
    private const string InvalidCredentials = "Invalid email or password.";

    /// <summary>Signs in and sets the HttpOnly auth cookie.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicy)]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Problem(InvalidCredentials, statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        }

        var user = await signInManager.UserManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Problem(InvalidCredentials, statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        }

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: request.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Problem(
                "Too many failed attempts. The account is locked for a few minutes.",
                statusCode: StatusCodes.Status423Locked,
                title: "Locked");
        }

        if (!result.Succeeded)
        {
            return Problem(InvalidCredentials, statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        }

        return Ok(new CurrentUserResponse(user.Email!));
    }

    /// <summary>Clears the auth cookie.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    /// <summary>Returns the signed-in admin, or 401.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var user = await signInManager.UserManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(new CurrentUserResponse(user.Email!));
    }
}
