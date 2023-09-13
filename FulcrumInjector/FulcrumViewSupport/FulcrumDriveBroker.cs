using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
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
    internal static class FulcrumDriveBroker
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private logger object for the drive broker helper
        private static readonly SharpLogger _driveServiceLogger = new(LoggerActions.UniversalLogger);

        // Private backing fields for drive service objects
        private static DriveService _driveService;                  // Static service object for our google drive service
        private static DriveExplorerAuthorization _explorerAuth;    // The authorization configuration for the drive service
        private static DriveExplorerConfiguration _explorerConfig;  // The initialization configuration for the drive service

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
        /// Class object used to define the JSON object of a google drive explorer configuration
        /// </summary>
        public class DriveExplorerConfiguration
        {
            [JsonProperty("auth_uri")] public string AuthUri { get; set; }
            [JsonProperty("token_uri")] public string TokenUri { get; set; }
            [JsonProperty("client_id")] public string ClientId { get; set; }
            [JsonProperty("project_id")] public string ProjectId { get; set; }
            [JsonProperty("client_secret")] public string ClientSecret { get; set; }
            [JsonProperty("redirect_uris")] public string[] RedirectUris { get; set; }
            [JsonProperty("auth_provider_x509_cert_url")] public string AuthProvider { get; set; }
        }
        /// <summary>
        /// Class object used to define the JSON object of our google drive authorization
        /// </summary>
        public class DriveExplorerAuthorization
        {
            [JsonProperty("type")] public string Type { get; set; }
            [JsonProperty("auth_uri")] public string AuthUri { get; set; }
            [JsonProperty("token_uri")] public string TokenUri { get; set; }
            [JsonProperty("client_id")] public string ClientId { get; set; }
            [JsonProperty("project_id")] public string ProjectId { get; set; }
            [JsonProperty("private_key")] public string PrivateKey { get; set; }
            [JsonProperty("client_email")] public string ClientEmail { get; set; }
            [JsonProperty("private_key_id")] public string PrivateKeyId { get; set; }
            [JsonProperty("universe_domain")] public string UniverseDomain { get; set; }
            [JsonProperty("client_x509_cert_url")] public string ClientCertUrl { get; set; }
            [JsonProperty("auth_provider_x509_cert_url")] public string AuthProviderUrl { get; set; }
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
            _driveServiceLogger.WriteLog("PULLED GOOGLE DRIVE ID APPLICATION NAME CORRECTLY!", LogType.InfoLog);
            _driveServiceLogger.WriteLog($"APPLICATION NAME: {ApplicationName}");

            // Pull in the configuration values for the drive explorer and unscramble needed strings
            _driveServiceLogger.WriteLog("LOADING AND UNSCRAMBLING CONFIGURATION FOR DRIVE SERVICE NOW...");
            _explorerConfig = ValueLoaders.GetConfigValue<DriveExplorerConfiguration>("FulcrumConstants.InjectorDriveExplorer.ExplorerConfiguration");
            _explorerConfig.ClientId = _explorerConfig.ClientId.UnscrambleString();
            _explorerConfig.ProjectId = _explorerConfig.ProjectId.UnscrambleString();
            _explorerConfig.ClientSecret = _explorerConfig.ClientSecret.UnscrambleString();

            // Pull in the configuration values for the drive explorer authorization and unscramble needed strings
            _driveServiceLogger.WriteLog("LOADING AND UNSCRAMBLING AUTHORIZATION FOR DRIVE SERVICE NOW...");
            _explorerAuth = ValueLoaders.GetConfigValue<DriveExplorerAuthorization>("FulcrumConstants.InjectorDriveExplorer.ExplorerAuthorization");
            _explorerAuth.ClientId = _explorerAuth.ClientId.UnscrambleString();
            _explorerAuth.ProjectId = _explorerAuth.ProjectId.UnscrambleString();
            _explorerAuth.ClientEmail = _explorerAuth.ClientEmail.UnscrambleString();
            _explorerAuth.PrivateKeyId = _explorerAuth.PrivateKeyId.UnscrambleString();
            _explorerAuth.ClientCertUrl = _explorerAuth.ClientCertUrl.UnscrambleString();
            _explorerAuth.PrivateKey = _explorerAuth.PrivateKey.UnscrambleString().Replace("\\n", string.Empty);

            // Log out that our unscramble routines have been completed
            _driveServiceLogger.WriteLog("PULLED GOOGLE DRIVE EXPLORER AUTHORIZATION AND CONFIGURATION INFORMATION CORRECTLY!", LogType.InfoLog);
            _driveServiceLogger.WriteLog($"DRIVE CLIENT ID: {_explorerConfig.ClientId}");
            _driveServiceLogger.WriteLog($"DRIVE PROJECT ID: {_explorerConfig.ProjectId}");
            _driveServiceLogger.WriteLog($"DRIVE SERVICE EMAIL: {_explorerAuth.ClientEmail}");

            try
            {
                // Configure the google drive service here
                _driveServiceLogger.WriteLog("BUILDING NEW GOOGLE DRIVE SERVICE NOW...", LogType.WarnLog);
                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    // Store the API configuration and Application name for the authorization helper
                    ApplicationName = ApplicationName,
                    HttpClientInitializer = GoogleCredential.FromJson(
                        JsonConvert.SerializeObject(_explorerAuth))
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
        /// <param name="DriveId">The ID of the location to search</param>
        /// <param name="LocatedFiles">The files/folders found in the location</param>
        /// <returns>True if one or more files/folders are found</returns>
        public static bool ListDriveContents(string DriveId, out List<File> LocatedFiles)
        {
            // Build a new list request for pulling all files in from the drive location
            var ListRequest = _driveService.Files.List();
            ListRequest.PageSize = 1000;
            ListRequest.DriveId = DriveId;
            ListRequest.Corpora = "drive";
            ListRequest.SupportsTeamDrives = true;
            ListRequest.IncludeTeamDriveItems = true;
            ListRequest.IncludeItemsFromAllDrives = true;

            // Build a new PageStreamer to automatically page through results of files
            var PageStreamer = new Google.Apis.Requests.PageStreamer<File, FilesResource.ListRequest, FileList, string>(
                (ReqObj, TokenObj) => ListRequest.PageToken = TokenObj,
                RespObj => RespObj.NextPageToken,
                RespObj => RespObj.Files);

            // Execute the request for pulling files from the drive here combining paged results one at a time
            LocatedFiles = PageStreamer.Fetch(ListRequest).ToList();

            // Return out based on the number of logs found 
            if (LocatedFiles.Count == 0) _driveServiceLogger.WriteLog($"WARNING! NO LOG FILES WERE FOUND FOR FOLDER ID {DriveId}!", LogType.WarnLog);
            else _driveServiceLogger.WriteLog($"REFRESHED FOLDER {DriveId} CORRECTLY! LOCATED A TOTAL OF {LocatedFiles.Count} OBJECTS!", LogType.InfoLog);
            return LocatedFiles.Count > 0;
        }
    }
}
