using JustSaying.Extensions;

namespace JustSaying.UnitTests.Extensions;

public class WhenFormattingTypeNames
{
    private sealed class Order;

    private sealed class Envelope<T>;

    private sealed class Pair<TKey, TValue>;

    [Test]
    public void ANonGenericTypeIsUnchanged()
    {
        typeof(Order).ToReadableName().ShouldBe(typeof(Order).Name);
        typeof(Order).ToReadableFullName().ShouldBe(typeof(Order).FullName);
    }

    [Test]
    public void AGenericTypeIsSpelledAsInCSharp()
    {
        typeof(Envelope<Order>).ToReadableName().ShouldBe("Envelope<Order>");
        typeof(Pair<string, Envelope<Order>>).ToReadableName().ShouldBe("Pair<String, Envelope<Order>>");
    }

    [Test]
    public void AGenericTypesFullNameCarriesNoAssemblyVersions()
    {
        var name = typeof(Envelope<Order[]>).ToReadableFullName();

        name.ShouldBe($"{typeof(WhenFormattingTypeNames).FullName}+Envelope<{typeof(Order).FullName}[]>");
        name.ShouldNotContain("Version=");
    }
}
