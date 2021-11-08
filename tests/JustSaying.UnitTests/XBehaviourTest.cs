using AutoFixture;

namespace JustSaying.UnitTests
{
    public abstract class XBehaviourTest<TSystemUnderTest>
    {
        private bool _recordThrownExceptions;

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
            catch (Exception ex) when (_recordThrownExceptions)
            {
                ThrownException = ex;
            }
        }

        protected abstract void Given();

        protected void RecordAnyExceptionsThrown()
        {
            _recordThrownExceptions = true;
        }

        protected abstract void WhenAction();
    }
}
