using JustSaying.Messaging.Interrogation;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

namespace JustSaying.AwsTools.MessageHandling.Dispatch;

/// <summary>
/// A <see cref="MiddlewareMap"/> is a register of middlewares keyed by type and queue. Calling <see cref="Add{T}"/>
/// with a queue name, type, and middleware will cause the middleware to be called when a message matching the type
/// arrives in the queue.
/// </summary>
public sealed class MiddlewareMap : IInterrogable
{
    private readonly Dictionary<(string queueName, Type type), HandleMessageMiddleware> _middlewares = new();

    /// <summary>
    /// Checks if a middleware has been added for a given queue and message type.
    /// </summary>
    /// <param name="queueName">The queue name to register the middleware for.</param>
    /// <param name="messageType">The type of message to handle for this queue.</param>
    /// <returns>Returns true if the middleware has been registered for the queue.</returns>
    public bool Contains(string queueName, Type messageType)
    {
        if (queueName is null) throw new ArgumentNullException(nameof(queueName));
        if (messageType is null) throw new ArgumentNullException(nameof(messageType));

        return _middlewares.ContainsKey((queueName, messageType));
    }

    /// <summary>
    /// Gets a unique list of types that are handled by all queues.
    /// </summary>
    public IEnumerable<Type> Types
    {
        get
        {
            var types = new HashSet<Type>();
            foreach ((var _, Type type) in _middlewares.Keys)
            {
                types.Add(type);
            }

            return types;
        }
    }

    /// <summary>
    /// Adds a middleware chain to be executed when a message arrives in a queue.
    /// If the middleware is already registered for a queue, it will not be added again.
    /// </summary>
    /// <typeparam name="T">The type of the message to handle on this queue.</typeparam>
    /// <param name="queueName">The queue to register the middleware for.</param>
    /// <param name="middleware">The factory function to create middleware with.</param>
    public MiddlewareMap Add<T>(string queueName, HandleMessageMiddleware middleware)
    {
        if (queueName is null) throw new ArgumentNullException(nameof(queueName));
        if (middleware is null) throw new ArgumentNullException(nameof(middleware));

        _middlewares[(queueName, typeof(T))] = middleware;

        return this;
    }

    /// <summary>
    /// Gets a middleware factory for a queue and message type.
    /// </summary>
    /// <param name="queueName">The queue name to get the middleware function for.</param>
    /// <param name="messageType">The message type to get the middleware function for.</param>
    /// <returns>The registered middleware or null.</returns>
    public HandleMessageMiddleware Get(string queueName, Type messageType)
    {
        if (queueName is null) throw new ArgumentNullException(nameof(queueName));
        if (messageType is null) throw new ArgumentNullException(nameof(messageType));

        return _middlewares.TryGetValue((queueName, messageType), out var middleware) ? middleware : null;
    }

    /// <summary>
    /// Gets the middleware to dispatch a message of <paramref name="messageType"/> to on a queue: the one
    /// registered for that exact type, otherwise the one for its closest base class, otherwise the one for
    /// the interface it implements. A message deserialized as a derived type (for example by a
    /// polymorphic serializer for a base type subscription) is then handled by the base type's handler.
    /// </summary>
    /// <returns>The middleware, or null if none matches or more than one interface matches.</returns>
    internal HandleMessageMiddleware GetForMessage(string queueName, Type messageType)
    {
        for (var type = messageType; type != null; type = type.BaseType)
        {
            if (_middlewares.TryGetValue((queueName, type), out var middleware))
            {
                return middleware;
            }
        }

        HandleMessageMiddleware match = null;
        foreach (var entry in _middlewares)
        {
            if (entry.Key.queueName == queueName && entry.Key.type.IsInterface && entry.Key.type.IsAssignableFrom(messageType))
            {
                if (match != null)
                {
                    return null;
                }

                match = entry.Value;
            }
        }

        return match;
    }

    public InterrogationResult Interrogate()
    {
        var middlewares = _middlewares.Select(item =>
            new
            {
                MessageType = item.Key.type.Name,
                QueueName = item.Key.queueName,
                MiddlewareChain = item.Value.Interrogate()
            }).ToList();

        return new InterrogationResult(new
        {
            Middlewares = middlewares
        });
    }
}
