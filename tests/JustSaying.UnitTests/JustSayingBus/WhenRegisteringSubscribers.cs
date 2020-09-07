using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Message = Amazon.SQS.Model.Message;

namespace JustSaying.UnitTests.JustSayingBus
{
    public sealed class WhenRegisteringSubscribers : GivenAServiceBus, IDisposable
    {
        private ISqsQueue _queue1;
        private ISqsQueue _queue2;
        private FakeAmazonSqs _client1;
        private FakeAmazonSqs _client2;
        private CancellationTokenSource _cts;

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
            SystemUnderTest.AddMessageHandler(_queue1.QueueName,
                () => new InspectableHandler<OrderAccepted>());
            SystemUnderTest.AddMessageHandler(_queue1.QueueName,
                () => new InspectableHandler<OrderRejected>());
            SystemUnderTest.AddMessageHandler(_queue1.QueueName,
                () => new InspectableHandler<SimpleMessage>());

            SystemUnderTest.AddQueue("groupA", _queue1);
            SystemUnderTest.AddQueue("groupB", _queue2);

            _cts = new CancellationTokenSource();
            _cts.CancelAfter(TimeSpan.FromSeconds(5));

            await SystemUnderTest.StartAsync(_cts.Token);
        }

        [Fact]
        public async Task SubscribersStartedUp()
        {
            await _cts.Token.WaitForCancellation();

            _client1.ReceiveMessageRequests.Count.ShouldBeGreaterThan(0);
            _client2.ReceiveMessageRequests.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void AndInterrogationShowsPublishersHaveBeenSet()
        {
            dynamic response = SystemUnderTest.Interrogate();

            string[] handledTypes = response.Data.HandledMessageTypes;

            handledTypes.ShouldBe(new[]
            {
                typeof(OrderAccepted).FullName,
                typeof(OrderRejected).FullName,
                typeof(SimpleMessage).FullName
            });
        }

        private static FakeAmazonSqs CreateSubstituteClient()
        {
            return new FakeAmazonSqs(() => new[] { new ReceiveMessageResponse()
            {
                Messages = new List<Message>()
                {
                    new TestMessage()
                }
            }, });
        }

        private class TestMessage : Message
        {
            public TestMessage()
            {
                Body = "TestMesage";
            }
        }

        public void Dispose()
        {
            _client1?.Dispose();
            _client2?.Dispose();
            _cts?.Dispose();
        }

        public WhenRegisteringSubscribers(ITestOutputHelper outputHelper) : base(outputHelper)
        { }
    }
}
