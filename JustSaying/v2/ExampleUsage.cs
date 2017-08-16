using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using JustSaying.v2.FluentApi;

namespace JustSaying.v2
{
    public class ExampleUsage
    {
        public async Task Moo()
        {
            // Configure bus (regions, dependencies etc.)
            var configuredBus = new BusBuilder().UsingAws(cfg =>
            {
                cfg.UseRegions("eu-west-1", "eu-central-1");
                cfg.ConfigureDependencies(dep =>
                {
                    dep.MessageMonitor = new NullOpMessageMonitor();
                });
            });

            // Publisher
            var publisher = await configuredBus.CreatePublishers()
                .AddTopicPublisher<MessageOne>(cfg =>
                {
                    cfg.TopicNameOverride = "override-topic-name";
                    cfg.AdditionalSubscribers = new[] {"123", "456", "789"};
                })
                .AddQueuePublisher<MessageTwo>(cfg =>
                {
                    cfg.QueueNameOverride = "override-queue-name";
                    cfg.DeliveryDelaySeconds = 10;
                    cfg.ErrorQueueOptOut = true;
                    cfg.ErrorQueueRetentionPeriodSeconds = 300;
                    cfg.MessageRetentionSeconds = 30;
                    cfg.RetryCountBeforeSendingToErrorQueue = 8;
                    cfg.VisibilityTimeoutSeconds = 30;
                })
                .BuildAsync();

            await publisher.PublishAsync(new MessageOne());

            // Subscriber
            var subscriber = await configuredBus.CreateSubscribers()
                .AddTopicSubscriber(cfg =>
                {
                    cfg.TopicNameOverride = "override-topic-name";
                })
                .WithHandler(new MessageOneHandler())
                .AddQueueSubscriber(cfg =>
                {
                    cfg.QueueNameOverride = "override-queue-name";
                })
                .WithHandler(new MessageTwoHandler())
                .BuildAsync();

            subscriber.StartListening();
            subscriber.StopListening();
        }
    }

    public class MessageOne : Message { }
    public class MessageTwo : Message { }

    public class MessageOneHandler : IHandlerAsync<MessageOne>
    {
        public Task<bool> Handle(MessageOne message) => Task.FromResult(true);
    }

    public class MessageTwoHandler : IHandlerAsync<MessageTwo>
    {
        public Task<bool> Handle(MessageTwo message) => Task.FromResult(true);
    }
}