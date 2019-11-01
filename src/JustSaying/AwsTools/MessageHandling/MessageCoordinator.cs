using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageProcessingStrategies;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    internal class MessageCoordinator : IMessageCoordinator
    {
        private readonly ILogger _log;
        private readonly IMessageReceiver _messageReceiver;
        private readonly IMessageDispatcher _messageDispatcher;
        private IMessageProcessingStrategy _messageProcessingStrategy;

        public MessageCoordinator(
            ILogger log,
            IMessageReceiver messageReceiver,
            IMessageDispatcher messageDispatcher,
            IMessageProcessingStrategy messageProcessingStrategy)
        {
            _log = log;
            _messageReceiver = messageReceiver;
            _messageDispatcher = messageDispatcher;
            _messageProcessingStrategy = messageProcessingStrategy;
        }

        public string QueueName => _messageReceiver.QueueName;
        public string Region => _messageReceiver.Region;

        public void WithMessageProcessingStrategy(IMessageProcessingStrategy messageProcessingStrategy)
        {
            _messageProcessingStrategy = messageProcessingStrategy;
        }

        public async Task ListenAsync(CancellationToken cancellationToken)
        {
            await ListenLoopAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task ListenLoopAsync(CancellationToken ct)
        {
            ReceiveMessageResponse sqsMessageResponse = null;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    sqsMessageResponse = await GetMessagesFromSqsQueueAsync(ct).ConfigureAwait(false);

                    int messageCount = sqsMessageResponse?.Messages?.Count ?? 0;

                    _log.LogTrace(
                        "Polled for messages on queue '{QueueName}' in region '{Region}', and received {MessageCount} messages.",
                        _messageReceiver.QueueName,
                        _messageReceiver.Region,
                        messageCount);
                }
                catch (InvalidOperationException ex)
                {
                    _log.LogTrace(
                        ex,
                        "Could not determine number of messages to read from queue '{QueueName}' in '{Region}'.",
                        _messageReceiver.QueueName,
                        _messageReceiver.Region);
                }
                catch (OperationCanceledException ex)
                {
                    _log.LogTrace(
                        ex,
                        "Suspected no message on queue '{QueueName}' in region '{Region}'.",
                        _messageReceiver.QueueName,
                        _messageReceiver.Region);
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _log.LogError(
                        ex,
                        "Error receiving messages on queue '{QueueName}' in region '{Region}'.",
                        _messageReceiver.QueueName,
                        _messageReceiver.Region);
                }

                if (sqsMessageResponse == null || sqsMessageResponse.Messages.Count < 1)
                {
                    continue;
                }

                try
                {
                    foreach (var message in sqsMessageResponse.Messages)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }

                        if (!await TryHandleMessageAsync(message, ct).ConfigureAwait(false))
                        {
                            // No worker free to process any messages
                            _log.LogWarning(
                                "Unable to process message with Id {MessageId} for queue '{QueueName}' in region '{Region}' as no workers are available.",
                                message.MessageId,
                                _messageReceiver.QueueName,
                        _messageReceiver.Region);
                        }
                    }
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _log.LogError(
                        ex,
                        "Error in message handling loop for queue '{QueueName}' in region '{Region}'.",
                        _messageReceiver.QueueName,
                        _messageReceiver.Region);
                }
            }
        }

        private async Task<ReceiveMessageResponse> GetMessagesFromSqsQueueAsync(CancellationToken ct)
        {
            int maxNumberOfMessages = await GetDesiredNumberOfMessagesToRequestFromSqsAsync()
                .ConfigureAwait(false);

            if (maxNumberOfMessages < 1)
            {
                return null;
            }

            var sqsMessageResponse = await _messageReceiver.GetMessages(maxNumberOfMessages, ct).ConfigureAwait(false);

            return sqsMessageResponse;
        }

        private async Task<int> GetDesiredNumberOfMessagesToRequestFromSqsAsync()
        {
            int maximumMessagesFromAws = MessageConstants.MaxAmazonMessageCap;
            int maximumWorkers = _messageProcessingStrategy.MaxConcurrency;

            int messagesToRequest = Math.Min(maximumWorkers, maximumMessagesFromAws);

            if (messagesToRequest < 1)
            {
                // Wait for the strategy to have at least one worker available
                int availableWorkers = await _messageProcessingStrategy.WaitForAvailableWorkerAsync().ConfigureAwait(false);

                messagesToRequest = Math.Min(availableWorkers, maximumMessagesFromAws);
            }

            if (messagesToRequest < 1)
            {
                _log.LogWarning("No workers are available to process SQS messages.");
                messagesToRequest = 0;
            }

            return messagesToRequest;
        }

        private Task<bool> TryHandleMessageAsync(Amazon.SQS.Model.Message message, CancellationToken ct)
        {
            async Task DispatchAsync()
            {
                await _messageDispatcher.DispatchMessage(message, ct).ConfigureAwait(false);
            }

            return _messageProcessingStrategy.StartWorkerAsync(DispatchAsync, ct);
        }
    }
}
