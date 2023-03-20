using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ControlzEx.Theming;
using FulcrumInjector.FulcrumViewContent;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumStyles;
using NLog.Targets;
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

        // Logger for our application instance object and the current theme configuration applied to it
        private SharpLogger _appLogger;
        internal AppThemeConfiguration ThemeConfiguration;

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
            JsonConfigFile.SetInjectorConfigFile("FulcrumInjectorSettings.json");

            // Setup our logging instance information
            this._configureInjectorLogging();
            this._configureExceptionHandlers();

            // Run single instance configuration
            this._configureSingleInstance();
            this._configureAppExitRoutine();
            this._configureInjectorWatchdog();

            // Configure settings and app theme
            this._configureCurrentTheme();
            this._configureUserSettings();
            this._configureSingletonViews();

            // Log out that all of our startup routines are complete and prepare to open up the main window instance
            this._appLogger.WriteLog(string.Join(string.Empty, Enumerable.Repeat("=", 200)), LogType.WarnLog);
            this._appLogger.WriteLog("ALL REQUIRED FULCRUM INJECTOR STARTUP ROUTINES ARE DONE! MAIN WINDOW OPENING UP NOW...", LogType.InfoLog);
            this._appLogger.WriteLog(string.Join(string.Empty, Enumerable.Repeat("=", 200)), LogType.WarnLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Imports the Injector logging and archiving configuration objects from our settings file and sets up logging for this instance
        /// </summary>
        private void _configureInjectorLogging()
        {
            // Load in and apply the log archive and log broker configurations for this instance
            var BrokerConfig = ValueLoaders.GetConfigValue<SharpLogBroker.BrokerConfiguration>("FulcrumLogging.LogBrokerConfiguration");
            var ArchiverConfig = ValueLoaders.GetConfigValue<SharpLogArchiver.ArchiveConfiguration>("FulcrumLogging.LogArchiveConfiguration");

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

            // Finally invoke an archive routine and child folder cleanup routine if needed
            Task.Run(() =>
            {
                // Log archive routines have been queued
                this._appLogger.WriteLog("LOGGING ARCHIVE ROUTINES HAVE BEEN KICKED OFF IN THE BACKGROUND!", LogType.WarnLog);
                this._appLogger.WriteLog("PROGRESS FOR ARCHIVAL ROUTINES WILL APPEAR IN THE CONSOLE/FILE TARGET OUTPUTS!");

                // Start with booting the archive routine
                SharpLogArchiver.ArchiveLogFiles();
                this._appLogger.WriteLog("ARCHIVE ROUTINES HAVE BEEN COMPLETED!", LogType.InfoLog);

                // Then finally invoke the archive cleanup routines
                SharpLogArchiver.CleanupArchiveHistory();
                this._appLogger.WriteLog("ARCHIVE CLEANUP ROUTINES HAVE BEEN COMPLETED!", LogType.InfoLog);
            });
            Task.Run(() =>
            {
                // Log archive routines have been queued
                this._appLogger.WriteLog("LOGGING SUBFOLDER PURGE ROUTINES HAVE BEEN KICKED OFF IN THE BACKGROUND!", LogType.WarnLog);
                this._appLogger.WriteLog("PROGRESS FOR SUBFOLDER PURGE ROUTINES WILL APPEAR IN THE CONSOLE/FILE TARGET OUTPUTS!");

                // Call the cleanup method to purge our subdirectories if needed
                SharpLogArchiver.CleanupSubdirectories();
                this._appLogger.WriteLog("CLEANED UP ALL CHILD LOGGING FOLDERS!", LogType.InfoLog);
            });
        }
        /// <summary>
        /// Configures a new DispatcherUnhandledExceptionEventHandler for ths injector instance so we can track
        /// logged failures as they occur inside this application in real time
        /// </summary>
        private void _configureExceptionHandlers()
        {
            // Start by spawning a dedicated exception catching logger instance
            string LoggerName = $"{SharpLogBroker.LogBrokerName}_ExceptionsLogger";

            // Log that our exception logger was built without issues
            this._appLogger.WriteLog("CONFIGURING NEW UNHANDLED EXCEPTION LOGGER AND APP EVENT HANDLER NOW...");
            this._appLogger.WriteLog($"BUILT NEW UNIVERSAL EXCEPTIONS LOGGER FOR THE INJECTOR APP OK!", LogType.InfoLog);

            // Now that we've got this logger, hook in a new event to our app instance to deal with unhandled exceptions
            this.DispatcherUnhandledException += (_, ExceptionArgs) =>
            {
                // Make sure our logging object is configured first
                SharpLogger InstanceLogger =
                    SharpLogBroker.FindLoggers(LoggerName).FirstOrDefault() ??
                    new SharpLogger(LoggerActions.UniversalLogger, LoggerName);

                // Now log the exception thrown and process the exception to a handled state
                string ExInfo = $"UNHANDLED APP LEVEL EXCEPTION PROCESSED AT {DateTime.Now:g}!";
                InstanceLogger.WriteException(ExInfo, ExceptionArgs.Exception, LogType.ErrorLog);
                ExceptionArgs.Handled = true;

                // Once our exception is handled, we can throw up our flyout for errors
                // TODO: Build new flyout view content for showing failures as they come up
            };
        }
        /// <summary>
        /// Checks for an existing fulcrum process object and kill all but the running one.
        /// </summary>
        private void _configureSingleInstance()
        {
            // Find all the fulcrum process objects now.
            var CurrentInjector = Process.GetCurrentProcess();
            this._appLogger?.WriteLog("KILLING EXISTING FULCRUM INSTANCES NOW!", LogType.WarnLog);
            this._appLogger?.WriteLog($"CURRENT FULCRUM PROCESS IS SEEN TO HAVE A PID OF {CurrentInjector.Id}", LogType.InfoLog);

            // Find the process values here.
            string CurrentInstanceName = ValueLoaders.GetConfigValue<string>("FulcrumConstants.AppInstanceName");
            this._appLogger?.WriteLog($"CURRENT INJECTOR PROCESS NAME FILTERS ARE: {CurrentInstanceName} AND {CurrentInjector.ProcessName}");
            var InjectorsTotal = Process.GetProcesses()
                .Where(ProcObj => ProcObj.Id != CurrentInjector.Id)
                .Where(ProcObj => ProcObj.ProcessName.Contains(CurrentInstanceName) || ProcObj.ProcessName.Contains(CurrentInjector.ProcessName))
                .ToList();

            // Now kill any existing instances
            this._appLogger?.WriteLog($"FOUND A TOTAL OF {InjectorsTotal.Count} INJECTORS ON OUR MACHINE");
            if (InjectorsTotal.Count > 0)
            {
                // Log removing files and delete the log output
                this._appLogger?.WriteLog("SINCE AN EXISTING INJECTOR WAS FOUND, KILLING ALL BUT THE EXISTING INSTANCE!", LogType.InfoLog);
                try { File.Delete(SharpLogBroker.LogFilePath); }
                catch { this._appLogger?.WriteLog("CAN NOT DELETE NON EXISTENT FILES!", LogType.ErrorLog); }

                // Exit the application
                Environment.Exit(100);
            }

            // Return passed output.
            this._appLogger?.WriteLog("NO OTHER INSTANCES FOUND! CLAIMING SINGLETON RIGHTS FOR THIS PROCESS OBJECT NOW...");
        }
        /// <summary>
        /// Builds an event control object for methods to run when the app closes out.
        /// </summary>
        private void _configureAppExitRoutine()
        {
            // Build event helper, Log done and return out.
            Current.Exit += (_, _) =>
            {
                // First spawn an exit helper logger and log information 
                SharpLogger ExitLogger = new SharpLogger(LoggerActions.UniversalLogger, "ExitEventLogger");
                ExitLogger.WriteLog("PROCESSED APP ENVIRONMENT OBJECT SHUTDOWN COMMAND OK!", LogType.WarnLog);
                ExitLogger.WriteLog("CLOSING THIS INSTANCE CLEANLY AND THEN FORCE RUNNING A TERMINATION COMMAND!", LogType.InfoLog);

                // Now build a process object. Simple bat file that runs a Taskkill instance on this app after waiting 3 seconds.
                string TempBat = Path.ChangeExtension(Path.GetTempFileName(), "bat");
                string CurrentInstanceName = ValueLoaders.GetConfigValue<string>("FulcrumConstants.AppInstanceName");
                string BatContents = string.Join("\n", new string[]
                {
                    "timeout /t 5 /nobreak > NUL",
                    $"taskkill /F /IM {CurrentInstanceName}*"
                });

                // Write temp bat file to output and then run it.
                ExitLogger.WriteLog($"BAT FILE LOCATION WAS GENERATED AND SET TO {TempBat}", LogType.InfoLog);
                ExitLogger.WriteLog($"BUILDING OUTPUT BAT FILE WITH CONTENTS OF {BatContents.Replace("\n", " ")}", LogType.TraceLog);
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
            this._appLogger?.WriteLog("TACKED ON NEW PROCESS EVENT WATCHDOG FOR EXIT ROUTINE!", LogType.InfoLog);
            this._appLogger?.WriteLog("WHEN OUR APP EXITS OUT, IT WILL INVOKE THE REQUESTED METHOD BOUND", LogType.TraceLog);
        }
        /// <summary>
        /// Configures a new instance of a watchdog helper for the injector log files folder and starts it
        /// </summary>
        private void _configureInjectorWatchdog()
        {
            // Make sure we actually want to use this watchdog service 
            if (!ValueLoaders.GetConfigValue<bool>("FulcrumWatchdog.WatchdogEnabled"))
            {
                // Log that the watchdog is disabled and exit out
                this._appLogger.WriteLog("WARNING! WATCHDOG SERVICE IS TURNED OFF IN OUR CONFIGURATION FILE! NOT BOOTING IT", LogType.WarnLog);
                this._appLogger.WriteLog("CHANGE THE VALUE OF JSON FIELD WatchdogEnabled TO TRUE TO ENABLE OUR WATCHDOG!", LogType.WarnLog);
                return;
            }

            // BUG: Starting new watchdog instances for many log files is broken
            // Spin up a new injector watchdog service here if needed           
            Task.Run(() =>
            {
                FulcrumConstants.FulcrumWatchdog = new FulcrumWatchdogService();
                FulcrumConstants.FulcrumWatchdog.StartWatchdogService();
            });

            // Log that we've booted this new service instance correctly and exit out
            this._appLogger.WriteLog("SPAWNED NEW INJECTOR WATCHDOG SERVICE OK! BOOTING IT NOW...", LogType.WarnLog);
            this._appLogger.WriteLog("BOOTED NEW INJECTOR WATCHDOG SERVICE OK! DIRECTORIES AND FILES WILL BE MONITORED!", LogType.InfoLog);
        }
        /// <summary>
        /// Pulls in the resource dictionaries from the given resource path and stores them in the app
        /// </summary>
        private void _configureSingletonViews()
        {
            // Log information. Pull files in and store them all. This tuple create call pulls types for views then types for view models
            this._appLogger?.WriteLog("GENERATING STATIC VIEW CONTENTS FOR HAMBURGER CORE CONTENTS NOW...", LogType.WarnLog);
            var LoopResultCast = Assembly.GetExecutingAssembly().GetTypes().Where(TypePulled =>
                    TypePulled.Namespace != null && !TypePulled.Name.Contains("HamburgerCore") && 
                    (TypePulled.Namespace.Contains("InjectorCoreView") || TypePulled.Namespace.Contains("InjectorOptionView")))
                .ToLookup(TypePulled => TypePulled.Name.EndsWith("View") || TypePulled.Name.EndsWith("ViewModel"));

            // Now build singleton instances for the types required.
            var ViewTypes = LoopResultCast[true].Where(TypeValue => TypeValue.Name.EndsWith("View")).ToArray();
            var ViewModelTypes = LoopResultCast[true].Where(TypeValue => TypeValue.Name.EndsWith("ViewModel")).ToArray();
            if (ViewTypes.Length != ViewModelTypes.Length) this._appLogger?.WriteLog("WARNING! TYPE OUTPUT LISTS ARE NOT EQUAL SIZES!", LogType.ErrorLog);

            // Configure a new Viewmodel base for the hamburger now
            this._appLogger.WriteLog("SPAWNING NEW HAMBURGER CORE VIEW AND VIEW MODEL...", LogType.InfoLog);
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.CreateSingletonInstance(
                typeof(FulcrumHamburgerCoreView),
                typeof(FulcrumHamburgerCoreViewModel));

            // Loop operation here
            int MaxLoopIndex = Math.Min(ViewTypes.Length, ViewModelTypes.Length);
            this._appLogger?.WriteLog($"BUILDING TYPE INSTANCES NOW...", LogType.InfoLog);
            this._appLogger?.WriteLog($"A TOTAL OF {MaxLoopIndex} BASE ASSEMBLY TYPES ARE BEING SPLIT AND PROCESSED...", LogType.InfoLog);
            for (int IndexValue = 0; IndexValue < MaxLoopIndex; IndexValue += 1)
            {
                // Pull type values here
                Type ViewType = ViewTypes[IndexValue]; Type ViewModelType = ViewModelTypes[IndexValue];
                this._appLogger?.WriteLog("PULLED IN NEW TYPES FOR ENTRY OBJECT OK!", LogType.InfoLog);
                this._appLogger?.WriteLog($"VIEW TYPE:       {ViewType.Name}", LogType.InfoLog);
                this._appLogger?.WriteLog($"VIEW MODEL TYPE: {ViewModelType.Name}", LogType.InfoLog);

                // Generate our singleton object here.
                var BuiltSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.CreateSingletonInstance(ViewType, ViewModelType);
                this._appLogger?.WriteLog("NEW SINGLETON INSTANCE BUILT FOR VIEW AND VIEWMODEL TYPES CORRECTLY!", LogType.InfoLog);
                this._appLogger?.WriteLog($"SINGLETON TYPE: {BuiltSingleton.GetType().FullName} WAS BUILT OK!", LogType.TraceLog);
            }

            // Log completed building and exit routine
            this._appLogger?.WriteLog("BUILT OUTPUT TYPE CONTENTS OK! THESE VALUES ARE NOW STORED ON OUR MAIN WINDOW INSTANCE!", LogType.WarnLog);
            this._appLogger?.WriteLog("THE TYPE OUTPUT BUILT IS BEING PROJECTED ONTO THE FULCRUM INJECTOR CONSTANTS STORE OBJECT!", LogType.WarnLog);
        }
        /// <summary>
        /// Configure new theme setup for instance objects.
        /// </summary>
        private void _configureCurrentTheme()
        {
            // Log infos and set values.
            this._appLogger?.WriteLog("SETTING UP MAIN APPLICATION THEME VALUES NOW...", LogType.InfoLog);

            // Set theme configurations
            ThemeManager.Current.SyncTheme();
            this.ThemeConfiguration = new AppThemeConfiguration();
            this.ThemeConfiguration.CurrentAppStyleModel = ThemeConfiguration.PresetThemes[0];
            this._appLogger?.WriteLog("CONFIGURED NEW APP THEME VALUES OK! THEME HAS BEEN APPLIED TO APP INSTANCE!", LogType.InfoLog);
        }
        /// <summary>
        /// Pulls in the user settings from our JSON configuration file and stores them to the injector store 
        /// </summary>
        private void _configureUserSettings()
        {
            // Pull our settings objects out from the settings file.
            FulcrumConstants.FulcrumSettings = FulcrumSettingsShare.GenerateSettingsShare();
            this._appLogger?.WriteLog($"PULLED IN ALL SETTINGS SEGMENTS OK!", LogType.InfoLog);
            this._appLogger?.WriteLog("IMPORTED SETTINGS OBJECTS CORRECTLY! READY TO GENERATE UI COMPONENTS FOR THEM NOW...");
        }
    }
}
