using System.Data.SqlClient;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace JustEat.Simples.DataAccess.UnitTests.Dapper
{
    public class WhenCallingDeadlockRetryExecuteAndDeadlockSqlExceptionIsThrown : DapperTestBase
    {
        protected override void Given()
        {
            Config.GetConnectionString(Tenant).Returns(x => { throw ExceptionHelpers.MakeSqlDeadlockException(); });
            base.Given();
        }

        protected override void When()
        {
            SystemUnderTest.DeadlockRetryExecute("", new { id = 1 }, 2);
        }

        [Then]
        public void MonitoringServiceIsCalledThreeTimesAsTheQueryIsTriedThreeTimes()
        {
            Monitor.Received(3).SqlDeadlockException();
        }

        [Then]
        public void SqlExceptionIsThrown()
        {
            Assert.IsInstanceOf<SqlException>(ThrownException);
        }
    }
}