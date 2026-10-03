using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAPublicationOwnsAQueue : IntegrationTestBase
{
    [Test]
    public async Task Then_The_Queue_Is_The_Same_As_A_Subscription_Would_Create()
    {
        // Arrange
        static QueueDestination Destination(string name) => QueueDestination.Named(name, q => q
            .WithVisibilityTimeout(TimeSpan.FromSeconds(9))
            .WithMessageRetention(TimeSpan.FromDays(2))
            .WithRetriesBeforeErrorQueue(9)
            .WithTag("team", "payments"));

        var publicationQueue = UniqueName;
        var subscriptionQueue = $"{UniqueName}-sub";

        await StartPublisherAsync(Destination(publicationQueue));
        await StartSubscriberAsync(Destination(subscriptionQueue));

        // Assert
        var client = CreateClientFactory().GetSqsClient(Region);

        var published = await GetQueueAsync(client, publicationQueue);
        var subscribed = await GetQueueAsync(client, subscriptionQueue);

        published.Attributes["VisibilityTimeout"].ShouldBe("9");
        published.Attributes["MessageRetentionPeriod"].ShouldBe(subscribed.Attributes["MessageRetentionPeriod"]);
        published.Attributes["VisibilityTimeout"].ShouldBe(subscribed.Attributes["VisibilityTimeout"]);
        RedrivePolicy.ConvertFromString(published.Attributes["RedrivePolicy"]).MaximumReceives.ShouldBe(9);

        published.Tags.ShouldContainKeyAndValue("team", "payments");
        (await GetQueueAsync(client, $"{publicationQueue}_error")).Tags.ShouldContainKeyAndValue("team", "payments");
        (await GetQueueAsync(client, $"{subscriptionQueue}_error")).Tags.ShouldContainKeyAndValue("team", "payments");
    }

    [Test]
    public async Task Then_The_Declared_Settings_Are_Updated_On_An_Existing_Queue()
    {
        // Arrange
        await StartPublisherAsync(QueueDestination.Named(UniqueName, q => q
            .WithVisibilityTimeout(TimeSpan.FromSeconds(5))
            .WithRetriesBeforeErrorQueue(5)));

        // Act
        await StartPublisherAsync(QueueDestination.Named(UniqueName, q => q
            .WithVisibilityTimeout(TimeSpan.FromSeconds(9))
            .WithRetriesBeforeErrorQueue(9)));

        // Assert
        var queue = await GetQueueAsync(CreateClientFactory().GetSqsClient(Region), UniqueName);

        queue.Attributes["VisibilityTimeout"].ShouldBe("9");
        RedrivePolicy.ConvertFromString(queue.Attributes["RedrivePolicy"]).MaximumReceives.ShouldBe(9);
    }

    [Test]
    public async Task Then_Settings_It_Does_Not_Declare_Are_Left_Alone_On_An_Existing_Queue()
    {
        // Arrange - a queue owned by a subscriber elsewhere, with no error queue
        var client = CreateClientFactory().GetSqsClient(Region);
        await client.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = UniqueName,
            Attributes = new()
            {
                ["VisibilityTimeout"] = "120",
                ["MessageRetentionPeriod"] = "3600",
            },
        });

        // Act
        await StartPublisherAsync(QueueDestination.Named(UniqueName, q => q.WithTag("team", "payments")));

        // Assert
        var queue = await GetQueueAsync(client, UniqueName);

        queue.Attributes["VisibilityTimeout"].ShouldBe("120");
        queue.Attributes["MessageRetentionPeriod"].ShouldBe("3600");
        queue.Attributes.ShouldNotContainKey("RedrivePolicy");
        queue.Tags.ShouldContainKeyAndValue("team", "payments");

        var queues = await client.ListQueuesAsync(UniqueName);
        queues.QueueUrls.ShouldAllBe((url) => !url.Contains("_error", StringComparison.Ordinal), "An error queue was added to the existing queue.");
    }

    [Test]
    public void Then_Invalid_Settings_Are_Rejected_When_The_Publisher_Is_Built()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Publications((options) =>
                options.WithQueue<SimpleMessage>(QueueDestination.Named(UniqueName, q => q.WithMessageRetention(TimeSpan.FromDays(30))))))
            .BuildServiceProvider();

        // Act and Assert
        var exception = Should.Throw<ConfigurationErrorsException>(() => serviceProvider.GetRequiredService<IMessagePublisher>());
        exception.Message.ShouldContain("MessageRetention");
        exception.Message.ShouldContain(UniqueName);
        exception.Message.ShouldContain(nameof(SimpleMessage));
    }

    private async Task StartPublisherAsync(QueueDestination destination)
    {
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Publications((options) => options.WithQueue<SimpleMessage>(destination)))
            .BuildServiceProvider();

        await serviceProvider.GetRequiredService<IMessagePublisher>().StartAsync(CancellationToken.None);
    }

    private async Task StartSubscriberAsync(QueueDestination destination)
    {
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Subscriptions((options) => options.ForQueue<SimpleMessage>(destination)))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(new InspectableHandler<SimpleMessage>())
            .BuildServiceProvider();

        using var cts = new CancellationTokenSource(Timeout);
        var bus = serviceProvider.GetRequiredService<IMessagingBus>();
        await bus.StartAsync(cts.Token);
        cts.Cancel();
    }

    private static async Task<(Dictionary<string, string> Attributes, Dictionary<string, string> Tags)> GetQueueAsync(IAmazonSQS client, string queueName)
    {
        var queueUrl = (await client.GetQueueUrlAsync(queueName)).QueueUrl;
        var attributes = await client.GetQueueAttributesAsync(new GetQueueAttributesRequest { QueueUrl = queueUrl, AttributeNames = ["All"] });
        var tags = await client.ListQueueTagsAsync(new ListQueueTagsRequest { QueueUrl = queueUrl });

        return (attributes.Attributes, tags.Tags ?? []);
    }
}
