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

        // Private backing fields for our watchdog service configuration
        private static FulcrumUpdater _serviceInstance;        

        // Private backing fields for the Git helper, timer, and updater configuration
        private readonly Stopwatch _downloadTimer;                       // Download timer for pulling in versions
        private readonly GitHubClient _gitUpdaterClient;                 // The updater client for pulling in versions
        private readonly UpdaterServiceSettings _serviceConfig;          // Updater configuration values

        // Private backing fields to hold version information helpers
        private string _latestInjectorVersion;                           // Private backing field holding the latest injector version
        private string[] _injectorVersionsFound;                         // Private backing field holding all injector versions

        #endregion //Fields

        #region Properties

        // Public facing fields holding information about our latest version information
        public string LatestInjectorVersion
        {
            get => _latestInjectorVersion ?? this.RefreshInjectorVersions().FirstOrDefault();
            private set => _latestInjectorVersion = value;
        }
        public Release LatestInjectorRelease { get; private set; }
        public string LatestInjectorReleaseNotes => this.LatestInjectorRelease.Body;

        // Public facing properties holding all versions found on the server
        public string[] InjectorVersionsFound
        {
            get
            {
                // Check if a value is setup or not. If not find new ones and pull in result.
                if (this._injectorVersionsFound != null) return this._injectorVersionsFound;
                this._injectorVersionsFound = this.RefreshInjectorVersions();
                this._latestInjectorVersion = this._injectorVersionsFound.FirstOrDefault();

                // Return versions 
                return _injectorVersionsFound;
            }
            private set => _injectorVersionsFound = value;
        }

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
        internal FulcrumUpdater(UpdaterServiceSettings ServiceSettings = null)
        {
            // Build and register a new watchdog logging target here for a file and the console
            this.ServiceLoggingTarget = LocateServiceFileTarget<FulcrumUpdater>();
            this._serviceLogger.RegisterTarget(this.ServiceLoggingTarget);

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

            // Spawn a new timer for tracking download time and authorize a new GitHub helper
            this._downloadTimer = new Stopwatch();
            Credentials LoginCredentials = new Credentials(this._serviceConfig.UpdaterSecretKey);
            this._gitUpdaterClient = new GitHubClient(new ProductHeaderValue(this._serviceConfig.UpdaterUserName)) { Credentials = LoginCredentials };
            this._serviceLogger.WriteLog("BUILT NEW GIT CLIENT FOR UPDATING OK! AUTHENTICATING NOW USING MEAT INC BOT TOKEN ACCESS...", LogType.InfoLog);
        }
        /// <summary>
        /// Static CTOR for an update broker instance. Simply pulls out the new singleton instance for our update broker
        /// </summary>
        /// <param name="ForceInit">When true, we force rebuild the requested service instance</param>
        /// <returns>The instance for our service singleton</returns>
        public static Task<FulcrumUpdater> InitializeUpdaterService(bool ForceInit = false)
        {
            // Build a static init logger for the service here
            SharpLogger ServiceInitLogger =
                SharpLogBroker.FindLoggers("ServiceInitLogger").FirstOrDefault()
                ?? new SharpLogger(LoggerActions.UniversalLogger, "ServiceInitLogger");

            // Make sure we actually want to use this watchdog service 
            var ServiceConfig = ValueLoaders.GetConfigValue<UpdaterServiceSettings>("FulcrumServices.FulcrumUpdaterService");
            if (!ServiceConfig.ServiceEnabled) {
                ServiceInitLogger.WriteLog("WARNING! UPDATER SERVICE IS TURNED OFF IN OUR CONFIGURATION FILE! NOT BOOTING IT", LogType.WarnLog);
                return null;
            }

            // Spin up a new injector email service here if needed           
            ServiceInitLogger.WriteLog($"SPAWNING A NEW UPDATER SERVICE INSTANCE NOW...", LogType.WarnLog);
            return Task.Run(() =>
            {
                // Check if we need to force rebuilt this service or not
                if (_serviceInstance != null && !ForceInit) {
                    ServiceInitLogger.WriteLog("FOUND EXISTING UPDATER SERVICE INSTANCE! RETURNING IT NOW...");
                    return _serviceInstance;
                }

                // Build and boot a new service instance for our watchdog
                _serviceInstance = new FulcrumUpdater(ServiceConfig);
                _serviceInstance.OnStart(null);
                ServiceInitLogger.WriteLog("BOOTED NEW INJECTOR UPDATER SERVICE OK!", LogType.InfoLog);

                // Return the service instance here
                return _serviceInstance;
            });
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

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
                        this._serviceLogger.WriteLog($"                                FulcrumInjector Updater Service Command Help", LogType.InfoLog);
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
                this._serviceLogger.WriteLog($"ERROR! FAILED TO INVOKE A CUSTOM COMMAND ON AN EXISTING {this.GetType().Name} INSTANCE!", LogType.ErrorLog);
                this._serviceLogger.WriteException($"EXCEPTION THROWN FROM THE CUSTOM COMMAND ROUTINE IS LOGGED BELOW", SendCustomCommandEx);
            }
        }
       
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Updates the injector version information on the class instance.
        /// </summary>
        public string[] RefreshInjectorVersions()
        {
            // Parse out the version information and return them out
            var ReleaseTagsFound = this._acquireInjectorReleaseObjects().Select(ReleaseObj => ReleaseObj.TagName).ToArray();
            this._serviceLogger.WriteLog("RELEASE TAGS LOCATED AND PROCESSED OK! SHOWING BELOW (IF TRACE LOGGING IS ON)", LogType.WarnLog);
            this._serviceLogger.WriteLog($"RELEASE TAGS BUILT: {string.Join(" | ", ReleaseTagsFound)}", LogType.TraceLog);

            // Clean up tag names here to only contain version information
            this._serviceLogger.WriteLog("CLEANING UP RELEASE TAG VALUES NOW...", LogType.WarnLog);
            ReleaseTagsFound = ReleaseTagsFound
                .Select(ReleaseTag => Regex.Match(ReleaseTag, @"(\d+(?>\.|))+").Value)
                .ToArray();

            // Return the version tags here
            this.InjectorVersionsFound = ReleaseTagsFound;
            this.LatestInjectorVersion = ReleaseTagsFound[0];
            this._serviceLogger.WriteLog("RETURNING NEWEST INJECTOR VERSION INFORMATION NOW...", LogType.InfoLog);
            return ReleaseTagsFound;
        }
        /// <summary>
        /// Checks if a version is ready to be updated or not.
        /// </summary>
        /// <param name="InputVersion">Current Version</param>
        /// <returns>True if updates ready. False if not.</returns>
        public bool CheckAgainstVersion(string InputVersion)
        {
            // Validate that the versions exist to compare
            if (this.InjectorVersionsFound == null || this.LatestInjectorVersion == null)
            {
                // IF no versions are found, then refresh them all now
                this._serviceLogger.WriteLog("WARNING! INJECTOR VERSION INFORMATION WAS NOT POPULATED! UPDATING IT NOW...", LogType.WarnLog);
                this.RefreshInjectorVersions();
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
            // First find our version to use using our version/release lookup tool
            var ReleaseToUse = this._acquireInjectorReleaseObjects()
                .FirstOrDefault(ReleaseObj => ReleaseObj.TagName.Contains(VersionTag));
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
        /// Pulls in all release objects from the injector repo
        /// </summary>
        /// <returns>A readonly list of objects used to index releases</returns>
        private Release[] _acquireInjectorReleaseObjects()
        {
            // Pull in the releases and return them out
            this._serviceLogger.WriteLog("PULLING IN ALL RELEASE VERSIONS NOW...", LogType.WarnLog);
            var ReleasesFound = this._gitUpdaterClient.Repository.Release.GetAll(this._serviceConfig.UpdaterOrgName, this._serviceConfig.UpdaterRepoName).Result.ToArray();
            this._serviceLogger.WriteLog($"PULLED IN A TOTAL OF {ReleasesFound.Length} RELEASE OBJECTS OK! PARSING THEM FOR VERSION INFORMATION NOW...");

            // Store our latest release object on this class
            if (ReleasesFound?.Length != 0) this.LatestInjectorRelease = ReleasesFound.FirstOrDefault();
            return ReleasesFound;
        }
    }
}
