namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// Provides access to the current Kafka message context using AsyncLocal.
/// This allows the context to flow across async/await boundaries.
/// </summary>
public class KafkaMessageContextAccessor : IKafkaMessageContextAccessor
{
    private static readonly AsyncLocal<KafkaMessageContextHolder> _contextCurrent = new();

    /// <inheritdoc />
    public KafkaMessageContext Context
    {
        get => _contextCurrent.Value?.Context;
        set
        {
            var holder = _contextCurrent.Value;
            if (holder != null)
            {
                // Clear current context trapped in the AsyncLocals, as it's done.
                holder.Context = null;
            }

            if (value != null)
            {
                // Use a holder to keep the context across await points
                _contextCurrent.Value = new KafkaMessageContextHolder { Context = value };
            }
        }
    }


    /// <summary>
    /// Holder class to ensure proper async flow.
    /// </summary>
    private class KafkaMessageContextHolder
    {
        public KafkaMessageContext Context;
    }
}

