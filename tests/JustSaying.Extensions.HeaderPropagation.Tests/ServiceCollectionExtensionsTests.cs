using JustSaying.Extensions.HeaderPropagation;
using JustSaying.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace JustSaying.Extensions.HeaderPropagation.Tests;

public class ServiceCollectionExtensionsTests
{
    private static IServiceCollection CreateServicesWithFakePublisher(out IMessagePublisher fakePublisher)
    {
        var publisher = Substitute.For<IMessagePublisher, IMessageBatchPublisher>();
        fakePublisher = publisher;

        var services = new ServiceCollection();
        services.AddSingleton<IMessagePublisher>(_ => publisher);
        services.AddSingleton<IMessageBatchPublisher>(sp =>
        {
            var pub = sp.GetRequiredService<IMessagePublisher>();
            return pub is IMessageBatchPublisher batch ? batch : throw new NotSupportedException();
        });
        services.TryAddSingleton<IHttpContextAccessor>(Substitute.For<IHttpContextAccessor>());
        return services;
    }

    [Test]
    public void AfterAddJustSayingHeaderPropagation_IMessagePublisherResolvesToHeaderPropagationPublisher()
    {
        var services = CreateServicesWithFakePublisher(out _);

        services.AddJustSayingHeaderPropagation("x-correlation-id");

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMessagePublisher>().ShouldBeOfType<HeaderPropagationPublisher>();
    }

    [Test]
    public void AfterAddJustSayingHeaderPropagation_IMessageBatchPublisherIsSameAsIMessagePublisher()
    {
        var services = CreateServicesWithFakePublisher(out _);
        services.AddJustSayingHeaderPropagation("x-correlation-id");

        using var sp = services.BuildServiceProvider();
        var publisher = sp.GetRequiredService<IMessagePublisher>();
        var batchPublisher = sp.GetRequiredService<IMessageBatchPublisher>();

        batchPublisher.ShouldBeSameAs(publisher);
    }

    [Test]
    public void CallingAddJustSayingHeaderPropagationBeforePublisherRegistered_IsNoOp_DoesNotThrow()
    {
        var services = new ServiceCollection();

        Should.NotThrow(() => services.AddJustSayingHeaderPropagation("x-correlation-id"));
    }

    [Test]
    public void HeaderPropagationOptions_IsResolvableWithCorrectHeaderNames()
    {
        var services = CreateServicesWithFakePublisher(out _);
        services.AddJustSayingHeaderPropagation("x-correlation-id", "x-feature-branch");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<HeaderPropagationOptions>();

        options.Headers.ShouldContain("x-correlation-id");
        options.Headers.ShouldContain("x-feature-branch");
        options.Headers.Count.ShouldBe(2);
    }
}
