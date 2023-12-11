using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ControlzEx.Theming;
using FulcrumDriveService;
using FulcrumEmailService;
using FulcrumEncryption;
using FulcrumInjector.FulcrumViewContent;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumStyles;
using FulcrumJson;
using FulcrumSupport;
using FulcrumUpdaterService;
using FulcrumWatchdogService;
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

            // Setup logging and exception handlers
            this._configureInjectorLogging();
            this._configureExceptionHandlers();

            // Configure our application theme, enforce a single instance, and setup our exit routines
            this._configureCurrentTheme();
            this._configureSingleInstance();
            this._configureAppExitRoutine();

            // Finally, validate our encryption settings, import user settings, and start our services
            this._configureUserSettings();
            this._configureCryptographicKeys();
            this._configureInjectorServices();

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
            // Load and configure our log broker instance here if needed
            var BrokerConfig = ValueLoaders.GetConfigValue<SharpLogBroker.BrokerConfiguration>("FulcrumLogging.LogBrokerConfiguration");
            if (!SharpLogBroker.InitializeLogging(BrokerConfig))
                throw new InvalidOperationException("Error! Failed to configure log broker instance for the FulcrumInjector!");

            // Load and configure our log archiver instance here if needed
            var ArchiverConfig = ValueLoaders.GetConfigValue<SharpLogArchiver.ArchiveConfiguration>("FulcrumLogging.LogArchiveConfiguration"); 
            if (!SharpLogArchiver.InitializeArchiving(ArchiverConfig))
                throw new InvalidOperationException("Error! Failed to configure log archiver instance for the FulcrumInjector!");

            // Build a new logger for this app instance and log our some basic information
            this._appLogger = new SharpLogger(LoggerActions.UniversalLogger);
            string CurrentShimVersion = FulcrumVersionInfo.ShimVersionString;
            string CurrentAppVersion = FulcrumVersionInfo.InjectorVersionString;

            // Log out this application has been booted correctly
            this._appLogger.WriteLog($"LOGGING FOR {SharpLogBroker.LogBrokerName} HAS BEEN STARTED OK!", LogType.WarnLog);
            this._appLogger.WriteLog($"{SharpLogBroker.LogBrokerName} APPLICATION IS NOW LIVE!", LogType.WarnLog);
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
                Environment.Exit(0);
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
        /// Pulls in the user settings from our JSON configuration file and stores them to the injector store 
        /// </summary>
        private void _configureUserSettings()
        {
            // Pull our settings objects out from the settings file.
            FulcrumConstants.FulcrumSettings = FulcrumSettingsShare.GenerateSettingsShare();
            this._appLogger?.WriteLog($"PULLED IN ALL SETTINGS SEGMENTS OK!", LogType.InfoLog);
            this._appLogger?.WriteLog("IMPORTED SETTINGS OBJECTS CORRECTLY! READY TO GENERATE UI COMPONENTS FOR THEM NOW...");
        }
        /// <summary>
        /// Validates the encryption key configuration for the injector application. Will allow a chance to
        /// provide keys to the application if no keys are given in the encryption file for debug runs.
        /// </summary>
        private void _configureCryptographicKeys()
        {
            // Log out that we're configuring encryption keys here and check if they're configured
            this._appLogger.WriteLog("VALIDATING ENCRYPTION KEY CONFIGURATION NOW...", LogType.WarnLog);
            if (FulcrumEncryptionKeys.IsEncryptionConfigured)
            {
                // Log out that our encryption keys are configured and exit out
                this._appLogger.WriteLog("ENCRYPTION KEYS ARE CONFIGURED CORRECTLY!", LogType.InfoLog);
                this._appLogger.WriteLog("MOVING ONTO REMAINDER OF INJECTOR STARTUP ROUTINES...", LogType.InfoLog);
                return;
            }

            // Invoke our configure encryption routine and check the result of it here
            this._appLogger.WriteLog("INVOKING NEW ENCRYPTION KEY CONFIGURATION ROUTINE NOW...", LogType.WarnLog);
            if (FulcrumEncryptionWindow.ConfigureEncryptionKeys()) return;

            // If the configuration is not determined still, exit out of this application
            this._appLogger.WriteLog("ERROR! ENCRYPTION IS STILL NOT CONFIGURED CORRECTLY!", LogType.ErrorLog);
            this._appLogger.WriteLog("EXITING THE INJECTOR APP NOW...", LogType.ErrorLog);
            Environment.Exit(0);
        }
        /// <summary>
        /// Configures service objects for use inside the injector application.
        /// Will either boot services in client consumption mode, or update them using debug builds and reboot them
        /// </summary>
        private void _configureInjectorServices()
        {
            // Begin by checking service installation state values here 
            this._appLogger?.WriteLog("VALIDATING INJECTOR SERVICE INSTALL LOCATIONS...", LogType.WarnLog);
            if (!FulcrumServiceErrorWindow.ValidateServiceConfiguration())
            {
                // Log out that we're missing one or more services and 
                this._appLogger?.WriteLog("ERROR! COULD NOT FIND ALL INJECTOR SERVICES!", LogType.ErrorLog);
                this._appLogger?.WriteLog("EXITING INJECTOR APPLICATION SINCE IS A FATAL ERROR!", LogType.ErrorLog);
                Environment.Exit(0);
            }

            // Log out that we're building these service instances in a release mode for use
            this._appLogger?.WriteLog("INJECTOR SERVICE CONFIGURATION IS NORMAL!", LogType.InfoLog);
            this._appLogger?.WriteLog("BOOTING INJECTOR SERVICE INSTANCES NOW...", LogType.WarnLog);

            // If no debugger is found, just spawn service instances and wait for creation
            FulcrumEmail.InitializeEmailService();
            FulcrumDrive.InitializeDriveService(); 
            FulcrumUpdater.InitializeUpdaterService();
            FulcrumWatchdog.InitializeWatchdogService();

            // Log out that we've booted all of our service instances here
            this._appLogger?.WriteLog("BOOTED ALL SERVICE TASKS CORRECTLY!", LogType.InfoLog);
            this._appLogger?.WriteLog("IF SERVICE ROUTINES FAIL TO EXECUTE, PLEASE ENSURE SERVICE INSTANCES ARE ACTIVE ON THIS MACHINE!", LogType.InfoLog);
        }
    }
}
