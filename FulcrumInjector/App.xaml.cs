using System;
using System.Collections.Generic;
using System.Configuration;
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
            // Startup override.
            base.OnStartup(e);
            
            // Logging config and app theme config.
            this.ConfigureLogging();
            this.ConfigureLogCleanup();
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
            JsonConfigFiles.SetNewAppConfigFile("FulcrumInjectorConfig.json");
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
            var ConfigObj = ValueLoaders.GetConfigValue<dynamic>("FulcrumInjectorLogging.LogArchiveSetup"); ;

            // Check to see if we need to archive or not.
            LogBroker.Logger?.WriteLog($"CLEANUP ARCHIVE FILE SETUP STARTED! CHECKING FOR {ConfigObj.ArchiveOnFileCount} OR MORE LOG FILES...");
            if (Directory.GetFiles(LogBroker.BaseOutputPath).Length < (int)ConfigObj.ArchiveOnFileCount)
            {
                // Log not cleaning up and return.
                LogBroker.Logger?.WriteLog("NO NEED TO ARCHIVE FILES AT THIS TIME! MOVING ON", LogType.WarnLog);
                return;
            }

            // Begin archive process 
            LogBroker.Logger?.WriteLog($"ARCHIVE PROCESS IS NEEDED! PATH TO STORE FILES IS SET TO {ConfigObj.LogArchivePath}");
            LogBroker.Logger?.WriteLog($"SETTING UP SETS OF {ConfigObj.ArchiveFileSetSize} FILES IN EACH ARCHIVE OBJECT!");
            LogBroker.CleanupLogHistory(ConfigObj.ToString());

            // Log done.
            LogBroker.Logger?.WriteLog($"DONE CLEANING UP LOG FILES! CHECK {ConfigObj.LogArchivePath} FOR NEWLY BUILT ARCHIVE FILES", LogType.InfoLog);
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
