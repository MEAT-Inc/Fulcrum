using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ControlzEx.Theming;
using FulcrumInjector.FulcrumViewContent;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonHelpers;
using FulcrumInjector.FulcrumViewSupport.FulcrumStyles;
using NLog;
using NLog.Fluent;
using SharpLogging;

namespace FulcrumInjector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger for our application instance object
        private SharpLogger _appLogger;

        // Color and Setting Configuration Objects from the config helpers
        internal static WindowBlurSetup WindowBlurHelper;
        internal static AppThemeConfiguration ThemeConfiguration;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Runs this on startup to configure themes and other settings
        /// </summary>
        /// <param name="e">Event args</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Startup override
            base.OnStartup(e);

            // Force the working directory. Build JSON settings objects
            JsonConfigFiles.SetInjectorConfigFile("FulcrumInjectorSettings.json");

            // Setup our logging instance information
            this._configureInjectorLogging();

            // Run single instance configuration
            this._configureSingleInstance();
            this._configureAppExitRoutine();

            // Configure settings and app theme
            this._configureCurrentTheme();
            this._configureUserSettings();
            this._configureSingletonViews();

            // Log out that all of our startup routines are complete
            SharpLogBroker.MasterLogger?.WriteLog("SETTINGS AND THEME SETUP ARE COMPLETE! BOOTING INTO MAIN INSTANCE NOW...", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Imports the Injector logging and archiving configuration objects from our settings file and sets up logging for this instance
        /// </summary>
        private void _configureInjectorLogging()
        {
            // Load in and apply the log archive and log broker configurations for this instance
            var BrokerConfig = ValueLoaders.GetConfigValue<SharpLogBroker.BrokerConfiguration>("InjectorLogging.LogBrokerConfiguration");
            var ArchiverConfig = ValueLoaders.GetConfigValue<SharpLogArchiver.ArchiveConfiguration>("InjectorLogging.LogArchiveConfiguration");

            // Make logger and build global logger object.
            SharpLogBroker.InitializeLogging(BrokerConfig);
            SharpLogArchiver.InitializeArchiving(ArchiverConfig);

            // Build a new logger for this app instance and log our some basic information
            this._appLogger = new SharpLogger(LoggerActions.UniversalLogger);
            string CurrentShimVersion = FulcrumConstants.FulcrumVersions.ShimVersionString;
            string CurrentAppVersion = FulcrumConstants.FulcrumVersions.InjectorVersionString;

            // Log out this application has been booted correctly
            this._appLogger.WriteLog($"LOGGING FOR {BrokerConfig.LogBrokerName} HAS BEEN STARTED OK!", LogType.WarnLog);
            this._appLogger.WriteLog($"{BrokerConfig.LogBrokerName} APPLICATION IS NOW LIVE!", LogType.WarnLog);
            this._appLogger.WriteLog($"--> INJECTOR VERSION: {CurrentAppVersion}", LogType.WarnLog);
            this._appLogger.WriteLog($"--> SHIM DLL VERSION: {CurrentShimVersion}", LogType.WarnLog);
        }
        /// <summary>
        /// Checks for an existing fulcrum process object and kill all but the running one.
        /// </summary>
        private void _configureSingleInstance()
        {
            // Find all the fulcrum process objects now.
            var CurrentInjector = Process.GetCurrentProcess();
            SharpLogBroker.MasterLogger?.WriteLog("KILLING EXISTING FULCRUM INSTANCES NOW!", LogType.WarnLog);
            SharpLogBroker.MasterLogger?.WriteLog($"CURRENT FULCRUM PROCESS IS SEEN TO HAVE A PID OF {CurrentInjector.Id}", LogType.InfoLog);

            // Find the process values here.
            string CurrentInstanceName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.AppInstanceName");
            SharpLogBroker.MasterLogger?.WriteLog($"CURRENT INJECTOR PROCESS NAME FILTERS ARE: {CurrentInstanceName} AND {CurrentInjector.ProcessName}");
            var InjectorsTotal = Process.GetProcesses()
                .Where(ProcObj => ProcObj.Id != CurrentInjector.Id)
                .Where(ProcObj => ProcObj.ProcessName.Contains(CurrentInstanceName)
                                  || ProcObj.ProcessName.Contains(CurrentInjector.ProcessName))
                .ToList();

            // Now kill any existing instances
            SharpLogBroker.MasterLogger?.WriteLog($"FOUND A TOTAL OF {InjectorsTotal.Count} INJECTORS ON OUR MACHINE");
            if (InjectorsTotal.Count > 0)
            {
                // Log removing files and delete the log output
                SharpLogBroker.MasterLogger?.WriteLog("SINCE AN EXISTING INJECTOR WAS FOUND, KILLING ALL BUT THE EXISTING INSTANCE!", LogType.InfoLog);
                try { File.Delete(SharpLogBroker.LogFilePath); }
                catch { SharpLogBroker.MasterLogger?.WriteLog("CAN NOT DELETE NON EXISTENT FILES!", LogType.ErrorLog); }

                // Exit the application
                Environment.Exit(100);
            }

            // Return passed output.
            SharpLogBroker.MasterLogger?.WriteLog("NO OTHER INSTANCES FOUND! CLAIMING SINGLETON RIGHTS FOR THIS PROCESS OBJECT NOW...");
        }
        /// <summary>
        /// Builds an event control object for methods to run when the app closes out.
        /// </summary>
        private void _configureAppExitRoutine()
        {
            // Build event helper, Log done and return out.
            Current.Exit += (SendingAppplication, ExitEventArgs) =>
            {
                // First spawn an exit helper logger and log information 
                SharpLogger ExitLogger = new SharpLogger(LoggerActions.UniversalLogger, "ExitEventLogger");
                ExitLogger.WriteLog("PROCESSED APP ENVIRONMENT OBJECT SHUTDOWN COMMAND OK!", LogType.WarnLog);
                ExitLogger.WriteLog("CLOSING THIS INSTANCE CLEANLY AND THEN FORCE RUNNING A TERMINATION COMMAND!", LogType.InfoLog);

                // Now build a process object. Simple bat file that runs a Taskkill instance on this app after waiting 3 seconds.
                string TempBat = Path.ChangeExtension(Path.GetTempFileName(), "bat");
                string CurrentInstanceName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.AppInstanceName");
                string BatContents = string.Join("\n", new string[]
                {
                    "timeout /t 5 /nobreak > NUL",
                    $"taskkill /F /IM {CurrentInstanceName}*"
                });

                // Write temp bat file to output and then run it.
                ExitLogger.WriteLog($"BAT FILE LOCATION WAS GENERATED AND SET TO {TempBat}", LogType.InfoLog);
                ExitLogger.WriteLog($"BUILDING OUTPUT BAT FILE WITH CONTENTS OF {BatContents}", LogType.TraceLog);
                File.WriteAllText(TempBat, BatContents);

                // Now run the output command.
                ExitLogger.WriteLog("RUNNING TERMINATION COMMAND INSTANCE NOW...", LogType.WarnLog);
                ExitLogger.WriteLog("THIS SHOULD BE THE LAST TIME THIS LOG FILE IS USED!", LogType.InfoLog);
                ProcessStartInfo TerminateInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    CreateNoWindow = true,
                    Arguments = $"/C \"{TempBat}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                // Execute here and exit out app.
                ExitLogger.WriteLog($"EXECUTING NOW! TIME OF APP EXIT: {DateTime.Now:R}", LogType.WarnLog);
                Process.Start(TerminateInfo);
            };

            // Log that we've hooked in a new exit routine to our window instance
            SharpLogBroker.MasterLogger?.WriteLog("TACKED ON NEW PROCESS EVENT WATCHDOG FOR EXIT ROUTINE!", LogType.InfoLog);
            SharpLogBroker.MasterLogger?.WriteLog("WHEN OUR APP EXITS OUT, IT WILL INVOKE THE REQUESTED METHOD BOUND", LogType.TraceLog);
        }
        /// <summary>
        /// Pulls in the resource dictionaries from the given resource path and stores them in the app
        /// </summary>
        private void _configureSingletonViews()
        {
            // Log information. Pull files in and store them all. This tuple create call pulls types for views then types for view models
            SharpLogBroker.MasterLogger?.WriteLog("GENERATING STATIC VIEW CONTENTS FOR HAMBURGER CORE CONTENTS NOW...", LogType.WarnLog);
            var LoopResultCast = Assembly.GetExecutingAssembly().GetTypes().Where(TypePulled =>
                    TypePulled.Namespace != null && !TypePulled.Name.Contains("HamburgerCore") && 
                    (TypePulled.Namespace.Contains("InjectorCoreView") || TypePulled.Namespace.Contains("InjectorOptionView")))
                .ToLookup(TypePulled => TypePulled.Name.EndsWith("View") || TypePulled.Name.EndsWith("ViewModel"));

            // Now build singleton instances for the types required.
            var ViewTypes = LoopResultCast[true].Where(TypeValue => TypeValue.Name.EndsWith("View")).ToArray();
            var ViewModelTypes = LoopResultCast[true].Where(TypeValue => TypeValue.Name.EndsWith("ViewModel")).ToArray();
            if (ViewTypes.Length != ViewModelTypes.Length) SharpLogBroker.MasterLogger?.WriteLog("WARNING! TYPE OUTPUT LISTS ARE NOT EQUAL SIZES!", LogType.ErrorLog);

            // Loop operation here
            int MaxLoopIndex = Math.Min(ViewTypes.Length, ViewModelTypes.Length);
            SharpLogBroker.MasterLogger?.WriteLog($"BUILDING TYPE INSTANCES NOW...", LogType.InfoLog);
            SharpLogBroker.MasterLogger?.WriteLog($"A TOTAL OF {MaxLoopIndex} BASE ASSEMBLY TYPES ARE BEING SPLIT AND PROCESSED...", LogType.InfoLog);
            for (int IndexValue = 0; IndexValue < MaxLoopIndex; IndexValue += 1)
            {
                // Pull type values here
                Type ViewType = ViewTypes[IndexValue]; Type ViewModelType = ViewModelTypes[IndexValue];
                SharpLogBroker.MasterLogger?.WriteLog("   --> PULLED IN NEW TYPES FOR ENTRY OBJECT OK!", LogType.InfoLog);
                SharpLogBroker.MasterLogger?.WriteLog($"   --> VIEW TYPE:       {ViewType.Name}", LogType.InfoLog);
                SharpLogBroker.MasterLogger?.WriteLog($"   --> VIEW MODEL TYPE: {ViewModelType.Name}", LogType.InfoLog);

                // Generate our singleton object here.
                var BuiltSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.CreateSingletonInstance(ViewType, ViewModelType);
                SharpLogBroker.MasterLogger?.WriteLog("   --> NEW SINGLETON INSTANCE BUILT FOR VIEW AND VIEWMODEL TYPES CORRECTLY!", LogType.InfoLog);
                SharpLogBroker.MasterLogger?.WriteLog($"   --> SINGLETON TYPE: {BuiltSingleton.GetType().FullName} WAS BUILT OK!", LogType.TraceLog);
            }

            // Log completed building and exit routine
            SharpLogBroker.MasterLogger?.WriteLog("BUILT OUTPUT TYPE CONTENTS OK! THESE VALUES ARE NOW STORED ON OUR MAIN WINDOW INSTANCE!", LogType.WarnLog);
            SharpLogBroker.MasterLogger?.WriteLog("THE TYPE OUTPUT BUILT IS BEING PROJECTED ONTO THE FULCRUM INJECTOR CONSTANTS STORE OBJECT!", LogType.WarnLog);
        }
        /// <summary>
        /// Configure new theme setup for instance objects.
        /// </summary>
        private void _configureCurrentTheme()
        {
            // Log infos and set values.
            SharpLogBroker.MasterLogger?.WriteLog("SETTING UP MAIN APPLICATION THEME VALUES NOW...", LogType.InfoLog);

            // Set theme configurations
            ThemeManager.Current.SyncTheme();
            ThemeConfiguration = new AppThemeConfiguration();
            ThemeConfiguration.CurrentAppStyleModel = ThemeConfiguration.PresetThemes[0];
            SharpLogBroker.MasterLogger?.WriteLog("CONFIGURED NEW APP THEME VALUES OK! THEME HAS BEEN APPLIED TO APP INSTANCE!", LogType.InfoLog);
        }
        /// <summary>
        /// Pulls in the user settings from our JSON configuration file and stores them to the injector store 
        /// </summary>
        private void _configureUserSettings()
        {
            // Pull our settings objects out from the settings file.
            FulcrumConstants._injectorSettings.GenerateSettingsModels();
            SharpLogBroker.MasterLogger?.WriteLog($"PULLED IN ALL SETTINGS SEGMENTS OK!", LogType.InfoLog);
            SharpLogBroker.MasterLogger?.WriteLog("IMPORTED SETTINGS OBJECTS CORRECTLY! READY TO GENERATE UI COMPONENTS FOR THEM NOW...");
        }
    }
}
