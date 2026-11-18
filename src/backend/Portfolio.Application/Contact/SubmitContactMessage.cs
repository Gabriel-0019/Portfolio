using FluentValidation;
using Portfolio.Domain.Common;
using Portfolio.Domain.Contact;

namespace Portfolio.Application.Contact;

/// <summary>Payload sent by the public contact form.</summary>
/// <param name="Website">
/// Honeypot field. It is hidden from humans in the UI; bots that fill it are silently ignored.
/// </param>
public sealed record SubmitContactMessageRequest(
    string Name,
    string Email,
    string? Subject,
    string Message,
    string? Website = null);

public sealed class SubmitContactMessageValidator : AbstractValidator<SubmitContactMessageRequest>
{
    public const int MessageMinLength = 10;

    public SubmitContactMessageValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ContactMessage.NameMaxLength);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(ContactMessage.EmailMaxLength);
        RuleFor(x => x.Subject).MaximumLength(ContactMessage.SubjectMaxLength);
        RuleFor(x => x.Message)
            .NotEmpty()
            .MinimumLength(MessageMinLength)
            .MaximumLength(ContactMessage.MessageMaxLength);
    }
}

public interface IContactMessageStore
{
    Task AddAsync(ContactMessage message, CancellationToken cancellationToken);
}

public interface IContactService
{
    /// <summary>Stores a contact message. The request must already be validated.</summary>
    Task SubmitAsync(SubmitContactMessageRequest request, string language, CancellationToken cancellationToken);
}

public sealed class ContactService(IContactMessageStore store, TimeProvider timeProvider) : IContactService
{
    public async Task SubmitAsync(
        SubmitContactMessageRequest request, string language, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            // Honeypot triggered: respond as if it worked, but do not store anything.
            return;
        }

        var message = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim(),
            Message = request.Message.Trim(),
            Language = Languages.Normalize(language),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        await store.AddAsync(message, cancellationToken);
    }
}
