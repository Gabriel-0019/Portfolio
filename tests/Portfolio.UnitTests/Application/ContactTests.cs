using Portfolio.Application.Contact;
using Portfolio.Domain.Contact;

namespace Portfolio.UnitTests.Application;

public sealed class SubmitContactMessageValidatorTests
{
    private readonly SubmitContactMessageValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes() =>
        Assert.True(_validator.Validate(ValidRequest()).IsValid);

    [Theory]
    [InlineData("", "ana@example.com", "Hello there, nice portfolio!", "Name")]
    [InlineData("Ana", "not-an-email", "Hello there, nice portfolio!", "Email")]
    [InlineData("Ana", "ana@example.com", "Too short", "Message")]
    public void InvalidRequest_ReportsField(string name, string email, string message, string field)
    {
        var result = _validator.Validate(new SubmitContactMessageRequest(name, email, null, message));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == field);
    }

    [Fact]
    public void MessageLongerThanLimit_Fails()
    {
        var request = ValidRequest() with { Message = new string('a', ContactMessage.MessageMaxLength + 1) };

        Assert.False(_validator.Validate(request).IsValid);
    }

    internal static SubmitContactMessageRequest ValidRequest() =>
        new("Ana", "ana@example.com", "Hi", "Hello there, nice portfolio!");
}

public sealed class ContactServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubmitAsync_StoresTrimmedMessageWithLanguageAndTimestamp()
    {
        var store = new InMemoryStore();
        var service = new ContactService(store, new FixedTimeProvider(Now));

        await service.SubmitAsync(
            new SubmitContactMessageRequest("  Ana ", " ana@example.com ", "  ", " Hello there, nice portfolio! "),
            "ES",
            CancellationToken.None);

        var stored = Assert.Single(store.Messages);
        Assert.Equal("Ana", stored.Name);
        Assert.Equal("ana@example.com", stored.Email);
        Assert.Null(stored.Subject);
        Assert.Equal("Hello there, nice portfolio!", stored.Message);
        Assert.Equal("es", stored.Language);
        Assert.Equal(Now, stored.CreatedAt);
        Assert.False(stored.IsRead);
    }

    [Fact]
    public async Task SubmitAsync_WhenHoneypotIsFilled_DoesNotStore()
    {
        var store = new InMemoryStore();
        var service = new ContactService(store, new FixedTimeProvider(Now));

        await service.SubmitAsync(
            SubmitContactMessageValidatorTests.ValidRequest() with { Website = "http://spam.example" },
            "en",
            CancellationToken.None);

        Assert.Empty(store.Messages);
    }

    private sealed class InMemoryStore : IContactMessageStore
    {
        public List<ContactMessage> Messages { get; } = [];

        public Task AddAsync(ContactMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
