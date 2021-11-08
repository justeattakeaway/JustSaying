namespace JustSaying.Tools
{
    internal static class EnumerableExtensions
    {
        internal static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> func)
        {
            foreach (var item in source)
            {
                await func(item).ConfigureAwait(false);
            }
        }
    }
}
