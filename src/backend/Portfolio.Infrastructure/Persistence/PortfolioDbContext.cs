using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Contact;
using Portfolio.Domain.Experiences;
using Portfolio.Domain.Profiles;
using Portfolio.Domain.Projects;
using Portfolio.Domain.Technologies;

namespace Portfolio.Infrastructure.Persistence;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTranslation> ProjectTranslations => Set<ProjectTranslation>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileTranslation> ProfileTranslations => Set<ProfileTranslation>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<ExperienceTranslation> ExperienceTranslations => Set<ExperienceTranslation>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
    }
}
