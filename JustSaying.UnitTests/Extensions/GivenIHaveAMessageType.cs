using System;
using JustBehave;
using JustSaying.Extensions;

namespace JustSaying.UnitTests.Extensions
{
    public abstract class GivenIHaveAMessageType : BehaviourTest<Type>
    {
        internal class GenericClass<T> { }

        internal class Poco { }

        protected string Result;

        protected override void Given()
        {
        }

        protected override void When()
        {
            Result = SystemUnderTest.ToTopicName();
        }
    }
}