using NLog;
using NLog.Config;
using NLog.Targets;

namespace JustSaying.TestingFramework
{
    public static class Logging
    {
        public static void ToConsole()
        {
            const string layout = @"${time}|${level}|${message}${onexception:inner=|${exception:format=ShortType,Message}}";
            ToConsole(layout);
        }

        public static void ToConsole(string layout)
        {
            var consoleTarget = new ConsoleTarget
            {
                Layout = layout
            };

            var config = new LoggingConfiguration();

            config.AddTarget("console", consoleTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, consoleTarget));

            LogManager.Configuration = config;

        }
    }
}