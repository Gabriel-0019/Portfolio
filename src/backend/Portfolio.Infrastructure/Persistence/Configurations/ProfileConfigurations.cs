using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Common;
using Portfolio.Domain.Contact;
using Portfolio.Domain.Experiences;
using Portfolio.Domain.Profiles;

namespace Portfolio.Infrastructure.Persistence.Configurations;

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.Property(p => p.FullName).HasMaxLength(Profile.FullNameMaxLength).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(Profile.EmailMaxLength);
        builder.Property(p => p.Location).HasMaxLength(Profile.LocationMaxLength);
        builder.Property(p => p.AvatarUrl).HasMaxLength(Profile.UrlMaxLength);
        builder.Property(p => p.CvUrlEn).HasMaxLength(Profile.UrlMaxLength);
        builder.Property(p => p.CvUrlEs).HasMaxLength(Profile.UrlMaxLength);

        builder.HasMany(p => p.Translations)
            .WithOne()
            .HasForeignKey(t => t.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.SocialLinks)
            .WithOne()
            .HasForeignKey(s => s.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProfileTranslationConfiguration : IEntityTypeConfiguration<ProfileTranslation>
{
    public void Configure(EntityTypeBuilder<ProfileTranslation> builder)
    {
        builder.Property(t => t.Language).HasMaxLength(Languages.CodeMaxLength).IsRequired();
        builder.Property(t => t.Headline).HasMaxLength(ProfileTranslation.HeadlineMaxLength).IsRequired();
        builder.Property(t => t.Bio).IsRequired();
        builder.HasIndex(t => new { t.ProfileId, t.Language }).IsUnique();
    }
}

internal sealed class SocialLinkConfiguration : IEntityTypeConfiguration<SocialLink>
{
    public void Configure(EntityTypeBuilder<SocialLink> builder)
    {
        builder.Property(s => s.Platform).HasMaxLength(SocialLink.PlatformMaxLength).IsRequired();
        builder.Property(s => s.Url).HasMaxLength(Profile.UrlMaxLength).IsRequired();
    }
}

internal sealed class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.Property(e => e.Company).HasMaxLength(Experience.CompanyMaxLength).IsRequired();
        builder.Property(e => e.CompanyUrl).HasMaxLength(Experience.UrlMaxLength);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(e => e.IsCurrent);

        builder.HasMany(e => e.Translations)
            .WithOne()
            .HasForeignKey(t => t.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ExperienceTranslationConfiguration : IEntityTypeConfiguration<ExperienceTranslation>
{
    public void Configure(EntityTypeBuilder<ExperienceTranslation> builder)
    {
        builder.Property(t => t.Language).HasMaxLength(Languages.CodeMaxLength).IsRequired();
        builder.Property(t => t.Role).HasMaxLength(ExperienceTranslation.RoleMaxLength).IsRequired();
        builder.Property(t => t.Location).HasMaxLength(ExperienceTranslation.LocationMaxLength);
        builder.Property(t => t.Description).IsRequired();
        builder.HasIndex(t => new { t.ExperienceId, t.Language }).IsUnique();
    }
}

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.Property(m => m.Name).HasMaxLength(ContactMessage.NameMaxLength).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(ContactMessage.EmailMaxLength).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(ContactMessage.SubjectMaxLength);
        builder.Property(m => m.Message).HasMaxLength(ContactMessage.MessageMaxLength).IsRequired();
        builder.Property(m => m.Language).HasMaxLength(Languages.CodeMaxLength).IsRequired();
        builder.HasIndex(m => new { m.IsRead, m.CreatedAt });
    }
}
