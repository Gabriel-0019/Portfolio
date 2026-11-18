using System.Net.Http.Json;
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
    public const string AdminEmail = "admin@tests.localhost";

    /// <summary>Generated per run; only exists in the throw-away test database.</summary>
    public string AdminPassword { get; } = $"Test-{Guid.NewGuid():N}-Aa1!";

    private readonly string _databaseName = $"Portfolio_Tests_{Guid.NewGuid():N}";

    public string UploadsPath { get; } = Path.Combine(Path.GetTempPath(), $"portfolio-uploads-{Guid.NewGuid():N}");

    private string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Portfolio", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("RateLimiting:Contact:PermitLimit", "1000");
        builder.UseSetting("RateLimiting:Login:PermitLimit", "1000");
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.UseSetting("Storage:RootPath", UploadsPath);
    }

    /// <summary>A client that keeps cookies and is already signed in as the admin.</summary>
    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = AdminEmail, password = AdminPassword });
        response.EnsureSuccessStatusCode();
        return client;
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
        if (Directory.Exists(UploadsPath))
        {
            Directory.Delete(UploadsPath, recursive: true);
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<PortfolioApiFactory>
{
    public const string Name = "api";
}
