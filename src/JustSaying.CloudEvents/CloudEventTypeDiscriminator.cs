using System.Text.Json;
using JustSaying.Messaging;

namespace JustSaying.CloudEvents;

/// <summary>
/// An <see cref="IMessageTypeDiscriminator"/> that reads the CloudEvents <c>type</c> attribute from a
/// structured-mode CloudEvents envelope, so a queue carrying several CloudEvents types can route each
/// message to the handler for its own type. A body is only treated as a CloudEvent when it carries all
/// of the required CloudEvents context attributes (<c>specversion</c>, <c>id</c>, <c>source</c> and
/// <c>type</c>), so a native JustSaying payload that happens to have its own <c>type</c> member is left
/// to the rest of the discriminator chain — the queue's routing does not depend on registration order.
/// </summary>
public sealed class CloudEventTypeDiscriminator : IMessageTypeDiscriminator
{
    // The attributes CloudEvents 1.0 requires on every event, minus `type` (read out below).
    private static readonly string[] RequiredAttributes = ["specversion", "id", "source"];

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
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var attribute in RequiredAttributes)
            {
                if (!HasNonEmptyString(root, attribute))
                {
                    return false;
                }
            }

            if (HasNonEmptyString(root, "type"))
            {
                typeName = root.GetProperty("type").GetString();
                return true;
            }
        }
        catch (JsonException)
        {
            // Not a (JSON) CloudEvent; let another discriminator in the chain try.
        }

        return false;
    }

    private static bool HasNonEmptyString(JsonElement root, string name)
        => root.TryGetProperty(name, out var element)
           && element.ValueKind == JsonValueKind.String
           && !string.IsNullOrEmpty(element.GetString());
}
