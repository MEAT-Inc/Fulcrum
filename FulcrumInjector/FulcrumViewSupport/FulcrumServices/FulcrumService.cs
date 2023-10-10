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

namespace FulcrumInjector.FulcrumViewSupport.FulcrumServices
{
    /// <summary>
    /// Base class definition for a fulcrum service instance
    /// </summary>
    internal class FulcrumService : ServiceBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private/protected fields for service instances
        protected readonly IContainer _components;                 // Component objects used by this service instance
        protected readonly SharpLogger _serviceLogger;             // Logger instance for our service
        
        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes
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
        /// Protected CTOR for a new FulcrumService instance. Builds our service container and sets up logging
        /// </summary>
        protected FulcrumService()
        {
            // Build a new component container for the service
            this._components = new Container();

            // Find the name of our service type and use it for logger configuration
            this.ServiceName = this.GetType().Name.Replace("Service", string.Empty); 
            this._serviceLogger = new SharpLogger(LoggerActions.FileLogger, $"{this.ServiceName}_Logger");

            // Build and register a new watchdog logging target here for a file and the console
            var ServiceFileTarget = LocateServiceFileTarget();
            this._serviceLogger.RegisterTarget(ServiceFileTarget);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Instance/debug startup method for the Watchdog service
        /// </summary>
        public void StartService()
        {
            // Check if we want to block execution of the service once we've kicked it off or not
            this._serviceLogger.WriteLog($"INVOKING AN OnStart METHOD FOR OUR {this.ServiceName} SERVICE...", LogType.WarnLog);

            // Boot the service instance now and exit out
            this.OnStart(null);
        }
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
        /// <summary>
        /// Instance/debug shutdown/stop method for the service
        /// </summary>
        public void StopService()
        {
            // Stop the service instance here if possible. This should only be done when it's running
            this._serviceLogger.WriteLog($"INVOKING AN OnStop METHOD FOR OUR {this.ServiceName} SERVICE...", LogType.WarnLog);
            this.OnStop();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures the logger for this service to output to a custom file path
        /// </summary>
        /// <returns>The configured file target to register for this service</returns>
        private FileTarget LocateServiceFileTarget()
        {
            // Make sure our output location exists first
            string OutputFolder = Path.Combine(SharpLogBroker.LogFileFolder, $"{this.ServiceName}Logs");
            if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);

            // Configure our new logger name and the output log file path for this logger instance 
            string ServiceLoggerTime = SharpLogBroker.LogFileName.Split('_').Last().Split('.')[0];
            string ServiceLoggerName = $"{this.ServiceName}Logging_{ServiceLoggerTime}";
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
