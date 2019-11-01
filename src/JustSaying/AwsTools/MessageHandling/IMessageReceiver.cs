using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface IMessageReceiver
    {
        Task<ReceiveMessageResponse> GetMessages(int maxNumberOfMessages, CancellationToken ct);
        string QueueName { get; }
        string Region { get; }
    }
}
