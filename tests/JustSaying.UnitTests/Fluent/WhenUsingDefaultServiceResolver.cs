using JustSaying.AwsTools;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingDefaultServiceResolver
{
    private readonly DefaultServiceResolver _sut = new();

    [Test]
    public void ShouldResolveILoggerFactoryToNullLoggerFactory()
    {
        _sut.ResolveService<ILoggerFactory>().ShouldBeOfType<NullLoggerFactory>();
    }

    [Test]
    public void ShouldResolveIAwsClientFactoryProxyToAwsClientFactoryProxy()
    {
        _sut.ResolveService<IAwsClientFactoryProxy>().ShouldBeOfType<AwsClientFactoryProxy>();
    }

    [Test]
    public void ShouldResolveIHandlerResolverAsNull()
    {
        _sut.ResolveOptionalService<IHandlerResolver>().ShouldBeNull();
    }

    [Test]
    public void ShouldResolveIMessagingConfigToMessagingConfig()
    {
        _sut.ResolveService<IMessagingConfig>().ShouldBeOfType<MessagingConfig>();
    }

    [Test]
    public void ShouldResolveIMessageSerializationFactoryToNewtonsoftSerializationFactory()
    {
        _sut.ResolveService<IMessageBodySerializationFactory>().ShouldBeOfType<NewtonsoftSerializationFactory>();
    }

    [Test]
    public void ShouldResolveIMessageSubjectProviderToNonGenericMessageSubjectProvider()
    {
        _sut.ResolveService<IMessageSubjectProvider>().ShouldBeOfType<NonGenericMessageSubjectProvider>();
    }

    [Test]
    public void ShouldResolveITopicNamingConventionToDefaultNamingConvention()
    {
        _sut.ResolveService<ITopicNamingConvention>().ShouldBeOfType<DefaultNamingConventions>();
    }

    [Test]
    public void ShouldResolveIQueueNamingConventionToDefaultNamingConvention()
    {
        _sut.ResolveService<IQueueNamingConvention>().ShouldBeOfType<DefaultNamingConventions>();
    }
}
