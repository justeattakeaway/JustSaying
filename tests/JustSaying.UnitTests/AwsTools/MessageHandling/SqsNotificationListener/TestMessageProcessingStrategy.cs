using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class TestMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public int MaxConcurrency => 1;

        public async Task<bool> StartWorkerAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            await action();
            return true;
        }

        public Task<int> WaitForAvailableWorkerAsync()
        {
            return Task.FromResult(MaxConcurrency);
        }

        public Task ReportMessageReceived(bool success)
        {
            return Task.CompletedTask;
        }
    }
}
