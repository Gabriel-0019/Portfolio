using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Portfolio.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core tools (<c>dotnet ef migrations add …</c>), so generating migrations needs neither
/// the API host nor any secret. Commands that touch a real database take the connection from
/// <c>--connection "…"</c> or the <c>ConnectionStrings__Portfolio</c> environment variable.
/// The API itself applies migrations on startup in development.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    private const string PlaceholderConnection = "Host=localhost;Database=portfolio_design_time;Username=postgres";

    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Portfolio");
        var options = new DbContextOptionsBuilder<PortfolioDbContext>();
        DependencyInjection.ConfigurePostgres(options, string.IsNullOrWhiteSpace(connection) ? PlaceholderConnection : connection);
        return new PortfolioDbContext(options.Options);
    }
}
