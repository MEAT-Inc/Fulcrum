using NLog.Targets;
using SharpLogging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using FulcrumSupport;
using FulcrumJson;

namespace FulcrumService
{
    /// <summary>
    /// Base class definition for a fulcrum service instance
    /// </summary>
    public class FulcrumServiceBase : ServiceBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private/protected fields for service instances
        private readonly IContainer _components;             // Component objects used by this service instance
        protected readonly ServiceTypes _serviceType;        // The type of service this class represents
        protected readonly SharpLogger _serviceLogger;       // Logger instance for our service
        protected static SharpLogger _serviceInitLogger;     // Logger used to configure service initialization routines

        #endregion // Fields

        #region Properties

        // Protected property holding our file target for this service
        protected FileTarget ServiceLoggingTarget { get; set; }

        // Properties holding information about the installed windows service object
        protected ServiceController ServiceInstance { get; set; }
        public bool IsServiceInstance => this.ServiceInstance == null;

        // Properties holding information about JSON output data location for custom commands
        protected string ServiceJsonLocation { get; set; }

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration holding different service types for our services
        /// </summary>
        protected enum ServiceTypes
        {
            [Description("FulcrumServiceBase")]  BASE_SERVICE,       // Default value. Base service type
            [Description("FulcrumWatchdog")]     WATCHDOG_SERVICE,   // Watchdog Service Type
            [Description("FulcrumDrive")]        DRIVE_SERVICE,      // Drive Service type
            [Description("FulcrumEmail")]        EMAIL_SERVICE,      // Email Service Type
            [Description("FulcrumUpdater")]      UPDATER_SERVICE     // Updater Service Type
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="IsDisposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool IsDisposing)
        {
            // Dispose our component collection and the base service
            if (IsDisposing && (_components != null)) _components.Dispose();
            base.Dispose(IsDisposing);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Static CTOR for a FulcrumService object. Defines the static service initialization logger
        /// </summary>
        static FulcrumServiceBase()
        {
            // If the settings file is configured, exit out
            if (!JsonConfigFile.IsConfigured)
            {

                // Store a local flag for if we're using a debug build or not
                bool IsDebugBuild = false;
#if DEBUG
                // Toggle our debug build flag value to true if needed
                IsDebugBuild = true;
#endif
                // If a debugger is attached to our service object, assume we're working in the debug folders
                string InjectorDirectory;
                if (!Debugger.IsAttached) InjectorDirectory = RegistryControl.InjectorInstallPath;
                else
                {
                    // Find our current working directory and map up to our injector debug folder
                    string WorkingDirectory = Assembly.GetExecutingAssembly().Location;
                    string[] WorkingDirSplit = WorkingDirectory.Split(Path.DirectorySeparatorChar)
                        .TakeWhile(PathPart => !PathPart.Contains("FulcrumShim"))
                        .Append("FulcrumShim\\FulcrumInjector\\bin")
                        .Append(IsDebugBuild ? "Debug" : "Release")
                        .ToArray();

                    // Combine the current working directory with our configuration type to build the desired location
                    InjectorDirectory = string.Join(Path.DirectorySeparatorChar.ToString(), WorkingDirSplit);
                }

                // Using the built injector directory location, set our configuration file location and name
                JsonConfigFile.SetInjectorConfigFile("FulcrumInjectorSettings.json", InjectorDirectory);
            }

            // If the executing assembly name is the fulcrum application, don't reconfigure logging
            if (!Assembly.GetEntryAssembly().FullName.Contains("FulcrumInjector"))
            {
                // Check if we need to configure logging or archiving before building our logger instance
                var BrokerConfig = ValueLoaders.GetConfigValue<SharpLogBroker.BrokerConfiguration>("FulcrumLogging.LogBrokerConfiguration");
                BrokerConfig.LogFilePath = Path.GetFullPath(BrokerConfig.LogFilePath);
                if (!SharpLogBroker.InitializeLogging(BrokerConfig))
                    throw new InvalidOperationException("Error! Failed to configure log broker instance for a FulcrumService!");

                // Load in and apply the log archiver configuration for this instance. Reformat output paths to log ONLY into the injector install location
                var ArchiverConfig = ValueLoaders.GetConfigValue<SharpLogArchiver.ArchiveConfiguration>("FulcrumLogging.LogArchiveConfiguration");
                ArchiverConfig.ArchivePath = Path.GetFullPath(ArchiverConfig.ArchivePath);
                ArchiverConfig.SearchPath = Path.GetFullPath(ArchiverConfig.SearchPath);
                if (!SharpLogArchiver.InitializeArchiving(ArchiverConfig))
                    throw new InvalidOperationException("Error! Failed to configure log archiver instance for a FulcrumService!");
            }

            // Build a static logger for service initialization routines here
            _serviceInitLogger =
                SharpLogBroker.FindLoggers($"{nameof(FulcrumServiceBase)}Logger").FirstOrDefault()
                ?? new SharpLogger(LoggerActions.UniversalLogger, $"{nameof(FulcrumServiceBase)}Logger");

            // If the executing assembly name is the fulcrum application, don't re-archive contents
            if (Assembly.GetEntryAssembly().FullName.Contains("FulcrumInjector")) return;

            // Finally invoke an archive routine and child folder cleanup routine if needed
            Task.Run(() =>
            {
                // Log archive routines have been queued
                _serviceInitLogger.WriteLog("LOGGING ARCHIVE ROUTINES HAVE BEEN KICKED OFF IN THE BACKGROUND!", LogType.WarnLog);
                _serviceInitLogger.WriteLog("PROGRESS FOR ARCHIVAL ROUTINES WILL APPEAR IN THE CONSOLE/FILE TARGET OUTPUTS!");

                // Start with booting the archive routine
                SharpLogArchiver.ArchiveLogFiles();
                _serviceInitLogger.WriteLog("ARCHIVE ROUTINES HAVE BEEN COMPLETED!", LogType.InfoLog);

                // Then finally invoke the archive cleanup routines
                SharpLogArchiver.CleanupArchiveHistory();
                _serviceInitLogger.WriteLog("ARCHIVE CLEANUP ROUTINES HAVE BEEN COMPLETED!", LogType.InfoLog);
            });
            Task.Run(() =>
            {
                // Log archive routines have been queued
                _serviceInitLogger.WriteLog("LOGGING SUBFOLDER PURGE ROUTINES HAVE BEEN KICKED OFF IN THE BACKGROUND!", LogType.WarnLog);
                _serviceInitLogger.WriteLog("PROGRESS FOR SUBFOLDER PURGE ROUTINES WILL APPEAR IN THE CONSOLE/FILE TARGET OUTPUTS!");

                // Call the cleanup method to purge our subdirectories if needed
                SharpLogArchiver.CleanupSubdirectories();
                _serviceInitLogger.WriteLog("CLEANED UP ALL CHILD LOGGING FOLDERS!", LogType.InfoLog);
            });
        }
        /// <summary>
        /// Protected CTOR for a new FulcrumService instance. Builds our service container and sets up logging
        /// <param name="ServiceType">The type of service being constructed</param>
        /// </summary>
        protected FulcrumServiceBase(ServiceTypes ServiceType)
        {
            // Build a new component container for the service
            this._components = new Container();

            // Find the name of our service type and use it for logger configuration
            this._serviceType = ServiceType;
            this.ServiceName = this._serviceType.ToDescriptionString();
            this._serviceLogger = new SharpLogger(LoggerActions.FileLogger, $"{this.ServiceName}Service_Logger");

            // See if this assembly is a direct instance of a service object or not first
            Assembly EntryAssembly = Assembly.GetEntryAssembly();
            if (EntryAssembly == null) throw new InvalidOperationException("Error! Could not find entry assembly for service instance!");
            if (EntryAssembly.FullName.Contains($"{this.ServiceName}Service"))
            {
                // Log out that we've got an actual service instance and return out
                this._serviceLogger.WriteLog("WARNING! SERVICE BEING BOOTED IS AN ACTUAL SERVICE INSTANCE!", LogType.WarnLog);
                this._serviceLogger.WriteLog("ALL SERVICE CALLS/COMMANDS EXECUTED WILL BE DONE SO USING JSON ROUTINES FOR PASSING DATA!", LogType.WarnLog);
                return;
            }

            // Try and consume an existing service instance here if possible
            try { this.ServiceInstance = new ServiceController(this.ServiceName); }
            catch (Exception ConsumeServiceEx)
            {
                // Catch the exception and log it out. Try and boot our service instance here
                this._serviceLogger.WriteLog($"ERROR! FAILED TO CONSUME SERVICE INSTANCE NAMED \"{this.ServiceName}\"!", LogType.ErrorLog);
                this._serviceLogger.WriteException("SERVICE CONSUMPTION EXCEPTION IS BEING LOGGED BELOW", ConsumeServiceEx);
                throw ConsumeServiceEx;
            }

            // Make sure our service instance is running here before moving on
            if (this.ServiceInstance.Status != ServiceControllerStatus.Running)
            {
                this._serviceLogger.WriteLog($"BOOTING SERVICE INSTANCE FOR SERVICE {this.ServiceName}...", LogType.WarnLog);
                this.ServiceInstance.Start();
            }

            try
            {
                // Wait for a running state for our service here. Max time to wait is 120 seconds.
                this.ServiceInstance.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(120));
                this._serviceLogger.WriteLog($"SERVICE {this.ServiceName} WAS FOUND AND APPEARS TO BE RUNNING!", LogType.InfoLog);
            }
            catch (Exception ServiceStateEx)
            {
                // Catch the exception and log it out. Try and boot our service instance here
                this._serviceLogger.WriteLog($"ERROR! FAILED TO CONSUME SERVICE INSTANCE NAMED \"{this.ServiceName}\"!", LogType.ErrorLog);
                this._serviceLogger.WriteException("SERVICE STATUS EXCEPTION IS BEING LOGGED BELOW", ServiceStateEx);
                throw ServiceStateEx;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Instance/debug custom command method for the service.
        /// This allows us to run custom actions on the service in real time if we've defined them here
        /// </summary>
        /// <param name="ServiceCommand">The int value of the command we're trying to run (128 is the help command)</param>
        public void RunCommand(int ServiceCommand)
        {
            // Check if we've got a service instance or if we're consuming our service here
            this._serviceLogger.WriteLog($"INVOKING AN OnCustomCommand METHOD FOR OUR {this.ServiceName} SERVICE");
            if (this.IsServiceInstance) 
            {
                // If we've got a hooked instance, execute this routine here
                this._serviceLogger.WriteLog("INVOKING COMMAND ON SERVICE CONTROLLER INSTANCE!", LogType.WarnLog);
                this.ServiceInstance.ExecuteCommand(ServiceCommand);
            } 
            else
            {
                // If this is the service instance itself, run the command locally
                this._serviceLogger.WriteLog("SERVICE INSTANCE IS BEING INVOKED DIRECTLY!", LogType.WarnLog);
                this.OnCustomCommand(ServiceCommand);
            }
        }
        /// <summary>
        /// Executes a custom command on the given service object as requested.
        /// This method is NOT supported on the base service type!
        /// </summary>
        /// <param name="ServiceCommand">The number of the command being executed for our service</param>
        /// <exception cref="NotImplementedException">Thrown when this method is called since base services are not supported</exception>
        protected override void OnCustomCommand(int ServiceCommand)
        {
            try
            {
                // Check what type of command is being executed and perform actions accordingly.
                switch (ServiceCommand)
                {
                    // For any other command value or something that is not recognized
                    case 128:

                        // Log out the command help information for the user to read in the log file.
                        this._serviceLogger.WriteLog("----------------------------------------------------------------------------------------------------------------", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"                                    FulcrumInjector Service Command Help", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- The provided command value of {ServiceCommand} is reserved to show this help message.", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- Enter any command number above 128 to execute an action on our service instance.", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"- Execute this command again with the service command ID 128 to get a list of all possible commands", LogType.InfoLog);
                        this._serviceLogger.WriteLog("", LogType.InfoLog);
                        this._serviceLogger.WriteLog("Help Commands", LogType.InfoLog);
                        this._serviceLogger.WriteLog("   Command 128:  Displays this help message", LogType.InfoLog);
                        this._serviceLogger.WriteLog("----------------------------------------------------------------------------------------------------------------", LogType.InfoLog);
                        return;
                }
            }
            catch (Exception SendCustomCommandEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO INVOKE A CUSTOM COMMAND ON AN EXISTING SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE CUSTOM COMMAND ROUTINE IS LOGGED BELOW", SendCustomCommandEx);
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to assign an action for the current service to run at the given time with provided configuration
        /// </summary>
        /// <param name="ServiceAction">The action we're looking to schedule for this service</param>
        /// <returns>True if the service task is scheduled. False if it is not</returns>
        public static bool ScheduleServiceAction(FulcrumServiceAction ServiceAction)
        {
            // TODO: Build logic for adding new scheduled actions
            return false;
        }
        /// <summary>
        /// Helper method used to cancel/stop an action for the current service based on the name of it
        /// </summary>
        /// <param name="ActionName">Name of the action we're killing</param>
        /// <returns>True if the action is stopped. False if not</returns>
        public static bool CancelServiceAction(string ActionName)
        {
            // TODO: Build logic for removing existing actions by name
            return false;
        }
        /// <summary>
        /// Helper method used to cancel/stop an action for the current service based on the GUID of it
        /// </summary>
        /// <param name="ActionGuid">GUID of the action we're killing</param>
        /// <returns>True if the action is stopped. False if not</returns>
        public static bool CancelServiceAction(Guid ActionGuid)
        {
            // TODO: Build logic for removing existing actions by GUID
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures the logger for this service to output to a custom file path
        /// </summary>
        /// <returns>The configured file target to register for this service</returns>
        public static FileTarget LocateServiceFileTarget<TServiceType>() where TServiceType : FulcrumServiceBase
        {
            // Make sure our output location exists first
            string ServiceName = typeof(TServiceType).Name;
            string OutputFolder = Path.Combine(SharpLogBroker.LogFileFolder, $"{ServiceName.Replace("Fulcrum", string.Empty)}ServiceLogs");
            if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);

            // Configure our new logger name and the output log file path for this logger instance 
            string ServiceLoggerTime = SharpLogBroker.LogFileName.Split('_').Last().Split('.')[0];
            string ServiceLoggerName = $"{ServiceName}Logging_{ServiceLoggerTime}";
            string OutputFileName = Path.Combine(OutputFolder, $"{ServiceLoggerName}.log");
            if (File.Exists(OutputFileName)) File.Delete(OutputFileName);

            // Spawn the new generation logger and attach in a new file target for it
            var ExistingTarget = SharpLogBroker.LoggingTargets.FirstOrDefault(LoggerTarget => LoggerTarget.Name == ServiceLoggerName);
            if (ExistingTarget is FileTarget LocatedFileTarget) return LocatedFileTarget;

            // Spawn the new generation logger and attach in a new file target for it
            string LayoutString = SharpLogBroker.DefaultFileFormat.LoggerFormatString;
            FileTarget ServiceFileTarget = new FileTarget(ServiceLoggerName)
            {
                KeepFileOpen = false,           // Allows multiple programs to access this file
                Layout = LayoutString,          // The output log line layout for the logger
                ConcurrentWrites = true,        // Allows multiple writes at one time or not
                FileName = OutputFileName,      // The name/full log file being written out
                Name = ServiceLoggerName,       // The name of the logger target being registered
            };

            // Return the output logger object built
            return ServiceFileTarget;
        }
    }
}