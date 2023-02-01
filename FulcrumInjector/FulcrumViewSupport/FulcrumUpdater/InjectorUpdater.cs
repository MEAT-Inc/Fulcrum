using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonHelpers;
using Octokit;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumUpdater
{
    /// <summary>
    /// Class which houses the logic for pulling in a new Fulcrum Injector MSI File.
    /// </summary>
    public class InjectorUpdater
    {
        // Logger object.
        private SubServiceLogger _injectorUpdateLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("InjectorUpdateLogger", LoggerActions.SubServiceLogger);

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Git Client object
        private readonly GitHubClient UpdaterClient;
        private readonly Stopwatch DownloadTimer;

        // Updater configuration values
        private readonly string UpdaterUserName;
        private readonly string UpdaterSecretKey;
        private readonly string UpdaterRepoName;
        private readonly string UpdaterOrgName;

        // Private version configuration helpers
        private string _latestInjectorVersion;
        private string[] _injectorVersionsFound;

        // Version information found
        public Release LatestInjectorRelease { get; private set; }
        public string LatestInjectorReleaseNotes => this.LatestInjectorRelease.Body;
        public string LatestInjectorVersion
        {
            get => _latestInjectorVersion ?? this.RefreshInjectorVersions().FirstOrDefault();
            private set => _latestInjectorVersion = value;
        }
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
        public string DownloadTimeElapsed => this.DownloadTimer == null ? "00:00" : this.DownloadTimer.Elapsed.ToString().Split('.')[0];
        public string DownloadTimeRemaining { get; private set; }

        // Event for download progress
        public Action<DownloadProgressChangedEventArgs> UpdateDownloadProgressAction;
        public Action<DownloadDataCompletedEventArgs> DownloadCompleteProgressAction;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new injector update helper object which pulls our GitHub release information
        /// </summary>
        public InjectorUpdater()
        {
            // Store values for updater configuration variables here
            this.UpdaterUserName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorUpdates.UpdaterUserName");
            this.UpdaterRepoName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorUpdates.UpdaterRepoName");
            this.UpdaterOrgName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorUpdates.UpdaterOrgName");

            // Pull in our API key here and decode it.
            byte[] KeyBytes = Convert.FromBase64String(ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorUpdates.UpdaterSecretKey"));
            this.UpdaterSecretKey = Encoding.UTF8.GetString(KeyBytes);

            // Log pulled in all values ok and continue on
            this._injectorUpdateLogger.WriteLog("PULLED IN OUR CONFIGURATIONS FOR INJECTOR UPDATER API CALLS OK!", LogType.InfoLog);

            // Configure updater here
            this.DownloadTimer = new Stopwatch();
            Credentials LoginCredentials = new Credentials(this.UpdaterSecretKey);
            this.UpdaterClient = new GitHubClient(new ProductHeaderValue(this.UpdaterUserName)) { Credentials = LoginCredentials };
            this._injectorUpdateLogger.WriteLog("BUILT NEW GIT CLIENT FOR UPDATING OK! AUTHENTICATING NOW USING MEAT INC BOT TOKEN ACCESS...", LogType.InfoLog);
        }

        /// <summary>
        /// Pulls in all release objects from the injector repo
        /// </summary>
        /// <returns>A readonly list of objects used to index releases</returns>
        private Release[] _acquireInjectorReleaseObjects()
        {
            // Pull in the releases and return them out
            this._injectorUpdateLogger.WriteLog("PULLING IN ALL RELEASE VERSIONS NOW...", LogType.WarnLog);
            var ReleasesFound = this.UpdaterClient.Repository.Release.GetAll(this.UpdaterOrgName, this.UpdaterRepoName).Result.ToArray();
            this._injectorUpdateLogger.WriteLog($"PULLED IN A TOTAL OF {ReleasesFound.Length} RELEASE OBJECTS OK! PARSING THEM FOR VERSION INFORMATION NOW...");

            // Store our latest release object on this class
            if (ReleasesFound?.Length != 0) 
                this.LatestInjectorRelease = ReleasesFound.FirstOrDefault();

            // Return the releases
            return ReleasesFound;
        }


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
            if (this.InjectorVersionsFound == null || this.LatestInjectorVersion == null) {
                this.RefreshInjectorVersions();
                this._injectorUpdateLogger.WriteLog("WARNING! INJECTOR VERSION INFORMATION WAS NOT POPULATED! UPDATING IT NOW...", LogType.WarnLog);
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
                if (this.UpdateDownloadProgressAction == null) return;
                this.UpdateDownloadProgressAction.Invoke(Args);

                // Find our approximate time left
                var ApproximateMillisLeft = this.DownloadTimer.ElapsedMilliseconds * Args.TotalBytesToReceive / Args.BytesReceived;
                TimeSpan ApproximateToSpan = TimeSpan.FromMilliseconds(ApproximateMillisLeft);
                this.DownloadTimeRemaining = ApproximateToSpan.ToString("mm:ss");
            };
            AssetDownloadHelper.DownloadDataCompleted += (Sender, Args) =>
            {
                // Invoke the event for progress done if it's not null
                if (this.DownloadCompleteProgressAction == null) return;
                this.DownloadCompleteProgressAction.Invoke(Args);
            };

            // Log done building setup and download the version output here
            this.DownloadTimer.Start(); 
            this._injectorUpdateLogger.WriteLog("BUILT NEW WEB CLIENT FOR DOWNLOADING ASSETS OK! STARTING DOWNLOAD NOW...", LogType.InfoLog);
            AssetDownloadHelper.DownloadFile(InjectorAssetUrl, InjectorAssetPath); this.DownloadTimer.Stop();
            this._injectorUpdateLogger.WriteLog($"TOTAL DOWNLOAD TIME TAKEN: {this.DownloadTimeElapsed}");

            // Return the path of our new asset
            this._injectorUpdateLogger.WriteLog("DOWNLOADED UPDATES OK! RETURNING OUTPUT PATH FOR ASSETS NOW...");
            return InjectorAssetPath;
        }
    }
}