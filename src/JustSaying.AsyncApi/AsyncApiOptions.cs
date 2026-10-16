using System.Text.Json;
using System.Text.Json.Nodes;

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
    /// Gets or sets the title of the application. Defaults to the host's application name
    /// (<c>IHostEnvironment.ApplicationName</c>, which is the application's assembly name unless the
    /// host is configured otherwise), or the entry assembly name when no host environment is registered.
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
    /// Gets or sets the URL of the terms of service for the API.
    /// </summary>
    public Uri TermsOfService { get; set; }

    /// <summary>
    /// Gets or sets the contact information for the API.
    /// </summary>
    public AsyncApiContactOptions Contact { get; set; }

    /// <summary>
    /// Gets or sets the license information for the API.
    /// </summary>
    public AsyncApiLicenseOptions License { get; set; }

    /// <summary>
    /// Gets the tags used to group the application logically, written to the document's <c>info</c>.
    /// </summary>
    public IList<AsyncApiTagOptions> Tags { get; } = [];

    /// <summary>
    /// Gets or sets additional external documentation for the application.
    /// </summary>
    public AsyncApiExternalDocsOptions ExternalDocs { get; set; }

    /// <summary>
    /// Gets the servers the application's channels are available on, by name (which must contain
    /// only letters, digits, <c>_</c> and <c>-</c>), for example one per environment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When empty (the default), the document describes the Amazon SNS and SQS endpoints of the regions
    /// the bus's destinations are in, and binds each channel to the endpoint of its own region.
    /// </para>
    /// <para>
    /// When any server is configured, the document's servers are exactly those configured, in the order
    /// they were added, and each channel is bound to the configured servers whose
    /// <see cref="AsyncApiServerOptions.Protocol"/> is its destination's: <c>sns</c> for a topic and
    /// <c>sqs</c> for a queue. A channel that no configured server matches is not bound to any, which in
    /// AsyncAPI means it is available on all of them. Configured servers do not distinguish regions, so
    /// a destination addressed explicitly in another region is bound to the same servers as the rest.
    /// </para>
    /// </remarks>
    public IDictionary<string, AsyncApiServerOptions> Servers { get; } = new Dictionary<string, AsyncApiServerOptions>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> used to generate message payload
    /// schemas. When <see langword="null"/>, each message's schema is generated from the options
    /// of the System.Text.Json serializer its registration uses.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets a delegate invoked with the generated document, as JSON, before it is written,
    /// allowing customization the other options do not cover. When it is set, the document is
    /// written from the modified JSON.
    /// </summary>
    public Action<JsonObject> PostProcess { get; set; }
}
