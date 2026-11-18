using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Portfolio.Api.Controllers;
using Portfolio.Api.Localization;
using Portfolio.Application;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Identity;
using Portfolio.Infrastructure.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddPortfolioLocalization();

// ---------- Admin authentication (ASP.NET Core Identity + HttpOnly cookie) ----------
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.AddAdminIdentity().AddSignInManager();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "portfolio.auth";
    options.Cookie.HttpOnly = true;
    // Strict: the cookie is never sent on cross-site requests, which blocks CSRF against the admin API.
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // This is an API: answer 401/403 instead of redirecting to a login page.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();

// ---------- Uploaded files ----------
builder.Services
    .AddOptions<FileStorageOptions>()
    .Bind(builder.Configuration.GetSection(FileStorageOptions.SectionName))
    .PostConfigure(options =>
    {
        var root = string.IsNullOrWhiteSpace(options.RootPath) ? Path.Combine("App_Data", "uploads") : options.RootPath;
        options.RootPath = Path.GetFullPath(root, builder.Environment.ContentRootPath);
    });

// ---------- Rate limiting ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(ContactController.RateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:Contact:PermitLimit", 5),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:Contact:WindowMinutes", 10)),
                QueueLimit = 0,
            }));

    options.AddPolicy(AuthController.RateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:Login:PermitLimit", 10),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:Login:WindowMinutes", 5)),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
    await AdminAccountSeeder.EnsureAdminAsync(app.Services);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHttpsRedirection();
}

var storage = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
Directory.CreateDirectory(storage.RootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storage.RootPath),
    RequestPath = storage.RequestPath,
    OnPrepareResponse = context =>
    {
        // Uploaded names are random and never reused, so they can be cached for a long time.
        context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        context.Context.Response.Headers.XContentTypeOptions = "nosniff";
    },
});

app.UseRequestLocalization();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes Program to WebApplicationFactory in integration tests.
public partial class Program;
