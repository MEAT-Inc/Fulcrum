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

        // Event for download progress
        public EventHandler<DownloadDataCompletedEventArgs> OnUpdaterComplete;
        public EventHandler<DownloadProgressChangedEventArgs> OnUpdaterProgress;

        #endregion //Custom Events

        #region Fields

        // Private backing fields for our updater service instance
        private static FulcrumUpdater _serviceInstance;           // Instance of our service object
        private static readonly object _serviceLock = new();      // Lock object for building service instances

        // Private backing fields for the Git helper, timer, and updater configuration
        private GitHubClient _gitUpdaterClient;                   // The updater client for pulling in versions
        private readonly Stopwatch _downloadTimer = new();        // Download timer for pulling in versions
        private readonly UpdaterServiceSettings _serviceConfig;   // Updater configuration values

        #endregion //Fields

        #region Properties

        // Public facing property holding our authorization state for the updater 
        public bool IsGitClientAuthorized { get; private set; }

        // Public facing properties holding information about our latest version information
        public Release[] InjectorReleases { get; private set; }
        public string LatestInjectorVersion => this.InjectorVersions[0];
        public Release LatestInjectorRelease => this.InjectorReleases[0];
        public string LatestInjectorReleaseNotes => this.LatestInjectorRelease.Body;
        public string[] InjectorVersions => this.InjectorReleases
            .Select(ReleaseTag => Regex.Match(ReleaseTag.TagName, @"(\d+(?>\.|))+").Value)
            .ToArray();

        // Time download elapsed and approximate time remaining
        public string DownloadTimeRemaining { get; private set; }
        public string DownloadTimeElapsed => this._downloadTimer == null ? "00:00" : this._downloadTimer.Elapsed.ToString().Split('.')[0];

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
            // Log we're building this new service and log out the name we located for it
            this._downloadTimer = new Stopwatch();
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
        /// Updates the injector version information on the class instance.
        /// </summary>
        public bool RefreshInjectorReleases()
        {
            // Make sure we're authorized on the GitHub client first 
            this._serviceLogger.WriteLog("PULLING IN ALL RELEASE VERSIONS NOW...", LogType.WarnLog);
            if (!this._authorizeGitClient())
                throw new AuthenticationException("Error! Failed to authorize GitHub client for the MEAT Inc Organization!");

            // Pull in the releases and return them out
            this.InjectorReleases = this._gitUpdaterClient.Repository.Release.GetAll(this._serviceConfig.UpdaterOrgName, this._serviceConfig.UpdaterRepoName).Result.ToArray();
            this._serviceLogger.WriteLog($"PULLED IN A TOTAL OF {this.InjectorReleases.Length} RELEASE OBJECTS OK! PARSING THEM FOR VERSION INFORMATION NOW...");

            // Parse out the version information and return them out
            this._serviceLogger.WriteLog("RELEASE TAGS LOCATED AND PROCESSED OK! SHOWING BELOW (IF TRACE LOGGING IS ON)", LogType.WarnLog);
            this._serviceLogger.WriteLog($"RELEASE TAGS BUILT: {string.Join(" | ", this.InjectorVersions)}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"FOUND LATEST INJECTOR VERSION TO BE {this.LatestInjectorVersion}", LogType.InfoLog);
            return this.InjectorReleases.Length != 0;
        }
        /// <summary>
        /// Checks if a version is ready to be updated or not.
        /// </summary>
        /// <param name="InputVersion">Current Version</param>
        /// <returns>True if updates ready. False if not.</returns>
        public bool CheckAgainstVersion(string InputVersion)
        {
            // Validate that the versions exist to compare
            if (this.InjectorReleases == null) 
            {
                // IF no versions are found, then refresh them all now
                this._serviceLogger.WriteLog("WARNING! INJECTOR VERSION INFORMATION WAS NOT POPULATED! UPDATING IT NOW...", LogType.WarnLog);
                this.RefreshInjectorReleases();
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
        /// Downloads a new release of the injector application and saves it
        /// </summary>
        /// <param name="VersionTag">The tag of the version to download</param>
        /// <param name="InjectorAssetUrl">The URL of the installer being pulled in</param>
        /// <returns>The path of our output msi file for the injector application</returns>
        public string DownloadInjectorRelease(string VersionTag, out string InjectorAssetUrl)
        {
            // Validate that the versions exist to compare
            if (this.InjectorReleases == null)
            {
                // IF no versions are found, then refresh them all now
                this._serviceLogger.WriteLog("WARNING! INJECTOR VERSION INFORMATION WAS NOT POPULATED! UPDATING IT NOW...", LogType.WarnLog);
                this.RefreshInjectorReleases();
            }

            // First find our version to use using our version/release lookup tool
            var ReleaseToUse = this.InjectorReleases.FirstOrDefault(ReleaseObj => ReleaseObj.TagName.Contains(VersionTag));
            this._serviceLogger.WriteLog("PULLED IN A NEW RELEASE OBJECT TO UPDATE WITH!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"RELEASE TAG: {ReleaseToUse.TagName}");

            // Now get the asset url and download it here into a temp file
            InjectorAssetUrl = ReleaseToUse.Assets.FirstOrDefault(AssetObj => AssetObj.BrowserDownloadUrl.EndsWith("msi")).BrowserDownloadUrl;
            string InjectorAssetPath = Path.ChangeExtension(Path.GetTempFileName(), "msi");
            this._serviceLogger.WriteLog($"RELEASE ASSET FOUND! URL IS: {InjectorAssetUrl}");
            this._serviceLogger.WriteLog($"TEMP PATH FOR ASSET BUILT:   {InjectorAssetPath}");

            // Return the URL of the path to download here
            WebClient AssetDownloadHelper = new WebClient();
            AssetDownloadHelper.DownloadProgressChanged += (Sender, Args) =>
            {
                // Invoke the event for progress changed if it's not null
                if (this.OnUpdaterProgress == null) return;
                this.OnUpdaterProgress?.Invoke(Sender ?? this, Args);

                // Find our approximate time left
                var ApproximateMillisLeft = this._downloadTimer.ElapsedMilliseconds * Args.TotalBytesToReceive / Args.BytesReceived;
                TimeSpan ApproximateToSpan = TimeSpan.FromMilliseconds(ApproximateMillisLeft);
                this.DownloadTimeRemaining = ApproximateToSpan.ToString("mm:ss");
            };
            AssetDownloadHelper.DownloadDataCompleted += (Sender, Args) =>
            {
                // Invoke the event for progress done if it's not null
                if (this.OnUpdaterComplete == null) return;
                this.OnUpdaterComplete.Invoke(Sender ?? this, Args);
            };

            // Log done building setup and download the version output here
            this._downloadTimer.Start();
            this._serviceLogger.WriteLog("BUILT NEW WEB CLIENT FOR DOWNLOADING ASSETS OK! STARTING DOWNLOAD NOW...", LogType.InfoLog);
            AssetDownloadHelper.DownloadFile(InjectorAssetUrl, InjectorAssetPath); this._downloadTimer.Stop();
            this._serviceLogger.WriteLog($"TOTAL DOWNLOAD TIME TAKEN: {this.DownloadTimeElapsed}");

            // Return the path of our new asset
            this._serviceLogger.WriteLog("DOWNLOADED UPDATES OK! RETURNING OUTPUT PATH FOR ASSETS NOW...");
            return InjectorAssetPath;
        }

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
    }
}