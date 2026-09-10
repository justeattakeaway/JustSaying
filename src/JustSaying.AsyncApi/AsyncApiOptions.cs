using System.Text.Json;
using ByteBard.AsyncAPI.Models;

namespace JustSaying.AsyncApi;

/// <summary>
/// Configures the AsyncAPI document generated for the messaging bus.
/// </summary>
public sealed class AsyncApiOptions
{
    /// <summary>
    /// Gets or sets the identifier of the application, used for the document's <c>id</c> field.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the application. Defaults to the entry assembly name.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the version of the application API. Defaults to <c>1.0.0</c>.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets a description of the application.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> used to generate message payload
    /// schemas. When <see langword="null"/>, the options are discovered from the configured
    /// message body serialization factory, falling back to the serializer defaults.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets a delegate invoked with the generated document before it is serialized,
    /// allowing arbitrary customization.
    /// </summary>
    public Action<AsyncApiDocument> PostProcess { get; set; }
}
