using System;
using System.IO;
using System.Runtime.CompilerServices;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using NLog;

namespace FulcrumInjector.FulcrumLogging.LoggerObjects
{
    /// <summary>
    /// Builds a logger object which can write to all possible targets at once.
    /// </summary>
    public class MasterLogger : BaseLogger
    {
        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new master logger object. THERE SHOULD ONLY BE ONE OF THESE AT ANY POINT IN TIME!
        /// </summary>
        /// <param name="LoggerName"></param>
        /// <param name="LogFileName"></param>
        /// <param name="MinLevel"></param>
        /// <param name="MaxLevel"></param>
        public MasterLogger([CallerMemberName] string LoggerName = "", string LogFileName = "", int MinLevel = 0, int MaxLevel = 5) :
            base(LoggerActions.MasterLogger, LoggerName, MinLevel, MaxLevel)
        {
            // Check file name.
            if (!string.IsNullOrEmpty(LogFileName))
            {
                // Check for broker file.
                if (FulcrumLogBroker.MainLogFileName != null) LogFileName = FulcrumLogBroker.MainLogFileName;
                else
                {
                    // Generate Dynamic values.
                    LogFileName = Path.Combine(
                        FulcrumLogBroker.BaseOutputPath,
                        $"{this.LoggerName}_LoggerOutput_{DateTime.Now.ToString("ddMMyyy-hhmmss")}.log"
                    );
                }
            }

            // Build Master Logging Configuration.
            this.LoggingConfig = LogManager.Configuration;
            this.LoggingConfig.AddRule(
                LogLevel.FromOrdinal(MinLevel),
                LogLevel.FromOrdinal(MaxLevel),
                WatchdogLoggerConfiguration.GenerateFileLogger(LogFileName));
            this.LoggingConfig.AddRule(
                LogLevel.FromOrdinal(MinLevel),
                LogLevel.FromOrdinal(MaxLevel),
                WatchdogLoggerConfiguration.GenerateConsoleLogger(LoggerName));

            // Store config
            LogManager.Configuration = this.LoggingConfig;
            this.NLogger = LogManager.GetCurrentClassLogger();
        }
    }
}
