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

    /// <summary>
    /// Optional direct (non-pooled) connection used only to apply migrations. Neon's pooled endpoint
    /// (PgBouncer) cannot run migrations, so production sets both strings; locally only "Portfolio" is needed.
    /// </summary>
    public const string MigrationsConnectionStringName = "PortfolioMigrations";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // The connection string is resolved when the DbContext is created (not at registration),
        // so test hosts and environment-specific configuration can override it.
        services.AddDbContext<PortfolioDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            ConfigurePostgres(options, RequireConnectionString(configuration, ConnectionStringName));
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
    /// Stores the keys that sign/encrypt the auth cookie in the database. Without this, keys live in the
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

    /// <summary>
    /// Applies pending migrations; EF Core then runs the seeding callbacks. Uses "PortfolioMigrations"
    /// (direct connection) when configured, otherwise the regular "Portfolio" connection.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var migrationsConnection = configuration.GetConnectionString(MigrationsConnectionStringName);

        if (string.IsNullOrWhiteSpace(migrationsConnection))
        {
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<PortfolioDbContext>().Database.MigrateAsync(cancellationToken);
            return;
        }

        var options = new DbContextOptionsBuilder<PortfolioDbContext>();
        ConfigurePostgres(options, migrationsConnection);
        await using var db = new PortfolioDbContext(options.Options);
        await db.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>PostgreSQL (Npgsql) with retries for transient failures (e.g. a Neon compute waking up).</summary>
    public static void ConfigurePostgres(DbContextOptionsBuilder options, string connectionString, bool seed = true)
    {
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());

        if (seed)
        {
            options
                .UseSeeding((context, _) => PortfolioSeeder.Seed(context))
                .UseAsyncSeeding((context, _, cancellationToken) => PortfolioSeeder.SeedAsync(context, cancellationToken));
        }
    }

    private static string RequireConnectionString(IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{name}' is not configured. "
                + $"Set 'ConnectionStrings:{name}' in user secrets (development) or as the environment variable "
                + $"'ConnectionStrings__{name}' (production).");
        }

        return connectionString;
    }
}
