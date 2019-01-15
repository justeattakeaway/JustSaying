using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenSubscribingToTopicInAnotherAccount
    {
        private readonly Future<SimpleMessage> _signal = new Future<SimpleMessage>();
        private readonly SimpleMessage _message = new SimpleMessage { Id = Guid.NewGuid() };

        public WhenSubscribingToTopicInAnotherAccount(ITestOutputHelper outputHelper)
        {
            LoggerFactory = outputHelper.ToLoggerFactory();
        }

        private ILoggerFactory LoggerFactory { get; }

        [NeedsTwoAwsAccountsFact]
        public async Task ICanReceiveMessagePublishedToTopicInAnotherAccount()
        {
            // Arrange
            string publisherAccount = TestEnvironment.AccountId;
            string subscriberAccount = TestEnvironment.SecondaryAccountId;

            var publishingBus = GetBus(TestEnvironment.Credentials);
            var subscribingBus = GetBus(TestEnvironment.SecondaryCredentials);

            publishingBus
                .WithNamingStrategy(() => new CrossAccountNamingStrategy())
                .ConfigurePublisherWith(cfg => cfg.AdditionalSubscriberAccounts = new List<string> { subscriberAccount })
                .WithSnsMessagePublisher<SimpleMessage>();


            var handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            handler.Handle(Arg.Any<SimpleMessage>()).Returns(true);
            handler
                .When(x => x.Handle(Arg.Any<SimpleMessage>()))
                .Do(async x => await _signal.Complete((SimpleMessage)x.Args()[0]));

            subscribingBus
                .WithNamingStrategy(() => new CrossAccountNamingStrategy())
                .WithSqsTopicSubscriber()
                .IntoQueue("crossaccount")
                .ConfigureSubscriptionWith(cfg => cfg.TopicSourceAccount = publisherAccount)
                .WithMessageHandler(handler);

            subscribingBus.StartListening();

            // Act
            await publishingBus.PublishAsync(_message);

            // Assert
            var done = await TaskHelpers.WaitWithTimeoutAsync(_signal.DoneSignal, TimeSpan.FromMinutes(1));
            _signal.HasReceived(_message).ShouldBeTrue();
        }

        private IMayWantOptionalSettings GetBus(AWSCredentials credentials)
        {
            return CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(TestEnvironment.Region.SystemName)
                .WithAwsClientFactory(() => new DefaultAwsClientFactory(credentials));
        }

        private class CrossAccountNamingStrategy : INamingStrategy
        {
            public string GetTopicName(string topicName, Type messageType)
            {
                return "test-" + messageType.ToTopicName();
            }

            public string GetQueueName(SqsReadConfiguration sqsConfig, Type messageType)
            {
                return "test-" + messageType.ToTopicName();
            }
        }
    }
}
