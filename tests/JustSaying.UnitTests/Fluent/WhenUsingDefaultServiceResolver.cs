using JustSaying.AwsTools;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageSerialization;
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
        public void ShouldResolveILoggerFactory()
        {
            _sut.ResolveService<ILoggerFactory>().ShouldBeOfType<NullLoggerFactory>();
        }

        [Fact]
        public void ShouldResolveIAwsClientFactoryProxy()
        {
            _sut.ResolveService<IAwsClientFactoryProxy>().ShouldBeOfType<AwsClientFactoryProxy>();
        }

        [Fact]
        public void ShouldResolveIHandlerResolverAsNull()
        {
            _sut.ResolveService<IHandlerResolver>().ShouldBeNull();
        }

        [Fact]
        public void ShouldResolveIMessagingConfig()
        {
            _sut.ResolveService<IMessagingConfig>().ShouldBeOfType<MessagingConfig>();
        }

        [Fact]
        public void ShouldResolveIMessageSerializationFactory()
        {
            _sut.ResolveService<IMessageSerializationFactory>().ShouldBeOfType<NewtonsoftSerializationFactory>();
        }

        [Fact]
        public void ShouldResolveIMessageSerializationRegister()
        {
            _sut.ResolveService<IMessageSerializationRegister>().ShouldBeOfType<MessageSerializationRegister>();
        }

        [Fact]
        public void ShouldResolveIMessageSubjectProvider()
        {
            _sut.ResolveService<IMessageSubjectProvider>().ShouldBeOfType<NonGenericMessageSubjectProvider>();
        }
    }
}
