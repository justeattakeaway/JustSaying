using JustSaying.Models;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

public static class HandleMessageContextExtensions
{
    /// <summary>
    /// A convenience method to get a message as <typeparamref name="TMessage"/> from the context.
    /// </summary>
    /// <param name="context">The context to get the message from.</param>
    /// <typeparam name="TMessage">The type of the message to try and get from the context.</typeparam>
    /// <returns>An instance of <typeparamref name="TMessage"/> or <see langword="null"/> if the message was not of type <typeparamref name="TMessage"/></returns>
    /// <exception cref="ArgumentNullException">The <see cref="context"/> object is <see langword="null"/>.</exception>
    public static TMessage MessageAs<TMessage>(this HandleMessageContext context) where TMessage : Message
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        return context.Message as TMessage;
    }
}