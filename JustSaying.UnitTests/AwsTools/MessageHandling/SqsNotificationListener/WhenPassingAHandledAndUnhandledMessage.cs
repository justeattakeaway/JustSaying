using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenPassingAHandledAndUnhandledMessage : BaseQueuePollingTest
    {
        protected override async Task Given()
        {
            await base.Given();
            Handler.Handle(null)
                .ReturnsForAnyArgs(info => true)
                .AndDoes(x => Thread.Sleep(1)); // Ensure at least one ms wait on processing
        }

        [Fact]
        public void MessagesGetDeserialisedByCorrectHandler()
        {
            SerialisationRegister.Received().DeserializeMessage(
                SqsMessageBody(MessageTypeString));
        }

        [Fact]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserialisedMessage);
        }

        [Fact]
        public void MonitoringToldMessageHandlingTime()
        {
            Monitor.Received().HandleTime(Arg.Is<long>(x => x > 0));
        }

        [Fact]
        public void AllMessagesAreClearedFromQueue()
        {
            SerialisationRegister.Received(2).DeserializeMessage(Arg.Any<string>());

            Sqs.Received().DeleteMessageAsync(Arg.Any<DeleteMessageRequest>());
        }
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. etc
    */
}
