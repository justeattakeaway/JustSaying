using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenThereAreExceptionsInMessageProcessing : XAsyncBehaviourTest<JustSaying.AwsTools.MessageHandling.SqsNotificationListener>
    {
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private readonly IMessageSerializationRegister _serializationRegister =
            Substitute.For<IMessageSerializationRegister>();

        private int _callCount;

        protected override Task<JustSaying.AwsTools.MessageHandling.SqsNotificationListener> CreateSystemUnderTestAsync()
        {
            var listener = new JustSaying.AwsTools.MessageHandling.SqsNotificationListener(
                new SqsQueueByUrl(RegionEndpoint.EUWest1, new Uri("http://foo.com"), _sqs),
                _serializationRegister,
                Substitute.For<IMessageMonitor>(),
                Substitute.For<ILoggerFactory>(),
                Substitute.For<IMessageContextAccessor>());

            return Task.FromResult(listener);
        }

        protected override Task Given()
        {
            _serializationRegister
                .DeserializeMessage(Arg.Any<string>())
                .Returns(x => throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing"));
            _sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(GenerateEmptyMessage()));

            _sqs.When(x => x.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>()))
                .Do(x => _callCount++);
            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);
            await Task.Delay(100);
            cts.Cancel();
        }

        [Fact]
        public void TheListenerDoesNotDie()
        {
            _callCount.ShouldBeGreaterThanOrEqualTo(3);
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse
            {
                Messages = new List<Message>()
            };
        }
    }
}
