using System.Runtime.CompilerServices;

namespace JustSaying.UnitTests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        ThreadPool.SetMinThreads(40, 1);
    }
}
