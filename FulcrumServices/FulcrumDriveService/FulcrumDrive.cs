using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Authentication;
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

        // Private backing fields for our drive service instance
        private static FulcrumDrive _serviceInstance;             // Instance of our service object
        private static readonly object _serviceLock = new();      // Lock object for building service instances

        // Private backing fields for drive service objects
        private DriveAuthorization _driveAuth;                    // The authorization configuration for the drive service
        private DriveConfiguration _driveConfig;                  // The initialization configuration for the drive service
        private static DriveService _driveService;                // Private static instance for our drive service object
        private DriveServiceSettings _serviceConfig;              // Settings configuration for our service

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
        public bool IsDriveServiceAuthorized { get; private set; }

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
        internal FulcrumDrive(DriveServiceSettings ServiceSettings = null) : base(ServiceTypes.DRIVE_SERVICE)
        {
            // Log we're building this new service and log out the name we located for it
            this._serviceLogger.WriteLog("SPAWNING NEW DRIVE SERVICE!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);

            // Pull our settings configuration for the service here 
            this._serviceConfig = ServiceSettings ?? ValueLoaders.GetConfigValue<DriveServiceSettings>("FulcrumServices.FulcrumDriveService");
            this._serviceLogger.WriteLog("PULLED BASE SERVICE CONFIGURATION VALUES CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SERVICE NAME: {this._serviceConfig.ServiceName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SERVICE ENABLED: {this._serviceConfig.ServiceEnabled}", LogType.TraceLog);

            // Pull in the drive ID and application name first
            this.GoogleDriveId = this._serviceConfig.GoogleDriveId; 
            this.ApplicationName = this._serviceConfig.ApplicationName; 
            this._serviceLogger.WriteLog("PULLED GOOGLE DRIVE ID AND APPLICATION NAME CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"DRIVE ID: {GoogleDriveId}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"APPLICATION NAME: {ApplicationName}", LogType.TraceLog);

            // Pull in new authorization and configuration objects here
            this._driveAuth = this._serviceConfig.ExplorerAuthorization;
            this._driveConfig = this._serviceConfig.ExplorerConfiguration;

            // Log out information about our configuration values here 
            this._serviceLogger.WriteLog("PULLED GOOGLE DRIVE EXPLORER AUTHORIZATION AND CONFIGURATION INFORMATION CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"DRIVE CLIENT ID: {this._driveConfig.ClientId}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"DRIVE PROJECT ID: {this._driveConfig.ProjectId}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"DRIVE SERVICE EMAIL: {this._driveAuth.ClientEmail}", LogType.TraceLog);
        }
        /// <summary>
        /// Static CTOR for the drive service which builds and configures a new drive service
        /// </summary>
        /// <param name="ForceInit">When true, we force rebuild the requested service instance</param>
        /// <returns>The built and configured drive helper service</returns>
        public static Task<FulcrumDrive> InitializeDriveService(bool ForceInit = false)
        {
            // Make sure we actually want to use this watchdog service 
            var ServiceConfig = ValueLoaders.GetConfigValue<DriveServiceSettings>("FulcrumServices.FulcrumDriveService");
            if (!ServiceConfig.ServiceEnabled) {
                _serviceInitLogger.WriteLog("WARNING! DRIVE SERVICE IS TURNED OFF IN OUR CONFIGURATION FILE! NOT BOOTING IT", LogType.WarnLog);
                return null;
            }

            // Spin up a new injector drive service here if needed           
            _serviceInitLogger.WriteLog($"SPAWNING A NEW DRIVE SERVICE INSTANCE NOW...", LogType.WarnLog);
            return Task.Run(() =>
            {
                // Lock our service object for thread safe operations
                lock (_serviceLock)
                {
                    // Check if we need to force rebuilt this service or not
                    if (_serviceInstance != null && !ForceInit) {
                        _serviceInitLogger.WriteLog("FOUND EXISTING DRIVE SERVICE INSTANCE! RETURNING IT NOW...");
                        return _serviceInstance;
                    }

                    // Build and boot a new service instance for our watchdog
                    _serviceInstance = new FulcrumDrive(ServiceConfig);
                    _serviceInitLogger.WriteLog("SPAWNED NEW INJECTOR DRIVE SERVICE OK!", LogType.InfoLog);

                    // Return the service instance here
                    return _serviceInstance;
                }
            });
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds a drive helper process
        /// </summary>
        /// <param name="StartupArgs">NOT USED!</param>
        protected override void OnStart(string[] StartupArgs)
        {
            // Log out what type of service is being configured currently
            this._serviceLogger.WriteLog($"BOOTING NEW {this.GetType().Name} SERVICE NOW...", LogType.WarnLog);
            this._serviceLogger.WriteLog($"CONFIGURING NEW GITHUB CONNECTION HELPER FOR INJECTOR SERVICE...", LogType.InfoLog);

            // Make sure our drive service is built and authenticated before moving on
            if (!this._authorizeDriveService())
                throw new AuthenticationException("Error! Failed to authorize Drive Service for the MEAT Inc Organization!");

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
            if (!this._authorizeDriveService())
                throw new AuthenticationException("Error! Failed to authorize Drive Service for the MEAT Inc Organization!");

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
            if (!this._authorizeDriveService())
                throw new AuthenticationException("Error! Failed to authorize Drive Service for the MEAT Inc Organization!");

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

        /// <summary>
        /// Private helper method used to authorize our Google Drive client on the MEAT Inc organization
        /// </summary>
        /// <returns>True if the client is authorized. False if not</returns>
        private bool _authorizeDriveService()
        {
            try
            {
                // Check if we're configured or not already 
                if (this.IsDriveServiceAuthorized) return true; 

                // Configure the google drive service here
                this._serviceLogger.WriteLog("BUILDING AND AUTHORIZING NEW GOOGLE DRIVE CLIENT NOW...", LogType.WarnLog);
                DriveService = new DriveService(new BaseClientService.Initializer()
                {
                    // Store the API configuration and Application name for the authorization helper
                    ApplicationName = ApplicationName,
                    HttpClientInitializer = GoogleCredential
                        .FromJson(JsonConvert.SerializeObject(_driveAuth, new DriveAuthJsonConverter(false)))
                        .CreateScoped(DriveService.Scope.DriveReadonly)
                });

                // Log out we authorized correctly, update our authorization flag and return true 
                this._serviceLogger.WriteLog("AUTHORIZED NEW GOOGLE DRIVE CLIENT CORRECTLY!", LogType.InfoLog);
                this.IsDriveServiceAuthorized = true;
                return true;
            }
            catch (Exception AuthEx)
            {
                // Log our exception and return false 
                this._serviceLogger.WriteLog("ERROR! FAILED TO AUTHORIZE NEW DRIVE SERVICE CLIENT!", LogType.ErrorLog);
                this._serviceLogger.WriteException("EXCEPTION DURING AUTHORIZATION IS BEING LOGGED BELOW", AuthEx);
                return false;
            }
        }
    }
}