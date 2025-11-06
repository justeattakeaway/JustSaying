using System.Collections.Generic;
using JustSaying.Messaging.MessageSerialization;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.SubjectProviders
{
    public class NonGenericMessageSubjectProviderTests
    {
        // this class is never instantiated, but the type is used in tests
#pragma warning disable CA1812
        class Foo { }
#pragma warning restore CA1812

        [Fact]
        public void GetSubjectForType_ReturnsTypeName() =>
            new NonGenericMessageSubjectProvider().GetSubjectForType(typeof(Foo))
                .ShouldBe("Foo");

        [Fact]
        public void GetSubjectForType_IgnoresAnyTypeParameters() =>
            new NonGenericMessageSubjectProvider().GetSubjectForType(typeof(List<Foo>))
                .ShouldBe("List`1");
    }
}
