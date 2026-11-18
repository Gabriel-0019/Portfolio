using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Portfolio.Infrastructure.Identity;

/// <summary>
/// Creates the single admin account from configuration (<c>Admin:Email</c>, <c>Admin:Password</c>)
/// when it does not exist yet. An existing account is never modified.
/// </summary>
public static class AdminAccountSeeder
{
    public static async Task EnsureAdminAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var configuration = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminAccountSeeder));
        var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!userManager.Users.Any())
            {
                logger.LogWarning(
                    "No admin account exists. Set 'Admin:Email' and 'Admin:Password' (e.g. with user secrets) to create one.");
            }

            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create the admin account: {errors}");
        }

        logger.LogInformation("Admin account {Email} created.", email);
    }
}
