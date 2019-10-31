using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface IMessageDispatcher
    {
        Task DispatchMessage(Message message, CancellationToken cancellationToken);
    }
}
