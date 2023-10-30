using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using FulcrumJson;
using FulcrumUpdaterService;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model content for the Updater view on the injector application
    /// </summary>
    public class FulcrumUpdaterViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // GitHub Updater Client
        public readonly FulcrumUpdater GitHubUpdateHelper;

        // Private backing fields for our public properties
        private bool _updateReady;                // Sets if there's an update ready or not.
        private bool _isDownloading;              // Sets if the updater is currently pulling in a file or not.
        private double _downloadProgress;         // Progress for when downloads are in the works
        private string _downloadTimeElapsed;      // Time downloading spent so far
        private string _downloadTimeRemaining;    // Approximate time left on the download

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool UpdateReady { get => _updateReady; set => PropertyUpdated(value); }
        public bool IsDownloading { get => _isDownloading; set => PropertyUpdated(value); }
        public double DownloadProgress { get => _downloadProgress; set => PropertyUpdated(value); }
        public string DownloadTimeElapsed { get => _downloadTimeElapsed; set => PropertyUpdated(value); }
        public string DownloadTimeRemaining { get => _downloadTimeRemaining; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes


        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="UpdaterViewUserControl">User control which holds the view content for the updater</param>
        public FulcrumUpdaterViewModel(UserControl UpdaterViewUserControl) : base(UpdaterViewUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP TITLE VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Setup basic view bound values
            this.DownloadProgress = 0;
            this.DownloadTimeElapsed = "00:00";
            this.DownloadTimeRemaining = "N/A";

            // Build new update helper
            this.GitHubUpdateHelper = FulcrumUpdater.InitializeUpdaterService().Result;
            this.ViewModelLogger.WriteLog("BUILT NEW UPDATE HELPER OK! UPDATE CHECK HAS PASSED! READY TO INVOKE NEW UPDATE IF NEEDED", LogType.InfoLog);

            // Check for force update toggle
            bool ForceUpdate = ValueLoaders.GetConfigValue<bool>("FulcrumServices.FulcrumUpdaterService.ForceUpdateReady");
            if (ForceUpdate) this.ViewModelLogger.WriteLog("WARNING! FORCING UPDATES IS ON! ENSURING SHOW UPDATE BUTTON IS VISIBLE!", LogType.WarnLog);

            // Check for our updates now.
            this.GitHubUpdateHelper.RefreshInjectorVersions();
            if (!this.GitHubUpdateHelper.CheckAgainstVersion(FulcrumVersionInfo.InjectorVersionString) && !ForceUpdate) {
                this.ViewModelLogger.WriteLog("NO UPDATE FOUND! MOVING ON TO MAIN EXECUTION ROUTINE", LogType.WarnLog);
                this.ViewModelLogger.WriteLog("NOT CONFIGURING UPDATE EVENT ROUTINES FOR OUR UPDATER OBJECT!", LogType.WarnLog);
                this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
                
                // Exit out of this routine once no update is found
                return;
            }

            // Now setup view content for update ready.
            this.UpdateReady = true;
            this._initializeUpdaterClientEvents();

            // Log completed building view model instance and exit out
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Invokes a new download on the git hub helper to pull in the newest release of the injector
        /// </summary>
        public string InvokeInjectorDownload()
        {
            // Start by invoking a new download of the newest version
            this.IsDownloading = true;
            string LatestTag = this.GitHubUpdateHelper.LatestInjectorVersion;
            this.ViewModelLogger.WriteLog($"PULLING IN RELEASE VERSION {LatestTag} NOW...", LogType.InfoLog);
            string OutputAssetPath = this.GitHubUpdateHelper.DownloadInjectorRelease(LatestTag, out string AssetUrl);

            // Log done downloading and return the path
            this.ViewModelLogger.WriteLog($"DOWNLOADED RELEASE {LatestTag} TO PATH {OutputAssetPath} OK!", LogType.InfoLog);
            this.IsDownloading = false;
            return OutputAssetPath;
        }
        /// <summary>
        /// Installs a new version of the Injector application from the given MSI path
        /// </summary>
        /// <param name="PathToInstaller">Path to the installer to run</param>
        /// <returns>True if started, false if not.</returns>
        public bool InstallInjectorRelease(string PathToInstaller)
        {
            // Setup our string for the command to run.
            string InvokeUpdateString = $"/C taskkill /F /IM Fulcrum* && msiexec /i {PathToInstaller}";
            if (!File.Exists(PathToInstaller)) {
                this.ViewModelLogger.WriteLog($"PATH {PathToInstaller} DOES NOT EXIST ON THE SYSTEM! UNABLE TO INSTALL A NEW VERSION!", LogType.ErrorLog);
                return false; 
            }

            // Build a process object and setup a CMD line call to install our new version
            Process InstallNewReleaseProcess = new Process();
            InstallNewReleaseProcess.StartInfo = new ProcessStartInfo()
            {
                Verb = "runas",                   // Set this to run as admin
                FileName = "cmd.exe",             // Boot a CMD window
                CreateNoWindow = true,            // Create no window on output
                UseShellExecute = false,          // Shell execution
                Arguments = InvokeUpdateString,   // Args to invoke.
            };

            // Log starting updates and return true
            this.ViewModelLogger.WriteLog("STARTING INJECTOR UPDATES NOW! THIS WILL KILL OUR INJECTOR INSTANCE!", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("BYE BYE BOOKWORM TIME TO KILL", LogType.InfoLog);

            // Boot the update and return true
            InstallNewReleaseProcess.Start();
            InstallNewReleaseProcess.CloseMainWindow();
            return true;
        }

        /// <summary>
        /// Configures updater event objects for when downloads are in the works
        /// </summary>
        private void _initializeUpdaterClientEvents()
        {
            // Build action for downloading in progress
            this.GitHubUpdateHelper.OnUpdaterProgress += _updateDownloadProgressEvent;
            this.GitHubUpdateHelper.OnUpdaterComplete += _updateDownloadCompleteProgressEvent;

            // Log done and exit routine
            this.ViewModelLogger.WriteLog("BUILT EVENTS FOR PROGRESS MONITORING CORRECTLY!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("DOWNLOAD PROGRESS WILL BE TRACKED AND UPDATED AS FILES ARE PULLED IN", LogType.InfoLog);
        }
        /// <summary>
        /// Event handler routine for when download progress is updated
        /// </summary>
        /// <param name="SendingUpdater">Updater who sent this event</param>
        /// <param name="UpdateArgs">Arguments fired with this event</param>
        private void _updateDownloadProgressEvent(object SendingUpdater, DownloadProgressChangedEventArgs UpdateArgs)
        {
            // Start by getting the current progress update value and set is downloading to true
            this.IsDownloading = true;
            this.DownloadProgress = UpdateArgs.ProgressPercentage;
            this.DownloadTimeElapsed = GitHubUpdateHelper.DownloadTimeElapsed;
            this.DownloadTimeRemaining = GitHubUpdateHelper.DownloadTimeRemaining;

            // Log the current byte count output
            string CurrentSize = UpdateArgs.BytesReceived.ToString();
            string TotalSize = UpdateArgs.TotalBytesToReceive.ToString();
            this.ViewModelLogger.WriteLog($"CURRENT DOWNLOAD PROGRESS: {CurrentSize} OF {TotalSize}");
        }
        /// <summary>
        /// Event handler routine for when download progress is completed
        /// </summary>
        /// <param name="SendingUpdater">Updater who sent this event</param>
        /// <param name="UpdateArgs">Arguments fired with this event</param>
        private void _updateDownloadCompleteProgressEvent(object SendingUpdater, DownloadDataCompletedEventArgs UpdateArgs)
        {
            // Update current progress to 100% and set is downloading state to false.
            this.IsDownloading = false;
            this.DownloadProgress = 100;
            this.DownloadTimeElapsed = GitHubUpdateHelper.DownloadTimeElapsed;
            this.DownloadTimeRemaining = "Download Done!";

            // Log done downloading and update values for the view model
            this.ViewModelLogger.WriteLog("DOWNLOADING COMPLETED WITHOUT ISSUES!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"TOTAL DOWNLOAD TIME ELAPSED: {GitHubUpdateHelper.DownloadTimeElapsed}");
        }
    }
}
