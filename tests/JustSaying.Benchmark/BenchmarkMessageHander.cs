using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Benchmark;

public class BenchmarkMessageHander(IReportConsumerMetric reporter) : IHandlerAsync<BenchmarkMessage>
{
    private readonly IReportConsumerMetric _reporter = reporter;

    public Task<bool> Handle(BenchmarkMessage message)
    {
        _reporter.Consumed<BenchmarkMessage>(message.Id);
        return Task.FromResult(true);
    }
}
