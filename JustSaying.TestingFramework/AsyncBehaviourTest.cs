using System.Threading.Tasks;
using NUnit.Framework;

namespace JustSaying.TestingFramework {
    [TestFixture]
    public abstract class AsyncBehaviourTest<TSystemUnderTest> : AsyncBehaviourTestBase<TSystemUnderTest>
    {
        [OneTimeSetUp]
        public async Task Go() => await Execute();

        [OneTimeTearDown]
        public new virtual Task PostAssertTeardown() => Task.CompletedTask;
    }
}