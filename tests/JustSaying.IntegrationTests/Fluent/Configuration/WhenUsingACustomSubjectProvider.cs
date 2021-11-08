using JustSaying.IntegrationTests.Fluent;
using JustSaying.IntegrationTests.Fluent.Subscribing;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JustSaying.Fluent.Configuration;

public class WhenUsingACustomSubjectProvider : IntegrationTestBase
{
    public WhenUsingACustomSubjectProvider(ITestOutputHelper outputHelper) : base(outputHelper)
    { }

    [AwsFact]
    public async Task ThenItIsUsed()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();
        var accessor = new RecordingMessageContextAccessor(new MessageContextAccessor());

        var subject = Guid.NewGuid().ToString();
        var subjectProvider = new ConstantSubjectProvider(subject);

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
            .ConfigureJustSaying(builder =>
                builder.Services(s => s.WithMessageContextAccessor(() => accessor)))
            .ConfigureJustSaying((builder) =>
                builder.Messaging(m =>
                    m.WithMessageSubjectProvider(subjectProvider)))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        var id = Guid.NewGuid();
        var message = new SimpleMessage()
        {
            Id = id
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Let's send an OrderPlaced, but the subject will be a GUID
                // because of the custom subject provider
                await publisher.PublishAsync(message, cancellationToken);

                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        var receivedMessage = handler.ReceivedMessages.ShouldHaveSingleItem();
                        receivedMessage.Id.ShouldBe(id);

                        var context = accessor.ValuesWritten.ShouldHaveSingleItem();
                        dynamic json = JsonConvert.DeserializeObject(context.Message.Body);
                        string subject = json.Subject;
                        subject.ShouldBe(subject);
                    });
            });
    }

    public class ConstantSubjectProvider : IMessageSubjectProvider
    {
        private readonly string _subject;

        public ConstantSubjectProvider(string subject)
        {
            _subject = subject;
        }

        public string GetSubjectForType(Type messageType)
        {
            return _subject;
        }
    }
}
