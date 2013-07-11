using System.Data.SqlClient;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.DataAccess.UnitTests.Dapper
{
    public class WhenCallingDeadlockRetryExecuteAndNonSqlExceptionIsThrown : DapperTestBase
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.DeadlockRetryExecute("", new { id = 1 }, 2);
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