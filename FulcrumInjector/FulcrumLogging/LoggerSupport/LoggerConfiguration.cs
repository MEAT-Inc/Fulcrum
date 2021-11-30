using NLog.Layouts;
using NLog.Targets;

namespace FulcrumInjector.FulcrumLogging.LoggerSupport
{
    internal static class WatchdogLoggerConfiguration
    {
        // Configuration Strings
        public static string BaseFormatConsole =
            "[${date:format=hh\\:mm\\:ss}][${level:uppercase=true}][${mdc:custom-name}][${mdc:item=calling-class-short}] ::: ${message}";
        public static string BaseFormatFile =
            "[${date:format=MM-dd-yyyy hh\\:mm\\:ss}][${level:uppercase=true}][${mdc:custom-name}][${mdc:item=calling-class}] ::: ${message}";

        // Logging setup for timer logging.
        public static string TimedFormatConsole =
            "[${date:format=hh\\:mm\\:ss}][${level:uppercase=true}][${mdc:custom-name}][${mdc:item=calling-class-short}] ::: [TIMER: ${mdc:item=stopwatch-time}] ::: ${message}";
        public static string TimedFormatFile =
            "[${date:format=MM-dd-yyyy hh\\:mm\\:ss}][${level:uppercase=true}][${mdc:custom-name}][${mdc:item=calling-class}] ::: [TIMER: ${mdc:item=stopwatch-time}] ::: ${message}";

        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a colored console logger.
        /// </summary>
        /// <returns>Console Logging Object</returns>
        public static ColoredConsoleTarget GenerateConsoleLogger(string TargetName, string Format = null)
        {
            // Get formatting string.
            string FormatValue = Format ?? BaseFormatConsole;

            // Make Logger and set format.
            var ConsoleLogger = new ColoredConsoleTarget(TargetName);
            ConsoleLogger.Layout = new SimpleLayout(FormatValue);

            // Add Coloring Rules
            ConsoleLogger.RowHighlightingRules.Add(new ConsoleRowHighlightingRule(
                "level == LogLevel.Trace",
                ConsoleOutputColor.DarkGray,
                ConsoleOutputColor.Black)
            );
            ConsoleLogger.RowHighlightingRules.Add(new ConsoleRowHighlightingRule(
                "level == LogLevel.Debug",
                ConsoleOutputColor.Gray,
                ConsoleOutputColor.Black)
            );
            ConsoleLogger.RowHighlightingRules.Add(new ConsoleRowHighlightingRule(
                "level == LogLevel.Info",
                ConsoleOutputColor.Green,
                ConsoleOutputColor.Black)
            );
            ConsoleLogger.RowHighlightingRules.Add(new ConsoleRowHighlightingRule(
                "level == LogLevel.Warn",
                ConsoleOutputColor.Red,
                ConsoleOutputColor.Yellow)
            );
            ConsoleLogger.RowHighlightingRules.Add(new ConsoleRowHighlightingRule(
                "level == LogLevel.Error",
                ConsoleOutputColor.Red,
                ConsoleOutputColor.Gray)
            );
            ConsoleLogger.RowHighlightingRules.Add(new ConsoleRowHighlightingRule(
                "level == LogLevel.Fatal",
                ConsoleOutputColor.Red,
                ConsoleOutputColor.White)
            );

            // Return Logger
            return ConsoleLogger;
        }
        /// <summary>
        /// Builds a new file logging target object.
        /// </summary>
        /// <param name="FileName">Name of file to log into</param>
        /// <param name="LogFormat">Format string.</param>
        /// <returns>File Logging target</returns>
        public static FileTarget GenerateFileLogger(string FileName, string Format = null)
        {
            // Get formatting string.
            string FormatValue = Format ?? BaseFormatFile;

            // Build Target
            var FileLogger = new FileTarget($"FileLogger_{FileName}");
            FileLogger.FileName = FileName;
            FileLogger.Layout = new SimpleLayout(FormatValue);
            FileLogger.ArchiveFileName = "${basedir}/LogArchives/" + FileName.Split('_')[0] + ".{####}.log";
            FileLogger.ArchiveEvery = FileArchivePeriod.Day;
            FileLogger.ArchiveNumbering = ArchiveNumberingMode.DateAndSequence;
            FileLogger.ArchiveAboveSize = 1953125;
            FileLogger.MaxArchiveFiles = 20;
            FileLogger.ConcurrentWrites = true;
            FileLogger.KeepFileOpen = false;

            // Return the logger
            return FileLogger;
        }
        /// <summary>
        /// Generate Database Logger Object
        /// </summary>
        /// <returns>Database logging object.</returns>
        public static DatabaseTarget GenerateDatabaseLogger(string ConnectionString, string DatabaseName)
        {
            // Build the SQL Logger
            var DatabaseLogger = new DatabaseTarget("ERSDatabaseLogger");
            DatabaseLogger.DBProvider = "System.Data.SqlClient";
            DatabaseLogger.ConnectionString = ConnectionString;
            DatabaseLogger.CommandText =
                @"INSERT INTO dbo.LOGGING_DB 
                    (Time, DeviceId, YMM, VIN, SessionId, LogLevel, CallSite, Message, Exception) 
                     values 
                    (@Time, @DeviceId, @YMM, @VIN, @SessionId, @LogLevel, @CallSite, @Message, @Exception);"
                .Replace("LOGGING_DB", DatabaseName);

            // Add DB Params here
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@Time", Layout.FromString("${date:format=yyyy\\-MM\\-dd HH\\:mm\\:ss.fff}")));
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@DeviceId", "${machinename}"));
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@YMM", "${gdc:YearMakeModel}"));
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@VIN", "${gdc:VIN}"));
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@SessionId", "${gdc:SessionId}"));
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@LogLevel", "${level}"));
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@Callsite", "${gdc:CallingName}"));
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@Message", "${gdc:CleanMessage}"));
            DatabaseLogger.Parameters.Add(new DatabaseParameterInfo("@Exception", "${exception:tostring}"));

            // Return the logger
            return DatabaseLogger;
        }

        // ----------------------------------------------------------------------------------------------

    }
}
