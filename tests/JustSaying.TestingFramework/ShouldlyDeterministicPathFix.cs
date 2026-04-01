#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Shouldly;
using Shouldly.Configuration;

namespace JustSaying.TestingFramework;

#pragma warning disable CA2255
/// <summary>
/// Workaround for Shouldly not resolving deterministic source paths when tests
/// run under MS CodeCoverage. See https://github.com/shouldly/shouldly/issues/1173
/// </summary>
internal static class ShouldlyDeterministicPathFix
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        ShouldlyConfiguration.ShouldMatchApprovedDefaults
            .Configure(c => c.TestMethodFinder = new DeterministicPathTestMethodFinder());
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<SourceFileDirectory>k__BackingField")]
    internal static extern ref string? SourceFileDirectoryRef(TestMethodInfo info);

    internal static string FixDeterministicPath(string path)
    {
        if (!IsDeterministicPath(path))
            return path;

        var repoRoot = GetRepoRoot();
        return repoRoot != null ? repoRoot + path.Substring(3) : path;
    }

    internal static bool IsDeterministicPath(string? path) =>
        path != null &&
        (path.StartsWith("/_/", StringComparison.Ordinal) ||
         path.StartsWith("\\_\\", StringComparison.Ordinal));

    private static string? _cachedRepoRoot;

    private static string? GetRepoRoot()
    {
        if (_cachedRepoRoot != null)
            return _cachedRepoRoot;

        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "global.json")))
            {
                _cachedRepoRoot = dir.EndsWith(Path.DirectorySeparatorChar)
                    ? dir
                    : dir + Path.DirectorySeparatorChar;
                return _cachedRepoRoot;
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}

internal sealed class DeterministicPathTestMethodFinder : ITestMethodFinder
{
    public TestMethodInfo GetTestMethodInfo(StackTrace stackTrace, int startAt)
    {
        for (var i = startAt; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            if (frame == null) continue;

            var method = frame.GetMethod();
            if (method == null || method.DeclaringType == null) continue;

            var ns = method.DeclaringType.Namespace;
            if (ns != null && ns.StartsWith("Shouldly", StringComparison.Ordinal))
                continue;

            var info = new TestMethodInfo(frame);

            ref var sourceDir = ref ShouldlyDeterministicPathFix.SourceFileDirectoryRef(info);
            if (ShouldlyDeterministicPathFix.IsDeterministicPath(sourceDir))
            {
                sourceDir = ShouldlyDeterministicPathFix.FixDeterministicPath(sourceDir!);
            }

            return info;
        }

        return new TestMethodInfo(stackTrace.GetFrame(startAt)!);
    }
}
#pragma warning restore CA2255
