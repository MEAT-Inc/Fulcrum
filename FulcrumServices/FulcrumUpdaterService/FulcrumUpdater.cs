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
using System.Threading;
using System.Threading.Tasks;
using FulcrumJson;
using FulcrumService;
using FulcrumSupport;
using FulcrumUpdaterService.UpdaterServiceModels;
using Newtonsoft.Json;
using Octokit;
using SharpLogging;
using System.Management;

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

        // Private backing fields for updater background tasks
        private Task _refreshTask;                                // Task holding our refresh operation
        private TimeSpan _refreshTime;                            // Timespan to delay for refresh routines
        private CancellationToken _refreshTaskToken;              // The cancellation token for refreshing
        private CancellationTokenSource _refreshTokenSource;      // The token source for refreshing

        // Private backing fields for our public facing properties
        private bool _isGitClientAuthorized;                      // Private backing bool value to store if we're authorized or not
        private Release[] _injectorReleases;                      // Collection of all releases found for the injector app

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

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Internal enumeration that's used to determine the type of command being invoked onto the updater service
        /// </summary>
        internal enum CustomCommands
        {
            // Different updater service command actions
            [Description("Report Version Information")] REPORT_INJECTOR_VERSIONS = 128,
            [Description("Download Latest Version")] DOWNLOAD_LATEST_VERSION = 129,
            [Description("Install Latest Version")] INSTALL_LATEST_VERSION = 130,
        }

        /// <summary>
        /// Class object used to hold information about a given release
        /// </summary>
        public class ReleaseInformation
        {
            #region Custom Events
            #endregion // Custom Events

            #region Fields
            #endregion // Fields

            #region Properties

            // Public facing properties holding information about our release information
            public string VersionTag { get; internal set; }
            public string ReleaseNotes { get; internal set; }
            public string InstallerUrl { get; internal set; }

            #endregion // Properties

            #region Structs and Classes
            #endregion // Structs and Classes

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Internal CTOR for an release information object.
            /// Stores tag value for the release in question
            /// </summary>
            /// <param name="TagValue">The version tag we're looking up</param>
            internal ReleaseInformation(string TagValue) { this.VersionTag = TagValue; }
        }

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
            this._serviceLogger.WriteLog($"INCLUDING PRE-RELEASES: {(this._serviceConfig.IncludePreReleases ? "YES" : "NO")}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"BACKGROUND REFRESH TIME (SECONDS): {this._serviceConfig.RefreshTimerDelay.TotalSeconds}", LogType.TraceLog);
            this._serviceLogger.WriteLog($"AUTOMATIC UPDATES ENABLED: {(this._serviceConfig.EnableAutomaticUpdates ? "ENABLED" : "DISABLED")}", LogType.TraceLog);

            // Configure our background updater tasks if needed here and exit out once complete
            if (!this._serviceConfig.EnableAutomaticUpdates) return;
            
            // Build a new background refresh task to pull in new injector versions
            DateTime LastUpdateCheckTime = DateTime.Now;
            this._refreshTokenSource = new CancellationTokenSource();
            this._refreshTaskToken = this._refreshTokenSource.Token;
            this._serviceLogger.WriteLog("BOOTING NEW BACKGROUND UPDATER TASK INSTANCE NOW...", LogType.WarnLog);
            this._serviceLogger.WriteLog($"BUILT TASK CONTROL OBJECTS AND STORED START TIME OF {LastUpdateCheckTime:F}", LogType.InfoLog);

            // Build the task instance here
            this._refreshTask = Task.Run(() =>
            {
                // Setup our timespan to wait between updates here
                while (!this._refreshTaskToken.IsCancellationRequested)
                {
                    // Log out that we're waiting for our given timespan before checking for updates
                    this._serviceLogger.WriteLog($"WAITING FOR {this._serviceConfig.RefreshTimerDelay:g} BEFORE CHECKING FOR UPDATES...", LogType.WarnLog);

                    // Wait for our given amount of time here
                    Thread.Sleep(this._serviceConfig.RefreshTimerDelay);
                    this._serviceLogger.WriteLog("CHECKING FOR UPDATES NOW...", LogType.InfoLog);
                    this._serviceLogger.WriteLog($"LAST UPDATE CHECK PERFORMED AT {LastUpdateCheckTime:D}", LogType.InfoLog);
                    LastUpdateCheckTime = DateTime.Now;

                    // Check for an update for the injector app based on the currently installed version of it
                    this._getInjectorRelease(null, true);
                    string CurrentVersion = RegistryControl.InjectorVersion.ToString();
                    if (!this.CheckForUpdate(CurrentVersion, out string LatestVersion))
                    {
                        // Log out that no update is ready and continue on
                        this._serviceLogger.WriteLog("NO UPDATE READY AT THIS TIME!", LogType.InfoLog);
                        this._serviceLogger.WriteLog($"INJECTOR VERSION {CurrentVersion} IS STILL THE LATEST RELEASE!", LogType.InfoLog);
                        continue;
                    }

                    // Log out some information about our version that is ready to be installed
                    this._serviceLogger.WriteLog("NEW VERSION OF THE INJECTOR IS READY TO BE INSTALLED!", LogType.InfoLog);
                    this._serviceLogger.WriteLog($"VERSION {LatestVersion} IS READY FOR INSTALL NOW!", LogType.InfoLog);

                    try
                    {
                        // Download our release version installer and install it here
                        if (!this.DownloadInjectorInstaller(LatestVersion, out string InstallerPath))
                            throw new InvalidOperationException($"Error! Failed to download installer for version {LatestVersion}!");
                        this._serviceLogger.WriteLog($"DOWNLOADED VERSION {LatestVersion} CORRECTLY! INSTALLING IT NOW...", LogType.InfoLog);

                        // Check if the injector application is open at this time or not.
                        Process InjectorProcess = Process.GetProcesses("FulcrumInjector").FirstOrDefault();
                        if (InjectorProcess == null) this._serviceLogger.WriteLog($"DOWNLOADING AND INSTALLING VERSION {LatestVersion} NOW...", LogType.WarnLog);
                        else
                        {
                            // Since we don't want to kill the app if it's running, wait and move on if the injector app is open
                            this._serviceLogger.WriteLog("WARNING! INJECTOR APPLICATION IS CURRENTLY OPEN! NOT KILLING!", LogType.WarnLog);
                            this._serviceLogger.WriteLog("WAITING UNTIL THE INJECTOR APPLICATION HAS BEEN CLOSED DOWN BEFORE TRYING TO INSTALL!", LogType.WarnLog);
                            if (!InjectorProcess.WaitForExit(3600000)) 
                            {
                                // If the app stays open for an hour, just move onto our next update check and install it then.
                                this._serviceLogger.WriteLog("INJECTOR APP SAT OPEN FOR AN HOUR! WAITING UNTIL NEXT UPDATE CHECK ROUTINE TO INSTALL!", LogType.ErrorLog);
                                continue;
                            }
                        }

                        // Now invoke the installer to update our injector installation
                        if (!this.InstallInjectorApplication(InstallerPath))
                            throw new InvalidOperationException($"Error! Failed to invoke a new install routine for version {LatestVersion}!");
                    }
                    catch (Exception InstallUpdateEx)
                    {
                        // Catch our installer exception and exit out once done
                        this._serviceLogger.WriteLog("ERROR! FAILED TO DOWNLOAD OR RUN INSTALLER!", LogType.ErrorLog);
                        this._serviceLogger.WriteException("EXCEPTION DURING UPDATE ROUTINE IS BEING LOGGED BELOW", InstallUpdateEx);
                    }
                }
            }, this._refreshTaskToken);
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
        /// <summary>
        /// Override method helper for running custom commands on our Updater Service
        /// </summary>
        /// <param name="CommandNumber">The number of the command being run on the updater service</param>
        protected override void OnCustomCommand(int CommandNumber)
        {
            // Validate our command number request here
            this._serviceLogger.WriteLog($"ATTEMPTING TO INVOKE SERVICE COMMAND {CommandNumber} NOW...", LogType.WarnLog);
            if (CommandNumber is < 128 or > 130)
            {
                // Log out our service command is invalid and print our supported command types
                this._serviceLogger.WriteLog($"ERROR! COMMAND NUMBER {CommandNumber} IS NOT SUPPORTED!", LogType.ErrorLog);
                this._serviceLogger.WriteLog("SUPPORTED COMMAND NUMBERS ARE AS FOLLOWS:", LogType.InfoLog);
                this._serviceLogger.WriteLog("\t128 --> Reports injector installed versions", LogType.InfoLog);
                this._serviceLogger.WriteLog("\t129 --> Downloads the latest injector version", LogType.InfoLog);
                this._serviceLogger.WriteLog("\t130 --> Installs the latest injector version", LogType.InfoLog); 
                return;
            }

            // Convert our command number into our enumeration type and execute it here
            CustomCommands CommandType = (CustomCommands)CommandNumber;
            this._serviceLogger.WriteLog($"PARSED COMMAND TYPE AS: {CommandType.ToDescriptionString()}", LogType.InfoLog);
            switch (CommandType)
            {
                // Case for reporting installed injector version information
                case CustomCommands.REPORT_INJECTOR_VERSIONS:

                    // Log out what we're executing and return out once complete
                    this._serviceLogger.WriteLog("PULLING INJECTOR VERSIONS AND REPORTING THEM NOW...", LogType.WarnLog);
                    this._serviceLogger.WriteLog("INJECTOR VERSIONS ARE BEING REPORTED BELOW", LogType.InfoLog);

                    // Write out our version information here
                    string[] VersionStrings = FulcrumVersionInfo.ToVersionTable().Split('\n');
                    foreach (string VersionString in VersionStrings) this._serviceLogger.WriteLog(VersionString); 
                    return;

                // Case for downloading the latest injector version installer
                case CustomCommands.INSTALL_LATEST_VERSION:
                case CustomCommands.DOWNLOAD_LATEST_VERSION:
                    
                    // Log out what we're executing and return out once complete
                    this._serviceLogger.WriteLog("DOWNLOADING LATEST INJECTOR VERSION TO A TEMP FILE NOW...", LogType.WarnLog);
                    if (CommandType == CustomCommands.DOWNLOAD_LATEST_VERSION) 
                        this._serviceLogger.WriteLog("TO INSTALL THIS FILE, PLEASE MANUALLY RUN THE INSTALLER FILE DOWNLOADED!", LogType.WarnLog);

                    // Find our latest version number here and invoke a download for it
                    this._serviceLogger.WriteLog("FINDING LATEST INJECTOR VERSION NOW...");
                    Release LatestRelease = this._getInjectorRelease(null, true);
                    this._serviceLogger.WriteLog("LATEST INJECTOR VERSION FOUND!", LogType.InfoLog);
                    this._serviceLogger.WriteLog($"LATEST VERSION: {LatestRelease.TagName}", LogType.InfoLog);

                    // Download our latest installer file
                    this._serviceLogger.WriteLog($"DOWNLOADING RELEASE VERSION {LatestRelease.Name} NOW..."); 
                    if (!this.DownloadInjectorInstaller(LatestRelease.TagName, out string DownloadedInstallerPath))
                    {
                        // Log out there was some kind of failure and exit out
                        this._serviceLogger.WriteLog($"ERROR! FAILED TO DOWNLOAD INSTALLER VERSION {LatestRelease.TagName}!", LogType.ErrorLog);
                        this._serviceLogger.WriteLog("PLEASE REFER TO THIS LOG FILE FOR A DOWNLOAD EXCEPTION STACK TRACE!", LogType.ErrorLog);
                        return;
                    }

                    // Check if we're installing this file nor not now
                    this._serviceLogger.WriteLog($"INSTALLER FILE FOR VERSION {LatestRelease.TagName} WAS DOWNLOADED CORRECTLY!", LogType.InfoLog);
                    this._serviceLogger.WriteLog($"DOWNLOADED INSTALLER TO: {DownloadedInstallerPath} CORRECTLY!", LogType.InfoLog);
                    if (CommandType == CustomCommands.DOWNLOAD_LATEST_VERSION) return;

                    // Invoke the install routine here if needed now
                    this._serviceLogger.WriteLog($"INSTALLING INJECTOR RELEASE {LatestRelease.TagName} NOW...");
                    this._serviceLogger.WriteLog("HOPEFULLY THIS WORKS CORRECTLY!", LogType.WarnLog);
                    if (this.InstallInjectorApplication(DownloadedInstallerPath)) return;

                    // Log out there was some kind of issue booting the new injector installer here
                    this._serviceLogger.WriteLog($"ERROR! FAILED TO INVOKE UPDATE ROUTINE FOR VERSION {LatestRelease.TagName}!", LogType.ErrorLog);
                    this._serviceLogger.WriteLog("PLEASE REFER TO THIS LOG FILE FOR A DOWNLOAD EXCEPTION STACK TRACE!", LogType.ErrorLog);
                    return;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Public method used to pull in information about a given release
        /// </summary>
        /// <param name="VersionTag">The version we're gathering information for</param>
        /// <returns>A newly built release information object holding properties of our release version</returns>
        public ReleaseInformation GetReleaseInformation(string VersionTag)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(GetReleaseInformation), VersionTag);
                return JsonConvert.DeserializeObject<ReleaseInformation>(PipeAction.PipeCommandResult.ToString());
            }

            // Build a new release information object and store the properties of it here
            this._serviceLogger.WriteLog($"BUILDING RELEASE INFORMATION FOR RELEASE {VersionTag}...", LogType.InfoLog);
            ReleaseInformation OutputInformation = new ReleaseInformation(VersionTag)
            {
                // Store our installer URL And release notes here
                InstallerUrl = this.GetInjectorInstallerUrl(VersionTag),
                ReleaseNotes = this.GetInjectorReleaseNotes(VersionTag)
            };

            // Log that we built our release information correctly and return it out
            this._serviceLogger.WriteLog($"BUILT NEW RELEASE INFORMATION FOR VERSION {VersionTag} OK!", LogType.InfoLog);
            this._serviceLogger.WriteLog("RELEASE VERSION IS BEING REPORTED AS FOLLOWS");
            this._serviceLogger.WriteLog($"VERSION:       {OutputInformation.VersionTag}");
            this._serviceLogger.WriteLog($"INSTALLER URL: {OutputInformation.InstallerUrl}");
            this._serviceLogger.WriteLog($"RELEASE NOTES: {OutputInformation.ReleaseNotes.Substring(0, 20)}...");

            // Return out the built release information here
            return OutputInformation;
        }
        /// <summary>
        /// Checks if a version is ready to be updated or not.
        /// </summary>
        /// <param name="VersionTag">Current Version</param>
        /// <param name="LatestVersion">The latest version of the injector application ready to install</param>
        /// <returns>True if updates ready. False if not.</returns>
        /// <exception cref="InvalidOperationException">Thrown when we're unable to refresh injector versions</exception>
        public bool CheckForUpdate(string VersionTag, out string LatestVersion)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(CheckForUpdate), VersionTag, string.Empty);
                LatestVersion = PipeAction.PipeMethodArguments[1].ToString();
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

            // First find our latest injector release object
            Release LatestInjectorRelease = this._getInjectorRelease();
            if (LatestInjectorRelease == null) throw new InvalidOperationException("Error! Failed to find latest injector release!");

            // Now compare the versions based on our most recent release
            Version InputVersionParsed = Version.Parse(Regex.Match(VersionTag, @"(\d+(?>\.|))+").Value);
            Version LatestVersionParsed = Version.Parse(Regex.Match(LatestInjectorRelease.TagName, @"(\d+(?>\.|))+").Value);
            this._serviceLogger.WriteLog("PARSED VERSIONS CORRECTLY! READY TO COMPARE AND RETURN", LogType.InfoLog);

            // Compare and log result
            LatestVersion = LatestVersionParsed.ToString();
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
        /// <exception cref="NullReferenceException">Thrown when the version we're looking to pull in can not be found</exception>
        public string GetInjectorInstallerUrl(string VersionTag)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(GetInjectorInstallerUrl), VersionTag);
                return PipeAction.PipeCommandResult.ToString();
            }

            // First find our version to use using our version/release lookup tool
            var ReleaseToUse = this._getInjectorRelease(VersionTag);
            if (ReleaseToUse == null) throw new NullReferenceException($"Error! Release version {VersionTag} could not be found!");
            this._serviceLogger.WriteLog("PULLED IN A NEW RELEASE OBJECT TO UPDATE WITH!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"RELEASE TAG: {ReleaseToUse.TagName}");

            // Now get the asset url and download it here into a temp file
            string InjectorAssetUrl = ReleaseToUse?.Assets?.FirstOrDefault(AssetObj => AssetObj.BrowserDownloadUrl.EndsWith("msi"))?.BrowserDownloadUrl;
            if (string.IsNullOrWhiteSpace(InjectorAssetUrl)) 
                throw new NullReferenceException($"Error! Unable to find valid release installer for version {VersionTag}!");

            // Pull in our asset download path and store our installer file
            this._serviceLogger.WriteLog($"RELEASE ASSET FOUND! URL IS: {InjectorAssetUrl}", LogType.InfoLog);
            return InjectorAssetUrl;
        }
        /// <summary>
        /// Finds the release version requested and pulls in the release notes for this version
        /// </summary>
        /// <param name="VersionTag">The version we're building release notes for</param>
        /// <returns>A string value holding our release notes once found</returns>
        /// <exception cref="InvalidOperationException">Thrown when we're unable to refresh injector versions</exception>
        public string GetInjectorReleaseNotes(string VersionTag)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(GetInjectorReleaseNotes), VersionTag);
                return PipeAction.PipeCommandResult.ToString();
            }

            // Find the release for the given version and return the release notes for it
            var ReleaseToUse = this._getInjectorRelease(VersionTag);
            if (ReleaseToUse == null) throw new NullReferenceException($"Error! Release version {VersionTag} could not be found!");
            this._serviceLogger.WriteLog("PULLED IN A NEW RELEASE OBJECT TO BUILD RELEASE NOTES FOR!", LogType.InfoLog);
            this._serviceLogger.WriteLog($"RELEASE TAG: {ReleaseToUse.TagName}");

            // Return the release notes for this object here if they exist
            if (string.IsNullOrWhiteSpace(ReleaseToUse.Body)) throw new InvalidOperationException($"Error! No release notes could be found for tag {VersionTag}!"); 
            this._serviceLogger.WriteLog("FOUND RELEASE NOTES! RETURNING THEM NOW...", LogType.InfoLog);
            return ReleaseToUse.Body;
        }
        /// <summary>
        /// Installs a new version of the injector application using the given MSI File path
        /// </summary>
        /// <param name="InstallerPath">The path to the installer for the injector app</param>
        /// <returns>True if invoked correctly, false if not</returns>
        /// <exception cref="FileNotFoundException">Thrown when no installer file can be found</exception>
        /// <exception cref="InvalidOperationException">Thrown when an invalid installer is provided</exception>
        public bool InstallInjectorApplication(string InstallerPath)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(InstallInjectorApplication), InstallerPath);
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }
            
            // Make sure our installer exists before moving on
            if (!File.Exists(InstallerPath))
                throw new FileNotFoundException($"Error! Installer \"{InstallerPath}\" could not be found!");
            if (!Path.GetExtension(InstallerPath).Contains("msi"))
                throw new InvalidOperationException("Error! Installer files must be an MSI file type!");

            // Kill existing instances of the injector application here if needed
            Process[] InjectorProcesses = Process.GetProcesses()
                .Where(ProcObj => ProcObj.ProcessName.Contains("FulcrumInjector"))
                .ToArray();         

            // If existing injector processes are found, kill them all one by one
            if (InjectorProcesses.Length == 0) this._serviceLogger.WriteLog("NO EXISTING INJECTOR PROCESSES FOUND! MOVING ON", LogType.InfoLog); 
            else 
            {
                // Log out that we're killing existing processes
                this._serviceLogger.WriteLog("KILLING EXISTING INJECTOR PROCESSES BEFORE STARTING UPDATE...", LogType.WarnLog);
                this._serviceLogger.WriteLog($"TOTAL OF {InjectorProcesses.Length} PROCESSES TO BE KILLED");

                // Iterate all of our processes and kill them one by one
                foreach (var InjectorProcess in InjectorProcesses)
                {
                    try
                    {
                        // Kill the process here and catch any failures if they're found
                        this._serviceLogger.WriteLog($"KILLING PROCESS \"{InjectorProcess.ProcessName}\" (PID: {InjectorProcess.Id})...");
                        InjectorProcess.Kill();
                    }
                    catch (Exception KillProcessEx)
                    {
                        // Catch the failure and log it out here
                        this._serviceLogger.WriteLog($"ERROR! FAILED TO KILL PROCESS WITH ID {InjectorProcess.Id}!", LogType.ErrorLog);
                        this._serviceLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW", KillProcessEx);
                    }
                }

                // Log out we're done killing processes here
                this._serviceLogger.WriteLog("DONE ATTEMPTING TO KILL PROCESSES FOR INJECTOR APPLICATION!", LogType.WarnLog);
                this._serviceLogger.WriteLog("HOPEFULLY WE DIDN'T KILL THE WRONG TASK THIS TIME!", LogType.WarnLog);
            }

            // Configure an installer log file here
            FileInfo InstallerInfo = new FileInfo(InstallerPath);
            string InstallerDirectory = InstallerInfo.DirectoryName;
            string InstallerName = Path.GetFileNameWithoutExtension(InstallerInfo.Name);
            string InstallerLogFile = Path.Combine(InstallerDirectory, $"InstallerLog_{InstallerName}.log");

            // Build our argument string for the msiexec process 
            string UpdaterArguments =
                $"/A \"{InstallerPath}\" " +                     // Install the package as an administrator
                $"TARGETDIR=\"C:\\Program Files (x86)\\\" " +    // Specify our target install directory
                $"/L*V \"{InstallerLogFile}\"";                  // Sets logging output to the log file name given

            // Build a new process to invoke our installer msi file here
            ProcessStartInfo UpdaterStartInfo = new ProcessStartInfo
            {
                // Configuration for process bootup
                Verb = "runas",                          // Forces the process to run with the runas verb
                UseShellExecute = true,                  // Uses shell execution or not
                FileName = "msiexec.exe",                // File to invoke. MSIEXEC for installing MSI files
                Arguments = UpdaterArguments,            // Arguments to pass into the MSIEXEC when booted
            };

            // Log out that we're booting our new updater process now
            this._serviceLogger.WriteLog("STARTING INSTALLER FOR INJECTOR APPLICATION!", LogType.WarnLog);
            this._serviceLogger.WriteLog($"INSTALLER FILE IS BEING BOOTED FROM PATH: {InstallerPath}");
            this._serviceLogger.WriteLog($"INSTALLER LOG FILE IS BEING SAVED AS: {InstallerLogFile}");

            // Build our process and store the standard output and error information
            Process UpdaterProcess = new Process();
            UpdaterProcess.StartInfo = UpdaterStartInfo;

            // Start the process and wait for it to exit
            UpdaterProcess.Start();
            UpdaterProcess.WaitForExit();
            
            // Check the status of the process once it's exited
            this._serviceLogger.WriteLog("INSTALLER PROCESS HAS COMPLETED! CHECKING STATUS OF UPDATE NOW...", LogType.WarnLog);
            if (UpdaterProcess.ExitCode == 0) this._serviceLogger.WriteLog("INSTALLER PROCESS EXITED WITH CODE 0! THIS IS PERFECT!", LogType.InfoLog);
            else this._serviceLogger.WriteLog($"WARNING! INSTALLER EXITING WITH EXIT CODE {UpdaterProcess.ExitCode}! THIS MAY BE AN ISSUE", LogType.WarnLog);

            // Return out if we've got an exit code of 0 or not 
            return UpdaterProcess.ExitCode == 0;
        }
        /// <summary>
        /// Downloads the given version installer for the injector application and stores it in a temp path
        /// </summary>
        /// <param name="VersionTag">The version of the injector we're looking to download</param>
        /// <param name="InstallerPath">The path to the installer pulled in from the repository</param>
        /// <returns>True if the MSI is pulled in. False if it is not</returns>
        /// <exception cref="InvalidOperationException">Thrown when we're unable to refresh injector versions</exception>
        public bool DownloadInjectorInstaller(string VersionTag, out string InstallerPath)
        {
            // Check if we're using a service instance or not first
            if (this.IsServiceClient)
            {
                // Invoke our pipe routine for this method if needed and store output results
                var PipeAction = this.ExecutePipeMethod(nameof(DownloadInjectorInstaller), VersionTag, string.Empty);
                InstallerPath = PipeAction.PipeMethodArguments[1].ToString();
                return bool.Parse(PipeAction.PipeCommandResult.ToString());
            }

            // Get the URL of the asset we're looking to download here
            this._serviceLogger.WriteLog($"LOCATING ASSET URL FOR VERSION TAG {VersionTag} NOW...", LogType.InfoLog);
            string AssetDownloadUrl = this.GetInjectorInstallerUrl(VersionTag);
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
            string InstallerName = Path.ChangeExtension(AssetDownloadUrl.Split('/').Last(), "msi");
            string InstallersFolder = Path.Combine(RegistryControl.InjectorInstallPath, "FulcrumInstallers");
            string DownloadFilePath = Path.Combine(InstallersFolder, InstallerName);
            this._serviceLogger.WriteLog($"PULLING IN RELEASE VERSION {InstallerName} NOW...", LogType.InfoLog);
            this._serviceLogger.WriteLog($"ASSET DOWNLOAD URL IS {AssetDownloadUrl}", LogType.InfoLog);
            this._serviceLogger.WriteLog($"PULLING DOWNLOADED MSI INTO TEMP FILE {DownloadFilePath}", LogType.InfoLog);

            // Make sure our installer download directory exists here 
            if (!Directory.Exists(InstallersFolder)) {
                this._serviceLogger.WriteLog("WARNING! INSTALLERS FOLDER DID NOT EXIST! BUILDING IT NOW...", LogType.WarnLog);
                Directory.CreateDirectory(InstallersFolder);
            }

            try
            {
                // Boot the download timer and kick off our download here
                DownloadTimer.Start();
                AssetDownloadHelper.DownloadFile(AssetDownloadUrl, DownloadFilePath);
                DownloadTimer.Stop();

                // Log out how long this download process took and return out our download path
                this._serviceLogger.WriteLog($"DOWNLOAD COMPLETE! ELAPSED TIME: {DownloadTimer.Elapsed:g}", LogType.InfoLog);
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

        /// <summary>
        /// Pulls in the requested injector release object for the version requested
        /// If no version is given, the latest release is pulled in 
        /// </summary>
        /// <param name="VersionTag">Optional version of the release we're looking to download</param>
        /// <param name="ForceRefresh">Forces refreshing of injector versions or not</param>
        /// <returns>The release object pulled in for the given version or the latest release if no version provided</returns>
        /// <exception cref="AuthenticationException">Thrown when our gir client fails to authorize</exception>
        /// <exception cref="NullReferenceException">Thrown when no releases are found on the repository</exception>
        private Release _getInjectorRelease(string VersionTag = null, bool ForceRefresh = false)
        {
            // Log out what operation we're performing now
            this._serviceLogger.WriteLog((VersionTag == null
                ? "ATTEMPTING TO FIND LATEST RELEASE FOR THE INJECTOR APP NOW..."
                : $"ATTEMPTING TO FIND RELEASE {VersionTag} NOW..."), LogType.WarnLog);

            // Make sure we're authorized on the GitHub client first 
            if (!this._authorizeGitClient())
                throw new AuthenticationException("Error! Failed to authorize GitHub client for the MEAT Inc Organization!");

            try
            {
                // Pull in the release requested based on a version value and return it out here
                if (this._injectorReleases == null || ForceRefresh)
                    this._injectorReleases = this._gitUpdaterClient.Repository.Release
                    .GetAll(this._serviceConfig.UpdaterOrgName, this._serviceConfig.UpdaterRepoName)
                    .Result.ToArray();

                // Check if we need a specific version or not
                Release InjectorRelease = VersionTag == null 
                    ? this._injectorReleases.FirstOrDefault() 
                    : this._injectorReleases.FirstOrDefault(ReleaseObj => ReleaseObj.TagName.Contains(VersionTag));

                // If the release object is null, throw a new exception out 
                if (InjectorRelease == null) 
                    throw new NullReferenceException($"Error! Could not find injector release {VersionTag}!");

                // Log out information about our pulled injector release
                if (VersionTag == null)
                {
                    // For the latest version, log the found version number out and exit out
                    this._serviceLogger.WriteLog("FOUND LATEST VERSION FOR INJECTOR APPLICATION CORRECTLY!", LogType.InfoLog);
                    this._serviceLogger.WriteLog($"LATEST VERSION WAS SEEN TO BE {InjectorRelease.TagName}!", LogType.InfoLog);
                }
                else
                {
                    // For specified versions, log out what version is located and exit out
                    this._serviceLogger.WriteLog($"FOUND INJECTOR RELEASE {VersionTag} CORRECTLY!", LogType.InfoLog);
                    this._serviceLogger.WriteLog($"RETURNING OUT RELEASE OBJECT FOR VERSION {VersionTag} NOW...", LogType.InfoLog);
                }

                // Return out the found injector release object here
                return InjectorRelease;
            }
            catch (Exception FindReleaseEx)
            {
                // Log out our exception and exit out false
                this._serviceLogger.WriteLog($"ERROR! FAILED TO FIND INJECTOR VERSION {VersionTag}!", LogType.ErrorLog);
                this._serviceLogger.WriteException("EXCEPTION THROWN DURING REFRESH ROUTINE IS BEING LOGGED BELOW", FindReleaseEx);
                return null;
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
    }
}