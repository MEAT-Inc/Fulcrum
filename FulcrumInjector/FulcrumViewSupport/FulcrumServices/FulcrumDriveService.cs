using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.DriveBrokerModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.WatchdogModels;
using Google.Apis.Drive.v3;
using NLog.Targets;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumServices
{
    /// <summary>
    /// The actual service base component used for the injector drive service helper
    /// </summary>
    public class FulcrumDriveService : FulcrumService
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our drive service configuration
        private DriveService _driveService;               // Backing drive service object
        private DriveServiceSettings _driveSettings;      // Settings configuration for our service

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR routine for this drive service. Sets up our component object and our logger instance
        /// </summary>
        /// <param name="ServiceSettings">Optional settings object for our service configuration</param>
        public FulcrumDriveService(DriveServiceSettings ServiceSettings = null)
        {
            // Build and register a new watchdog logging target here for a file and the console
            this.ServiceLoggingTarget = LocateServiceFileTarget<FulcrumDriveService>();
            this._serviceLogger.RegisterTarget(this.ServiceLoggingTarget);

            // Log we're building this new service and log out the name we located for it
            this._serviceLogger.WriteLog("SPAWNING NEW DRIVE SERVICE!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);

            // Pull our settings configuration for the service here 
            this._driveSettings = ServiceSettings ?? ValueLoaders.GetConfigValue<DriveServiceSettings>("FulcrumDriveService");

            // Build a new google drive service
            if (!FulcrumDriveBroker.ConfigureDriveService(out this._driveService))
                throw new InvalidOperationException("ERROR! FAILED TO BUILD GOOGLE DRIVE SERVICE!");

            // Log that our service has been built correctly and exit out
            this._serviceLogger.WriteLog("FULCRUM INJECTOR DRIVE SERVICE HAS BEEN BUILT AND IS READY TO RUN!", LogType.InfoLog);
        }
        /// <summary>
        /// Static CTOR for the drive service which builds and configures a new drive service
        /// </summary>
        /// <returns>The built and configured drive helper service</returns>
        public static FulcrumDriveService InitializeDriveService(bool ForceInit = false)
        {
            // Build a static init logger for the service here
            SharpLogger ServiceInitLogger =
                SharpLogBroker.FindLoggers("ServiceInitLogger").FirstOrDefault()
                ?? new SharpLogger(LoggerActions.UniversalLogger, "ServiceInitLogger");

            // Make sure we actually want to use this watchdog service 
            var DriveConfig = ValueLoaders.GetConfigValue<DriveServiceSettings>("FulcrumDriveService");
            if (!DriveConfig.DriveEnabled)
            {
                // Log that the watchdog is disabled and exit out
                ServiceInitLogger.WriteLog("WARNING! DRIVE SERVICE IS TURNED OFF IN OUR CONFIGURATION FILE! NOT BOOTING IT", LogType.WarnLog);
                ServiceInitLogger.WriteLog("CHANGE THE VALUE OF JSON FIELD DriveEnabled TO TRUE TO ENABLE OUR DRIVE SERVICE!", LogType.WarnLog);
                return null;
            }

            // Spin up a new injector drive service here if needed           
            Task.Run(() =>
            {
                // Check if we need to force rebuilt this service or not
                if (FulcrumConstants.FulcrumDriveService != null && !ForceInit) return;

                // Build and boot a new service instance for our watchdog
                FulcrumConstants.FulcrumDriveService = new FulcrumDriveService(DriveConfig);
                FulcrumConstants.FulcrumDriveService.StartService();
            });

            // Log that we've booted this new service instance correctly and exit out
            ServiceInitLogger.WriteLog("SPAWNED NEW INJECTOR DRIVE SERVICE OK! BOOTING IT NOW...", LogType.WarnLog);
            ServiceInitLogger.WriteLog("BOOTED NEW INJECTOR DRIVE SERVICE OK!", LogType.InfoLog);

            // Return the built service instance 
            return FulcrumConstants.FulcrumDriveService;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds a watchdog helper process
        /// </summary>
        /// <param name="StartupArgs">NOT USED!</param>
        protected override void OnStart(string[] StartupArgs)
        {
            // Ensure the drive service exists first
            this._serviceLogger.WriteLog("BOOTING NEW DRIVE SERVICE NOW...", LogType.WarnLog);
            if (this._driveService != null) this._serviceLogger.WriteLog("DRIVE SERVICE EXISTS! STARTING SERVICE INSTANCE NOW...", LogType.InfoLog); 
            else 
            { 
                // Build a new google drive service
                if (!FulcrumDriveBroker.ConfigureDriveService(out this._driveService))
                    throw new InvalidOperationException("ERROR! FAILED TO BUILD GOOGLE DRIVE SERVICE!");
            }

            // Log that our service has been configured correctly
            this._serviceLogger.WriteLog("DRIVE SERVICE HAS BEEN CONFIGURED AND BOOTED CORRECTLY!", LogType.InfoLog);
        }
        /// <summary>
        /// Invokes a custom command routine for our service based on the int code provided to it.
        /// </summary>
        /// <param name="ServiceCommand">The command to execute on our service instance (128-255)</param>
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
                        this._serviceLogger.WriteLog($"                                FulcrumInjector Drive Service Command Help", LogType.InfoLog);
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
                this._serviceLogger.WriteLog("ERROR! FAILED TO INVOKE A CUSTOM COMMAND ON AN EXISTING DRIVE SERVICE INSTANCE!", LogType.ErrorLog);
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
                // Log done building and prepare to get our input directories
                this._serviceLogger.WriteLog("FULCRUM INJECTOR DRIVE SERVICE IS SHUTTING DOWN NOW...", LogType.InfoLog);
                this._serviceLogger.WriteLog("FULCRUM INJECTOR DRIVE SERVICE HAS BEEN SHUT DOWN WITHOUT ISSUES!", LogType.InfoLog);
            }
            catch (Exception StopDriveServiceEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO SHUTDOWN EXISTING DRIVE SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE STOP ROUTINE IS LOGGED BELOW", StopDriveServiceEx);
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Uploads requested files to the google drive location dedicated to this machine
        /// </summary>
        /// <param name="FileNames">Files we're looking to upload to our google drive</param>
        public void UploadFiles(params string[] FileNames)
        {
            // TODO: Build the logic needed for updating files here
        }
    }
}
