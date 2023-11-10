using NLog.Targets;
using SharpLogging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FulcrumSupport;
using FulcrumJson;
using FulcrumService.FulcrumServiceModels;

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
        private readonly IContainer _components;                      // Component objects used by this service instance
        protected readonly SharpLogger _serviceLogger;                // Logger instance for our service
        protected static readonly SharpLogger _serviceInitLogger;     // Logger used to configure service initialization routines

        #endregion // Fields

        #region Properties

        // Properties holding information about the installed windows service object
        private ServiceController ServiceInstance { get; set; }
        protected internal ServiceTypes ServiceType { get; private set; }
        protected internal bool IsServiceClient => this.ServiceInstance != null;

        // Protected pipe object for our service control routines
        protected FulcrumServicePipe ServicePipe { get; private set; }

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration holding different service types for our services
        /// </summary>
        public enum ServiceTypes
        {
            [Description("FulcrumServiceBase")] BASE_SERVICE,    // Default value. Base service type
            [Description("FulcrumWatchdog")] WATCHDOG_SERVICE,   // Watchdog Service Type
            [Description("FulcrumDrive")] DRIVE_SERVICE,         // Drive Service type
            [Description("FulcrumEmail")] EMAIL_SERVICE,         // Email Service Type
            [Description("FulcrumUpdater")] UPDATER_SERVICE      // Updater Service Type
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
            string EntryAssemblyName = Assembly.GetEntryAssembly().GetName().Name;
            string ServiceLoggingFolder = EntryAssemblyName.Replace("Fulcrum", "") + "Logs";
            if (!EntryAssemblyName.Contains("FulcrumInjector"))
            {
                // Check if we need to configure logging or archiving before building our logger instance
                var BrokerConfig = ValueLoaders.GetConfigValue<SharpLogBroker.BrokerConfiguration>("FulcrumLogging.LogBrokerConfiguration");
                BrokerConfig.LogFilePath = Path.Combine(Path.GetFullPath(BrokerConfig.LogFilePath), ServiceLoggingFolder);
                BrokerConfig.LogFileName = $"{EntryAssemblyName}_Logging_$LOGGER_TIME.log";
                BrokerConfig.LogBrokerName = $"{EntryAssemblyName}";
                if (!SharpLogBroker.InitializeLogging(BrokerConfig))
                    throw new InvalidOperationException("Error! Failed to configure log broker instance for a FulcrumService!");

                // Load in and apply the log archiver configuration for this instance. Reformat output paths to log ONLY into the injector install location
                var ArchiverConfig = ValueLoaders.GetConfigValue<SharpLogArchiver.ArchiveConfiguration>("FulcrumLogging.LogArchiveConfiguration");
                ArchiverConfig.SearchPath = Path.Combine(Path.GetFullPath(ArchiverConfig.SearchPath), ServiceLoggingFolder);
                ArchiverConfig.ArchivePath = Path.GetFullPath(ArchiverConfig.ArchivePath);
                ArchiverConfig.ArchiveFileFilter = $"{EntryAssemblyName}*.*";
                if (!SharpLogArchiver.InitializeArchiving(ArchiverConfig))
                    throw new InvalidOperationException("Error! Failed to configure log archiver instance for a FulcrumService!");
            }

            // Build a static logger for service initialization routines here
            _serviceInitLogger =
                SharpLogBroker.FindLoggers($"{nameof(FulcrumServiceBase)}Logger").FirstOrDefault()
                ?? new SharpLogger(LoggerActions.UniversalLogger, $"{nameof(FulcrumServiceBase)}Logger");

            // If the executing assembly name is the fulcrum application, don't re-archive contents
            if (EntryAssemblyName.Contains("FulcrumInjector")) return;

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
            this.ServiceType = ServiceType;
            this.ServiceName = this.ServiceType.ToDescriptionString();
            this._serviceLogger = new SharpLogger(LoggerActions.FileLogger, $"{this.ServiceName}Service_Logger");

            // See if this assembly is a direct instance of a service object or not first
            Assembly EntryAssembly = Assembly.GetEntryAssembly();
            if (EntryAssembly.FullName.Contains($"{this.ServiceName}Service"))
            {
                // Log out that we've got an actual service instance and return out
                this._serviceLogger.WriteLog("WARNING! SERVICE BEING BOOTED IS AN ACTUAL SERVICE INSTANCE!", LogType.WarnLog);
                this._serviceLogger.WriteLog("ALL SERVICE CALLS/COMMANDS EXECUTED WILL BE DONE SO USING JSON ROUTINES FOR PASSING DATA!", LogType.WarnLog);
            }
            else
            {
                // Try and consume an existing service instance here if possible
                try { this.ServiceInstance = new ServiceController(this.ServiceName); }
                catch (Exception ConsumeServiceEx)
                {
                    // Catch the exception and log it out. Try and boot our service instance here
                    this._serviceLogger.WriteLog($"ERROR! FAILED TO CONSUME SERVICE INSTANCE NAMED \"{this.ServiceName}\"!", LogType.ErrorLog);
                    this._serviceLogger.WriteException("SERVICE CONSUMPTION EXCEPTION IS BEING LOGGED BELOW", ConsumeServiceEx);
                    throw;
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
                    throw;
                }
            }

            // Finally build and consume our pipe objects and store it on our instance
            this._serviceLogger.WriteLog($"SPAWNING NEW PIPES FOR SERVICE {this.ServiceName} NOW...", LogType.WarnLog);
            this.ServicePipe = new FulcrumServicePipe(this);
            if (this.ServicePipe == null) 
                throw new InvalidOperationException($"Error! Failed to initialize pipe instance for service {this.ServiceName}!");

            // Log our that our service is configured and ready for use
            this._serviceLogger.WriteLog("SPAWNED SERVICE COMPONENTS AND PIPES CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SERVICE {this.ServiceName} IS READY FOR INTERNAL OR EXTERNAL CONTROL!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Instance/debug startup method for the service.
        /// This method will simply call the OnStart method for our service with given arguments
        /// <param name="ServiceArgs">Arguments for the service startup routine</param>
        /// </summary>
        public void StartService(params string[] ServiceArgs)
        {
            // Check if we've got a service instance or if we're consuming our service here
            this._serviceLogger.WriteLog($"INVOKING AN OnStart METHOD FOR OUR {this.ServiceName} SERVICE");
            if (this.IsServiceClient)
            {
                // If we've got a hooked instance, execute this routine here
                this._serviceLogger.WriteLog("INVOKING ROUTINE ON SERVICE CONTROLLER INSTANCE!", LogType.WarnLog);
                this.ServiceInstance.Start(ServiceArgs);
            }
            else
            {
                // If this is the service instance itself, run the command locally
                this._serviceLogger.WriteLog("SERVICE INSTANCE IS BEING INVOKED DIRECTLY!", LogType.WarnLog);
                this.OnStart(ServiceArgs);
            }
        }
        /// <summary>
        /// Instance/debug stop command for the service
        /// This will simply call the OnStop method for our service instance
        /// </summary>
        public void StopService()
        {
            // Check if we've got a service instance or if we're consuming our service here
            this._serviceLogger.WriteLog($"INVOKING AN OnStop METHOD FOR OUR {this.ServiceName} SERVICE");
            if (this.IsServiceClient)
            {
                // If we've got a hooked instance, execute this routine here
                this._serviceLogger.WriteLog("INVOKING ROUTINE ON SERVICE CONTROLLER INSTANCE!", LogType.WarnLog);
                this.ServiceInstance.Stop();
            }
            else
            {
                // If this is the service instance itself, run the command locally
                this._serviceLogger.WriteLog("SERVICE INSTANCE IS BEING INVOKED DIRECTLY!", LogType.WarnLog);
                this.Stop();
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Protected instance method used to invoke a routine on a host service instance
        /// </summary>
        /// <param name="MethodName">Name of the method being invoked</param>
        /// <param name="MethodArgs">Arguments of the method being invoked</param>
        /// <returns>The pipe action invoked for this routine if passed</returns>
        /// <exception cref="InvalidOperationException">Thrown when the pipe action fails to execute</exception>
        protected FulcrumServicePipeAction ExecutePipeRoutine(string MethodName, params object[] MethodArgs)
        {
            // Log out that we're using the service instance object and pull our results back
            this._serviceLogger.WriteLog("WARNING! CONSUMING SERVICE FOR EXECUTION INSTEAD OF CALLING LOCAL METHOD!", LogType.WarnLog);
            var PipeAction = new FulcrumServicePipeAction(this.ServiceType, MethodName, MethodArgs);

            // Queue our pipe action to the host service instance and execute it
            if (!this.ServicePipe.QueuePipeAction(PipeAction))
            {
                // Log that we failed to queue this action and exit out
                this._serviceLogger.WriteLog("ERROR! FAILED TO QUEUE NEW PIPE ACTION!", LogType.ErrorLog);
                throw new InvalidOperationException("Error! Failed to invoke a new pipe action!");
            }

            // Wait for our pipe action to come back and store the values of it as our return information
            if (!this.ServicePipe.WaitForAction(PipeAction.PipeActionGuid, out PipeAction))
            {
                // Log that we failed to find our pipe response and fail out of this method 
                this._serviceLogger.WriteLog("ERROR! FAILED TO FIND PIPE ACTION RESPONSE!", LogType.ErrorLog);
                throw new InvalidOperationException("Error! Failed to find pipe action response!");
            }

            // Store our pipe response values as our output values and exit this method
            this._serviceLogger.WriteLog("PIPE ROUTINE EXECUTION COMPLETED CORRECTLY! RETURNING CONTENT NOW...", LogType.InfoLog);
            return PipeAction;
        }
    }
}