using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.DataAccess.UnitTests.Dapper
{
    public class WhenDapperThrowsAnExceptionWhenExecuting : DapperTestBase
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.Execute("", null);
        }

        [Then]
        public void ExceptionWasNotSwallowed()
        {
            Assert.IsNotNull(ThrownException);
        }
    }
}