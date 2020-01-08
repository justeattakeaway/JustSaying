using JustSaying.AwsTools;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Fluent
{
    public class WhenUsingDefaultServiceResolver
    {
        private readonly DefaultServiceResolver _sut;

        public WhenUsingDefaultServiceResolver()
        {
            _sut = new DefaultServiceResolver();
        }

        [Fact]
        public void ShouldResolveILoggerFactoryToNullLoggerFactory()
        {
            _sut.ResolveService<ILoggerFactory>().ShouldBeOfType<NullLoggerFactory>();
        }

        [Fact]
        public void ShouldResolveIAwsClientFactoryProxyToAwsClientFactoryProxy()
        {
            _sut.ResolveService<IAwsClientFactoryProxy>().ShouldBeOfType<AwsClientFactoryProxy>();
        }

        [Fact]
        public void ShouldResolveIHandlerResolverAsNull()
        {
            _sut.ResolveService<IHandlerResolver>().ShouldBeNull();
        }

        [Fact]
        public void ShouldResolveIMessagingConfigToMessagingConfig()
        {
            _sut.ResolveService<IMessagingConfig>().ShouldBeOfType<MessagingConfig>();
        }

        [Fact]
        public void ShouldResolveIMessageSerializationFactoryToNewtonsoftSerializationFactory()
        {
            _sut.ResolveService<IMessageSerializationFactory>().ShouldBeOfType<NewtonsoftSerializationFactory>();
        }

        [Fact]
        public void ShouldResolveIMessageSerializationRegisterToMessageSerializationRegister()
        {
            _sut.ResolveService<IMessageSerializationRegister>().ShouldBeOfType<MessageSerializationRegister>();
        }

        [Fact]
        public void ShouldResolveIMessageSubjectProviderToNonGenericMessageSubjectProvider()
        {
            _sut.ResolveService<IMessageSubjectProvider>().ShouldBeOfType<NonGenericMessageSubjectProvider>();
        }

        [Fact]
        public void ShouldResolveITopicNamingConventionToDefaultNamingConvention()
        {
            _sut.ResolveService<ITopicNamingConvention>().ShouldBeOfType<DefaultNamingConventions>();
        }

        [Fact]
        public void ShouldResolveIQueueNamingConventionToDefaultNamingConvention()
        {
            _sut.ResolveService<IQueueNamingConvention>().ShouldBeOfType<DefaultNamingConventions>();
        }
    }
}
