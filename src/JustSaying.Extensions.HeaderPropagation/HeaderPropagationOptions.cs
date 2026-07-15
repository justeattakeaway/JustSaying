namespace JustSaying.Extensions.HeaderPropagation;

/// <summary>
/// Configuration options for header propagation.
/// </summary>
public sealed class HeaderPropagationOptions
{
    /// <summary>
    /// Initializes a new instance of <see cref="HeaderPropagationOptions"/> with the specified header names.
    /// </summary>
    /// <param name="headers">The HTTP header names to propagate as message attributes.</param>
    public HeaderPropagationOptions(IEnumerable<string> headers)
    {
        Headers = [..headers];
    }

    /// <summary>
    /// Gets the HTTP header names that will be propagated as message attributes.
    /// </summary>
    public IReadOnlyCollection<string> Headers { get; }
}
