using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Remoting.Lifetime;
using System.Security.Authentication;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FulcrumJson;
using FulcrumService;
using FulcrumUpdaterService.UpdaterServiceModels;
using Newtonsoft.Json;
using Octokit;
using SharpLogging;

namespace FulcrumUpdaterService
{
    /// <summary>
    /// Class which houses the logic for pulling in a new Fulcrum Injector MSI File.
    /// </summary>
    public partial class FulcrumUpdater : FulcrumServiceBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our updater service instance
        private static FulcrumUpdater _serviceInstance;           // Instance of our service object
        private static readonly object _serviceLock = new();      // Lock object for building service instances

        // Private backing fields for the Git helper, timer, and updater configuration
        private GitHubClient _gitUpdaterClient;                   // The updater client for pulling in versions
        private readonly UpdaterServiceSettings _serviceConfig;   // Updater configuration values

        // Private backing fields for our public facing properties
        private Release[] _injectorReleases;                      // Collection of all releases found for the injector app
        private bool _isGitClientAuthorized;                      // Private backing bool value to store if we're authorized or not

        #endregion //Fields

        #region Properties

        // Public facing property holding our authorization state for the updater 
        public bool IsGitClientAuthorized
        {
            // Pull the value from our service host or the local instance based on client configuration
            get => !this.IsServiceClient
                ? this._isGitClientAuthorized
                : bool.Parse(this.GetPipeMemberValue(nameof(IsGitClientAuthorized)).ToString());

            private set
            {
                // Check if we're using a service client or not and set the value accordingly
                if (!this.IsServiceClient)
                {
                    // Set our value and exit out
                    this._isGitClientAuthorized = value;
                    return;
                }

                // If we're using a client instance, invoke a pipe routine
                if (!this.SetPipeMemberValue(nameof(IsGitClientAuthorized), value))
                    throw new InvalidOperationException($"Error! Failed to update pipe member {nameof(IsGitClientAuthorized)}!");
            }
        }

        // Public facing properties holding information about our latest version information
        public Release[] InjectorReleases
        { 
            // Pull the value from our service host or the local instance based on client configuration
            get => !this.IsServiceClient
                ? this._injectorReleases
                : this.GetPipeMemberValue(nameof(InjectorReleases)) as Release[];

            private set
            {
                // Check if we're using a service client or not and set the value accordingly
                if (!this.IsServiceClient)
                {
                    // Set our value and exit out
                    this._injectorReleases = value;
                    return;
                }

                // If we're using a client instance, invoke a pipe routine
                if (!this.SetPipeMemberValue(nameof(InjectorReleases), value))
                    throw new InvalidOperationException($"Error! Failed to update pipe member {nameof(InjectorReleases)}!");
            }
        }
        public string LatestInjectorVersion => this.InjectorVersions[0];
        public Release LatestInjectorRelease => this.InjectorReleases[0];
        public string LatestInjectorReleaseNotes => this.LatestInjectorRelease.Body;
        public string[] InjectorVersions => this.InjectorReleases
            .Select(ReleaseTag => Regex.Match(ReleaseTag.TagName, @"(\d+(?>\.|))+").Value)
            .ToArray();

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new injector update helper object which pulls our GitHub release information
        /// </summary>
        /// <param name="ServiceSettings">Optional settings object for our service configuration</param>
        internal FulcrumUpdater(UpdaterServiceSettings ServiceSettings = null) : base(ServiceTypes.UPDATER_SERVICE)
        {
            // Check if we're consuming this service instance or not
            if (this.IsServiceClient)
            {
                // If we're a client, just log out that we're piping commands across to our service and exit out
                this._serviceLogger.WriteLog("WARNING! UPDATER SERVICE IS BEING BOOTED IN CLIENT CONFIGURATION!", LogType.WarnLog);
                this._serviceLogger.WriteLog("ALL COMMANDS/ROUTINES EXECUTED ON THE DRIVE SERVICE WILL BE INVOKED USING THE HOST SERVICE!", LogType.WarnLog);
                return;
            }

            // Log we're building this new service and log out the name we located for it
            this._serviceLogger.WriteLog("SPAWNING NEW UPDATER SERVICE!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLED IN A NEW SERVICE NAME OF {this.ServiceName}", LogType.InfoLog);

            // Store the configuration for our update helper object here
            this._serviceConfig = ServiceSettings ?? ValueLoaders.GetConfigValue<UpdaterServiceSettings>("FulcrumServices.FulcrumUpdaterService");
            this._serviceLogger.WriteLog("PULLED BASE SERVICE CONFIGURATION VALUES CORRECTLY!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"SERVICE NAME: {this._serviceConfig.ServiceName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"SERVICE ENABLED: {this._serviceConfig.ServiceEnabled}", LogType.TraceLog);

            // Log out information about the updater configuration here
            this._serviceLogger.WriteLog("PULLED IN OUR CONFIGURATIONS FOR INJECTOR UPDATER API CALLS OK!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"USERNAME: {this._serviceConfig.UpdaterUserName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"FORCE UPDATES: {this._serviceConfig.ForceUpdateReady}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"REPOSITORY NAME:  {this._serviceConfig.UpdaterRepoName}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"ORGANIZATION NAME: {this._serviceConfig.UpdaterOrgName}", LogType.TraceLog);
        }
        /// <summary>
        /// Static CTOR for an update broker instance. Simply pulls out the new singleton instance for our update broker
        /// </summary>
        /// <param name="ForceInit">When true, we force rebuild the requested service instance</param>
        /// <returns>The instance for our service singleton</returns>
        public static Task<FulcrumUpdater> InitializeUpdaterService(bool ForceInit = false)
        {
            // Make sure we actually want to use this watchdog service 
            var ServiceConfig = ValueLoaders.GetConfigValue<UpdaterServiceSettings>("FulcrumServices.FulcrumUpdaterService");
            if (!ServiceConfig.ServiceEnabled) {
                _serviceInitLogger.WriteLog("WARNING! UPDATER SERVICE IS TURNED OFF IN OUR CONFIGURATION FILE! NOT BOOTING IT", LogType.WarnLog);
                return null;
            }

            // Spin up a new injector email service here if needed           
            _serviceInitLogger.WriteLog($"SPAWNING A NEW UPDATER SERVICE INSTANCE NOW...", LogType.WarnLog);
            return Task.Run(() =>
            {
                // Lock our service object for thread safe operations
                lock (_serviceLock)
                {
                    // Check if we need to force rebuilt this service or not
                    if (_serviceInstance != null && !ForceInit) {
                        _serviceInitLogger.WriteLog("FOUND EXISTING UPDATER SERVICE INSTANCE! RETURNING IT NOW...");
                        return _serviceInstance;
                    }

                    // Build and boot a new service instance for our watchdog
                    _serviceInstance = new FulcrumUpdater(ServiceConfig);
                    _serviceInitLogger.WriteLog("SPAWNED NEW INJECTOR UPDATER SERVICE OK!", LogType.InfoLog);

                    // Return the service instance here
                    return _serviceInstance;
                }
            });
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the service up and builds an update helper process
        /// </summary>
        /// <param name="StartupArgs">NOT USED!</param>
        protected override void OnStart(string[] StartupArgs)
        {
            try
            {
                // Log out what type of service is being configured currently
                this._serviceLogger.WriteLog($"BOOTING NEW {this.GetType().Name} SERVICE NOW...", LogType.WarnLog);
                this._serviceLogger.WriteLog($"CONFIGURING NEW GITHUB CONNECTION HELPER FOR INJECTOR SERVICE...", LogType.InfoLog);

                // Authorize our git client here if needed
                if (!this._authorizeGitClient())
                    throw new AuthenticationException("Error! Failed to authorize Git Client for the MEAT Inc Organization!");

                // Log out that our service has been booted without issues
                this._serviceLogger.WriteLog("UPDATER SERVICE HAS BEEN CONFIGURED AND BOOTED CORRECTLY!", LogType.InfoLog);
            }
            catch (Exception StartWatchdogEx)
            {
                // Log out the failure and exit this method
                this._serviceLogger.WriteLog("ERROR! FAILED TO BOOT NEW UPDATER SERVICE INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE START ROUTINE IS LOGGED BELOW", StartWatchdogEx);
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Checks if a version is ready to be updated or not.
        /// </summary>
        /// <param name="InputVersion">Current Version</param>
        /// <returns>True if updates ready. False if not.</returns>
        /// <exception cref="InvalidOperationException">Thrown when we're unable to refresh injector versions</exception>
        public bool CheckAgainstVersion(string InputVersion)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(CheckAgainstVersion), InputVersion);
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

            // Validate that the versions exist to compare
            if (this.InjectorReleases == null) 
            {
                // IF no versions are found, then refresh them all now
                this._serviceLogger.WriteLog("WARNING! INJECTOR VERSION INFORMATION WAS NOT POPULATED! UPDATING IT NOW...", LogType.WarnLog); 
                if (!this._refreshInjectorReleases())
                    throw new InvalidOperationException("Error! Failed to refresh Injector Versions!");
            }

            // Now compare the versions
            Version InputVersionParsed = Version.Parse(Regex.Match(InputVersion, @"(\d+(?>\.|))+").Value);
            Version LatestVersionParsed = Version.Parse(Regex.Match(this.LatestInjectorVersion, @"(\d+(?>\.|))+").Value);
            this._serviceLogger.WriteLog("PARSED VERSIONS CORRECTLY! READY TO COMPARE AND RETURN", LogType.InfoLog);

            // Compare and log result
            bool NeedsUpdate = InputVersionParsed < LatestVersionParsed;
            this._serviceLogger.WriteLog($"RESULT FROM VERSION COMPARISON: {NeedsUpdate}", LogType.WarnLog);
            this._serviceLogger.WriteLog("UPDATE CHECK PASSED! PLEASE EXECUTE ACCORDINGLY...", LogType.InfoLog);
            return NeedsUpdate;
        }
        /// <summary>
        /// Finds the asset URL for the installer needed based on the given version tag
        /// </summary>
        /// <param name="VersionTag">The tag of the version to find the URL for</param>
        /// <returns>The URL of the MSI being returned for this version</returns>
        /// <exception cref="InvalidOperationException">Thrown when we're unable to refresh injector versions</exception>
        public string GetInjectorAssetUrl(string VersionTag)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(GetInjectorAssetUrl), VersionTag);
                return PipeAction.PipeCommandResult.ToString();
            }

            // Validate that the versions exist to compare
            if (this.InjectorReleases == null)
            {
                // IF no versions are found, then refresh them all now
                this._serviceLogger.WriteLog("WARNING! INJECTOR VERSION INFORMATION WAS NOT POPULATED! UPDATING IT NOW...", LogType.WarnLog);
                if (!this._refreshInjectorReleases())
                    throw new InvalidOperationException("Error! Failed to refresh Injector Versions!");
            }

            // First find our version to use using our version/release lookup tool
            var ReleaseToUse = this.InjectorReleases.FirstOrDefault(ReleaseObj => ReleaseObj.TagName.Contains(VersionTag));
            this._serviceLogger.WriteLog("PULLED IN A NEW RELEASE OBJECT TO UPDATE WITH!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"RELEASE TAG: {ReleaseToUse.TagName}");

            // Now get the asset url and download it here into a temp file
            string InjectorAssetUrl = ReleaseToUse.Assets.FirstOrDefault(AssetObj => AssetObj.BrowserDownloadUrl.EndsWith("msi")).BrowserDownloadUrl;
            string InjectorAssetPath = Path.ChangeExtension(Path.GetTempFileName(), "msi");
            this._serviceLogger.WriteLog($"RELEASE ASSET FOUND! URL IS: {InjectorAssetUrl}", LogType.InfoLog);
            return InjectorAssetPath;
        }
        /// <summary>
        /// Downloads the given version installer for the injector application and stores it in a temp path
        /// </summary>
        /// <param name="VersionTag">The version of the injector we're looking to download</param>
        /// <param name="InstallerPath">The path to the installer pulled in from the repository</param>
        /// <returns>True if the MSI is pulled in. False if it is not</returns>
        /// <exception cref="InvalidOperationException">Thrown when we're unable to refresh injector versions</exception>
        public bool DownloadInjectorAsset(string VersionTag, out string InstallerPath)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(DownloadInjectorAsset), VersionTag, string.Empty);
                InstallerPath = PipeAction.PipeMethodArguments[1].ToString();
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

            // Validate that the versions exist to compare
            if (this.InjectorReleases == null)
            {
                // IF no versions are found, then refresh them all now
                this._serviceLogger.WriteLog("WARNING! INJECTOR VERSION INFORMATION WAS NOT POPULATED! UPDATING IT NOW...", LogType.WarnLog);
                if (!this._refreshInjectorReleases())
                    throw new InvalidOperationException("Error! Failed to refresh Injector Versions!");
            }

            // Get the URL of the asset we're looking to download here
            this._serviceLogger.WriteLog($"LOCATING ASSET URL FOR VERSION TAG {VersionTag} NOW...", LogType.InfoLog);
            string AssetDownloadUrl = this.GetInjectorAssetUrl(VersionTag);
            if (string.IsNullOrWhiteSpace(AssetDownloadUrl))
            {
                // Log out that no release could be found for this version number and return out
                this._serviceLogger.WriteLog($"ERROR! NO URL COULD BE FOUND FOR AN ASSET BELONGING TO VERSION TAG {VersionTag}!", LogType.ErrorLog);
                this._serviceLogger.WriteLog("THIS IS A FATAL ISSUE! PLEASE ENSURE YOU PROVIDED A VALID VERSION TAG!", LogType.ErrorLog);

                // Null out the installer path and return false
                InstallerPath = string.Empty;
                return false;
            }

            // Build a new web client and configure a temporary file to download our release installer into
            Stopwatch DownloadTimer = new Stopwatch();
            WebClient AssetDownloadHelper = new WebClient();
            string DownloadFilePath = Path.Combine(Path.GetTempPath(), $"FulcrumInstaller_{VersionTag}.msi");
            this._serviceLogger.WriteLog($"PULLING IN RELEASE VERSION {VersionTag} NOW...", LogType.InfoLog);
            this._serviceLogger.WriteLog($"ASSET DOWNLOAD URL IS {AssetDownloadUrl}", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLING DOWNLOADED MSI INTO TEMP FILE {DownloadFilePath}", LogType.InfoLog);

            try
            {
                // Boot the download timer and kick off our download here
                DownloadTimer.Start();
                AssetDownloadHelper.DownloadFile(AssetDownloadUrl, DownloadFilePath);
                DownloadTimer.Stop();

                // Log out how long this download process took and return out our download path
                this._serviceLogger.WriteLog($"DOWNLOAD COMPLETE! ELAPSED TIME: {DownloadTimer.Elapsed:mm:ss}", LogType.InfoLog);
                if (!File.Exists(DownloadFilePath)) throw new FileNotFoundException("Error! Failed to find injector installer after download!");

                // Store our downloaded file in the output path and return true
                InstallerPath = DownloadFilePath;
                return true;
            }
            catch (Exception DownloadFileEx)
            {
                // Catch our exception and log it out here
                this._serviceLogger.WriteLog($"ERROR! FAILED TO DOWNLOAD INJECTOR INSTALLER VERSION {VersionTag}!", LogType.ErrorLog);
                this._serviceLogger.WriteException("EXCEPTION THROWN DURING DOWNLOAD ROUTINE IS BEING LOGGED BELOW", DownloadFileEx);

                // Null out our output variables and return out
                InstallerPath = string.Empty;
                return false;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private helper method used to authorize our GitHub client on the MEAT Inc organization
        /// </summary>
        /// <returns>True if the client is authorized. False if not</returns>
        private bool _authorizeGitClient()
        {
            try
            {
                // Check if we're configured or not already 
                if (this.IsGitClientAuthorized) return true;

                // Build a new git client here for authorization
                this._serviceLogger.WriteLog("BUILDING AND AUTHORIZING GIT CLIENT NOW...", LogType.InfoLog);
                Credentials LoginCredentials = new Credentials(this._serviceConfig.UpdaterSecretKey);
                this._gitUpdaterClient = new GitHubClient(new ProductHeaderValue(this._serviceConfig.UpdaterUserName)) { Credentials = LoginCredentials };
                this._serviceLogger.WriteLog("BUILT NEW GIT CLIENT FOR UPDATING OK! AUTHENTICATION WITH BOT LOGIN ACCESS PASSED!", LogType.InfoLog);

                // Return true once completed and mark our client authorized
                this.IsGitClientAuthorized = true;
                return true;
            }
            catch (Exception AuthEx)
            {
                // Log our exception and return false 
                this._serviceLogger.WriteLog("ERROR! FAILED TO AUTHORIZE NEW GIT CLIENT!", LogType.ErrorLog);
                this._serviceLogger.WriteException("EXCEPTION DURING AUTHORIZATION IS BEING LOGGED BELOW", AuthEx);
                return false;
            }
        }
        /// <summary>
        /// Updates the injector version information on the class instance.
        /// </summary>
        /// <exception cref="AuthenticationException">Thrown when our gir client fails to authorize</exception>
        private bool _refreshInjectorReleases()
        {
            // Make sure we're authorized on the GitHub client first 
            this._serviceLogger.WriteLog("PULLING IN ALL RELEASE VERSIONS NOW...", LogType.WarnLog);
            if (!this._authorizeGitClient())
                throw new AuthenticationException("Error! Failed to authorize GitHub client for the MEAT Inc Organization!");

            try
            {
                // Pull in the releases and return them out
                this._injectorReleases = this._gitUpdaterClient.Repository.Release.GetAll(this._serviceConfig.UpdaterOrgName, this._serviceConfig.UpdaterRepoName).Result.ToArray();
                this._serviceLogger.WriteLog($"PULLED IN A TOTAL OF {this._injectorReleases.Length} RELEASE OBJECTS OK! PARSING THEM FOR VERSION INFORMATION NOW...");

                // Parse out the version information and return them out
                this._serviceLogger.WriteLog("RELEASE TAGS LOCATED AND PROCESSED OK! SHOWING BELOW (IF TRACE LOGGING IS ON)", LogType.WarnLog);
                this._serviceLogger.WriteLog($"RELEASE TAGS BUILT: {string.Join(" | ", this.InjectorVersions)}", LogType.TraceLog);
                this._serviceLogger.WriteLog($"FOUND LATEST INJECTOR VERSION TO BE {this.LatestInjectorVersion}", LogType.InfoLog);

                // Return out based on how many releases are found
                return this._injectorReleases.Length != 0;
            }
            catch (Exception RefreshVersionsEx)
            {
                // Log out our exception and exit out false
                this._serviceLogger.WriteLog("ERROR! FAILED TO REFRESH INJECTOR VERSIONS!", LogType.ErrorLog);
                this._serviceLogger.WriteException("EXCEPTION THROWN DURING REFRESH ROUTINE IS BEING LOGGED BELOW", RefreshVersionsEx);
                return false;
            }
        }
    }
}