using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Common;
using Portfolio.Domain.Projects;
using Portfolio.Domain.Technologies;

namespace Portfolio.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(p => p.Slug).HasMaxLength(Project.SlugMaxLength).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => new { p.Status, p.IsFeatured, p.DisplayOrder });

        builder.Property(p => p.CoverImageUrl).HasMaxLength(Project.UrlMaxLength);
        builder.Property(p => p.RepoUrl).HasMaxLength(Project.UrlMaxLength);
        builder.Property(p => p.DemoUrl).HasMaxLength(Project.UrlMaxLength);

        builder.Ignore(p => p.IsPubliclyVisible);

        builder.HasMany(p => p.Translations)
            .WithOne()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Technologies)
            .WithMany(t => t.Projects)
            .UsingEntity(join => join.ToTable("ProjectTechnologies"));
    }
}

internal sealed class ProjectTranslationConfiguration : IEntityTypeConfiguration<ProjectTranslation>
{
    public void Configure(EntityTypeBuilder<ProjectTranslation> builder)
    {
        builder.Property(t => t.Language).HasMaxLength(Languages.CodeMaxLength).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(ProjectTranslation.TitleMaxLength).IsRequired();
        builder.Property(t => t.Summary).HasMaxLength(ProjectTranslation.SummaryMaxLength).IsRequired();
        builder.Property(t => t.Content).IsRequired();
        builder.HasIndex(t => new { t.ProjectId, t.Language }).IsUnique();
    }
}

internal sealed class ProjectImageConfiguration : IEntityTypeConfiguration<ProjectImage>
{
    public void Configure(EntityTypeBuilder<ProjectImage> builder)
    {
        builder.Property(i => i.Url).HasMaxLength(Project.UrlMaxLength).IsRequired();
        builder.Property(i => i.AltEn).HasMaxLength(ProjectImage.AltMaxLength).IsRequired();
        builder.Property(i => i.AltEs).HasMaxLength(ProjectImage.AltMaxLength).IsRequired();
    }
}

internal sealed class TechnologyConfiguration : IEntityTypeConfiguration<Technology>
{
    public void Configure(EntityTypeBuilder<Technology> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(Technology.NameMaxLength).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();
        builder.Property(t => t.IconKey).HasMaxLength(Technology.IconKeyMaxLength);
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(20);
    }
}
