using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Ploeh.AutoFixture;

namespace JustSaying.TestingFramework
{
    public abstract class AsyncBehaviourTestBase<TSystemUnderTest>
    {
        protected IFixture Fixture { get; private set; }

        protected Logger Log { get; private set; }

        protected TargetWithLayout LoggingTarget { get; private set; }

        protected TSystemUnderTest SystemUnderTest { get; private set; }

        protected Exception ThrownException { get; private set; }

        private ExceptionMode ExceptionMode { get; set; }

        private LogLevel LogLevel { get; set; }

        protected AsyncBehaviourTestBase()
        {
            ExceptionMode = ExceptionMode.Throw;
            LoggingTarget = ConfigureLoggingTarget();
            LogLevel = ConfigureLogLevel();
            SimpleConfigurator.ConfigureForTargetLogging(LoggingTarget, LogLevel);
            Log = LogManager.GetCurrentClassLogger();
            Fixture = new Fixture();
            CustomizeAutoFixture(Fixture);
        }

        protected virtual LogLevel ConfigureLogLevel() => LogLevel.Warn;

        protected virtual TargetWithLayout ConfigureLoggingTarget() => new ColoredConsoleTarget {Layout = LogLayout()};

        protected virtual Task<TSystemUnderTest> CreateSystemUnderTest() => Task.FromResult(Fixture.Create<TSystemUnderTest>());

        protected virtual void CustomizeAutoFixture(IFixture fixture)
        {
        }

        protected async Task Execute()
        {
            Given();
            try
            {
                SystemUnderTest = await CreateSystemUnderTest();
                await When();
            }
            catch (Exception ex)
            {
                if (ExceptionMode == ExceptionMode.Record)
                    ThrownException = ex;
                else
                    throw;
            }
            finally
            {
                Teardown();
            }
        }

        protected abstract void Given();

        protected virtual Layout LogLayout() => "${message}";

        protected virtual void PostAssertTeardown()
        {
        }

        protected void RecordAnyExceptionsThrown()
        {
            ExceptionMode = ExceptionMode.Record;
        }

        protected virtual void Teardown()
        {
        }

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "When really is the best name for this message")]
        protected abstract Task When();
    }
}
