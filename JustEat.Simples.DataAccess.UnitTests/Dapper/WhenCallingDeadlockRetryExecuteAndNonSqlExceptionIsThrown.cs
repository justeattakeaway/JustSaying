using System.Data.SqlClient;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace JustEat.Simples.DataAccess.UnitTests.Dapper
{
    public class WhenCallingDeadlockRetryExecuteAndNonSqlExceptionIsThrown : DapperTestBase
    {
        protected override void When()
        {
            SystemUnderTest.DeadlockRetryExecute("", new { id = 1 }, 2);
        }

        [Then]
        public void MonitoringServiceIsCalled()
        {
            Monitor.Received().SqlException();
        }

        [Then]
        public void ExceptionIsNotNull()
        {
            Assert.IsNotNull(ThrownException);
        }

        [Then]
        public void SqlExceptionIsNotThrown()
        {
            Assert.IsNotInstanceOf<SqlException>(ThrownException);
        }
    }
}