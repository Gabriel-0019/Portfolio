// One-off tool: copies all portfolio data from the old SQL Server (LocalDB) database into PostgreSQL.
//
//   dotnet run --project tools/Portfolio.DataMigration                 copy into an empty database
//   dotnet run --project tools/Portfolio.DataMigration -- --replace    wipe the PostgreSQL data first
//
// Source: --source "…" (default: LocalDB "Portfolio").
// Target: --target "…", or ConnectionStrings:Portfolio from the API's user secrets (secrets.json).
// The SQL Server database is only read, never modified. IDs are preserved and the PostgreSQL identity
// sequences are moved past them, so new records keep working. Uploaded files are not touched: their URLs
// (/uploads/…) are copied as-is and keep pointing at the same folder.

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Persistence;

// Table names in raw SQL below come only from the fixed lists in TableCounts, never from input.
#pragma warning disable EF1002

const string ApiUserSecretsId = "portfolio-api-6f2a1c4e";
const string DefaultSource =
    "Server=(localdb)\\MSSQLLocalDB;Database=Portfolio;Trusted_Connection=True;TrustServerCertificate=True";

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(ApiUserSecretsId)
    .AddEnvironmentVariables()
    .AddCommandLine(args.Where(a => a != "--replace").ToArray())
    .Build();

var sourceConnection = configuration["source"] ?? DefaultSource;
var targetConnection = configuration["target"] ?? configuration.GetConnectionString(DependencyInjection.ConnectionStringName);
var replace = args.Contains("--replace");

if (string.IsNullOrWhiteSpace(targetConnection))
{
    Console.Error.WriteLine("No target: pass --target \"Host=…\" or set ConnectionStrings:Portfolio in the API's user secrets.");
    return 1;
}

var sourceOptions = new DbContextOptionsBuilder<PortfolioDbContext>().UseSqlServer(sourceConnection).Options;
var targetBuilder = new DbContextOptionsBuilder<PortfolioDbContext>();
DependencyInjection.ConfigurePostgres(targetBuilder, targetConnection, seed: false);

await using var source = new PortfolioDbContext(sourceOptions);
await using var target = new PortfolioDbContext(targetBuilder.Options);

Console.WriteLine("1/5 Applying PostgreSQL migrations (without sample data)…");
await target.Database.MigrateAsync();

Console.WriteLine("2/5 Checking that the target is empty…");
var tables = TableCounts.Tables;
var existing = await TableCounts.CountAsync(target);
if (existing.Values.Any(count => count > 0))
{
    if (!replace)
    {
        Console.Error.WriteLine("The PostgreSQL database already has data:");
        foreach (var (table, count) in existing.Where(e => e.Value > 0))
        {
            Console.Error.WriteLine($"  {table}: {count}");
        }

        Console.Error.WriteLine("Run again with --replace to delete it and copy from SQL Server.");
        return 2;
    }

    Console.WriteLine("   --replace: deleting existing PostgreSQL data…");
    var truncate = string.Join(", ", tables.Select(t => $"\"{t}\""));
    await target.Database.ExecuteSqlRawAsync($"TRUNCATE {truncate} RESTART IDENTITY CASCADE");
}

Console.WriteLine("3/5 Reading SQL Server…");
var technologies = await source.Technologies.AsNoTracking().ToListAsync();
var profiles = await source.Profiles.AsNoTracking().Include(p => p.Translations).Include(p => p.SocialLinks).AsSplitQuery().ToListAsync();
var experiences = await source.Experiences.AsNoTracking().Include(e => e.Translations).AsSplitQuery().ToListAsync();
var projects = await source.Projects.AsNoTracking()
    .Include(p => p.Translations).Include(p => p.Images).Include(p => p.Technologies)
    .AsSplitQuery().ToListAsync();
var messages = await source.ContactMessages.AsNoTracking().ToListAsync();
var users = await source.Users.AsNoTracking().ToListAsync();
var roles = await source.Roles.AsNoTracking().ToListAsync();
var userRoles = await source.UserRoles.AsNoTracking().ToListAsync();
var userClaims = await source.UserClaims.AsNoTracking().ToListAsync();
var userLogins = await source.UserLogins.AsNoTracking().ToListAsync();
var userTokens = await source.UserTokens.AsNoTracking().ToListAsync();
var roleClaims = await source.RoleClaims.AsNoTracking().ToListAsync();
var keys = await source.Set<DataProtectionKey>().AsNoTracking().ToListAsync();

// PostgreSQL "timestamp with time zone" only accepts UTC values.
foreach (var p in projects) { p.CreatedAt = p.CreatedAt.ToUniversalTime(); p.UpdatedAt = p.UpdatedAt.ToUniversalTime(); }
foreach (var m in messages) { m.CreatedAt = m.CreatedAt.ToUniversalTime(); }
foreach (var u in users) { u.LockoutEnd = u.LockoutEnd?.ToUniversalTime(); }

Console.WriteLine("4/5 Writing PostgreSQL (one transaction)…");
var strategy = target.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var transaction = await target.Database.BeginTransactionAsync();

    target.Technologies.AddRange(technologies);
    await target.SaveChangesAsync();

    // Re-point each project's technologies at the tracked rows just inserted (same IDs).
    var technologyById = technologies.ToDictionary(t => t.Id);
    foreach (var project in projects)
    {
        project.Technologies = project.Technologies.Select(t => technologyById[t.Id]).ToList();
    }

    target.Profiles.AddRange(profiles);
    target.Experiences.AddRange(experiences);
    target.Projects.AddRange(projects);
    target.ContactMessages.AddRange(messages);
    target.Users.AddRange(users);
    target.Roles.AddRange(roles);
    await target.SaveChangesAsync();

    target.UserRoles.AddRange(userRoles);
    target.UserClaims.AddRange(userClaims);
    target.UserLogins.AddRange(userLogins);
    target.UserTokens.AddRange(userTokens);
    target.RoleClaims.AddRange(roleClaims);
    target.Set<DataProtectionKey>().AddRange(keys);
    await target.SaveChangesAsync();

    // Explicit IDs do not advance identity sequences: move each one past the highest copied ID.
    foreach (var table in TableCounts.IdentityTables)
    {
        await target.Database.ExecuteSqlRawAsync(
            $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"{table}\"), 0) + 1, false)");
    }

    await transaction.CommitAsync();
});

Console.WriteLine("5/5 Verifying row counts…");
var before = await TableCounts.CountAsync(source);
var after = await TableCounts.CountAsync(target);
var ok = true;
Console.WriteLine($"   {"Table",-28}{"SQL Server",12}{"PostgreSQL",12}");
foreach (var table in tables)
{
    var match = before[table] == after[table];
    ok &= match;
    Console.WriteLine($"   {table,-28}{before[table],12}{after[table],12}{(match ? "" : "   <-- MISMATCH")}");
}

Console.WriteLine(ok ? "Done: every table matches." : "Counts differ: review the output above.");
return ok ? 0 : 3;

internal static class TableCounts
{
    /// <summary>Every table that holds data (order does not matter: TRUNCATE … CASCADE handles foreign keys).</summary>
    public static readonly string[] Tables =
    [
        "Technologies", "Profiles", "ProfileTranslations", "SocialLinks", "Experiences", "ExperienceTranslations",
        "Projects", "ProjectTranslations", "ProjectImages", "ProjectTechnologies", "ContactMessages",
        "AspNetUsers", "AspNetRoles", "AspNetUserRoles", "AspNetUserClaims", "AspNetUserLogins",
        "AspNetUserTokens", "AspNetRoleClaims", "DataProtectionKeys",
    ];

    /// <summary>Tables whose integer "Id" is generated by PostgreSQL.</summary>
    public static readonly string[] IdentityTables =
    [
        "Technologies", "Profiles", "ProfileTranslations", "SocialLinks", "Experiences", "ExperienceTranslations",
        "Projects", "ProjectTranslations", "ProjectImages", "ContactMessages", "AspNetUserClaims",
        "AspNetRoleClaims", "DataProtectionKeys",
    ];

    public static async Task<Dictionary<string, int>> CountAsync(PortfolioDbContext db)
    {
        var counts = new Dictionary<string, int>();
        var sqlServer = db.Database.ProviderName?.Contains("SqlServer", StringComparison.Ordinal) == true;
        foreach (var table in Tables)
        {
            var sql = sqlServer
                ? $"SELECT COUNT(*) AS [Value] FROM [{table}]"
                : $"SELECT COUNT(*)::int AS \"Value\" FROM \"{table}\"";
            counts[table] = await db.Database.SqlQueryRaw<int>(sql).SingleAsync();
        }

        return counts;
    }
}
