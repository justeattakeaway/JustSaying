using JustSaying.Messaging.MessageSerialization;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.SubjectProviders
{
    public class GenericMessageSubjectProviderTests
    {
        class Foo { }

        class Bar<T> { }

        [Fact]
        public void GetSubjectForType_NonGenericType_ReturnsTypeNameWithNamespace_NonWordCharactersReplaced() =>
            new GenericMessageSubjectProvider().GetSubjectForType(typeof(Foo))
                .ShouldBe("Foo_JustSaying_UnitTests_Messaging_Serialization_SubjectProviders");

        [Fact]
        public void GetSubjectForType_GenericType_ReturnsFlattenedTypeNamesWithNamepaces_TrunactedToMaxSnsSubjectLength()
        {
            var subject = new GenericMessageSubjectProvider().GetSubjectForType(typeof(Bar<Foo>));
            subject.ShouldStartWith("Bar_1_JustSaying_UnitTests_Messaging_Serialization_SubjectProviders_Foo_JustSaying_");
            subject.Length.ShouldBe(100);
        }
    }
}
