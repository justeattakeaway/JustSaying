using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using Microsoft.AspNetCore.Http;

namespace JustSaying.Extensions.HeaderPropagation;

/// <summary>
/// An <see cref="IMessagePublisher"/> decorator that propagates HTTP request headers as SNS/SQS message attributes.
/// </summary>
public sealed class HeaderPropagationPublisher : IMessagePublisher, IMessageBatchPublisher
{
    private readonly IMessagePublisher _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HeaderPropagationOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="HeaderPropagationPublisher"/>.
    /// </summary>
    /// <param name="inner">The inner publisher to delegate to.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor used to read the current request headers.</param>
    /// <param name="options">The header propagation options.</param>
    public HeaderPropagationPublisher(
        IMessagePublisher inner,
        IHttpContextAccessor httpContextAccessor,
        HeaderPropagationOptions options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public InterrogationResult Interrogate() => _inner.Interrogate();

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken stoppingToken) => _inner.StartAsync(stoppingToken);

    /// <inheritdoc/>
    public Task PublishAsync(Message message, CancellationToken cancellationToken)
        => PublishAsync(message, new PublishMetadata(), cancellationToken);

    /// <inheritdoc/>
    public Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        metadata ??= new PublishMetadata();
        InjectHeaders(metadata);
        return _inner.PublishAsync(message, metadata, cancellationToken);
    }

    /// <inheritdoc/>
    public Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata? metadata, CancellationToken cancellationToken)
    {
        if (_inner is not IMessageBatchPublisher batchPublisher)
            throw new NotSupportedException("The inner publisher does not support batch publishing.");

        metadata ??= new PublishBatchMetadata();
        InjectHeaders(metadata);
        return batchPublisher.PublishAsync(messages, metadata, cancellationToken);
    }

    private void InjectHeaders(PublishMetadata metadata)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        foreach (var header in _options.Headers)
        {
            if (httpContext.Request.Headers.TryGetValue(header, out var value))
                metadata.AddMessageAttribute(header, value.ToString());
        }
    }
}
