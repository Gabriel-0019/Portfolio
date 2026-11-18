using Portfolio.Domain.Common;

namespace Portfolio.Domain.Contact;

public sealed class ContactMessage : Entity
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const int SubjectMaxLength = 150;
    public const int MessageMaxLength = 4000;

    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Subject { get; set; }
    public required string Message { get; set; }

    /// <summary>Language of the site when the message was sent.</summary>
    public required string Language { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
