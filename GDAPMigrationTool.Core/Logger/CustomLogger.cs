using Microsoft.Extensions.Logging;
using PartnerLed.Utility;
using System.Reflection;
using System.Text;

namespace PartnerLed.Logger
{
    public sealed class CustomLogger : ILogger
    {
        private string logFormat = string.Empty;
        private string logPath = string.Empty;
        private readonly string _name;
        private static object lockerFile = new Object();
        private readonly Func<CustomLoggerConfiguration> _getCurrentConfig;

        public CustomLogger(
            string name,
            Func<CustomLoggerConfiguration> getCurrentConfig) =>
            (_name, _getCurrentConfig) = (name, getCurrentConfig);

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) =>
            _getCurrentConfig().LogLevels.ContainsKey(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = $"[{eventId.Id}: {logLevel}] {_name} - {formatter(state, exception)}";
            if (logLevel >= LogLevel.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            LogWrite(message);
        }


        public void LogWrite(string logMessage)
        {
            logPath = $"{Environment.GetEnvironmentVariable(Constants.BasepathVariable)}/Logs/LogFile{DateTime.Now.ToString("MMddyyyy")}.txt";
            logFormat = "\n" + DateTime.Now.ToString() + ":";

            try
            {
                lock (lockerFile)
                {
                    using (FileStream file = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (StreamWriter writer = new StreamWriter(file, Encoding.Unicode))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(logFormat + logMessage);
                        writer.Write(sb.ToString());
                    }
                }
            }
            catch
            {
                //Do not do anything 
            }
        }
    }
}
