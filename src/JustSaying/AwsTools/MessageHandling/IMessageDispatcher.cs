using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using Message = JustSaying.Models.Message;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface IMessageDispatcher
    {
        Task DispatchMessage(SQSMessage message, CancellationToken cancellationToken);
        bool AddMessageHandler<T>(Func<IHandlerAsync<T>> futureHandler) where T : Message;
    }
}
