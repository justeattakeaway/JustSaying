#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using JustSaying.AwsTools;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

/// <summary>
/// JustSaying's default System.Text.Json options leave null members out of what they write, so a
/// nullable member must not be documented as required: the producer's own messages would fail the
/// schema whenever the member is null.
/// </summary>
public class WhenAMemberIsNullable
{
    public sealed record CustomerRegistered(string CustomerId, string? Nickname, int? Age, int Visits)
    {
        public required string Email { get; init; }

        public required string? Referrer { get; init; }
    }

    private static async Task<List<string>> RequiredMembersAsync(JsonSerializerOptions? serializerOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) => x.WithTopic<CustomerRegistered>());
        });
        services.AddJustSayingAsyncApi((options) => options.SerializerOptions = serializerOptions);

        var provider = services.BuildServiceProvider().GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        using var document = JsonDocument.Parse(writer.ToString());

        var payload = document.RootElement.GetProperty("channels").GetProperty("customerregistered")
            .GetProperty("messages").EnumerateObject().First().Value.GetProperty("payload");

        return payload.TryGetProperty("required", out var required)
            ? [.. required.EnumerateArray().Select((r) => r.GetString()!)]
            : [];
    }

    [Test]
    public async Task NullableMembersAreNotRequiredWhenNullsAreNotWritten()
    {
        var required = await RequiredMembersAsync();

        // Nullable positional parameters and nullable C# required members are omitted when null.
        await Assert.That(required).DoesNotContain(nameof(CustomerRegistered.Nickname));
        await Assert.That(required).DoesNotContain(nameof(CustomerRegistered.Age));
        await Assert.That(required).DoesNotContain(nameof(CustomerRegistered.Referrer));

        // Non-nullable ones are always written.
        await Assert.That(required).Contains(nameof(CustomerRegistered.CustomerId));
        await Assert.That(required).Contains(nameof(CustomerRegistered.Visits));
        await Assert.That(required).Contains(nameof(CustomerRegistered.Email));
    }

    [Test]
    public async Task NullableMembersStayRequiredWhenNullsAreWritten()
    {
        var required = await RequiredMembersAsync(new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.Never });

        await Assert.That(required).Contains(nameof(CustomerRegistered.Nickname));
        await Assert.That(required).Contains(nameof(CustomerRegistered.Referrer));
    }

    [Test]
    public async Task ValueTypeMembersAreNotRequiredWhenDefaultsAreNotWritten()
    {
        var required = await RequiredMembersAsync(new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });

        await Assert.That(required).DoesNotContain(nameof(CustomerRegistered.Visits));
        await Assert.That(required).DoesNotContain(nameof(CustomerRegistered.Nickname));
        await Assert.That(required).Contains(nameof(CustomerRegistered.Email));
    }
}
