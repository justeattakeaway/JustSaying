using JustEat.Testing;
using NSubstitute;

namespace JustEat.Simples.DataAccess.UnitTests.Dapper
{
    public abstract class DapperTestBase : BehaviourTest<DataAccess.Dapper.Dapper>
    {
        protected readonly IDatabaseConfiguration Config = Substitute.For<IDatabaseConfiguration>();
        protected readonly ISqlMonitoringService Monitor = Substitute.For<ISqlMonitoringService>();

        protected override DataAccess.Dapper.Dapper CreateSystemUnderTest()
        {
            return new DataAccess.Dapper.Dapper(Monitor, Config);
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }
    }
}