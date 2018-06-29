using System.Text.RegularExpressions;
using JustSaying.Extensions;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests
{
    public class TypeExtensionTests
    {
        public class Foo { }

        public class Bar<T> { }

        [Fact]
        public void ToTopicName_NonGenericType_GivesLowercaseName() =>
            typeof(Foo).ToTopicName()
                .ShouldBe("foo");

        [Fact]
        public void ToTopicName_GenericType_GivesLowercaseFullNameWithNonWordCharactersReplaced()
        {
            //Don't hardcode this, otherwise test will fail on assembly version upgrade
            var assemblyNamePart = Regex.Replace(typeof(Bar<>).Assembly.FullName.ToLower(), @"\W", "_");
            typeof(Bar<Foo>).ToTopicName()
                .ShouldBe($"justsaying_unittests_typeextensiontests_bar_1__justsaying_unittests_typeextensiontests_foo__{assemblyNamePart}__");
        }

        [Fact]
        public void ToTopicName_LongGenericType_TruncatesToMaxAllowedSnsTopicLength() =>
            typeof(Bar<Bar<Bar<Foo>>>).ToTopicName().Length
                .ShouldBe(256);
    }
}
