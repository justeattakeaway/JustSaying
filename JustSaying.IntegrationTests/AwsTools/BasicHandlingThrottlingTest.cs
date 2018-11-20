using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class BasicHandlingThrottlingTest
    {
        public BasicHandlingThrottlingTest(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        [AwsTheory]
        [InlineData(1000)]
        public async Task HandlingManyMessages(int throttleMessageCount)
        {
            // Arrange
            var fixture = new JustSayingFixture(OutputHelper);
            var client = fixture.CreateSqsClient();

            var queue = new SqsQueueByName(fixture.Region, fixture.UniqueName, client, 1, fixture.LoggerFactory);

            if (!await queue.ExistsAsync())
            {
                await queue.CreateAsync(new SqsBasicConfiguration());

                if (!JustSayingFixture.IsSimulator)
                {
                    // Wait for up to 60 secs for queue creation to be guaranteed completed by AWS
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2));

                            if (await queue.ExistsAsync())
                            {
                                break;
                            }
                        }
                    }
                }
            }

            Assert.True(await queue.ExistsAsync(), "The queue was not created.");

            OutputHelper.WriteLine($"{DateTime.Now} - Adding {throttleMessageCount} messages to the queue.");

            var entriesAdded = 0;

            // Add some messages
            do
            {
                var entries = new List<SendMessageBatchRequestEntry>();

                for (var j = 0; j < 10; j++)
                {
                    var batchEntry = new SendMessageBatchRequestEntry
                    {
                        MessageBody = $"{{\"Subject\":\"GenericMessage\", \"Message\": \"{entriesAdded}\"}}",
                        Id = Guid.NewGuid().ToString()
                    };

                    entries.Add(batchEntry);
                    entriesAdded++;
                }

                await client.SendMessageBatchAsync(queue.Uri.AbsoluteUri, entries);
            }
            while (entriesAdded < throttleMessageCount);

            OutputHelper.WriteLine($"{DateTime.Now} - Done adding messages.");

            var handleCount = 0;
            var serialisations = Substitute.For<IMessageSerialisationRegister>();
            var monitor = Substitute.For<IMessageMonitor>();
            var handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            handler.Handle(null).ReturnsForAnyArgs(true).AndDoes(_ => Interlocked.Increment(ref handleCount));

            serialisations.DeserializeMessage(string.Empty).ReturnsForAnyArgs(new SimpleMessage());
            var listener = new SqsNotificationListener(queue, serialisations, monitor, fixture.LoggerFactory);
            listener.AddMessageHandler(() => handler);

            // Act
            var stopwatch = Stopwatch.StartNew();

            var listenerCts = new CancellationTokenSource();
            listener.Listen(listenerCts.Token);

            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                do
                {
                    if (!JustSayingFixture.IsSimulator)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }

                    OutputHelper.WriteLine($"{DateTime.Now} - Handled {handleCount} messages. Waiting for completion.");
                }
                while (handleCount < throttleMessageCount && !cts.IsCancellationRequested);
            }

            listenerCts.Cancel();
            stopwatch.Stop();

            OutputHelper.WriteLine($"{DateTime.Now} - Handled {handleCount:N0} messages.");
            OutputHelper.WriteLine($"{DateTime.Now} - Took {stopwatch.ElapsedMilliseconds} ms");
            OutputHelper.WriteLine($"{DateTime.Now} - Throughput {(float)handleCount / stopwatch.ElapsedMilliseconds * 1000} messages/second");

            // Assert
            Assert.Equal(throttleMessageCount, handleCount);
        }
    }
}
