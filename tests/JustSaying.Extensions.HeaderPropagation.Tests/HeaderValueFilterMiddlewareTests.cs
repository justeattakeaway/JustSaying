using JustSaying.Extensions.HeaderPropagation;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using NSubstitute;
using SqsMessage = Amazon.SQS.Model.Message;

namespace JustSaying.Extensions.HeaderPropagation.Tests;

public class HeaderValueFilterMiddlewareTests
{
    private static HandleMessageContext BuildContext(string? headerName = null, string? headerValue = null)
    {
        var attributes = new Dictionary<string, MessageAttributeValue>();
        if (headerName != null)
        {
            attributes[headerName] = new MessageAttributeValue { StringValue = headerValue, DataType = "String" };
        }

        return new HandleMessageContext(
            "test-queue",
            new SqsMessage(),
            new TestMessage(),
            typeof(TestMessage),
            Substitute.For<IMessageVisibilityUpdater>(),
            Substitute.For<IMessageDeleter>(),
            new Uri("http://test-queue"),
            new MessageAttributes(attributes));
    }

    [Test]
    public async Task WhenHeaderMatchesExpectedValue_FuncIsInvoked()
    {
        var middleware = new HeaderValueFilterMiddleware("x-feature-branch", "my-branch");
        var context = BuildContext("x-feature-branch", "my-branch");

        var invoked = false;
        var result = await middleware.RunAsync(context, ct => { invoked = true; return Task.FromResult(true); }, CancellationToken.None);

        invoked.ShouldBeTrue();
        result.ShouldBeTrue();
    }

    [Test]
    public async Task WhenHeaderDoesNotMatchExpectedValue_FuncIsNotInvokedAndReturnsTrue()
    {
        var middleware = new HeaderValueFilterMiddleware("x-feature-branch", "my-branch");
        var context = BuildContext("x-feature-branch", "other-branch");

        var invoked = false;
        var result = await middleware.RunAsync(context, ct => { invoked = true; return Task.FromResult(true); }, CancellationToken.None);

        invoked.ShouldBeFalse();
        result.ShouldBeTrue();
    }

    [Test]
    public async Task WhenExpectedValueIsNull_AndAttributeAbsent_FuncIsInvoked()
    {
        var middleware = new HeaderValueFilterMiddleware("x-feature-branch", null);
        var context = BuildContext(); // no attribute

        var invoked = false;
        var result = await middleware.RunAsync(context, ct => { invoked = true; return Task.FromResult(true); }, CancellationToken.None);

        invoked.ShouldBeTrue();
        result.ShouldBeTrue();
    }

    [Test]
    public async Task WhenExpectedValueIsNull_AndAttributePresent_FuncIsNotInvoked()
    {
        var middleware = new HeaderValueFilterMiddleware("x-feature-branch", null);
        var context = BuildContext("x-feature-branch", "some-branch");

        var invoked = false;
        var result = await middleware.RunAsync(context, ct => { invoked = true; return Task.FromResult(true); }, CancellationToken.None);

        invoked.ShouldBeFalse();
        result.ShouldBeTrue();
    }

    [Test]
    public async Task ComparisonIsCaseSensitive_DifferentCaseDoesNotMatch()
    {
        var middleware = new HeaderValueFilterMiddleware("x-feature-branch", "Feature-A");
        var context = BuildContext("x-feature-branch", "feature-a");

        var invoked = false;
        await middleware.RunAsync(context, ct => { invoked = true; return Task.FromResult(true); }, CancellationToken.None);

        invoked.ShouldBeFalse();
    }

    private sealed class TestMessage : Message;
}
