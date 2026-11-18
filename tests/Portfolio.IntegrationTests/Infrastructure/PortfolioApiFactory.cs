using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the API against a throw-away SQL Server LocalDB database that is migrated and seeded
/// on startup and dropped when the test run finishes.
/// </summary>
public sealed class PortfolioApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"Portfolio_Tests_{Guid.NewGuid():N}";

    private string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Portfolio", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("RateLimiting:Contact:PermitLimit", "1000");
    }

    /// <summary>Runs <paramref name="action"/> with a fresh DbContext scope.</summary>
    public async Task WithDbAsync(Func<PortfolioDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<PortfolioDbContext>());
    }

    public Task InitializeAsync()
    {
        // Touching Server starts the host, which applies migrations and seeds the database.
        _ = Server;
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await WithDbAsync(db => db.Database.EnsureDeletedAsync());
        await DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<PortfolioApiFactory>
{
    public const string Name = "api";
}
