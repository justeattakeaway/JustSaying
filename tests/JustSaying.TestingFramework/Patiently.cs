using System.Diagnostics;
using NSubstitute.Exceptions;
using Shouldly;

namespace JustSaying.TestingFramework;

public static class Patiently
{
    // A real floci container is slower to deliver than the instant in-memory bus,
    // so polling assertions get more headroom in floci mode. This only affects how
    // long a not-yet-satisfied assertion waits before failing; an assertion that
    // passes returns immediately, so the in-memory path is unaffected.
    private static TimeSpan DefaultTimeout => TestEnvironment.UseFloci ? 20.Seconds() : 5.Seconds();

    public static async Task AssertThatAsync(
        Action func,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "") =>
        await AssertThatAsync(null, func, memberName);

    public static async Task AssertThatAsync(
        TextWriter output,
        Action func,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "")
        => await AssertThatAsync(output,
            () =>
            {
                func();
                return true;
            },
            memberName).ConfigureAwait(false);

    public static async Task AssertThatAsync(
        TextWriter output,
        Func<bool> func,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "",
        [System.Runtime.CompilerServices.CallerArgumentExpression("func")]
        string assertionExpression = "")
        => await AssertThatAsyncInner(output, func, DefaultTimeout, memberName, assertionExpression).ConfigureAwait(false);

    public static async Task AssertThatAsync(
        TextWriter output,
        Func<bool> func,
        TimeSpan timeout,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "",
        [System.Runtime.CompilerServices.CallerArgumentExpression("func")]
        string assertionExpression = "")
        => await AssertThatAsyncInner(output, func, timeout, memberName, assertionExpression).ConfigureAwait(false);

    public static async Task AssertThatAsync(TextWriter output, Func<Task<bool>> func) =>
        await AssertThatAsync(output, func, DefaultTimeout).ConfigureAwait(false);

    public static async Task AssertThatAsync(
        TextWriter output,
        Func<Task<bool>> func,
        TimeSpan timeout)
    {
        var watch = new Stopwatch();
        watch.Start();
        do
        {
            try
            {
                if (await func.Invoke().ConfigureAwait(false))
                {
                    return;
                }
            }
            catch (ShouldAssertException)
            { }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            { }

            await Task.Delay(50.Milliseconds()).ConfigureAwait(false);

            output?.WriteLine(
                $"Waiting for {watch.Elapsed.TotalMilliseconds} ms - Still Checking.");
        } while (watch.Elapsed < timeout);

        var result = await func.Invoke().ConfigureAwait(false);
        result.ShouldBeTrue();
    }

    private static async Task AssertThatAsyncInner(
        TextWriter output,
        Func<bool> func,
        TimeSpan timeout,
        string description,
        string assertionExpression)
    {
        var watch = new Stopwatch();
        watch.Start();
        do
        {
            try
            {
                if (func.Invoke())
                {
                    return;
                }
            }
            catch (ShouldAssertException)
            { }
            catch (SubstituteException)
            { }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            { }

            await Task.Delay(50.Milliseconds()).ConfigureAwait(false);

            output?.WriteLine(
                $"Waiting for {watch.Elapsed.TotalMilliseconds} ms - Still waiting for {description}.");
        } while (watch.Elapsed < timeout);

        func.Invoke().ShouldBeTrue($"Failed to assert that {assertionExpression} within {timeout}");
    }
}

public static class TimeExtensions
{
    public static TimeSpan Seconds(this int n) => TimeSpan.FromSeconds(n);

    public static TimeSpan Milliseconds(this int n) => TimeSpan.FromMilliseconds(n);
}
