using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using JustSaying.AotTest;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Smoke test that JustSaying's typical wire-up survives Native AOT publish.
// Builds the DI container — does not start the bus or contact AWS — so the
// publish-time ilc analysis exercises the full configuration path without
// needing any external services at runtime.

var services = new ServiceCollection();

var serializerOptions = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    TypeInfoResolver = AotTestSerializerContext.Default,
};
services.TryAddSingleton<IMessageBodySerializationFactory>(_ => new SystemTextJsonSerializationFactory(serializerOptions));

ConfigureJustSaying(services);

using var provider = services.BuildServiceProvider();

Console.WriteLine("JustSaying.AotTest: container built successfully under Native AOT.");

[UnconditionalSuppressMessage("Trimming", "IL2026",
    Justification = "We have replaced the default IMessageBodySerializationFactory with an AOT-safe SystemTextJson source-gen one above.")]
[UnconditionalSuppressMessage("AOT", "IL3050",
    Justification = "We have replaced the default IMessageBodySerializationFactory with an AOT-safe SystemTextJson source-gen one above.")]
static void ConfigureJustSaying(IServiceCollection services)
{
    services.AddJustSaying(config =>
    {
        config.Messaging(x => x.WithRegion("eu-west-1"));
        config.Publications(x => x.WithTopic<TestMessage>());
        config.Subscriptions(x => x.ForTopic<TestMessage>());
    });

    services.AddJustSayingHandler<TestMessage, TestMessageHandler>();
}

namespace JustSaying.AotTest
{
    public sealed class TestMessage : Message;

    public sealed class TestMessageHandler : IHandlerAsync<TestMessage>
    {
        public Task<bool> Handle(TestMessage message) => Task.FromResult(true);
    }

    [JsonSerializable(typeof(TestMessage))]
    public sealed partial class AotTestSerializerContext : JsonSerializerContext;
}
