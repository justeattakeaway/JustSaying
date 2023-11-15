using System.Diagnostics;
using NSubstitute.Exceptions;
using Shouldly;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace JustSaying.TestingFramework;

public static class Patiently
{
    public static async Task AssertThatAsync(
        Action func,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "") =>
        await AssertThatAsync(null, func, memberName);

    public static async Task AssertThatAsync(
        ITestOutputHelper output,
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
        ITestOutputHelper output,
        Func<bool> func,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "")
        => await AssertThatAsyncInner(output, func, 5.Seconds(), memberName).ConfigureAwait(false);

    public static async Task AssertThatAsync(
        ITestOutputHelper output,
        Func<bool> func,
        TimeSpan timeout,
        [System.Runtime.CompilerServices.CallerMemberName]
        string memberName = "")
        => await AssertThatAsyncInner(output, func, timeout, memberName).ConfigureAwait(false);

    public static async Task AssertThatAsync(ITestOutputHelper output, Func<Task<bool>> func) =>
        await AssertThatAsync(output, func, 5.Seconds()).ConfigureAwait(false);

    public static async Task AssertThatAsync(
        ITestOutputHelper output,
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
            catch (XunitException)
            { }

            await Task.Delay(50.Milliseconds()).ConfigureAwait(false);

            output?.WriteLine(
                $"Waiting for {watch.Elapsed.TotalMilliseconds} ms - Still Checking.");
        } while (watch.Elapsed < timeout);

        var result = await func.Invoke().ConfigureAwait(false);
        result.ShouldBeTrue();
    }

    private static async Task AssertThatAsyncInner(
        ITestOutputHelper output,
        Func<bool> func,
        TimeSpan timeout,
        string description)
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
            catch (XunitException)
            { }
            catch (SubstituteException)
            { }

            await Task.Delay(50.Milliseconds()).ConfigureAwait(false);

            output?.WriteLine(
                $"Waiting for {watch.Elapsed.TotalMilliseconds} ms - Still waiting for {description}.");
        } while (watch.Elapsed < timeout);

        func.Invoke().ShouldBeTrue();
    }
}

public static class TimeExtensions
{
    public static TimeSpan Seconds(this int n) => TimeSpan.FromSeconds(n);

    public static TimeSpan Milliseconds(this int n) => TimeSpan.FromMilliseconds(n);
}
