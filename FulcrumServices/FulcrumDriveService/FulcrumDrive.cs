using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FulcrumDriveService.DriveServiceModels;
using FulcrumDriveService.JsonConverters;
using FulcrumJson;
using FulcrumService;
using FulcrumSupport;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Newtonsoft.Json;
using SharpLogging;
using File = Google.Apis.Drive.v3.Data.File;

namespace FulcrumDriveService
{
    /// <summary>
    /// The actual service base component used for the injector drive service helper
    /// </summary>
    public partial class FulcrumDrive : FulcrumServiceBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for drive service objects
        private DriveAuthorization _driveAuth;                    // The authorization configuration for the drive service
        private DriveConfiguration _driveConfig;                  // The initialization configuration for the drive service
        private static DriveService _driveService;                // Private static instance for our drive service object
        private DriveServiceSettings _driveSettings;              // Settings configuration for our service
        private static FulcrumDrive _serviceInstance;             // Static service instance object

        #endregion //Fields

        #region Properties

        // Public readonly properties holding information about the drive configuration
        public static DriveService DriveService
        {
            get
            {
                // If our drive service is null, build it and return it out
                if (_driveService == null) InitializeDriveService();
                return _driveService;
            }
            private set => _driveService = value;
        }
        public string GoogleDriveId { get; private set; }
        public string ApplicationName { get; private set; }

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration used to help filter the resulting object types from a query to the drive/folder
        /// </summary>
        public enum ResultTypes
        {
            [Description("")] ALL_RESULTS,                                      // Pulls in all results from the search 
            [Description("application/vnd.google-apps.file")] FILES_ONLY,       // Pulls only files in from the search
            [Description("application/vnd.google-apps.folder")] FOLDERS_ONLY    // Pulls only folders in from the search
        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR routine for this drive service. Sets up our component object and our logger instance
        /// </summary>
        /// <param name="ServiceSettings">Optional settings object for our service configuration</param>
        internal FulcrumDrive(DriveServiceSettings ServiceSettings = null)
        {
            // Build and register a new watchdog logging target here for a file and the console
            this.ServiceLoggingTarget = LocateServiceFileTarget<FulcrumDrive>();
            this._serviceLogger.RegisterTarget(this.ServiceLoggingTarget);

            // Log we're building this new service and log out the name we located for it
            this._serviceLogger.WriteLog("SPAWNING NEW DRIVE SERVICE!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);

            // Pull our settings configuration for the service here 
            this._driveSettings = ServiceSettings ?? ValueLoaders.GetConfigValue<DriveServiceSettings>("FulcrumDriveService");
            this._serviceLogger.WriteLog("FULCRUM INJECTOR DRIVE SERVICE HAS BEEN BUILT AND IS READY TO RUN!", LogType.InfoLog);

            // Pull in the drive ID and application name first
            this.ApplicationName = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorDriveExplorer.ApplicationName");
            this.GoogleDriveId = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorDriveExplorer.GoogleDriveId");
            this._serviceLogger.WriteLog("PULLED GOOGLE DRIVE ID AND APPLICATION NAME CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"DRIVE ID: {GoogleDriveId}");
            this._serviceLogger.WriteLog($"APPLICATION NAME: {ApplicationName}");

            // Pull in the configuration values for the drive explorer and unscramble needed strings
            this._serviceLogger.WriteLog("LOADING AND UNSCRAMBLING CONFIGURATION FOR DRIVE SERVICE NOW...");
            this._driveConfig = ValueLoaders.GetConfigValue<DriveConfiguration>("FulcrumConstants.InjectorDriveExplorer.ExplorerConfiguration");

            // Pull in the configuration values for the drive explorer authorization and unscramble needed strings
            this._serviceLogger.WriteLog("LOADING AND UNSCRAMBLING AUTHORIZATION FOR DRIVE SERVICE NOW...");
            this._driveAuth = ValueLoaders.GetConfigValue<DriveAuthorization>("FulcrumConstants.InjectorDriveExplorer.ExplorerAuthorization");

            // Log out that our unscramble routines have been completed
            this._serviceLogger.WriteLog("PULLED GOOGLE DRIVE EXPLORER AUTHORIZATION AND CONFIGURATION INFORMATION CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"DRIVE CLIENT ID: {_driveConfig.ClientId}");
            this._serviceLogger.WriteLog($"DRIVE PROJECT ID: {_driveConfig.ProjectId}");
            this._serviceLogger.WriteLog($"DRIVE SERVICE EMAIL: {_driveAuth.ClientEmail}");

            // Configure the google drive service here
            this._serviceLogger.WriteLog("BUILDING NEW GOOGLE DRIVE SERVICE NOW...", LogType.WarnLog);
            DriveService = new DriveService(new BaseClientService.Initializer()
            {
                // Store the API configuration and Application name for the authorization helper
                ApplicationName = ApplicationName,
                HttpClientInitializer = GoogleCredential
                    .FromJson(JsonConvert.SerializeObject(_driveAuth, new DriveAuthJsonConverter(false)))
                    .CreateScoped(DriveService.Scope.DriveReadonly)
            });

            // Return the new drive service object 
            this._serviceLogger.WriteLog("BUILT NEW GOOGLE DRIVE EXPLORER SERVICE WITHOUT ISSUES!", LogType.InfoLog);
        }
        /// <summary>
        /// Static CTOR for the drive service which builds and configures a new drive service
        /// </summary>
        /// <param name="ForceInit">When true, we force rebuild the requested service instance</param>
        /// <returns>The built and configured drive helper service</returns>
        public static FulcrumDrive InitializeDriveService(bool ForceInit = false)
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
                if (_serviceInstance != null && !ForceInit) return;

                // Build and boot a new service instance for our watchdog
                _serviceInstance = new FulcrumDrive(DriveConfig);
                _serviceInstance.OnStart(null);
            });

            // Log that we've booted this new service instance correctly and exit out
            ServiceInitLogger.WriteLog("SPAWNED NEW INJECTOR DRIVE SERVICE OK! BOOTING IT NOW...", LogType.WarnLog);
            ServiceInitLogger.WriteLog("BOOTED NEW INJECTOR DRIVE SERVICE OK!", LogType.InfoLog);

            // Return the built service instance 
            return _serviceInstance;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds a drive helper process
        /// </summary>
        /// <param name="StartupArgs">NOT USED!</param>
        protected override void OnStart(string[] StartupArgs)
        {
            // Ensure the drive service exists first
            this._serviceLogger.WriteLog("BOOTING NEW DRIVE SERVICE NOW...", LogType.WarnLog);
            if (DriveService != null) this._serviceLogger.WriteLog("DRIVE SERVICE EXISTS! STARTING SERVICE INSTANCE NOW...", LogType.InfoLog);
            else
            {
                // Configure the google drive service here
                this._serviceLogger.WriteLog("BUILDING NEW GOOGLE DRIVE SERVICE NOW...", LogType.WarnLog);
                DriveService = new DriveService(new BaseClientService.Initializer()
                {
                    // Store the API configuration and Application name for the authorization helper
                    ApplicationName = ApplicationName,
                    HttpClientInitializer = GoogleCredential
                        .FromJson(JsonConvert.SerializeObject(_driveAuth, new DriveAuthJsonConverter(false)))
                        .CreateScoped(DriveService.Scope.DriveReadonly)
                });

                // Return the new drive service object 
                this._serviceLogger.WriteLog("BUILT NEW GOOGLE DRIVE EXPLORER SERVICE WITHOUT ISSUES!", LogType.InfoLog);
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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to query a given location on a google drive and return all files found for it
        /// </summary>
        /// <param name="LocatedObjects">The files/folders found in the location</param>
        /// <returns>True if one or more files/folders are found</returns>
        /// <exception cref="InvalidOperationException">Thrown when the google drive service could not be built</exception>
        public bool ListDriveContents(out List<File> LocatedObjects, ResultTypes ResultFilter = ResultTypes.ALL_RESULTS)
        {
            // Validate our drive service first
            if (DriveService == null)
                throw new InvalidOperationException("Error! Drive Service is not configured!");

            // Build a new list request for pulling all files in from the drive location
            var ListRequest = DriveService.Files.List();
            ListRequest.PageSize = 1000;
            ListRequest.Corpora = "drive";
            ListRequest.DriveId = GoogleDriveId;
            ListRequest.SupportsTeamDrives = true;
            ListRequest.IncludeTeamDriveItems = true;
            ListRequest.IncludeItemsFromAllDrives = true;

            // Build a new PageStreamer to automatically page through results of files and execute the fetch routine
            LocatedObjects = new Google.Apis.Requests.PageStreamer<File, FilesResource.ListRequest, FileList, string>(
                (_, TokenObj) => ListRequest.PageToken = TokenObj,
                RespObj => RespObj.NextPageToken,
                RespObj => RespObj.Files)
                .Fetch(ListRequest)
                .ToList();

            // Filter our results based on the result type provided if needed
            switch (ResultFilter)
            {
                // Filter for files only (Excludes folder mimeTypes)
                case ResultTypes.FILES_ONLY:
                    LocatedObjects = LocatedObjects.Where(DriveObj => DriveObj.MimeType != ResultTypes.FOLDERS_ONLY.ToDescriptionString()).ToList();
                    break;

                // Filter for folders only (Includes only folder mimeTypes)
                case ResultTypes.FOLDERS_ONLY:
                    LocatedObjects = LocatedObjects.Where(DriveObj => DriveObj.MimeType == ResultTypes.FOLDERS_ONLY.ToDescriptionString()).ToList();
                    break;
            }

            // Return out based on the number of logs found 
            return LocatedObjects.Count > 0;
        }
        /// <summary>
        /// Helper method used to query a given location on a google drive and return all files found for it
        /// </summary>
        /// <param name="FolderId">The ID of the location to search</param>
        /// <param name="LocatedObjects">The files/folders found in the location</param>
        /// <returns>True if one or more files/folders are found</returns>
        /// <exception cref="InvalidOperationException">Thrown when the google drive service could not be built</exception>
        public bool ListFolderContents(string FolderId, out List<File> LocatedObjects, ResultTypes ResultFilter = ResultTypes.ALL_RESULTS)
        {
            // Validate our drive service first
            if (DriveService == null)
                throw new InvalidOperationException("Error! Drive Service is not configured!");

            // Build a new list request for pulling all files in from the drive location
            var ListRequest = DriveService.Files.List();
            ListRequest.PageSize = 1000;
            ListRequest.Corpora = "drive";
            ListRequest.DriveId = GoogleDriveId;
            ListRequest.SupportsTeamDrives = true;
            ListRequest.IncludeTeamDriveItems = true;
            ListRequest.IncludeItemsFromAllDrives = true;
            ListRequest.Q = $"'{FolderId}' in parents";

            // Build a new PageStreamer to automatically page through results of files and execute the fetch routine
            LocatedObjects = new Google.Apis.Requests.PageStreamer<File, FilesResource.ListRequest, FileList, string>(
                    (_, TokenObj) => ListRequest.PageToken = TokenObj,
                    RespObj => RespObj.NextPageToken,
                    RespObj => RespObj.Files)
                .Fetch(ListRequest)
                .ToList();

            // Filter our results based on the result type provided if needed
            switch (ResultFilter)
            {
                // Filter for files only (Excludes folder mimeTypes)
                case ResultTypes.FILES_ONLY:
                    LocatedObjects = LocatedObjects.Where(DriveObj => DriveObj.MimeType != ResultTypes.FOLDERS_ONLY.ToDescriptionString()).ToList();
                    break;

                // Filter for folders only (Includes only folder mimeTypes)
                case ResultTypes.FOLDERS_ONLY:
                    LocatedObjects = LocatedObjects.Where(DriveObj => DriveObj.MimeType == ResultTypes.FOLDERS_ONLY.ToDescriptionString()).ToList();
                    break;
            }

            // Return out based on the number of logs found 
            return LocatedObjects.Count > 0;
        }
    }
}
