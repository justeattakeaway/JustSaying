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

    [Test]
    public void ArgExceptionThrown()
    {
        ((ArgumentException)ThrownException).ParamName.ShouldBe("queue");
    }
}