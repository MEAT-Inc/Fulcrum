using NLog.Targets;
using SharpLogging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        protected readonly IContainer _components;             // Component objects used by this service instance
        protected readonly ServiceTypes _serviceType;          // The type of service this class represents
        protected readonly SharpLogger _serviceLogger;         // Logger instance for our service
        protected static SharpLogger _serviceInitLogger;       // Logger used to configure service initialization routines

        #endregion // Fields

        #region Properties

        // Public facing property holding our file target for this service
        protected FileTarget ServiceLoggingTarget { get; set; }

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
            // Build a static logger for service initialization routines here
            _serviceInitLogger =
                SharpLogBroker.FindLoggers("ServiceInitLogger").FirstOrDefault()
                ?? new SharpLogger(LoggerActions.UniversalLogger, "ServiceInitLogger");

            // Configure our AppSettings file if needed as well
            JsonConfigFile.SetInjectorConfigFile("FulcrumInjectorSettings.json");
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
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Instance/debug custom command method for the service.
        /// This allows us to run custom actions on the service in real time if we've defined them here
        /// </summary>
        /// <param name="ServiceCommand">The int value of the command we're trying to run (128 is the help command)</param>
        public void RunCommand(int ServiceCommand)
        {
            // Invoke the service command and exit out
            this._serviceLogger.WriteLog($"INVOKING AN OnCustomCommand METHOD FOR OUR {this.ServiceName} SERVICE...", LogType.WarnLog);
            this.OnCustomCommand(ServiceCommand);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds a helper process
        /// </summary>
        /// <param name="StartupArgs">NOT USED!</param>
        protected override void OnStart(string[] StartupArgs)
        {
            // Ensure the drive service exists first
            this._serviceLogger.WriteLog($"BOOTING NEW {this.GetType().Name} SERVICE NOW...", LogType.WarnLog);

            // TODO: Perform any needed startup configuration in this service here

            // Log that our service has been configured correctly
            this._serviceLogger.WriteLog($"{this.GetType().Name} SERVICE HAS BEEN CONFIGURED AND BOOTED CORRECTLY!", LogType.InfoLog);
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
        /// <summary>
        /// Stops the service and runs cleanup routines
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                // Log that we're killing this service and exit out
                this._serviceLogger.WriteLog($"{this.GetType().Name} IS SHUTTING DOWN NOW...", LogType.InfoLog);
                this._serviceLogger.WriteLog($"{this.GetType().Name} HAS BEEN SHUT DOWN WITHOUT ISSUES!", LogType.InfoLog);
            }
            catch (Exception StopDriveServiceEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog($"ERROR! FAILED TO SHUTDOWN EXISTING {this.GetType().Name} INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE STOP ROUTINE IS LOGGED BELOW", StopDriveServiceEx);
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
