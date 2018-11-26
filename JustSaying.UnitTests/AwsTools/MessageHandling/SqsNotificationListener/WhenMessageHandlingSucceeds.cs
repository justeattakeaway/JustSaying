using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenMessageHandlingSucceeds : BaseQueuePollingTest
    {
        protected override async Task Given()
        {
            await base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(true);
        }

        [Fact]
        public void MessagesGetDeserializedByCorrectHandler()
        {
            SerializationRegister.Received().DeserializeMessage(SqsMessageBody(MessageTypeString));
        }

        [Fact]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserializedMessage);
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
