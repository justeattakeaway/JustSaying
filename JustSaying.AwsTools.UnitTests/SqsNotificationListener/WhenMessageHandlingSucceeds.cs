using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenMessageHandlingSucceeds : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(true);
        }

        [Then]
        public async Task MessagesGetDeserialisedByCorrectHandler()
        {
            await Patiently.VerifyExpectationAsync(
                () => Serialiser.Received().Deserialise(
                    MessageBody, 
                    typeof(GenericMessage)));
        }

        [Then]
        public async Task ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            await Patiently.VerifyExpectationAsync(
                () => Handler.Received().Handle(DeserialisedMessage));
        }

        [Then]
        public async Task AllMessagesAreClearedFromQueue()
        {
            await Patiently.VerifyExpectationAsync(
                () => Sqs.Received(2).DeleteMessage(
                    Arg.Any<DeleteMessageRequest>()));
        }

        [Then]
        public async Task ReceiveMessageTimeStatsSent()
        {
            await Patiently.VerifyExpectationAsync(
                () => Monitor.Received().ReceiveMessageTime(
                    Arg.Any<long>()));
        }
    }
}