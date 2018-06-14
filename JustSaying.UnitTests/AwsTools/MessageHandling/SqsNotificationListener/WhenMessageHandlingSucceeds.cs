using System;
using Amazon.SQS.Model;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenMessageHandlingSucceeds : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(true);
        }

        [Fact]
        public void MessagesGetDeserialisedByCorrectHandler()
        {
            SerialisationRegister.Received().DeserializeMessage(SqsMessageBody(MessageTypeString));
        }

        [Fact]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserialisedMessage);
        }

        [Fact]
        public void AllMessagesAreClearedFromQueue()
        {
            Sqs.Received(2).DeleteMessageAsync(Arg.Any<DeleteMessageRequest>());
        }

        [Fact]
        public void ReceiveMessageTimeStatsSent()
        {
            Monitor.Received().ReceiveMessageTime(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void ExceptionIsNotLoggedToMonitor()
        {
            Monitor.DidNotReceiveWithAnyArgs().HandleException(Arg.Any<Type>());
        }
    }
}
