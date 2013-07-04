using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenPassingAHandledAndUnhandledMessage : BaseQueuePollingTest
    {
        [Then]
        public void MessagesGetDeserialisedByCorrectHandler()
        {
            Serialiser.Received().Deserialise(MessageBody);
        }

        [Then]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Invoke(DeserialisedMessage);
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            Serialiser.Received(1).Deserialise(Arg.Any<string>());
            Sqs.Received(2).DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. Message failed processing does not get deleted
     * 3. etc
    */
}
