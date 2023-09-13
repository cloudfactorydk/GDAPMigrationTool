using Microsoft.Extensions.Logging;

namespace GDAPMigrationTool.Core.Logger
{
    public class CustomLoggerConfiguration
    {
        public int EventId { get; set; }

        public Dictionary<LogLevel, ConsoleColor> LogLevels { get; set; } = new()
        {
            [LogLevel.Information] = ConsoleColor.Green
        };
    }
}
