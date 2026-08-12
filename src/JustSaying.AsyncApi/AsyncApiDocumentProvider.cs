using ByteBard.AsyncAPI;
using ByteBard.AsyncAPI.Models;
using JustSaying.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi;

/// <summary>
/// The default <see cref="IAsyncApiDocumentProvider"/>. Forces the messaging bus to be built
/// (which populates the metadata registry without starting the bus or provisioning any AWS
/// infrastructure) and serializes the generated document.
/// </summary>
internal sealed class AsyncApiDocumentProvider(IServiceProvider serviceProvider) : IAsyncApiDocumentProvider
{
    internal const string DefaultDocumentName = "asyncapi";

    public IReadOnlyList<string> GetDocumentNames() => [DefaultDocumentName];

    public async Task GenerateAsync(string documentName, TextWriter writer, CancellationToken cancellationToken = default)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (documentName != DefaultDocumentName)
        {
            throw new ArgumentException($"Unknown AsyncAPI document '{documentName}'.", nameof(documentName));
        }

        // Building the publisher and the subscribers runs the fluent builders' configuration,
        // which populates the metadata registry. Neither starts the bus.
        try
        {
            _ = serviceProvider.GetService<IMessagePublisher>();
            _ = serviceProvider.GetService<IMessagingBus>();
        }
        catch (Exception exception) when (exception is InvalidOperationException or HandlerNotRegisteredWithContainerException or NotSupportedException)
        {
            throw new InvalidOperationException(
                "Generating an AsyncAPI document builds the JustSaying messaging bus (without starting it), and building the bus failed. " +
                "A common cause in documentation-only entry points is a subscription whose message handler is not registered; " +
                "handlers must be registered (for example with AddJustSayingHandler) even when only generating a document. " +
                $"The underlying error was: {exception.Message}",
                exception);
        }

        var generator = serviceProvider.GetRequiredService<AsyncApiDocumentGenerator>();
        var document = generator.Generate();

        await writer.WriteAsync(document.SerializeAsJson(AsyncApiVersion.AsyncApi3_0).AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
