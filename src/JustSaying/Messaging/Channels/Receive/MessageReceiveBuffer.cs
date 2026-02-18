using System.Diagnostics;
using System.Threading.Channels;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Receive;

internal class MessageReceiveBuffer : IMessageReceiveBuffer
{
    private static readonly TimeSpan PauseReceivingBusyWaitDelay = TimeSpan.FromMilliseconds(100);

    private readonly Channel<IQueueMessageContext> _channel;
    private readonly int _prefetch;
    private readonly int _bufferSize;
    private readonly TimeSpan _readTimeout;
    private readonly TimeSpan _sqsWaitTime;
    private readonly SqsQueueReader _sqsQueueReader;
    private readonly MiddlewareBase<ReceiveMessagesContext, IList<Message>> _sqsMiddleware;
    private readonly IMessageReceivePauseSignal _messageReceivePauseSignal;
    private readonly IMessageMonitor _monitor;
    private readonly ILogger _logger;

    private readonly HashSet<string> _requestMessageAttributeNames = new();

    public ChannelReader<IQueueMessageContext> Reader => _channel.Reader;

    public string QueueName => _sqsQueueReader.QueueName;

    public MessageReceiveBuffer(
        int prefetch,
        int bufferSize,
        TimeSpan readTimeout,
        TimeSpan sqsWaitTime,
        SqsSource sqsSource,
        MiddlewareBase<ReceiveMessagesContext, IList<Message>> sqsMiddleware,
        IMessageReceivePauseSignal messageReceivePauseSignal,
        IMessageMonitor monitor,
        ILogger<IMessageReceiveBuffer> logger)
    {
        _prefetch = prefetch;
        _bufferSize = bufferSize;
        _readTimeout = readTimeout;
        _sqsWaitTime = sqsWaitTime;
        if (sqsSource == null) throw new ArgumentNullException(nameof(sqsSource));
        _sqsQueueReader = new SqsQueueReader(sqsSource.SqsQueue, sqsSource.MessageConverter);
        _sqsMiddleware = sqsMiddleware ?? throw new ArgumentNullException(nameof(sqsMiddleware));
        _messageReceivePauseSignal = messageReceivePauseSignal;
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = Channel.CreateBounded<IQueueMessageContext>(bufferSize);

        _requestMessageAttributeNames.Add(MessageSystemAttributeName.ApproximateReceiveCount);
    }

    /// <summary>
    /// Starts the receive buffer until it's cancelled by the stopping token.
    /// </summary>
    /// <param name="stoppingToken">A <see cref="CancellationToken"/> that will stop the buffer when signalled.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation to receive the messages.</returns>
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        ChannelWriter<IQueueMessageContext> writer = _channel.Writer;
        try
        {
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (_messageReceivePauseSignal?.IsPaused == true)
                {
                    await Task.Delay(PauseReceivingBusyWaitDelay, stoppingToken);

                    continue;
                }

                using (_monitor.MeasureThrottle())
                {
                    bool canWrite = await writer.WaitToWriteAsync(stoppingToken).ConfigureAwait(false);
                    if (!canWrite)
                    {
                        break;
                    }
                }

                IList<Message> messages;
                var receiveWatch = Stopwatch.StartNew();
                using (_monitor.MeasureReceive(_sqsQueueReader.QueueName, _sqsQueueReader.RegionSystemName))
                {
                    messages = await GetMessagesAsync(_prefetch, stoppingToken).ConfigureAwait(false);
                }

                receiveWatch.Stop();
                JustSayingDiagnostics.ClientOperationDuration.Record(
                    receiveWatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object>("messaging.operation.type", "receive"));
                if (messages is not null)
                {
                    JustSayingDiagnostics.MessagesReceived.Add(messages.Count);
                }

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Downloaded {MessageCount} messages from queue {QueueName}.", messages?.Count ?? 0, _sqsQueueReader.QueueName);
                }

                if (messages is null)
                {
                    continue;
                }

                foreach (Message message in messages)
                {
                    IQueueMessageContext messageContext = _sqsQueueReader.ToMessageContext(message);

                    await writer.WriteAsync(messageContext, stoppingToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Receive buffer for queue {QueueName} has completed, shutting down channel",
                _sqsQueueReader.Uri);
            writer.Complete();
        }
    }

    private async Task<IList<Message>> GetMessagesAsync(int count, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        using var receiveTimeout = new CancellationTokenSource(_readTimeout);
        IList<Message> messages;

        try
        {
            using var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, receiveTimeout.Token);

            var context = new ReceiveMessagesContext
            {
                Count = count,
                QueueName = _sqsQueueReader.QueueName,
                RegionName = _sqsQueueReader.RegionSystemName,
            };

            messages = await _sqsMiddleware.RunAsync(context,
                    async ct =>
                        await _sqsQueueReader
                            .GetMessagesAsync(count, _sqsWaitTime, _requestMessageAttributeNames.ToList(), ct)
                            .ConfigureAwait(false),
                    linkedCts.Token)
                .ConfigureAwait(false);
        }
        finally
        {
            if (receiveTimeout.Token.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Timed out while receiving messages from queue '{QueueName}' in region '{Region}'.",
                    _sqsQueueReader.QueueName,
                    _sqsQueueReader.RegionSystemName);
            }
        }

        return messages;
    }

    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            BufferSize = _bufferSize,
            _sqsQueueReader.QueueName,
            Region = _sqsQueueReader.RegionSystemName,
            Prefetch = _prefetch,
        });
    }
}
