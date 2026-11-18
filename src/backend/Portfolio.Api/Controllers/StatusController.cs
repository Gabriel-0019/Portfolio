using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Api.Controllers;

/// <summary>
/// Lightweight health/status endpoint used by the frontend to confirm the API is reachable.
/// </summary>
[ApiController]
[Route("api/status")]
public sealed class StatusController(IHostEnvironment environment, TimeProvider timeProvider) : ControllerBase
{
    private static readonly string Version =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    [HttpGet]
    [ProducesResponseType<StatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<StatusResponse> Get() =>
        Ok(new StatusResponse(
            Status: "ok",
            Service: "Portfolio.Api",
            Version: Version,
            Environment: environment.EnvironmentName,
            ServerTimeUtc: timeProvider.GetUtcNow()));
}

public sealed record StatusResponse(
    string Status,
    string Service,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc);
