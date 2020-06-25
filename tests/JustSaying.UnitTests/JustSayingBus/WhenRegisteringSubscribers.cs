using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling;
using NSubstitute;
using Shouldly;
using Xunit;
using Message = Amazon.SQS.Model.Message;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringSubscribers : GivenAServiceBus
    {
        private ISqsQueue _queue1;
        private ISqsQueue _queue2;
        private IAmazonSQS _client1;
        private IAmazonSQS _client2;

        protected override void Given()
        {
            base.Given();

            _client1 = CreateSubstituteClient();
            _client2 = CreateSubstituteClient();

            _queue1 = Substitute.For<ISqsQueue>();
            _queue1.QueueName.Returns("queue1");
            _queue1.Uri.Returns(new Uri("http://test.com"));

            _queue1.Client.Returns(_client1);

            _queue2 = Substitute.For<ISqsQueue>();
            _queue2.QueueName.Returns("queue2");
            _queue2.Uri.Returns(new Uri("http://test.com"));
            _queue2.Client.Returns(_client2);
        }

        protected override async Task WhenAsync()
        {
            SystemUnderTest.AddMessageHandler(_queue1.QueueName, () => new InspectableHandler<OrderAccepted>());
            SystemUnderTest.AddMessageHandler(_queue1.QueueName, () => new InspectableHandler<OrderRejected>());
            SystemUnderTest.AddMessageHandler(_queue1.QueueName, () => new InspectableHandler<SimpleMessage>());

            SystemUnderTest.AddQueue("region1", typeof(TestMessage).FullName, _queue1);
            SystemUnderTest.AddQueue("region1", typeof(TestMessage).FullName, _queue2);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

           await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SystemUnderTest.StartAsync(cts.Token));
        }

        [Fact]
        public async Task SubscribersStartedUp()
        {
            await _client1.Received().ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>());
            await _client2.Received().ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void AndInterrogationShowsPublishersHaveBeenSet()
        {
            var response = SystemUnderTest.WhatDoIHave();

            response.Subscribers.Count().ShouldBe(3);
            response.Subscribers.First(x => x.MessageType == typeof(OrderAccepted)).ShouldNotBe(null);
            response.Subscribers.First(x => x.MessageType == typeof(OrderRejected)).ShouldNotBe(null);
            response.Subscribers.First(x => x.MessageType == typeof(SimpleMessage)).ShouldNotBe(null);
        }

        private static IAmazonSQS CreateSubstituteClient()
        {
            var client = Substitute.For<IAmazonSQS>();
            client
                .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(new ReceiveMessageResponse
                {
                    Messages = new List<Message>
                    {
                        new TestMessage(),
                    }
                });

            return client;
        }

        private class TestMessage : Message
        {
        }
    }
}
