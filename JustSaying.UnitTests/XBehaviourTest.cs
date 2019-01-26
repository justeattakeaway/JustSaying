using System;
using AutoFixture;

namespace JustSaying.UnitTests
{
    public abstract class XBehaviourTest<TSystemUnderTest>
    {
        private bool _recordExceptions;

        protected XBehaviourTest()
        {
            Execute();
        }

        protected TSystemUnderTest SystemUnderTest { get; private set; }
        protected Exception ThrownException { get; private set; }
 
        protected virtual TSystemUnderTest CreateSystemUnderTest()
        {
            var fixture = new Fixture();
            return fixture.Create<TSystemUnderTest>();
        }

        protected void Execute()
        {
            Given();

            try
            {
                SystemUnderTest = CreateSystemUnderTest();
                WhenAction();
            }
            catch (Exception ex)
            {
                if (_recordExceptions)
                {
                    ThrownException = ex;
                }
                else
                {
                    throw;
                }
            }
        }

        protected abstract void Given();

        protected void RecordAnyExceptionsThrown()
        {
            _recordExceptions = true;
        }

        protected abstract void WhenAction();
    }
}
