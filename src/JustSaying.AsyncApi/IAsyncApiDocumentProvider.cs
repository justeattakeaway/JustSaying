namespace JustSaying.AsyncApi;

/// <summary>
/// Provides serialized AsyncAPI documents for the application.
/// </summary>
/// <remarks>
/// This interface is resolved by name by external tooling (analogous to how ASP.NET Core's
/// build-time OpenAPI generation resolves <c>Microsoft.Extensions.ApiDescriptions.IDocumentProvider</c>),
/// so its full name and member signatures must remain stable.
/// </remarks>
public interface IAsyncApiDocumentProvider
{
    /// <summary>
    /// Gets the names of the documents that can be generated.
    /// </summary>
    /// <returns>The available document names.</returns>
    IReadOnlyList<string> GetDocumentNames();

    /// <summary>
    /// Generates the specified document as AsyncAPI 3.0 JSON and writes it to the writer.
    /// </summary>
    /// <param name="documentName">The name of the document to generate.</param>
    /// <param name="writer">The writer to write the document to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the document has been written.</returns>
    Task GenerateAsync(string documentName, TextWriter writer, CancellationToken cancellationToken = default);
}
