using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Admin.Content;
using Portfolio.Application.Admin.Projects;
using Portfolio.Application.Contact;
using Portfolio.Application.Experiences;
using Portfolio.Application.Profiles;
using Portfolio.Application.Projects;
using Portfolio.Application.Technologies;
using Portfolio.Infrastructure.Admin;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Infrastructure.Persistence.Seeding;
using Portfolio.Infrastructure.Queries;
using Portfolio.Infrastructure.Storage;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Portfolio";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // The connection string is resolved when the DbContext is created (not at registration),
        // so test hosts and environment-specific configuration can override it.
        services.AddDbContext<PortfolioDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(ConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is not configured. "
                    + $"Set 'ConnectionStrings:{ConnectionStringName}' in appsettings or user secrets.");
            }

            options
                .UseSqlServer(connectionString)
                .UseSeeding((context, _) => PortfolioSeeder.Seed(context))
                .UseAsyncSeeding((context, _, cancellationToken) => PortfolioSeeder.SeedAsync(context, cancellationToken));
        });

        services.AddScoped<IProjectQueries, ProjectQueries>();
        services.AddScoped<IProfileQueries, ProfileQueries>();
        services.AddScoped<IExperienceQueries, ExperienceQueries>();
        services.AddScoped<ITechnologyQueries, TechnologyQueries>();
        services.AddScoped<IContactMessageStore, ContactMessageStore>();

        services.AddScoped<IAdminProjectService, AdminProjectService>();
        services.AddScoped<IAdminTechnologyService, AdminTechnologyService>();
        services.AddScoped<IAdminExperienceService, AdminExperienceService>();
        services.AddScoped<IAdminProfileService, AdminProfileService>();
        services.AddScoped<IAdminMessageService, AdminMessageService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }

    /// <summary>
    /// Identity stores for the single admin account. The API adds the sign-in manager and cookie.
    /// </summary>
    public static IdentityBuilder AddAdminIdentity(this IServiceCollection services) =>
        services
            .AddIdentityCore<IdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<PortfolioDbContext>();

    /// <summary>
    /// Stores the keys that sign/encrypt the auth cookie in SQL Server. Without this, keys live in the
    /// container/app file system and every restart or redeploy would sign the admin out.
    /// </summary>
    public static IServiceCollection AddPersistentDataProtection(this IServiceCollection services)
    {
        services
            .AddDataProtection()
            .SetApplicationName("Portfolio")
            .PersistKeysToDbContext<PortfolioDbContext>();
        return services;
    }

    /// <summary>Applies pending migrations; EF Core then runs the seeding callbacks.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
