using System.Diagnostics;
using Amazon;
using CommandLine;
using JustSaying.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Serilog;
using SerilogTimings;

namespace JustSaying.Benchmark;

[Verb("benchmark", HelpText = "Runs a benchmark against an SQS queue to test queue throughput")]
public class JustSayingBenchmark
{
    [Option(HelpText = "The number of messages to send and receive in this test",
        Required = false, Default = 1000)]
    public int MessageCount { get; set; }

    public static async Task RunTest(JustSayingBenchmark options)
    {
        Console.WriteLine("Running benchmark with message count of {0}", options.MessageCount);

        var capture = new MessageMetricCapture(options.MessageCount);

        var services = new ServiceCollection()
            .AddSingleton<IReportConsumerMetric>(capture)
            .AddLogging(lg => lg.AddSerilog());

        RegisterJustSaying(services);

        var provider = services.BuildServiceProvider();
        var publisher = provider.GetService<IMessagePublisher>();

        using (Operation.Time("Executing startup work"))
        {
            await publisher.StartAsync(CancellationToken.None);
            var bus = provider.GetService<IMessagingBus>();
            await bus.StartAsync(CancellationToken.None);
        }

        Console.WriteLine("Completed startup, beginning benchmark");

        var watch = new Stopwatch();

        var taskBatches = Enumerable.Range(0, options.MessageCount).Batch(20)
            .Select(async batch =>
            {
                var messageTasks =
                    batch.Select(id => new BenchmarkMessage(watch.Elapsed, id))
                        .Select(async x => await capture.Sent(x.Id, publisher.PublishAsync(x)));

                await Task.WhenAll(messageTasks);
            }).ToList();

        var batchId = 1;
        var batchCount = taskBatches.Count;
        foreach (var taskBatch in taskBatches)
        {
            using (Operation.Time("Sending batch id {BatchId} of {BatchCount}",
                       batchId,
                       batchCount))
            {
                await taskBatch;
            }
        }

        Log.Information("Waiting for sends to complete...");
        await capture.SendCompleted;

        Log.Information("Waiting for consumes to complete...");
        await capture.ConsumeCompleted;

        Log.Information("Sends and Consumes completed!");

        var messageMetrics = capture.GetMessageMetrics();

        Console.WriteLine("Avg Ack Time: {0:F0}ms",
            messageMetrics.Average(x => x.AckLatency) * 1000 / Stopwatch.Frequency);
        Console.WriteLine("Min Ack Time: {0:F0}ms",
            messageMetrics.Min(x => x.AckLatency) * 1000 / Stopwatch.Frequency);
        Console.WriteLine("Max Ack Time: {0:F0}ms",
            messageMetrics.Max(x => x.AckLatency) * 1000 / Stopwatch.Frequency);
        Console.WriteLine("Med Ack Time: {0:F0}ms",
            messageMetrics.Median(x => x.AckLatency) * 1000 / Stopwatch.Frequency);
        Console.WriteLine("95t Ack Time: {0:F0}ms",
            messageMetrics.Percentile(x => x.AckLatency) * 1000 / Stopwatch.Frequency);

        Console.WriteLine("Avg Consume Time: {0:F0}ms",
            messageMetrics.Average(x => x.ConsumeLatency) * 1000 / Stopwatch.Frequency);
        Console.WriteLine("Min Consume Time: {0:F0}ms",
            messageMetrics.Min(x => x.ConsumeLatency) * 1000 / Stopwatch.Frequency);
        Console.WriteLine("Max Consume Time: {0:F0}ms",
            messageMetrics.Max(x => x.ConsumeLatency) * 1000 / Stopwatch.Frequency);
        Console.WriteLine("Med Consume Time: {0:F0}ms",
            messageMetrics.Median(x => x.ConsumeLatency) * 1000 / Stopwatch.Frequency);
        Console.WriteLine("95t Consume Time: {0:F0}ms",
            messageMetrics.Percentile(x => x.ConsumeLatency) * 1000 / Stopwatch.Frequency);

        DrawResponseTimeGraph(messageMetrics, m => m.ConsumeLatency);
    }

    static void RegisterJustSaying(IServiceCollection services)
    {
        services.AddJustSaying(config =>
        {
            config.Messaging(x => { x.WithRegion(RegionEndpoint.EUWest1); });

            config.Publications(x => { x.WithTopic<BenchmarkMessage>(); });

            config.Subscriptions(x => { x.ForTopic<BenchmarkMessage>(cfg => cfg.WithQueueName("justsaying-benchmark")); });
        });

        services.AddJustSayingHandler<BenchmarkMessage, BenchmarkMessageHander>();
    }

    static void DrawResponseTimeGraph(MessageMetric[] metrics, Func<MessageMetric, long> selector)
    {
        long maxTime = metrics.Max(selector);
        long minTime = metrics.Min(selector);

        const int segments = 10;

        long span = maxTime - minTime;
        long increment = span / segments;

        var histogram = (from x in metrics.Select(selector)
            let key = ((x - minTime) * segments / span)
            where key >= 0 && key < segments
            let groupKey = key
            group x by groupKey
            into segment
            orderby segment.Key
            select new { Value = segment.Key, Count = segment.Count() }).ToList();

        int maxCount = histogram.Max(x => x.Count);

        foreach (var item in histogram)
        {
            int barLength = item.Count * 60 / maxCount;
            Console.WriteLine("{0,5}ms {2,-60} ({1,7})",
                (minTime + increment * item.Value) * 1000 / Stopwatch.Frequency,
                item.Count,
                new string('*', barLength));
        }
    }
}