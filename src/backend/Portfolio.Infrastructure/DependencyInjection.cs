using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Contact;
using Portfolio.Application.Experiences;
using Portfolio.Application.Profiles;
using Portfolio.Application.Projects;
using Portfolio.Application.Technologies;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Infrastructure.Persistence.Seeding;
using Portfolio.Infrastructure.Queries;

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
