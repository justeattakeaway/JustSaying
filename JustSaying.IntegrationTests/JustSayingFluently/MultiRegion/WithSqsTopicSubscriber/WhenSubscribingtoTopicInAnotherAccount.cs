using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenSubscribingtoTopicInAnotherAccount
    {
        private readonly Future<GenericMessage> _signal = new Future<GenericMessage>();
        readonly GenericMessage _message = new GenericMessage {Id = Guid.NewGuid()};

        // TODO Only works with real accounts and for two accounts
        [Fact(Skip = "Requires credentials for 2 accounts")]
        public async Task ICanReceiveMessagePublishedToTopicInAnotherAccount()
        {
            string publisherAccount = "<enter publisher account id>";
            string subscriberAccount = "<enter subscriber account id>";
            var publishingBus = GetBus("<enter publisher access key>", "<enter publisher secret key>");
            var subscribingBus = GetBus("<enter subscriber access key>", "<enter subscriber secret key>");

            publishingBus
                .WithNamingStrategy(() => new NamingStrategy())
                .ConfigurePublisherWith(cfg => cfg.AdditionalSubscriberAccounts = new List<string> { subscriberAccount })
                .WithSnsMessagePublisher<GenericMessage>();


            var handler = Substitute.For<IHandlerAsync<GenericMessage>>();
            handler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            handler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(async x => await _signal.Complete((GenericMessage)x.Args()[0]));

            subscribingBus
                .WithNamingStrategy(() => new NamingStrategy())
                .WithSqsTopicSubscriber()
                .IntoQueue("crossaccount")
                .ConfigureSubscriptionWith(cfg => cfg.TopicSourceAccount = publisherAccount)
                .WithMessageHandler(handler);
            subscribingBus.StartListening();

            //Act
            await publishingBus.PublishAsync(_message);

            //Assert
            var done = await Tasks.WaitWithTimeoutAsync(_signal.DoneSignal, TimeSpan.FromMinutes(1));
            _signal.HasReceived(_message).ShouldBeTrue();
        }

        private IMayWantOptionalSettings GetBus(string accessKey, string secretKey)
        {
            return CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithAwsClientFactory(()=>new DefaultAwsClientFactory(new BasicAWSCredentials(accessKey, secretKey)));
        }
    }

    internal class NamingStrategy : INamingStrategy
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
