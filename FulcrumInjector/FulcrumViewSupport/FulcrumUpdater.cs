using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using Newtonsoft.Json;
using Octokit;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Class which houses the logic for pulling in a new Fulcrum Injector MSI File.
    /// </summary>
    public class FulcrumUpdater
    {
        #region Custom Events

        // Event for download progress
        public EventHandler<DownloadDataCompletedEventArgs> OnUpdaterComplete;
        public EventHandler<DownloadProgressChangedEventArgs> OnUpdaterProgress;

        #endregion //Custom Events

        #region Fields

        // Logger object for this update helper instance 
        private readonly SharpLogger _injectorUpdateLogger;

        // Private backing fields for the Git helper, timer, and updater configuration
        private readonly Stopwatch _downloadTimer;
        private readonly GitHubClient _gitUpdaterClient;
        private readonly FulcrumUpdaterConfiguration _updaterConfiguration;

        // Private backing fields to hold version information helpers
        private string _latestInjectorVersion;
        private string[] _injectorVersionsFound;

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

        /// <summary>
        /// Private class instance used to hold our injector configuration values for updates
        /// </summary>
        [JsonConverter(typeof(UpdaterConfigJsonConverter))]
        public class FulcrumUpdaterConfiguration
        {
            public bool ForceUpdateReady { get; set; }
            public string UpdaterOrgName { get; set; }
            public string UpdaterRepoName { get; set; }
            public string UpdaterUserName { get; set; }
            public string UpdaterSecretKey { get; set; }
        }


        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new injector update helper object which pulls our GitHub release information
        /// </summary>
        public FulcrumUpdater()
        {
            // Construct a new logger instance and build a new configuration for the updater
            this._injectorUpdateLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this._updaterConfiguration = ValueLoaders.GetConfigValue<FulcrumUpdaterConfiguration>("FulcrumConstants.InjectorUpdates");
            this._injectorUpdateLogger.WriteLog("PULLED IN OUR CONFIGURATIONS FOR INJECTOR UPDATER API CALLS OK!", LogType.InfoLog);

            // Configure updater here
            this._downloadTimer = new Stopwatch();
            Credentials LoginCredentials = new Credentials(this._updaterConfiguration.UpdaterSecretKey);
            this._gitUpdaterClient = new GitHubClient(new ProductHeaderValue(this._updaterConfiguration.UpdaterUserName)) { Credentials = LoginCredentials };
            this._injectorUpdateLogger.WriteLog("BUILT NEW GIT CLIENT FOR UPDATING OK! AUTHENTICATING NOW USING MEAT INC BOT TOKEN ACCESS...", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Updates the injector version information on the class instance.
        /// </summary>
        public string[] RefreshInjectorVersions()
        {
            // Parse out the version information and return them out
            var ReleaseTagsFound = this._acquireInjectorReleaseObjects().Select(ReleaseObj => ReleaseObj.TagName).ToArray();
            this._injectorUpdateLogger.WriteLog("RELEASE TAGS LOCATED AND PROCESSED OK! SHOWING BELOW (IF TRACE LOGGING IS ON)", LogType.WarnLog);
            this._injectorUpdateLogger.WriteLog($"RELEASE TAGS BUILT: {string.Join(" | ", ReleaseTagsFound)}", LogType.TraceLog);

            // Clean up tag names here to only contain version information
            this._injectorUpdateLogger.WriteLog("CLEANING UP RELEASE TAG VALUES NOW...", LogType.WarnLog);
            ReleaseTagsFound = ReleaseTagsFound
                .Select(ReleaseTag => Regex.Match(ReleaseTag, @"(\d+(?>\.|))+").Value)
                .ToArray();

            // Return the version tags here
            this.InjectorVersionsFound = ReleaseTagsFound;
            this.LatestInjectorVersion = ReleaseTagsFound[0];
            this._injectorUpdateLogger.WriteLog("RETURNING NEWEST INJECTOR VERSION INFORMATION NOW...", LogType.InfoLog);
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
                this._injectorUpdateLogger.WriteLog("WARNING! INJECTOR VERSION INFORMATION WAS NOT POPULATED! UPDATING IT NOW...", LogType.WarnLog);
                this.RefreshInjectorVersions();
            }

            // Now compare the versions
            Version InputVersionParsed = Version.Parse(Regex.Match(InputVersion, @"(\d+(?>\.|))+").Value);
            Version LatestVersionParsed = Version.Parse(Regex.Match(this.LatestInjectorVersion, @"(\d+(?>\.|))+").Value);
            this._injectorUpdateLogger.WriteLog("PARSED VERSIONS CORRECTLY! READY TO COMPARE AND RETURN", LogType.InfoLog);

            // Compare and log result
            bool NeedsUpdate = InputVersionParsed < LatestVersionParsed;
            this._injectorUpdateLogger.WriteLog($"RESULT FROM VERSION COMPARISON: {NeedsUpdate}", LogType.WarnLog);
            this._injectorUpdateLogger.WriteLog("UPDATE CHECK PASSED! PLEASE EXECUTE ACCORDINGLY...", LogType.InfoLog);
            return NeedsUpdate;
        }
        /// <summary>
        /// Downloads a new release of the injector application and saves it
        /// </summary>
        /// <param name="ReleaseVersion">Version to download</param>
        /// <returns>The path of our output msi file for the injector application</returns>
        public string DownloadInjectorRelease(string VersionTag, out string InjectorAssetUrl)
        {
            // First find our version to use using our version/release lookup tool
            var ReleaseToUse = this._acquireInjectorReleaseObjects()
                .FirstOrDefault(ReleaseObj => ReleaseObj.TagName.Contains(VersionTag));
            this._injectorUpdateLogger.WriteLog("PULLED IN A NEW RELEASE OBJECT TO UPDATE WITH!", LogType.InfoLog);
            this._injectorUpdateLogger.WriteLog($"RELEASE TAG: {ReleaseToUse.TagName}");

            // Now get the asset url and download it here into a temp file
            InjectorAssetUrl = ReleaseToUse.Assets.FirstOrDefault(AssetObj => AssetObj.BrowserDownloadUrl.EndsWith("msi")).BrowserDownloadUrl;
            string InjectorAssetPath = Path.ChangeExtension(Path.GetTempFileName(), "msi");
            this._injectorUpdateLogger.WriteLog($"RELEASE ASSET FOUND! URL IS: {InjectorAssetUrl}");
            this._injectorUpdateLogger.WriteLog($"TEMP PATH FOR ASSET BUILT:   {InjectorAssetPath}");

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
            this._injectorUpdateLogger.WriteLog("BUILT NEW WEB CLIENT FOR DOWNLOADING ASSETS OK! STARTING DOWNLOAD NOW...", LogType.InfoLog);
            AssetDownloadHelper.DownloadFile(InjectorAssetUrl, InjectorAssetPath); this._downloadTimer.Stop();
            this._injectorUpdateLogger.WriteLog($"TOTAL DOWNLOAD TIME TAKEN: {this.DownloadTimeElapsed}");

            // Return the path of our new asset
            this._injectorUpdateLogger.WriteLog("DOWNLOADED UPDATES OK! RETURNING OUTPUT PATH FOR ASSETS NOW...");
            return InjectorAssetPath;
        }

        /// <summary>
        /// Pulls in all release objects from the injector repo
        /// </summary>
        /// <returns>A readonly list of objects used to index releases</returns>
        private Release[] _acquireInjectorReleaseObjects()
        {
            // Pull in the releases and return them out
            this._injectorUpdateLogger.WriteLog("PULLING IN ALL RELEASE VERSIONS NOW...", LogType.WarnLog);
            var ReleasesFound = this._gitUpdaterClient.Repository.Release.GetAll(this._updaterConfiguration.UpdaterOrgName, this._updaterConfiguration.UpdaterRepoName).Result.ToArray();
            this._injectorUpdateLogger.WriteLog($"PULLED IN A TOTAL OF {ReleasesFound.Length} RELEASE OBJECTS OK! PARSING THEM FOR VERSION INFORMATION NOW...");

            // Store our latest release object on this class
            if (ReleasesFound?.Length != 0) this.LatestInjectorRelease = ReleasesFound.FirstOrDefault(); 
            return ReleasesFound;
        }
    }
}