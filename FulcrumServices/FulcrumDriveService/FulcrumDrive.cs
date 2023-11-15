using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Threading.Tasks;
using FulcrumDriveService.DriveServiceModels;
using FulcrumDriveService.JsonConverters;
using FulcrumJson;
using FulcrumService;
using FulcrumService.FulcrumServiceModels;
using FulcrumSupport;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private DriveService _driveService;                       // Private static instance for our drive service object
        private DriveAuthorization _driveAuth;                    // The authorization configuration for the drive service
        private DriveServiceSettings _serviceConfig;              // Settings configuration for our service

        // Private backing fields for our public facing properties
        private string _googleDriveId;                           // The ID of the google drive we're hooked into
        private string _applicationName;                         // The name of the application connected to our drive
        private bool _isDriveServiceAuthorized;                  // Holds the state of our drive service

        #endregion //Fields

        #region Properties

        // Public readonly properties holding information about the drive configuration
        public string GoogleDriveId
        {
            // Pull the value from our service host or the local instance based on client configuration
            get => !this.IsServiceClient
                ? this._googleDriveId
                : this.GetPipeMemberValue(nameof(GoogleDriveId)).ToString();

            private set
            {
                // Check if we're using a service client or not and set the value accordingly
                if (!this.IsServiceClient) this._googleDriveId = value; 
                if (!this.SetPipeMemberValue(nameof(GoogleDriveId), value))
                    throw new InvalidOperationException($"Error! Failed to update pipe member {nameof(GoogleDriveId)}!");
            }
        }
        public string ApplicationName
        {
            // Pull the value from our service host or the local instance based on client configuration
            get => !this.IsServiceClient
                ? this._applicationName
                : this.GetPipeMemberValue(nameof(ApplicationName)).ToString();

            private set
            {
                // Check if we're using a service client or not and set the value accordingly
                if (!this.IsServiceClient) this._applicationName = value;
                if (!this.SetPipeMemberValue(nameof(ApplicationName), value))
                    throw new InvalidOperationException($"Error! Failed to update pipe member {nameof(ApplicationName)}!");
            }
        }
        public bool IsDriveServiceAuthorized
        {
            // Pull the value from our service host or the local instance based on client configuration
            get => !this.IsServiceClient 
                ? this._isDriveServiceAuthorized
                : bool.Parse(this.GetPipeMemberValue(nameof(IsDriveServiceAuthorized)).ToString());

            private set
            {
                // Check if we're using a service client or not and set the value accordingly
                if (!this.IsServiceClient) this._isDriveServiceAuthorized = value;
                if (!this.SetPipeMemberValue(nameof(IsDriveServiceAuthorized), value))
                    throw new InvalidOperationException($"Error! Failed to update pipe member {nameof(IsDriveServiceAuthorized)}!");
            }
        }

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
            // Check if we're consuming this service instance or not
            if (this.IsServiceClient)
            {
                // If we're a client, just log out that we're piping commands across to our service and exit out
                this._serviceLogger.WriteLog("WARNING! DRIVE SERVICE IS BEING BOOTED IN CLIENT CONFIGURATION!", LogType.WarnLog);
                this._serviceLogger.WriteLog("ALL COMMANDS/ROUTINES EXECUTED ON THE DRIVE SERVICE WILL BE INVOKED USING THE HOST SERVICE!", LogType.WarnLog);
                return;
            }

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

            // Log out information about our configuration values here 
            this._serviceLogger.WriteLog("PULLED GOOGLE DRIVE EXPLORER AUTHORIZATION AND CONFIGURATION INFORMATION CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"DRIVE CLIENT ID: {this._driveAuth.ClientId}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"DRIVE PROJECT ID: {this._driveAuth.ProjectId}", LogType.TraceLog);
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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to query a given location on a google drive and return all files found for it
        /// </summary>
        /// <param name="LocatedObjects">The files/folders found in the location</param>
        /// <returns>True if one or more files/folders are found</returns>
        /// <exception cref="InvalidOperationException">Thrown when the google drive service could not be built</exception>
        public bool ListDriveContents(out List<File> LocatedObjects, ResultTypes ResultFilter = ResultTypes.ALL_RESULTS)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(ListDriveContents), new List<File>(), ResultFilter);

                // Store our output value for results and exit out
                bool ExecutionPassed = bool.Parse(PipeAction.PipeCommandResult.ToString());
                LocatedObjects = ExecutionPassed ? PipeAction.PipeMethodArguments[0] as List<File> : new List<File>();
                return ExecutionPassed;
            }

            // Validate our drive service first
            if (!this._authorizeDriveService())
                throw new AuthenticationException("Error! Failed to authorize Drive Service for the MEAT Inc Organization!");

            // Build a new list request for pulling all files in from the drive location
            var ListRequest = _driveService.Files.List();
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
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(ListFolderContents), FolderId, new List<File>(), ResultFilter);

                // Store our output value for results and exit out
                bool ExecutionPassed = bool.Parse(PipeAction.PipeCommandResult.ToString());
                LocatedObjects = ExecutionPassed ? PipeAction.PipeMethodArguments[1] as List<File> : new List<File>();
                return ExecutionPassed;
            }

            // Validate our drive service first
            if (!this._authorizeDriveService())
                throw new AuthenticationException("Error! Failed to authorize Drive Service for the MEAT Inc Organization!");

            // Build a new list request for pulling all files in from the drive location
            var ListRequest = _driveService.Files.List();
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
        /// Helper method used to download a file from our Google Drive using the given file ID
        /// </summary>
        /// <param name="FileId">ID of the file to pull in</param>
        /// <param name="OutputFile">Path to store the downloaded file into</param>
        /// <returns>True if the file is downloaded and exists, false if not</returns>
        public bool DownloadDriveFile(string FileId, string OutputFile)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method and return out based on the result of the action
                var PipeAction = this.ExecutePipeMethod(nameof(DownloadDriveFile), FileId, OutputFile);
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

            // Build a new request to locate and download our file based on an ID value
            FilesResource.GetRequest FileRequest = _driveService.Files.Get(FileId);
            string DriveFileName = FileRequest.Execute().Name;

            // Log out where our file is being downloaded to and the ID of the file being pulled
            this._serviceLogger.WriteLog($"ATTEMPTING TO DOWNLOAD FILE {DriveFileName} (ID: {FileId})...", LogType.InfoLog);
            this._serviceLogger.WriteLog($"OUTPUT FILE NAME: {OutputFile}", LogType.InfoLog);

            // Invoke our file download routine here
            FileRequest.Download(new FileStream(OutputFile, FileMode.OpenOrCreate));

            // Return out based on if our file exists or not
            bool DownloadPassed = System.IO.File.Exists(OutputFile); 
            if (DownloadPassed) this._serviceLogger.WriteLog($"DOWNLOADED FILE {FileId} TO OUTPUT FILE {OutputFile} WITHOUT ISSUES!", LogType.InfoLog);
            else this._serviceLogger.WriteLog($"ERROR! FAILED TO DOWNLOAD {FileId} TO OUTPUT FILE {OutputFile}!", LogType.ErrorLog);

            // Return the result of our file state here
            return DownloadPassed;
        }
        /// <summary>
        /// Helper method which is used to download multiple files from the Google Drive into a requested folder
        /// </summary>
        /// <param name="FileIds">IDs of the files to download</param>
        /// <param name="OutputFolder">The folder to store our downloaded files into</param>
        /// <returns>True if all files are downloaded. False if not</returns>
        public bool DownloadDriveFiles(IEnumerable<string> FileIds, string OutputFolder)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method and return out based on the result of the action
                var PipeAction = this.ExecutePipeMethod(nameof(DownloadDriveFiles), FileIds, OutputFolder);
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

            // Log out where our files are being downloaded to and download them all in parallel here
            this._serviceLogger.WriteLog($"ATTEMPTING TO DOWNLOAD FILES INTO FOLDER {OutputFolder}...", LogType.InfoLog);
            this._serviceLogger.WriteLog($"TOTAL OF {FileIds.Count()} FILES ARE BEING DOWNLOADED", LogType.InfoLog);

            // Make sure our output folder exists first
            if (!Directory.Exists(OutputFolder))
            {
                // Build a directory for our output files here 
                this._serviceLogger.WriteLog($"WARNING! FOLDER {OutputFolder} DID NOT EXIST!", LogType.WarnLog);
                this._serviceLogger.WriteLog("BUILDING OUTPUT FOLDER FOR DOWNLOADED FILES NOW...", LogType.WarnLog);
                Directory.CreateDirectory(OutputFolder);
            }

            // Build a new request to locate and download our file based on an ID value
            bool DownloadsPassed = true;
            Parallel.ForEach(FileIds, FileId =>
            {
                // Check if we're able to keep downloading or not
                if (!DownloadsPassed) return; 

                // Build a new request for each file object
                FilesResource.GetRequest FileRequest = _driveService.Files.Get(FileId);
                string DriveFileName = FileRequest.Execute().Name;
                string OutputFile = Path.Combine(OutputFolder, DriveFileName);

                // Log out what file is being downloaded and save it
                this._serviceLogger.WriteLog($"--> DOWNLOADING FILE {DriveFileName} (ID: {FileId}) TO FILE {OutputFile}...", LogType.InfoLog);
                FileRequest.Download(new FileStream(OutputFile, FileMode.OpenOrCreate));

                // Make sure the file exists before moving on
                DownloadsPassed = System.IO.File.Exists(OutputFile);
                if (!DownloadsPassed) this._serviceLogger.WriteLog($"ERROR! FAILED TO DOWNLOAD FILE {DriveFileName} (ID: {FileId}) TO OUTPUT FILE {OutputFile}!", LogType.ErrorLog);
            });

            // Return the result of our file state here
            this._serviceLogger.WriteLog("DOWNLOAD ROUTINE FOR PROVIDED FILE IDS COMPLETE! RETURNING RESULTS NOW...");
            return DownloadsPassed;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

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
                _driveService = new DriveService(new BaseClientService.Initializer()
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