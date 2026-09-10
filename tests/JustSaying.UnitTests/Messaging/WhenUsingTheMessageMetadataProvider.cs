using JustSaying.Messaging;
using JustSaying.Models;

namespace JustSaying.UnitTests.Messaging;

public class WhenUsingTheMessageMetadataProvider
{
    private sealed class PocoMessage;

    private sealed class TestMessage : Message;

    [Test]
    public void ForAMessageItReadsTheIntrinsicMetadata()
    {
        var provider = DefaultMessageMetadataProvider.Instance;
        var message = new TestMessage();

        provider.GetId(message).ShouldBe(message.Id.ToString());
        provider.GetTimestamp(message).ShouldBe(new DateTimeOffset(DateTime.SpecifyKind(message.TimeStamp, DateTimeKind.Utc)));
        provider.TryGetDeduplicationKey(message, out var key).ShouldBeTrue();
        key.ShouldBe(message.UniqueKey());
    }

    [Test]
    public void ForANonMessagePayloadItReportsAbsence()
    {
        var provider = DefaultMessageMetadataProvider.Instance;
        var message = new PocoMessage();

        provider.GetId(message).ShouldBeNull();
        provider.GetTimestamp(message).ShouldBeNull();
        provider.TryGetDeduplicationKey(message, out var key).ShouldBeFalse();
        key.ShouldBeNull();
    }

    [Test]
    public void TheConfigExposesADefaultProvider()
    {
        new MessagingConfig().MessageMetadataProvider.ShouldBe(DefaultMessageMetadataProvider.Instance);
    }
}
