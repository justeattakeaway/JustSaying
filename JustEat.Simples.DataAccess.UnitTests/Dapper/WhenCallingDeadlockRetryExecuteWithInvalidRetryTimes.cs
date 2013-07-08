using System;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.DataAccess.UnitTests.Dapper
{
    public class WhenCallingDeadlockRetryExecuteWithInvalidRetryTimes : DapperTestBase
    {
        protected override void When()
        {
            SystemUnderTest.DeadlockRetryExecute("", null, -1);
        }

        [Then]
        public void ArgumentExceptionIsThrown()
        {
            Assert.IsInstanceOf<ArgumentException>(ThrownException);
        }
    }
}