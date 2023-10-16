using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumControls;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumEncryption;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.DriveBrokerModels;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Newtonsoft.Json;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Static helper class used to configure and consume google drive service
    /// </summary>
    public static class FulcrumDriveBroker
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private logger object for the drive broker helper
        private static readonly SharpLogger _driveServiceLogger = new(LoggerActions.UniversalLogger);

        // Private backing fields for drive service objects
        private static DriveService _driveService;          // Static service object for our google drive service
        private static DriveAuthorization _driveAuth;       // The authorization configuration for the drive service
        private static DriveConfiguration _driveConfig;     // The initialization configuration for the drive service

        #endregion // Fields

        #region Properties
        
        // Public readonly property holding our drive service
        public static DriveService DriveService => _driveService; 

        // Public readonly properties holding information about the drive configuration
        public static string GoogleDriveId { get; private set; }
        public static string ApplicationName { get; private set; }

        #endregion // Properties

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

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new google drive explorer service for finding files 
        /// </summary>
        /// <returns>True if the service is built. False if not</returns>
        /// <param name="BuiltService">The built google drive service. Null if failed to build</param>
        /// <param name="ForceCreate">When true, we force recreate a drive service regardless of the state of the existing one</param>
        public static bool ConfigureDriveService(out DriveService BuiltService, bool ForceCreate = false)
        {
            // Make sure the drive service isn't configured yet and we're not forcing creation
            if (_driveService != null && !ForceCreate)
            {
                // Log out that this service already exists and that we're just returning it out
                _driveServiceLogger.WriteLog("DRIVE SERVICE EXISTED ALREADY! NOT CREATING A NEW ONE", LogType.WarnLog);
                BuiltService = _driveService;
                return true;
            }
            
            // Pull in the drive ID and application name first
            ApplicationName = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorDriveExplorer.ApplicationName");
            GoogleDriveId = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorDriveExplorer.GoogleDriveId");
            _driveServiceLogger.WriteLog("PULLED GOOGLE DRIVE ID AND APPLICATION NAME CORRECTLY!", LogType.InfoLog);
            _driveServiceLogger.WriteLog($"DRIVE ID: {GoogleDriveId}");
            _driveServiceLogger.WriteLog($"APPLICATION NAME: {ApplicationName}");

            // Pull in the configuration values for the drive explorer and unscramble needed strings
            _driveServiceLogger.WriteLog("LOADING AND UNSCRAMBLING CONFIGURATION FOR DRIVE SERVICE NOW...");
            _driveConfig = ValueLoaders.GetConfigValue<DriveConfiguration>("FulcrumConstants.InjectorDriveExplorer.ExplorerConfiguration");

            // Pull in the configuration values for the drive explorer authorization and unscramble needed strings
            _driveServiceLogger.WriteLog("LOADING AND UNSCRAMBLING AUTHORIZATION FOR DRIVE SERVICE NOW...");
            _driveAuth = ValueLoaders.GetConfigValue<DriveAuthorization>("FulcrumConstants.InjectorDriveExplorer.ExplorerAuthorization");

            // Log out that our unscramble routines have been completed
            _driveServiceLogger.WriteLog("PULLED GOOGLE DRIVE EXPLORER AUTHORIZATION AND CONFIGURATION INFORMATION CORRECTLY!", LogType.InfoLog);
            _driveServiceLogger.WriteLog($"DRIVE CLIENT ID: {_driveConfig.ClientId}");
            _driveServiceLogger.WriteLog($"DRIVE PROJECT ID: {_driveConfig.ProjectId}");
            _driveServiceLogger.WriteLog($"DRIVE SERVICE EMAIL: {_driveAuth.ClientEmail}");

            try
            {
                // Configure the google drive service here
                _driveServiceLogger.WriteLog("BUILDING NEW GOOGLE DRIVE SERVICE NOW...", LogType.WarnLog);
                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    // Store the API configuration and Application name for the authorization helper
                    ApplicationName = ApplicationName,
                    HttpClientInitializer = GoogleCredential
                        .FromJson(JsonConvert.SerializeObject(_driveAuth, new DriveAuthJsonConverter(false)))
                        .CreateScoped(DriveService.Scope.DriveReadonly)
                });

                // Return the new drive service object 
                _driveServiceLogger.WriteLog("BUILT NEW GOOGLE DRIVE EXPLORER SERVICE WITHOUT ISSUES!", LogType.InfoLog);
                BuiltService = _driveService;
                return true;
            }
            catch (Exception ServiceInitEx)
            {
                // Log out the failure for the service creation and exit out false 
                _driveServiceLogger.WriteLog("ERROR! FAILED TO BUILD NEW DRIVE EXPLORER SERVICE!", LogType.ErrorLog);
                _driveServiceLogger.WriteException("EXCEPTION THROWN DURING SERVICE CREATION IS BEING LOGGED BELOW", ServiceInitEx);
                _driveService = null;
                BuiltService = null;
                return false;
            }
        }

        /// <summary>
        /// Helper method used to query a given location on a google drive and return all files found for it
        /// </summary>
        /// <param name="LocatedObjects">The files/folders found in the location</param>
        /// <returns>True if one or more files/folders are found</returns>
        /// <exception cref="InvalidOperationException">Thrown when the google drive service could not be built</exception>
        public static bool ListDriveContents(out List<File> LocatedObjects, ResultTypes ResultFilter = ResultTypes.ALL_RESULTS)
        {
            // Validate our drive service first
            if (_driveService == null && !ConfigureDriveService(out _driveService))
                throw new InvalidOperationException("Error! Failed to configure Drive Service!");

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
        public static bool ListFolderContents(string FolderId, out List<File> LocatedObjects, ResultTypes ResultFilter = ResultTypes.ALL_RESULTS)
        {
            // Validate our drive service first
            if (_driveService == null && !ConfigureDriveService(out _driveService))
                throw new InvalidOperationException("Error! Failed to configure Drive Service!");

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
    }
}
