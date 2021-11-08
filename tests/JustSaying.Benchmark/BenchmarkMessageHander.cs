using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Benchmark
{
    public class BenchmarkMessageHander : IHandlerAsync<BenchmarkMessage>
    {
        private readonly IReportConsumerMetric _reporter;

        public BenchmarkMessageHander(IReportConsumerMetric reporter)
        {
            _reporter = reporter;
        }

        public Task<bool> Handle(BenchmarkMessage message)
        {
            _reporter.Consumed<BenchmarkMessage>(message.Id);
            return Task.FromResult(true);
        }
    }
}
