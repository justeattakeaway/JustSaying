using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
{
    public class WhenSubscribingtoTopicInAnotherAccount
    {
        private readonly Future<GenericMessage> _signal = new Future<GenericMessage>();
        readonly GenericMessage _message = new GenericMessage {Id = Guid.NewGuid()};

        [Test, Category("Integration"), Ignore("Requires credentials for 2 accounts")]
        public async Task ICanReceiveMessagePublishedToTopicInAnotherAccount()
        {
            string publisherAccount = "<enter publisher account id>";
            string subscriberAccount = "<enter subscriber account id>";
            var publishingBus = await GetBus("<enter publisher access key>", "<enter publisher secret key>")

                .WithNamingStrategy(() => new NamingStrategy())
                .ConfigurePublisherWith(cfg => cfg.AdditionalSubscriberAccounts = new List<string> { subscriberAccount })
                .WithSnsMessagePublisher<GenericMessage>()
                .BuildPublisherAsync();

            var handler = Substitute.For<IHandlerAsync<GenericMessage>>();
            handler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            handler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(async x => await _signal.Complete((GenericMessage)x.Args()[0]));

            var subscribingBus = await GetBus("<enter subscriber access key>", "<enter subscriber secret key>")
                .WithNamingStrategy(() => new NamingStrategy())
                .WithSqsTopicSubscriber()
                .IntoQueue("crossaccount")
                .ConfigureSubscriptionWith(cfg => cfg.TopicSourceAccount = publisherAccount)
                .WithMessageHandler(handler)
                .BuildSubscriberAsync();

            subscribingBus.StartListening();

            //Act
            publishingBus.Publish(_message);

            //Assert
            var done = await Tasks.WaitWithTimeoutAsync(_signal.DoneSignal, TimeSpan.FromMinutes(1));
            Assert.That(_signal.HasReceived(_message));

        }

        private IMayWantOptionalSettings GetBus(string accessKey, string secretKey)
        {
            return CreateMeABus
                .WithLogging(new LoggerFactory()).InRegion("eu-west-1").WithAwsClientFactory(()=>new DefaultAwsClientFactory(new BasicAWSCredentials(accessKey, secretKey) ));
        }
    }
    class NamingStrategy : INamingStrategy
    {
        public string GetTopicName(string topicName, string messageType)
        {
            return "test-" + messageType;
        }

        public string GetQueueName(SqsReadConfiguration sqsConfig, string messageType)
        {
            return "test-" + messageType;
        }
    }
}