using JustSaying.Models;

namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a builder for a publication.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the messages to publish.
    /// </typeparam>
    internal interface IPublicationBuilder<out T>
        where T : Message
    {
        /// <summary>
        /// Configures the publication for the <see cref="JustSayingFluently"/>.
        /// </summary>
        void Configure(JustSayingFluently bus);
    }
}
