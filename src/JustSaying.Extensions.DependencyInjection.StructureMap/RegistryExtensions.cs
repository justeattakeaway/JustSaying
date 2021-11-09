using System.ComponentModel;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace StructureMap;

/// <summary>
/// A class containing extension methods for the <see cref="Registry"/> class. This class cannot be inherited.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RegistryExtensions
{
    /// <summary>
    /// Adds a JustSaying message handler to the registry.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message handled.</typeparam>
    /// <typeparam name="THandler">The type of the message handler to register.</typeparam>
    /// <param name="registry">The <see cref="Registry"/> to add the message handler to.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="registry"/> is <see langword="null"/>.
    /// </exception>
    public static void AddJustSayingHandler<TMessage, THandler>(this Registry registry)
        where TMessage : Message
        where THandler : class, IHandlerAsync<TMessage>
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        registry.For<IHandlerAsync<TMessage>>().Use<THandler>();
    }
}