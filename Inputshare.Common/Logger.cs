using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common
{
    public static class Logger
    {
        private static Serilog.ILogger _logger = new LoggerConfiguration().
            WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj} (Thread {ThreadId}) {NewLine}{Exception}").
            WriteTo.File("Inputshare_Log.txt", outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj} (Thread {ThreadId}) {NewLine}{Exception}")
            .Enrich.
            WithThreadId().MinimumLevel.Verbose().
            CreateLogger();

        public static void Verbose(string message, params object[] args)
        {
            _logger.Verbose(message, args);
        }
        public static void Debug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }
        public static void Information(string message, params object[] args)
        {
            _logger.Information(message, args);
        }
        public static void Warning(string message, params object[] args)
        {
            _logger.Warning(message, args);
        }
        public static void Error(string message, params object[] args)
        {
            _logger.Error(message, args);
        }
        public static void Fatal(string message, params object[] args)
        {
            _logger.Fatal(message, args);
        }

    }
}
