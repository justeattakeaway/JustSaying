using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.Fluent;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingQueueAddressSubscriptionBuilder
{
    private const string QueueArn = "arn:aws:sqs:eu-west-1:111122223333:queue1";
    private const string QueueUrl = "https://sqs.eu-west-1.amazonaws.com/111122223333/queue1";

    private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();

    public WhenUsingQueueAddressSubscriptionBuilder()
    {
        _sqs.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ReceiveMessageResponse { Messages = [] });
    }

    [Test]
    public async Task DoesNotCheckExistenceByDefault()
    {
        using var cancellation = new CancellationTokenSource();
        var bus = BuildBus(checkExistence: false);

        try
        {
            await bus.StartAsync(cancellation.Token);

            _ = _sqs.DidNotReceive()
                .GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            cancellation.Cancel();
            await bus.Completion;
        }
    }

    [Test]
    public async Task CheckExistenceChecksQueueAttributesBeforeStarting()
    {
        using var cancellation = new CancellationTokenSource();
        _sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetQueueAttributesResponse());
        var bus = BuildBus(checkExistence: true);

        try
        {
            await bus.StartAsync(cancellation.Token);

            _ = _sqs.Received(1).GetQueueAttributesAsync(
                Arg.Is<GetQueueAttributesRequest>(request =>
                    request.QueueUrl == QueueUrl &&
                    request.AttributeNames.Contains("QueueArn")),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            cancellation.Cancel();
            await bus.Completion;
        }
    }

    [Test]
    public async Task CheckExistenceThrowsWhenQueueDoesNotExist()
    {
        _sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetQueueAttributesResponse>(new QueueDoesNotExistException("Queue does not exist.")));
        var bus = BuildBus(checkExistence: true);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => bus.StartAsync(CancellationToken.None));

        exception.Message.ShouldBe($"SQS queue 'queue1' with URL '{QueueUrl}' does not exist.");
    }

    private global::JustSaying.JustSayingBus BuildBus(bool checkExistence)
    {
        var clientFactory = Substitute.For<IAwsClientFactory>();
        clientFactory.GetSqsClient(Arg.Any<RegionEndpoint>()).Returns(_sqs);
        clientFactory.GetSnsClient(Arg.Any<RegionEndpoint>()).Returns(Substitute.For<IAmazonSimpleNotificationService>());

        var handlerResolver = Substitute.For<IHandlerResolver>();
        handlerResolver.ResolveHandler<Order>(Arg.Any<HandlerResolutionContext>())
            .Returns(new NoOpHandler<Order>());

        return (global::JustSaying.JustSayingBus)new MessagingBusBuilder()
            .Client(builder => builder.WithClientFactory(() => clientFactory))
            .WithServiceResolver(new TestServiceResolver())
            .Services(services => services
                .WithHandlerResolver(handlerResolver)
                .WithLoggerFactory(NullLoggerFactory.Instance))
            .Subscriptions(subscriptions => subscriptions.ForQueueArn<Order>(
                QueueArn,
                queue =>
                {
                    queue.WithMiddlewareConfiguration(middleware => middleware.UseHandler<Order>());

                    if (checkExistence)
                    {
                        queue.CheckExistence();
                    }
                }))
            .BuildSubscribers();
    }

    private sealed class TestServiceResolver : IServiceResolver
    {
        private readonly MessageReceivePauseSignal _pauseSignal = new();
        private readonly NullOpMessageMonitor _monitor = new();
        private readonly NewtonsoftSerializationFactory _serializationFactory = new();
        private readonly MessagingConfig _messagingConfig = new();

        public T ResolveService<T>() where T : class
        {
            return ResolveOptionalService<T>() ??
                   throw new NotSupportedException($"Resolving a service of type {typeof(T).Name} is not supported.");
        }

        public T ResolveOptionalService<T>() where T : class
        {
            if (typeof(T) == typeof(IMessageReceivePauseSignal))
            {
                return (T)(object)_pauseSignal;
            }

            if (typeof(T) == typeof(IMessageMonitor))
            {
                return (T)(object)_monitor;
            }

            if (typeof(T) == typeof(IMessageBodySerializationFactory))
            {
                return (T)(object)_serializationFactory;
            }

            if (typeof(T) == typeof(IMessagingConfig) || typeof(T) == typeof(IPublishBatchConfiguration))
            {
                return (T)(object)_messagingConfig;
            }

            return null;
        }
    }
}
