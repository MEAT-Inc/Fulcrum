using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ControlzEx.Theming;
using FulcrumInjector.AppStyles;
using FulcrumInjector.AppStyles.AppStyleLogic;
using FulcrumInjector.JsonHelpers;
using SharpLogger;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Color and Setting Configuration Objects from the config helpers
        public static WindowBlurSetup WindowBlurHelper;
        public static AppThemeConfiguration ThemeConfiguration;

        // ------------------------------------------------------------------------------------

        /// <summary>
        /// Runs this on startup to configure themes and other settings
        /// </summary>
        /// <param name="e">Event args</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Startup override
            base.OnStartup(e);

            // Force the working directory to the running location of the application or set to the debug directory
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            // Logging config and app theme config.
            this.ConfigureLogging();
            this.ConfigureLogCleanup();
            this.ConfigureSingleInstance();
            this.ConfigureCurrentAppTheme();

            // Log passed and ready to run.
            LogBroker.Logger?.WriteLog("LOGGING CONFIGURATION AND THEME SETUP ARE COMPLETE! BOOTING INTO MAIN INSTANCE NOW...", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------

        /// <summary>
        /// Configure new logging instance setup for configurations.
        /// </summary>
        private void ConfigureLogging()
        {
            // Start by building a new logging configuration object and init the broker.
            JsonConfigFiles.SetNewAppConfigFile("FulcrumInjectorSettings.json");
            string AppName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorSettings.AppInstanceName");
            string LoggingPath = ValueLoaders.GetConfigValue<string>("FulcrumInjectorLogging.DefaultLoggingPath");

            // Make logger and build global logger object.
            LogBroker.ConfigureLoggingSession(AppName, LoggingPath);
            LogBroker.BrokerInstance.FillBrokerPool();

            // Log information and current application version.
            string CurrentAppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogBroker.Logger?.WriteLog($"LOGGING FOR {AppName} HAS BEEN STARTED OK!", LogType.WarnLog);
            LogBroker.Logger?.WriteLog($"{AppName} APPLICATION IS NOW LIVE! VERSION: {CurrentAppVersion}", LogType.WarnLog);
        }
        /// <summary>
        /// Configures logging cleanup to archives if needed.
        /// </summary>
        private void ConfigureLogCleanup()
        {
            // Pull values for log archive trigger and set values
            var ConfigObj = ValueLoaders.GetConfigValue<dynamic>("FulcrumInjectorLogging.LogArchiveSetup");

            // Check to see if we need to archive or not.
            LogBroker.Logger?.WriteLog($"CLEANUP ARCHIVE FILE SETUP STARTED! CHECKING FOR {ConfigObj.ArchiveOnFileCount} OR MORE LOG FILES...");
            if (Directory.GetFiles(LogBroker.BaseOutputPath).Length < (int)ConfigObj.ArchiveOnFileCount)
            {
                // Log not cleaning up and return.
                LogBroker.Logger?.WriteLog("NO NEED TO ARCHIVE FILES AT THIS TIME! MOVING ON", LogType.WarnLog);
                return;
            }

            // Begin archive process 
            var ShimFileFilterName = ValueLoaders.GetConfigValue<dynamic>("FulcrumInjectorSettings.ShimInstanceName"); ;
            LogBroker.Logger?.WriteLog($"ARCHIVE PROCESS IS NEEDED! PATH TO STORE FILES IS SET TO {ConfigObj.LogArchivePath}");
            LogBroker.Logger?.WriteLog($"SETTING UP SETS OF {ConfigObj.ArchiveFileSetSize} FILES IN EACH ARCHIVE OBJECT!");
            Task.Run(() =>
            {
                // Run on different thread to avoid clogging up UI
                LogBroker.CleanupLogHistory(ConfigObj.ToString());
                LogBroker.CleanupLogHistory(ConfigObj.ToString(), ShimFileFilterName);

                // See if we have too many archives
                string[] ArchivesFound = Directory.GetFiles(ConfigObj.LogArchivePath);
                int ArchiveSetCount = ConfigObj.ArchiveFileSetSize is int ? (int)ConfigObj.ArchiveFileSetSize : 0;
                if (ArchivesFound.Length >= ArchiveSetCount * 2)
                {
                    // List of files to remove now.
                    LogBroker.Logger?.WriteLog("REMOVING OVERFLOW OF ARCHIVE VALUES NOW...", LogType.WarnLog);
                    var RemoveThese = ArchivesFound
                        .OrderByDescending(FileObj => new FileInfo(FileObj).LastWriteTime)
                        .Skip(ArchiveSetCount * 2);

                    // Remove the remainder now.
                    LogBroker.Logger?.WriteLog($"FOUND A TOTAL OF {RemoveThese.Count()} FILES TO PRUNE");
                    foreach (var FileObject in RemoveThese) { File.Delete(FileObject); }
                    LogBroker.Logger?.WriteLog($"REMOVED ALL THE REQUIRED ARCHIVES OK! LEFT A TOTAL OF {ArchiveSetCount * 2} ARCHIVES BEHIND!", LogType.InfoLog);
                }

                // Log done.
                LogBroker.Logger?.WriteLog($"DONE CLEANING UP LOG FILES! CHECK {ConfigObj.LogArchivePath} FOR NEWLY BUILT ARCHIVE FILES", LogType.InfoLog);
            });
        }
        /// <summary>
        /// Checks for an existing fulcrum process object and kill all but the running one.
        /// </summary>
        private bool ConfigureSingleInstance()
        {
            // Find all the fulcrum process objects now.
            var CurrentInjector = Process.GetCurrentProcess();
            LogBroker.Logger?.WriteLog("KILLING EXISTING FULCRUM INSTANCES NOW!", LogType.WarnLog);
            LogBroker.Logger?.WriteLog($"CURRENT FULCRUM PROCESS IS SEEN TO HAVE A PID OF {CurrentInjector.Id}", LogType.InfoLog);

            // Find the process values here.
            string CurrentInstanceName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorSettings.AppInstanceName");
            LogBroker.Logger?.WriteLog($"CURRENT INJECTOR PROCESS NAME FILTERS ARE: {CurrentInstanceName} AND {CurrentInjector.ProcessName}");
            var InjectorsTotal = Process.GetProcesses()
                .Where(ProcObj => ProcObj.Id != CurrentInjector.Id)
                .Where(ProcObj => ProcObj.ProcessName.Contains(CurrentInstanceName)
                                  || ProcObj.ProcessName.Contains(CurrentInjector.ProcessName))
                .ToList();

            // THIS IS A POTENTIAL ISSUE!
            // BUG: KILLING NEW CAN DROP COMMANDS TO OUR PIPE! WE NEED TO BUILD THIS SO THAT THE OLDEST INSTANCE REMAINS ALIVE!

            // Now kill any existing instances
            LogBroker.Logger?.WriteLog($"FOUND A TOTAL OF {InjectorsTotal.Count} INJECTORS ON OUR MACHINE");
            if (InjectorsTotal.Count > 0)
            {
                // Log removing files and delete the log output
                LogBroker.Logger?.WriteLog("SINCE AN EXISTING INJECTOR WAS FOUND, KILLING ALL BUT THE EXISTING INSTANCE!", LogType.InfoLog);
                File.Delete(LogBroker.MainLogFileName);
                Environment.Exit(100);
            }

            // Return passed output.
            LogBroker.Logger?.WriteLog("NO OTHER INSTANCES FOUND! CLAIMING SINGLETON RIGHTS FOR THIS PROCESS OBJECT NOW...");
            return true;
        }
        /// <summary>
        /// Configure new theme setup for instance objects.
        /// </summary>
        private void ConfigureCurrentAppTheme()
        {
            // Log infos and set values.
            LogBroker.Logger?.WriteLog("SETTING UP MAIN APPLICATION THEME VALUES NOW...", LogType.InfoLog);

            // Set theme configurations
            ThemeManager.Current.SyncTheme();
            ThemeConfiguration = new AppThemeConfiguration();
            ThemeConfiguration.CurrentAppTheme = ThemeConfiguration.PresetThemes[0];
            LogBroker.Logger?.WriteLog("CONFIGURED NEW APP THEME VALUES OK! THEME HAS BEEN APPLIED TO APP INSTANCE!", LogType.InfoLog);
        }
    }
}
