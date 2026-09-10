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
            // Lead with the actual error; the bus-build context and the handler hint come after, and the
            // hint only when a missing handler is what failed, so a CloudEvents or naming misconfiguration
            // is not mislabelled as a handler problem.
            string message =
                $"{exception.Message} " +
                "(Generating an AsyncAPI document builds the JustSaying messaging bus, without starting it, and building the bus failed.";

            if (IsMissingHandler(exception))
            {
                message +=
                    " Handlers must be registered, for example with AddJustSayingHandler, even in a documentation-only entry point, " +
                    "because the bus resolves each subscription's handler when it is built.";
            }

            throw new InvalidOperationException(message + ")", exception);
        }

        var generator = serviceProvider.GetRequiredService<AsyncApiDocumentGenerator>();
        var document = generator.Generate();

        await writer.WriteAsync(document.SerializeAsJson(AsyncApiVersion.AsyncApi3_0).AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static bool IsMissingHandler(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is HandlerNotRegisteredWithContainerException ||
                current.Message.Contains("No handler for message type", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
