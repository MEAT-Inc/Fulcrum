using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using Octokit;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;


namespace FulcrumInjector.FulcrumLogic.FulcrumUpdater
{
    /// <summary>
    /// Class which houses the logic for pulling in a new Fulcrum Injector MSI File.
    /// </summary>
    public class InjectorUpdater
    {
        // Logger object.
        private SubServiceLogger _injectorUpdateLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorUpdateLogger")) ?? new SubServiceLogger("InjectorUpdateLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Git Client object
        private readonly GitHubClient UpdaterClient;

        // Updater configuration values
        private readonly string UpdaterUserName;
        private readonly string UpdaterSecretKey;
        private readonly string UpdaterRepoName;
        private readonly string UpdaterOrgName;

        // Private version configuration helpers
        private string _latestInjectorVersion;
        private string[] _injectorVersionsFound;

        // Version information found
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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new injector update helper object which pulls our GitHub release information
        /// </summary>
        public InjectorUpdater()
        {
            // Store values for updater configuration variables here
            this.UpdaterUserName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorUpdates.UpdaterUserName");
            this.UpdaterSecretKey = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorUpdates.UpdaterSecretKey");
            this.UpdaterRepoName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorUpdates.UpdaterRepoName");
            this.UpdaterOrgName = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorUpdates.UpdaterOrgName");
            this._injectorUpdateLogger.WriteLog("PULLED IN OUR CONFIGURATIONS FOR INJECTOR UPDATER API CALLS OK!", LogType.InfoLog);
        
            // Configure updater here
            Credentials LoginCredentials = new Credentials(this.UpdaterSecretKey);
            this.UpdaterClient = new GitHubClient(new ProductHeaderValue(this.UpdaterUserName)) { Credentials = LoginCredentials };
            this._injectorUpdateLogger.WriteLog("BUILT NEW GIT CLIENT FOR UPDATING OK! AUTHENTICATING NOW USING MEAT INC BOT TOKEN ACCESS...", LogType.InfoLog);
        }

        /// <summary>
        /// Updates the injector version information on the class instance.
        /// </summary>
        public string[] RefreshInjectorVersions()
        {
            // Pull in all the releases here
            this._injectorUpdateLogger.WriteLog("PULLING IN ALL RELEASE VERSIONS NOW...", LogType.WarnLog);
            var ReleasesFound = this.UpdaterClient.Repository.Release.GetAll(this.UpdaterOrgName, this.UpdaterRepoName).Result;
            this._injectorUpdateLogger.WriteLog($"PULLED IN A TOTAL OF {ReleasesFound.Count} RELEASE OBJECTS OK! PARSING THEM FOR VERSION INFORMATION NOW...");

            // Parse out the version information and return them out
            var ReleaseTagsFound = ReleasesFound.Select(ReleaseObj => ReleaseObj.TagName).ToArray();
            this._injectorUpdateLogger.WriteLog("RELEASE TAGS LOCATED AND PROCESSED OK! SHOWING BELOW (IF TRACE LOGGING IS ON)", LogType.WarnLog);
            this._injectorUpdateLogger.WriteLog($"RELEASE TAGS BUILT: {string.Join("\n\t", ReleaseTagsFound)}", LogType.TraceLog);

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
            // TODO: WRITE LOGIC FOR CHECKING FOR UPDATES!
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
        public string DownloadInjectorRelease(string ReleaseVersion)
        {
            // TODO: BUILD LOGIC FOR DOWNLOADING NEW INJECTOR VERSIONS!
            return null;
        }
    }
}