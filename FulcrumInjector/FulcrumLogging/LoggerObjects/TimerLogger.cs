using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using NLog;

namespace FulcrumInjector.FulcrumLogging.LoggerObjects
{
    /// <summary>
    /// Logger class used to log method types and outputs.
    /// </summary>
    public class TimerLogger : BaseLogger
    {
        // File Paths for logger
        public string LoggerFile;           // Path of the logger file.
        public string OutputPath;           // Base output path.

        // Stopwatch for timing methods and infos.
        internal Stopwatch LoggerStopwatch;
        internal Stopwatch MethodStopwatch;

        // Last stacktrace.
        private string LastCalledClass;

        // Time elapsed for the current string object.
        private string StopwatchTimeString => (bool)this.MethodStopwatch?.IsRunning ?
            "(METHOD) " + this.MethodStopwatch.Elapsed.ToString("hh\\:mm\\:ss") :
            "(LOGGER) " + this.LoggerStopwatch.Elapsed.ToString("hh\\:mm\\:ss");

        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new falcon file logging object.
        /// </summary>
        /// <param name="LoggerName"></param>
        /// <param name="MinLevel"></param>
        /// <param name="MaxLevel"></param>
        public TimerLogger([CallerMemberName] string LoggerName = "", string LogFileName = "", int MinLevel = 0, int MaxLevel = 5) : base(LoggerActions.TimerLogger, LoggerName, MinLevel, MaxLevel)
        {
            // Start Main Logger
            this.LoggerStopwatch = new Stopwatch();
            this.LoggerStopwatch.Start();

            // Store path and file name here.
            if (!string.IsNullOrEmpty(LogFileName))
            {
                // Store values here.
                this.LoggerFile = LogFileName;
                int SplitNameLength = this.LoggerFile.Split('\\').Length - 2;
                this.OutputPath = this.LoggerFile.Split(Path.DirectorySeparatorChar).Take(SplitNameLength).ToString();
            }
            else
            {
                // Generate Dynamic values.
                this.OutputPath = FulcrumLogBroker.BaseOutputPath;
                this.LoggerFile = Path.Combine(
                    this.OutputPath,
                    $"{this.LoggerName}_LoggerOutput_{DateTime.Now.ToString("ddMMyyy-hhmmss")}.log"
                );
            }

            // Build Logger object now.
            this.LoggingConfig = LogManager.Configuration;
            this.LoggingConfig.AddRule(
                LogLevel.Trace,
                LogLevel.Fatal,
                WatchdogLoggerConfiguration.GenerateConsoleLogger(LoggerName, WatchdogLoggerConfiguration.TimedFormatConsole),
                LoggerName,
                false
            );
            this.LoggingConfig.AddRule(
                LogLevel.Trace,
                LogLevel.Fatal,
                WatchdogLoggerConfiguration.GenerateFileLogger(LoggerName, WatchdogLoggerConfiguration.TimedFormatFile),
                LoggerName,
                false
            );

            // Store configuration
            LogManager.Configuration = this.LoggingConfig;
            this.NLogger = LogManager.GetCurrentClassLogger();
            this.PrintLoggerInfos();
        }


        /// <summary>
        /// Starts up the method timer object and uses the current call stack to get it.
        /// </summary>
        public void StartMethodTimer()
        {
            // Get the current callstack.
            this.MethodStopwatch = new Stopwatch();
            this.MethodStopwatch.Start();
            this.NLogger.Log(LogLevel.Trace, "STARTED METHOD DIAGNOSTIC STOPWATCH OK!");
        }

        /// <summary>
        /// Overrise the log output for exceptions setting the time elapsed at the moment of calling.
        /// </summary>
        /// <param name="Ex">Exception thrown</param>
        /// <param name="Level">Level to log at</param>
        public override void WriteLog(Exception Ex, LogType Level = LogType.ErrorLog)
        {
            // Check logger and make sure timer is on.
            if (this.MethodStopwatch == null || this.MethodStopwatch?.IsRunning == false)
                this.StartMethodTimer();

            // Set time and write log output.
            MappedDiagnosticsContext.Set("stopwatch-time", this.StopwatchTimeString);
            base.WriteLog(Ex, Level);

            // Make sure the stacktrace matches all around.
            if (this.LastCalledClass != null)
            {
                // Compare context values.
                var CurrentClass = MappedDiagnosticsContext.Get("calling-class-short");
                if (CurrentClass != this.LastCalledClass) { this.StartMethodTimer(); }
            }

            // Store context value
            this.LastCalledClass = MappedDiagnosticsContext.Get("calling-class-short");
        }
        /// <summary>
        /// Override the log output method and store the time string in here instead.
        /// </summary>
        /// <param name="LogMessage">Message to write</param>
        /// <param name="Level">Level of logging </param>
        public override void WriteLog(string LogMessage, LogType Level = LogType.DebugLog)
        {
            // Check logger and make sure timer is on.
            if (this.MethodStopwatch == null || this.MethodStopwatch?.IsRunning == false)
                this.StartMethodTimer();

            // Set time and write log output.
            MappedDiagnosticsContext.Set("stopwatch-time", this.StopwatchTimeString);
            base.WriteLog(LogMessage, Level);

            // Make sure the stacktrace matches all around.
            if (this.LastCalledClass != null)
            {
                // Compare context values.
                var CurrentClass = MappedDiagnosticsContext.Get("calling-class-short");
                if (CurrentClass != this.LastCalledClass) { this.StartMethodTimer(); }
            }

            // Store context value
            this.LastCalledClass = MappedDiagnosticsContext.Get("calling-class-short");
        }
        /// <summary>
        /// Override the log output and append the logger time into it.
        /// </summary>
        /// <param name="MessageExInfo">Custom info string</param>
        /// <param name="Ex">Exception thrown</param>
        /// <param name="LevelTypes">Level of logging</param>
        public override void WriteLog(string MessageExInfo, Exception Ex, LogType[] LevelTypes = null)
        {
            // Check logger and make sure timer is on.
            if (this.MethodStopwatch == null || this.MethodStopwatch?.IsRunning == false)
                this.StartMethodTimer();

            // Set time and write log output.
            MappedDiagnosticsContext.Set("stopwatch-time", this.StopwatchTimeString);
            base.WriteLog(MessageExInfo, Ex, LevelTypes);

            // Make sure the stacktrace matches all around.
            if (this.LastCalledClass != null)
            {
                // Compare context values.
                var CurrentClass = MappedDiagnosticsContext.Get("calling-class-short");
                if (CurrentClass != this.LastCalledClass) { this.StartMethodTimer(); }
            }

            // Store context value
            this.LastCalledClass = MappedDiagnosticsContext.Get("calling-class-short");
        }
    }
}
