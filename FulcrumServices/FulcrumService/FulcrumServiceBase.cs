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
using TimeoutException = System.TimeoutException;

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
        /// <summary>
        /// Class object holding the definition for a service instance on the host machine
        /// </summary>
        public class FulcrumServiceInfo
        {
            #region Custom Events
            #endregion // Custom Events

            #region Fields
            #endregion // Fields

            #region Properties

            // Public facing properties for our service installed state
            public string ServiceName { get; internal set; }
            public string ServicePath { get; internal set; }
            public string ServiceVersion { get; internal set; }
            public bool ServiceInstalled { get; internal set; }

            #endregion // Properties

            #region Structs and Classes
            #endregion // Structs and Classes

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Default CTOR for a service information object
            /// </summary>
            public FulcrumServiceInfo() { }
            /// <summary>
            /// Spawns a new service information object with the given configuration
            /// </summary>
            /// <param name="ServiceName">Name of the service object being checked</param>
            /// <param name="ServicePath">Path to the service object being checked</param>
            /// <param name="ServiceVersion">Version of the service object being checked</param>
            /// <param name="ServiceInstalled">Tells us if the given service is installed or not</param>
            public FulcrumServiceInfo(string ServiceName, string ServicePath = null, string ServiceVersion = null, bool ServiceInstalled = false)
            {
                // Store the path, name, and fallback values here
                this.ServiceName = ServiceName;
                this.ServiceInstalled = ServiceInstalled;
                this.ServicePath = ServicePath ?? "Service Missing!";
                this.ServiceVersion = ServiceVersion ?? "Service Missing!";
            }
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

            // Build an exception handler to catch all exceptions on our service instance to avoid crashes
            AppDomain CurrentDomain = AppDomain.CurrentDomain;
            CurrentDomain.UnhandledException += (_, ExceptionArgs) =>
            {
                // Now log the exception thrown and process the exception to a handled state
                if (ExceptionArgs.ExceptionObject is not Exception CastException) return;
                string ExInfo = $"UNHANDLED APP LEVEL {CastException.GetType().Name} EXCEPTION PROCESSED AT {DateTime.Now:g}!";
                this._serviceLogger.WriteException(ExInfo, (Exception)ExceptionArgs.ExceptionObject, LogType.ErrorLog);
            };

            // See if this assembly is a direct instance of a service object or not first
            Assembly EntryAssembly = Assembly.GetEntryAssembly();
            if (EntryAssembly.FullName.Contains($"{this.ServiceName}Service"))
            {
                // Log out that we've got an actual service instance and return out
                this._serviceLogger.WriteLog("WARNING! SERVICE BEING BOOTED IS AN ACTUAL SERVICE INSTANCE!", LogType.WarnLog);
                this._serviceLogger.WriteLog("ALL SERVICE CALLS/COMMANDS EXECUTED WILL BE DONE SO USING JSON ROUTINES FOR PASSING DATA!", LogType.WarnLog);

                try
                {
                    // Check if the service is currently running. If it is, stop it so we can consume our pipe
                    ServiceController InstalledInstance = new ServiceController(this.ServiceName);
                    if (InstalledInstance.Status != ServiceControllerStatus.Running)
                    {
                        // Log out that our service was not seen to be running at this point
                        this._serviceLogger.WriteLog("SERVICE INSTANCE WAS NOT RUNNING WHEN CHECKED! THIS IS FINE FOR DEBUG BUILDS!");
                        this._serviceLogger.WriteLog("PIPES SHOULD BE OPEN FOR NEW SERVICE INSTANCES!", LogType.InfoLog);
                    }
                    else
                    {
                        // Stop the service and wait for it to complete shutting down
                        this._serviceLogger.WriteLog("STOPPING INSTALLED INSTANCE OF SERVICE OBJECT TO ALLOW PIPE CONSUMPTION...", LogType.InfoLog);
                        InstalledInstance.Stop();
                        InstalledInstance.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(120));
                    }
                }
                catch (Exception ServiceShutdownEx)
                {
                    // Check for a timeout exception for stopping the service
                    if (ServiceShutdownEx is TimeoutException)
                    {
                        this._serviceLogger.WriteLog($"ERROR! FAILED TO STOP SERVICE {this.ServiceName} WITHIN 120 SECONDS!", LogType.ErrorLog);
                        this._serviceLogger.WriteLog("THIS IS A FATAL EXCEPTION! PIPE CREATION CAN NOT CONTINUE!", LogType.ErrorLog);
                        throw;
                    }

                    // Log out that we were unable to find our service on the local machine
                    this._serviceLogger.WriteLog($"WARNING! INSTALLED INSTANCE OF SERVICE {this.ServiceName} COULD NOT BE FOUND!", LogType.WarnLog);
                    this._serviceLogger.WriteLog("IF THIS IS A TRUE DEBUG RUN (BUILT FROM SOURCE AND RUN INSIDE VS, THIS IS NOT A PROBLEM!", LogType.WarnLog);
                }
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
                if (this.ServiceInstance.Status != ServiceControllerStatus.Running) {
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
        /// Builds a new collection of service information objects for all service types
        /// </summary>
        /// <returns>A collection of service status objects</returns>
        public static FulcrumServiceInfo[] GetServiceStates()
        {
            // Find all of our enum values and pull each one here for the service type given
            _serviceInitLogger.WriteLog("GENERATING SERVICE INFORMATION FOR ALL SERVICES NOW...", LogType.WarnLog);
            var ServiceInstanceTypes = Enum.GetValues(typeof(ServiceTypes))
                .Cast<ServiceTypes>()
                .ToArray();

            // Iterate all the service types and return new values for them here 
            return ServiceInstanceTypes.Select(GetServiceState).ToArray();
        }
        /// <summary>
        /// Builds a new service information object for the given service type
        /// </summary>
        /// <param name="ServiceType">The type of service we're building a configuration for</param>
        /// <returns>The status object for this service instance</returns>
        public static FulcrumServiceInfo GetServiceState(ServiceTypes ServiceType)
        {
            // Store temp variables for service state values 
            bool ServiceInstalled = true;
            string InstallPath = "Service Missing!";
            string InstallVersion = "Service Missing!";

            // Log out what service we're building information for
            _serviceInitLogger.WriteLog($"GENERATING SERVICE INFORMATION FOR SERVICE TYPE {ServiceType.ToDescriptionString()}...");

            try
            {
                // Find our install path using the registry helper object here
                InstallPath = ServiceType switch
                {
                    ServiceTypes.DRIVE_SERVICE => RegistryControl.DriveServiceExecutable,
                    ServiceTypes.EMAIL_SERVICE => RegistryControl.EmailServiceExecutable,
                    ServiceTypes.BASE_SERVICE => RegistryControl.InjectorServiceInstallPath,
                    ServiceTypes.UPDATER_SERVICE => RegistryControl.UpdaterServiceExecutable,
                    ServiceTypes.WATCHDOG_SERVICE => RegistryControl.WatchdogServiceExecutable,
                    _ => throw new Exception($"Error! Service type {ServiceType.ToDescriptionString()} does not have an install path!")
                };

                // Make sure this path exists before moving on
                if (!File.Exists(InstallPath) && !Directory.Exists(InstallPath))
                {
                    // Set installed state to false and log out this issue
                    _serviceInitLogger.WriteLog($"ERROR! FAILED TO FIND A VALID EXECUTABLE FOR SERVICE {ServiceType.ToDescriptionString()}!");
                    ServiceInstalled = false;
                }
            }
            catch (Exception FindServicePathEx)
            {
                // Log out that we failed to find a path for the requested service
                _serviceInitLogger.WriteLog($"ERROR! FAILED TO FIND INSTALL PATH FOR SERVICE TYPE {ServiceType.ToDescriptionString()}!", LogType.ErrorLog);
                _serviceInitLogger.WriteException("EXCEPTION DURING LOOKUP ROUTINE IS BEING LOGGED BELOW", FindServicePathEx);

                // Set our installed state to false 
                ServiceInstalled = false;
            }

            try
            {
                // Find our install version using the registry helper object here
                InstallVersion = ServiceType switch
                {
                    ServiceTypes.DRIVE_SERVICE => RegistryControl.DriveServiceVersion.ToString(),
                    ServiceTypes.EMAIL_SERVICE => RegistryControl.EmailServiceVersion.ToString(),
                    ServiceTypes.BASE_SERVICE => RegistryControl.InjectorServiceVersion.ToString(),
                    ServiceTypes.UPDATER_SERVICE => RegistryControl.UpdaterServiceVersion.ToString(),
                    ServiceTypes.WATCHDOG_SERVICE => RegistryControl.WatchdogServiceVersion.ToString(),
                    _ => throw new Exception($"Error! Service type {ServiceType.ToDescriptionString()} does not have an install version!")
                };

                // Make sure our version value is not 0.0.0.0
                if (InstallVersion == new Version().ToString()) 
                {
                    // Set installed state to false and log out this issue
                    _serviceInitLogger.WriteLog($"ERROR! FAILED TO FIND A VALID VERSION VALUE FOR SERVICE {ServiceType.ToDescriptionString()}!");
                    ServiceInstalled = false;
                }
            }
            catch (Exception FindServiceVersionEx)
            {
                // Log out that we failed to find a path for the requested service
                _serviceInitLogger.WriteLog($"ERROR! FAILED TO FIND INSTALL VERSION FOR SERVICE TYPE {ServiceType.ToDescriptionString()}!", LogType.ErrorLog);
                _serviceInitLogger.WriteException("EXCEPTION DURING LOOKUP ROUTINE IS BEING LOGGED BELOW", FindServiceVersionEx);

                // Set our installed state to false 
                ServiceInstalled = false;
            }

            // Build a new service state object here and add it to our collection of services
            _serviceInitLogger.WriteLog($"GENERATED NEW SERVICE INFORMATION FOR SERVICE {ServiceType.ToDescriptionString()}!", LogType.InfoLog);
            return new FulcrumServiceInfo()
            {
                ServicePath = InstallPath,
                ServiceVersion = InstallVersion,
                ServiceInstalled = ServiceInstalled,
                ServiceName = ServiceType.ToDescriptionString(),
            };
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Protected instance method used to invoke a get member routine on our host service
        /// </summary>
        /// <param name="MemberName">Name of the member being pulled</param>
        /// <returns>The pipe action invoked for this routine if passed</returns>
        /// <exception cref="InvalidOperationException">Thrown when the pipe action fails to execute</exception>
        protected object GetPipeMemberValue(string MemberName)
        {
            // Configure our pipe action routine based on our input arguments and invoke it 
            var PipeAction = new FulcrumServicePipeAction(this.ServiceType, MemberName);
            if (!this._invokePipeAction(ref PipeAction))
                throw new InvalidOperationException("Error! Failed to get a member value from a service pipe!");

            // Return our value pulled from the service pipe
            return PipeAction.PipeCommandResult;
        }
        /// <summary>
        /// Protected instance method used to invoke a set member routine on our host service
        /// </summary>
        /// <param name="MemberName">Name of the member being set</param>
        /// <param name="MemberValue">Value of the member member being set</param>
        /// <returns>The pipe action invoked for this routine if passed</returns>
        /// <exception cref="InvalidOperationException">Thrown when the pipe action fails to execute</exception>
        protected bool SetPipeMemberValue(string MemberName, object MemberValue)
        {
            // Configure our pipe action routine based on our input arguments and invoke it 
            var PipeAction = new FulcrumServicePipeAction(this.ServiceType, MemberName, MemberValue);
            if (!this._invokePipeAction(ref PipeAction))
                throw new InvalidOperationException("Error! Failed to set a member value with a service pipe!");

            // Return passed once we've gotten to this point 
            return true; 
        }
        /// <summary>
        /// Protected instance method used to invoke a routine on a host service instance
        /// </summary>
        /// <param name="ActionName">Name of the method/member being invoked</param>
        /// <param name="MethodArgs">Arguments of the member routine/method being invoked</param>
        /// <returns>The pipe action invoked for this routine if passed</returns>
        /// <exception cref="InvalidOperationException">Thrown when the pipe action fails to execute</exception>
        protected FulcrumServicePipeAction ExecutePipeMethod(string ActionName, params object[] MethodArgs)
        {
            // Configure our pipe action routine based on our input arguments and invoke it 
            var PipeAction = new FulcrumServicePipeAction(this.ServiceType, ActionName, MethodArgs?.ToArray());
            if (!this._invokePipeAction(ref PipeAction))
                throw new InvalidOperationException("Error! Failed to execute a method over a service pipe!");

            // Return our built pipe action values
            return PipeAction;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to invoke a new pipe action routine onto one of our services
        /// </summary>
        /// <param name="PipeAction">The action being invoked</param>
        /// <returns>The updated action after invocation of it has been called</returns>
        /// <exception cref="InvalidOperationException">Thrown when the pipe action fails to execute</exception>
        private bool _invokePipeAction(ref FulcrumServicePipeAction PipeAction)
        {
            try
            {
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

                // Return out true once we've invoked our pipe routine 
                return true;
            }
            catch (Exception InvokePipeActionEx)
            {
                // Log out our pipe action exception and return false
                this._serviceLogger.WriteLog("ERROR! FAILED TO INVOKE A NEW PIPE ACTION!", LogType.ErrorLog);
                this._serviceLogger.WriteLog($"PIPE ACTION NAME: {PipeAction.PipeActionName}", LogType.ErrorLog);
                this._serviceLogger.WriteLog($"PIPE ACTION GUID: {PipeAction.PipeActionGuid.ToString("D").ToUpper()}", LogType.ErrorLog);
                this._serviceLogger.WriteException("EXCEPTION THROWN DURING INVOCATION IS BEING LOGGED BELOW", InvokePipeActionEx);

                // Return out failed at this point 
                return false;
            }
        }
    }
}