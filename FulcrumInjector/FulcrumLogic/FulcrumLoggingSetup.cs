using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumJsonHelpers;
using FulcrumInjector.FulcrumLogging;
using FulcrumInjector.FulcrumLogging.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic
{
    /// <summary>
    /// Class used to configure new Fulcrum logging configurations
    /// </summary>
    public class FulcrumLoggingSetup
    {
        // Class values for the name and logging path fo this application
        public readonly string AppName;
        public readonly string LoggingPath;

        // ------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new fulcrum logging setup class routine.
        /// </summary>
        /// <param name="AppName"></param>
        /// <param name="LoggingPath"></param>
        public FulcrumLoggingSetup(string AppName, string LoggingPath)
        {
            // Store class values here.
            this.AppName = AppName;
            this.LoggingPath = LoggingPath;
        }


        /// <summary>
        /// Configure new logging instance setup for configurations.
        /// </summary>
        public void ConfigureLogging()
        {
            // Make logger and build global logger object.
            FulcrumLogBroker.ConfigureLoggingSession(this.AppName, this.LoggingPath);
            FulcrumLogBroker.BrokerInstance.FillBrokerPool();

            // Log information and current application version.
            string CurrentAppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            FulcrumLogBroker.Logger?.WriteLog($"LOGGING FOR {AppName} HAS BEEN STARTED OK!", LogType.WarnLog);
            FulcrumLogBroker.Logger?.WriteLog($"{AppName} APPLICATION IS NOW LIVE! VERSION: {CurrentAppVersion}", LogType.WarnLog);
        }
        /// <summary>
        /// Configures logging cleanup to archives if needed.
        /// </summary>
        public void ConfigureLogCleanup()
        {
            // Pull values for log archive trigger and set values
            var ConfigObj = ValueLoaders.GetConfigValue<dynamic>("FulcrumLogging.LogArchiveSetup");
            FulcrumLogBroker.Logger?.WriteLog($"CLEANUP ARCHIVE FILE SETUP STARTED! CHECKING FOR {ConfigObj.ArchiveOnFileCount} OR MORE LOG FILES...");
            if (Directory.GetFiles(FulcrumLogBroker.BaseOutputPath).Length < (int)ConfigObj.ArchiveOnFileCount)
            {
                // Log not cleaning up and return.
                FulcrumLogBroker.Logger?.WriteLog("NO NEED TO ARCHIVE FILES AT THIS TIME! MOVING ON", LogType.WarnLog);
                return;
            }

            // Begin archive process 
            FulcrumLogBroker.Logger?.WriteLog($"ARCHIVE PROCESS IS NEEDED! PATH TO STORE FILES IS SET TO {ConfigObj.LogArchivePath}");
            FulcrumLogBroker.Logger?.WriteLog($"SETTING UP SETS OF {ConfigObj.ArchiveFileSetSize} FILES IN EACH ARCHIVE OBJECT!");
            FulcrumLogBroker.CleanupLogHistory(ConfigObj.ToString());

            // Log done.
            FulcrumLogBroker.Logger?.WriteLog($"DONE CLEANING UP LOG FILES! CHECK {ConfigObj.LogArchivePath} FOR NEWLY BUILT ARCHIVE FILES", LogType.InfoLog);
        }
    }
}
