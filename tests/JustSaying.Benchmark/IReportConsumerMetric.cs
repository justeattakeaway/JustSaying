using System;
using System.Threading.Tasks;

namespace JustSaying.Benchmark
{
    public interface IReportConsumerMetric
    {
        Task Consumed<T>(Guid messageId)
            where T : class;

        Task Sent(Guid messageId, Task sendTask);
    }
}
