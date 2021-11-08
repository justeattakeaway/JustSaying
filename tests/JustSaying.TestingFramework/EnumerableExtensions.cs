namespace JustSaying.TestingFramework;

public static class EnumerableExtensions
{
    public static IEnumerable<T> Infinite<T>(this T item)
    {
        while (true)
        {
            yield return item;
        }
    }

    public static Func<IEnumerable<T>> GenerateInfinite<T>(Func<T> itemFunc)
    {
        return () => RepeatInfinitely(itemFunc);
    }

    public static IEnumerable<T> RepeatInfinitely<T>(Func<T> itemFunc)
    {
        while (true)
        {
            yield return itemFunc();
        }
    }
}