using System.Threading.Channels;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Receive;

internal class MessageReceiveBuffer : IMessageReceiveBuffer
{
    private readonly Channel<IQueueMessageContext> _channel;
    private readonly int _prefetch;
    private readonly int _bufferSize;
    private readonly TimeSpan _readTimeout;
    private readonly TimeSpan _sqsWaitTime;
    private readonly SqsQueueReader _sqsQueueReader;
    private readonly MiddlewareBase<ReceiveMessagesContext, IList<Message>> _sqsMiddleware;
    private readonly IMessageReceiveStatusSetter _messageReceiveStatusSetter;
    private readonly TimeSpan _notReceivingBusyWaitInterval;
    private readonly IMessageMonitor _monitor;
    private readonly ILogger _logger;

    private readonly HashSet<string> _requestMessageAttributeNames = new HashSet<string>();

    public ChannelReader<IQueueMessageContext> Reader => _channel.Reader;

    public string QueueName => _sqsQueueReader.QueueName;

    public MessageReceiveBuffer(
        int prefetch,
        int bufferSize,
        TimeSpan readTimeout,
        TimeSpan sqsWaitTime,
        ISqsQueue sqsQueue,
        MiddlewareBase<ReceiveMessagesContext, IList<Message>> sqsMiddleware,
        IMessageReceiveStatusSetter messageReceiveStatusSetter,
        TimeSpan notReceivingBusyWaitInterval,
        IMessageMonitor monitor,
        ILogger<IMessageReceiveBuffer> logger)
    {
        _prefetch = prefetch;
        _bufferSize = bufferSize;
        _readTimeout = readTimeout;
        _sqsWaitTime = sqsWaitTime;
        if (sqsQueue == null) throw new ArgumentNullException(nameof(sqsQueue));
        _sqsQueueReader = new SqsQueueReader(sqsQueue);
        _sqsMiddleware = sqsMiddleware ?? throw new ArgumentNullException(nameof(sqsMiddleware));
        _messageReceiveStatusSetter = messageReceiveStatusSetter ?? throw new ArgumentNullException(nameof(messageReceiveStatusSetter));
        _notReceivingBusyWaitInterval = notReceivingBusyWaitInterval;
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

                await CheckMessageReceiveStatus(stoppingToken);

                using (_monitor.MeasureThrottle())
                {
                    bool canWrite = await writer.WaitToWriteAsync(stoppingToken).ConfigureAwait(false);
                    if (!canWrite)
                    {
                        break;
                    }
                }

                IList<Message> messages;
                using (_monitor.MeasureReceive(_sqsQueueReader.QueueName, _sqsQueueReader.RegionSystemName))
                {
                    messages = await GetMessagesAsync(_prefetch, stoppingToken).ConfigureAwait(false);

                    if (messages == null)
                    {
                        continue;
                    }
                }

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Downloaded {MessageCount} messages from queue {QueueName}.", messages.Count, _sqsQueueReader.QueueName);
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

            _requestMessageAttributeNames.Add("content");

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

    private async Task CheckMessageReceiveStatus(CancellationToken stoppingToken)
    {
        if (_messageReceiveStatusSetter.Status.Equals(MessageReceiveStatus.NotReceiving))
        {
            _logger.LogInformation("Paused listening for messages from queue '{QueueName}'.", QueueName);
            while (true)
            {
                if (!_messageReceiveStatusSetter.Status.Equals(MessageReceiveStatus.Receiving))
                {
                    _logger.LogInformation("Started listening for messages from queue '{QueueName}' after pausing.", QueueName);
                    break;
                }
                // Delay to decrease CPU usage while polling
                await Task.Delay(_notReceivingBusyWaitInterval, stoppingToken);
            }
        }
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
