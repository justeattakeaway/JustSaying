using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenSubscribingAndNotPassingATopic : GivenAServiceBus
{
    protected override void Given()
    {
        base.Given();
        RecordAnyExceptionsThrown();
    }

    protected override Task WhenAsync()
    {
        SystemUnderTest.AddQueue("test", null);
        return Task.CompletedTask;
    }

    [Fact]
    public void ArgExceptionThrown()
    {
        ((ArgumentException)ThrownException).ParamName.ShouldBe("queue");
    }

    public WhenSubscribingAndNotPassingATopic(ITestOutputHelper outputHelper) : base(outputHelper)
    { }
}