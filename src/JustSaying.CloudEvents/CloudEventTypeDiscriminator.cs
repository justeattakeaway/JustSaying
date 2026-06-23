using System.Text.Json;
using JustSaying.Messaging;

namespace JustSaying.CloudEvents;

/// <summary>
/// An <see cref="IMessageTypeDiscriminator"/> that reads the CloudEvents <c>type</c> attribute from a
/// structured-mode CloudEvents envelope, so a queue carrying several CloudEvents types can route each
/// message to the handler for its own type.
/// </summary>
public sealed class CloudEventTypeDiscriminator : IMessageTypeDiscriminator
{
    /// <inheritdoc />
    public bool TryGetMessageTypeName(MessageDiscriminationContext context, out string typeName)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        typeName = null;

        if (string.IsNullOrEmpty(context.Body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(context.Body);

            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("type", out var typeElement)
                && typeElement.ValueKind == JsonValueKind.String)
            {
                typeName = typeElement.GetString();
                return !string.IsNullOrEmpty(typeName);
            }
        }
        catch (JsonException)
        {
            // Not a (JSON) CloudEvent; let another discriminator in the chain try.
        }

        return false;
    }
}
