using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace JustEat.Simples.DataAccess.UnitTests.Dapper
{
    public class WhenDapperThrowsAnExceptionWhenQuerying : DapperTestBase
    {
        protected override void When()
        {
            SystemUnderTest.Query<string>("", null);
        }

        [Then]
        public void MonitoringServiceIsCalled()
        {
            Monitor.Received(1).SqlException();
        }

        [Then]
        public void ExceptionWasNotSwallowed()
        {
            Assert.IsNotNull(ThrownException);
        }
    }
}
