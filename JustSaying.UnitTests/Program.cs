using NUnitLite;
using System.Reflection;

namespace JustSaying.UnitTests
{
    class Program
    {
        public static int Main(string[] args)
        {
            return new AutoRun(typeof(Program).GetTypeInfo().Assembly).Execute(args);
        }
    }

}
