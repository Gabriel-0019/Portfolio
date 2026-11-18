using Portfolio.Application.Contact;
using Portfolio.Domain.Contact;

namespace Portfolio.Infrastructure.Persistence;

internal sealed class ContactMessageStore(PortfolioDbContext db) : IContactMessageStore
{
    public async Task AddAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        db.ContactMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
    }
}
