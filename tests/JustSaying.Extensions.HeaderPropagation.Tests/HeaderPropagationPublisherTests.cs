using JustSaying.Extensions.HeaderPropagation;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace JustSaying.Extensions.HeaderPropagation.Tests;

public class HeaderPropagationPublisherTests
{
    private static (HeaderPropagationPublisher publisher, IMessagePublisher inner, DefaultHttpContext httpContext) Create(params string[] headers)
    {
        var inner = Substitute.For<IMessagePublisher, IMessageBatchPublisher>();
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var options = new HeaderPropagationOptions(headers);
        var publisher = new HeaderPropagationPublisher(inner, accessor, options);
        return (publisher, inner, httpContext);
    }

    private static string? AttributeValue(PublishMetadata? m, string key)
    {
        if (m?.MessageAttributes is not { } attrs) return null;
        attrs.TryGetValue(key, out var val);
        return val?.StringValue;
    }

    [Test]
    public async Task PublishAsync_WithHeaderPresent_InjectsAttributeIntoMetadata()
    {
        var (publisher, inner, httpContext) = Create("x-correlation-id");
        httpContext.Request.Headers["x-correlation-id"] = "abc-123";

        var message = new SimpleTestMessage();
        await publisher.PublishAsync(message, CancellationToken.None);

        await inner.Received(1).PublishAsync(
            message,
            Arg.Is<PublishMetadata>(m => AttributeValue(m, "x-correlation-id") == "abc-123"),
            CancellationToken.None);
    }

    [Test]
    public async Task PublishAsync_WithNullHttpContext_PublishesWithoutAttributes()
    {
        var inner = Substitute.For<IMessagePublisher>();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var publisher = new HeaderPropagationPublisher(inner, accessor, new HeaderPropagationOptions(["x-correlation-id"]));

        var message = new SimpleTestMessage();
        await publisher.PublishAsync(message, CancellationToken.None);

        await inner.Received(1).PublishAsync(
            message,
            Arg.Is<PublishMetadata>(m => AttributeValue(m, "x-correlation-id") == null),
            CancellationToken.None);
    }

    [Test]
    public async Task PublishAsync_HeaderAbsentFromRequest_DoesNotAddAttribute()
    {
        var (publisher, inner, _) = Create("x-correlation-id", "x-feature-branch");
        // neither header is set in the request

        var message = new SimpleTestMessage();
        await publisher.PublishAsync(message, CancellationToken.None);

        await inner.Received(1).PublishAsync(
            message,
            Arg.Is<PublishMetadata>(m => AttributeValue(m, "x-correlation-id") == null),
            CancellationToken.None);
    }

    [Test]
    public async Task PublishAsync_MultipleHeaders_AllPresentHeadersInjected()
    {
        var (publisher, inner, httpContext) = Create("x-correlation-id", "x-feature-branch");
        httpContext.Request.Headers["x-correlation-id"] = "corr-1";
        httpContext.Request.Headers["x-feature-branch"] = "my-branch";

        var message = new SimpleTestMessage();
        await publisher.PublishAsync(message, CancellationToken.None);

        await inner.Received(1).PublishAsync(
            message,
            Arg.Is<PublishMetadata>(m =>
                AttributeValue(m, "x-correlation-id") == "corr-1" &&
                AttributeValue(m, "x-feature-branch") == "my-branch"),
            CancellationToken.None);
    }

    [Test]
    public async Task PublishAsync_WithMetadata_InjectsHeadersIntoProvidedMetadata()
    {
        var (publisher, inner, httpContext) = Create("x-correlation-id");
        httpContext.Request.Headers["x-correlation-id"] = "test-id";

        var message = new SimpleTestMessage();
        var metadata = new PublishMetadata { Delay = TimeSpan.FromSeconds(5) };
        await publisher.PublishAsync(message, metadata, CancellationToken.None);

        await inner.Received(1).PublishAsync(
            message,
            Arg.Is<PublishMetadata>(m =>
                AttributeValue(m, "x-correlation-id") == "test-id" &&
                m!.Delay == TimeSpan.FromSeconds(5)),
            CancellationToken.None);
    }

    [Test]
    public async Task BatchPublishAsync_InnerIsBatchPublisher_InjectsHeaders()
    {
        var (publisher, inner, httpContext) = Create("x-correlation-id");
        httpContext.Request.Headers["x-correlation-id"] = "batch-id";

        var messages = new[] { new SimpleTestMessage() };
        await publisher.PublishAsync(messages, null, default);

        await ((IMessageBatchPublisher)inner).Received(1).PublishAsync(
            messages,
            Arg.Is<PublishBatchMetadata?>(m => m != null && AttributeValue(m, "x-correlation-id") == "batch-id"),
            default);
    }

    [Test]
    public async Task BatchPublishAsync_InnerIsNotBatchPublisher_ThrowsNotSupportedException()
    {
        var inner = Substitute.For<IMessagePublisher>(); // does NOT implement IMessageBatchPublisher
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var publisher = new HeaderPropagationPublisher(inner, accessor, new HeaderPropagationOptions([]));

        await Should.ThrowAsync<NotSupportedException>(
            () => publisher.PublishAsync(Array.Empty<Message>(), null, default));
    }

    [Test]
    public async Task StartAsync_DelegatesToInner()
    {
        var (publisher, inner, _) = Create();
        using var cts = new CancellationTokenSource();

        await publisher.StartAsync(cts.Token);

        await inner.Received(1).StartAsync(cts.Token);
    }

    [Test]
    public void Interrogate_DelegatesToInner()
    {
        var (publisher, inner, _) = Create();
        var expected = new InterrogationResult(new object());
        inner.Interrogate().Returns(expected);

        var result = publisher.Interrogate();

        result.ShouldBeSameAs(expected);
    }

    private sealed class SimpleTestMessage : Message;
}
