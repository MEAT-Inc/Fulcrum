using SharpLogging;
using SharpSimulator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using static FulcrumInjector.FulcrumViewSupport.FulcrumUpdater;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Google.Apis.Services;
using Newtonsoft.Json;
using Octokit.Internal;

// Static using calls for Google Drive API objects
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;
using GoogleDriveFileList = Google.Apis.Drive.v3.Data.FileList;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model for the Google Drive viewing content used throughout the Injector application
    /// </summary>
    internal class FulcrumGoogleDriveViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for google drive explorer
        private string _googleDriveId;                                      // ID of the drive we're searching 
        private string _applicationName;                                    // The name of the application searching the drive
        private DriveService _driveService;                                 // The service used to navigate our google drive
        private DriveExplorerAuthorization _explorerAuth;                   // Authorization object used for drive explorer auth
        private DriveExplorerConfiguration _explorerConfig;                 // Configuration object used for drive explorer setup

        // Private backing field for refresh timer
        private Stopwatch _refreshTimer;                                    // Timer used to track refresh duration

        // Private backing field for the collection of loaded logs 
        private ObservableCollection<DriveLogFileModel> _locatedLogFiles;   // Collection of all loaded log files found

        // Private backing fields for filtering collections
        private ObservableCollection<string> _yearFilters;                  // Years we can filter by 
        private ObservableCollection<string> _makeFilters;                  // Makes we can filter by
        private ObservableCollection<string> _modelFilters;                 // Models we can filter by

        #endregion // Fields

        #region Properties

        // Public property for refresh timer
        public Stopwatch RefreshTimer { get => _refreshTimer; set => PropertyUpdated(value); }

        // Public facing properties holding our collection of log files loaded
        public ObservableCollection<DriveLogFileModel> LocatedLogFiles { get => this._locatedLogFiles; set => PropertyUpdated(value); }

        // Public facing properties holding our different filter lists for the file filtering configuration
        public ObservableCollection<string> YearFilters { get => this._yearFilters; set => PropertyUpdated(value); }
        public ObservableCollection<string> MakeFilters { get => this._makeFilters; set => PropertyUpdated(value); }
        public ObservableCollection<string> ModelFilters { get => this._modelFilters; set => PropertyUpdated(value); }

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
        /// Builds a new google drive view model
        /// </summary>
        /// <param name="GoogleDriveUserControl">UserControl which holds the content for our google drive view</param>
        public FulcrumGoogleDriveViewModel(UserControl GoogleDriveUserControl) : base(GoogleDriveUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP GOOGLE DRIVE VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Try and build our drive service here
            if (!this.ConfigureDriveService(out this._driveService))
                throw new InvalidComObjectException("Error! Failed to build new Drive Explorer Service!");

            // Log completed building view model instance and exit out
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new google drive explorer service for finding files 
        /// </summary>
        /// <returns>True if the service is built. False if not</returns>
        /// <param name="BuiltService">The built google drive service. Null if failed to build</param>
        public bool ConfigureDriveService(out DriveService BuiltService)
        {
            // Pull in the drive ID and application name first
            this._applicationName = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorDriveExplorer.ApplicationName");
            this._googleDriveId = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorDriveExplorer.GoogleDriveId").UnscrambleString();
            this.ViewModelLogger.WriteLog("PULLED GOOGLE DRIVE ID AND APPLICATION NAME CORRECTLY!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"DRIVE ID: {this._googleDriveId}");
            this.ViewModelLogger.WriteLog($"APPLICATION NAME: {this._applicationName}");

            // Pull in the configuration values for the drive explorer and unscramble needed strings
            this.ViewModelLogger.WriteLog("LOADING AND UNSCRAMBLING CONFIGURATION FOR DRIVE SERVICE NOW...");
            this._explorerConfig = ValueLoaders.GetConfigValue<DriveExplorerConfiguration>("FulcrumConstants.InjectorDriveExplorer.ExplorerConfiguration");
            this._explorerConfig.ClientId = this._explorerConfig.ClientId.UnscrambleString();
            this._explorerConfig.ProjectId = this._explorerConfig.ProjectId.UnscrambleString();
            this._explorerConfig.ClientSecret = this._explorerConfig.ClientSecret.UnscrambleString();

            // Pull in the configuration values for the drive explorer authorization and unscramble needed strings
            this.ViewModelLogger.WriteLog("LOADING AND UNSCRAMBLING AUTHORIZATION FOR DRIVE SERVICE NOW...");
            this._explorerAuth = ValueLoaders.GetConfigValue<DriveExplorerAuthorization>("FulcrumConstants.InjectorDriveExplorer.ExplorerAuthorization");
            this._explorerAuth.ClientId = this._explorerAuth.ClientId.UnscrambleString();
            this._explorerAuth.ProjectId = this._explorerAuth.ProjectId.UnscrambleString();
            this._explorerAuth.ClientEmail = this._explorerAuth.ClientEmail.UnscrambleString();
            this._explorerAuth.PrivateKeyId = this._explorerAuth.PrivateKeyId.UnscrambleString();
            this._explorerAuth.ClientCertUrl = this._explorerAuth.ClientCertUrl.UnscrambleString();
            this._explorerAuth.PrivateKey = this._explorerAuth.PrivateKey.UnscrambleString().Replace("\\n", string.Empty);

            // Log out that our unscramble routines have been completed
            this.ViewModelLogger.WriteLog("PULLED GOOGLE DRIVE EXPLORER AUTHORIZATION AND CONFIGURATION INFORMATION CORRECTLY!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"DRIVE CLIENT ID: {this._explorerConfig.ClientId}");
            this.ViewModelLogger.WriteLog($"DRIVE PROJECT ID: {this._explorerConfig.ProjectId}");
            this.ViewModelLogger.WriteLog($"DRIVE SERVICE EMAIL: {this._explorerAuth.ClientEmail}");

            try
            {
                // Configure the google drive service here
                this.ViewModelLogger.WriteLog("BUILDING NEW GOOGLE DRIVE SERVICE NOW...", LogType.WarnLog);
                BuiltService = new DriveService(new BaseClientService.Initializer()
                {
                    // Store the API configuration and Application name for the authorization helper
                    ApplicationName = this._applicationName,
                    HttpClientInitializer = GoogleCredential.FromJson(
                        JsonConvert.SerializeObject(this._explorerAuth))
                        .CreateScoped(DriveService.Scope.DriveReadonly)
                });

                // Return the new drive service object 
                this.ViewModelLogger.WriteLog("BUILT NEW GOOGLE DRIVE EXPLORER SERVICE WITHOUT ISSUES!", LogType.InfoLog);
                return true;
            }
            catch (Exception ServiceInitEx)
            {
                // Log out the failure for the service creation and exit out false 
                BuiltService = null;
                this.ViewModelLogger.WriteLog("ERROR! FAILED TO BUILD NEW DRIVE EXPLORER SERVICE!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN DURING SERVICE CREATION IS BEING LOGGED BELOW", ServiceInitEx);
                return false;
            }
        }
        /// <summary>
        /// Helper function used to list all the files in the google drive location holding all injector files
        /// </summary>
        /// <returns>True if the files are queried correctly and one or more are found. False if none are located.</returns>
        /// <param name="InjectorLogSets">The located injector log file sets</param>
        /// <exception cref="InvalidOperationException">Thrown when the google drive helper service is not yet built and can not be configured</exception>
        public bool LocateInjectorLogFiles(out List<DriveLogFileModel> InjectorLogSets)
        {
            // Initialize our list of output files and a timer for diagnostic purposes
            this.ViewModelLogger.WriteLog("REFRESHING INJECTOR LOG FILE SETS NOW...");
            this.LocatedLogFiles ??= new ObservableCollection<DriveLogFileModel>();
            this.RefreshTimer = new Stopwatch();
            this.RefreshTimer.Start();

            // Validate our Drive Explorer service is built and ready for use
            this.ViewModelLogger.WriteLog("VALIDATING INJECTOR DRIVE SERVICE...");
            if (this._driveService == null && !this.ConfigureDriveService(out this._driveService))
                throw new InvalidOperationException("Error! Google Drive explorer service has not been configured!");

            // Build a new request to list all the files in the drive
            this.ViewModelLogger.WriteLog("DRIVE SERVICE IS CONFIGURED!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("BUILDING REQUEST TO QUERY DRIVE CONTENTS NOW...");
            var ListFilesRequest = this._driveService.Files.List();
            ListFilesRequest.PageSize = 1000;
            ListFilesRequest.Corpora = "drive";
            ListFilesRequest.SupportsTeamDrives = true;
            ListFilesRequest.IncludeTeamDriveItems = true;
            ListFilesRequest.DriveId = this._googleDriveId;
            ListFilesRequest.IncludeItemsFromAllDrives = true;

            // Build a new PageStreamer to automatically page through results of files
            this.ViewModelLogger.WriteLog("BUILDING PAGE STREAMER TO COMBINE ALL LISTED FILE RESULTS...");
            var FilePageStreamer = new Google.Apis.Requests.PageStreamer<GoogleDriveFile, FilesResource.ListRequest, GoogleDriveFileList, string>(
                (ReqObj, TokenObj) => ListFilesRequest.PageToken = TokenObj,
                RespObj => RespObj.NextPageToken,
                RespObj => RespObj.Files);

            // Execute the request for pulling files from the drive here combining paged results one at a time
            this.LocatedLogFiles.Clear();
            this.ViewModelLogger.WriteLog("EXECUTING REQUEST FOR DRIVE CONTENTS NOW...");
            GoogleDriveFileList CombinedFileLists = new GoogleDriveFileList { Files = new List<GoogleDriveFile>() };
            foreach (var LocatedFile in FilePageStreamer.Fetch(ListFilesRequest)) CombinedFileLists.Files.Add(LocatedFile);
            foreach (var FileLocated in CombinedFileLists.Files) this.LocatedLogFiles.Add(new DriveLogFileModel(FileLocated));

            // Stop our timer and log out the results of this routine
            InjectorLogSets = this.LocatedLogFiles.ToList();
            this.ViewModelLogger.WriteLog("DONE REFRESHING INJECTOR LOG FILE SETS!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"FOUND A TOTAL OF {InjectorLogSets.Count} FILES IN {this.RefreshTimer.Elapsed:mm\\:ss\\:fff}");
            this.RefreshTimer.Stop();

            // Return out based on the number of files loaded in 
            return InjectorLogSets.Count != 0;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to build filters for all the injector log files located in the google drive.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no log files are located for filtering</exception>
        public void _buildInjectorLogFilters()
        {
            // Check to make sure log files exist at this point
            this.ViewModelLogger.WriteLog("FILTERING LOGS LOCATED FROM GOOGLE DRIVE NOW...", LogType.WarnLog);
            if (this.LocatedLogFiles.Count == 0 && !this.LocateInjectorLogFiles(out _))
                throw new InvalidOperationException("Error! Failed to find any log files to filter!");

            // Setup filtering lists
            this.YearFilters = new ObservableCollection<string>();
            this.MakeFilters = new ObservableCollection<string>(); 
            this.ModelFilters = new ObservableCollection<string>();
            this.ViewModelLogger.WriteLog($"FILTERING {this.LocatedLogFiles.Count} LOG FILES NOW...");
            this.ViewModelLogger.WriteLog("CONFIGURED EMPTY FILTERING LISTS CORRECTLY!", LogType.InfoLog);

            // Iterate all the log files and pull out the parts of each name to build filtering context

        }
    }
}
