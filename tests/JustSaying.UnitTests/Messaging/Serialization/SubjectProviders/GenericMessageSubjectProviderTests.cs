using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.UnitTests.Messaging.Serialization.SubjectProviders;

public class GenericMessageSubjectProviderTests
{
    // these classes are never instantiated, but the types are used in tests
#pragma warning disable CA1812
    class Foo { }

    class Bar<T> { }
#pragma warning restore CA1812

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