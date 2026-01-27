namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// Provides access to the current Kafka message context.
/// Use this interface to access detailed message metadata in handlers.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// public class OrderEventHandler : IHandlerAsync&lt;OrderEvent&gt;
/// {
///     private readonly IKafkaMessageContextAccessor _contextAccessor;
///     
///     public OrderEventHandler(IKafkaMessageContextAccessor contextAccessor)
///     {
///         _contextAccessor = contextAccessor;
///     }
///     
///     public Task&lt;bool&gt; Handle(OrderEvent message)
///     {
///         var ctx = _contextAccessor.Context;
///         _logger.LogInformation(
///             "Processing from partition {Partition} with lag {Lag}ms",
///             ctx.Partition, ctx.LagMilliseconds);
///         
///         return Task.FromResult(true);
///     }
/// }
/// </code>
/// </remarks>
public interface IKafkaMessageContextAccessor
{
    /// <summary>
    /// Gets or sets the current Kafka message context.
    /// Returns null if not currently processing a Kafka message.
    /// </summary>
    KafkaMessageContext Context { get; set; }
}

