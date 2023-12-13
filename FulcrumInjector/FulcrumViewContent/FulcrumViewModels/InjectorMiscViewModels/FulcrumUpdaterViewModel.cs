using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xaml;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using FulcrumJson;
using FulcrumSupport;
using FulcrumUpdaterService;
using SharpLogging;

// Using calls for MarkDig conversion routines
using Markdig;
using Markdown = Markdig.Wpf.Markdown;
using XamlReader = System.Windows.Markup.XamlReader;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model content for the Updater view on the injector application
    /// </summary>
    public class FulcrumUpdaterViewModel : FulcrumViewModelBase
    {
        #region Custom Events

        // Event for download progress
        private EventHandler<DownloadDataCompletedEventArgs> OnUpdaterComplete;
        private EventHandler<DownloadProgressChangedEventArgs> OnUpdaterProgress;

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
            this.DownloadTimeElapsed = this.DownloadTimeElapsed;
            this.DownloadTimeRemaining = this.DownloadTimeRemaining;
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
            this.DownloadTimeElapsed = this.DownloadTimeElapsed;
            this.DownloadTimeRemaining = "Download Done!";

            // Log done downloading and update values for the view model
            this.ViewModelLogger.WriteLog("DOWNLOADING COMPLETED WITHOUT ISSUES!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"TOTAL DOWNLOAD TIME ELAPSED: {this.DownloadTimeElapsed}");
        }

        #endregion // Custom Events

        #region Fields

        // GitHub Updater Client service
        public readonly FulcrumUpdater GitHubUpdateHelper;

        // Private backing fields for our public properties
        private bool _updateReady;                // Sets if there's an update ready or not.
        private bool _isDownloading;              // Sets if the updater is currently pulling in a file or not.
        private double _downloadProgress;         // Progress for when downloads are in the works
        private string _downloadTimeElapsed;      // Time downloading spent so far
        private string _downloadTimeRemaining;    // Approximate time left on the download
        private string _latestInjectorVersion;    // The latest version of the injector application ready

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool UpdateReady { get => _updateReady; set => PropertyUpdated(value); }
        public bool IsDownloading { get => _isDownloading; set => PropertyUpdated(value); }
        public double DownloadProgress { get => _downloadProgress; set => PropertyUpdated(value); }
        public string DownloadTimeElapsed { get => _downloadTimeElapsed; set => PropertyUpdated(value); }
        public string DownloadTimeRemaining { get => _downloadTimeRemaining; set => PropertyUpdated(value); }
        public string LatestInjectorVersion { get => _latestInjectorVersion; set => PropertyUpdated(value); }

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

            // Build action for downloading in progress
            this.OnUpdaterProgress += _updateDownloadProgressEvent;
            this.OnUpdaterComplete += _updateDownloadCompleteProgressEvent;
            this.ViewModelLogger.WriteLog("HOOKED NEW EVENTS FOR DOWNLOAD PROGRESS ON UPDATE HELPER CORRECTLY!", LogType.InfoLog);

            // Build new update helper
            this.GitHubUpdateHelper = FulcrumUpdater.InitializeUpdaterService().Result;
            this.ViewModelLogger.WriteLog("BUILT NEW UPDATE HELPER OK! UPDATE CHECK HAS PASSED! READY TO INVOKE NEW UPDATE IF NEEDED", LogType.InfoLog);

            // Check for force update toggle
            bool ForceUpdate = ValueLoaders.GetConfigValue<bool>("FulcrumServices.FulcrumUpdaterService.ForceUpdateReady");
            if (ForceUpdate) this.ViewModelLogger.WriteLog("WARNING! FORCING UPDATES IS ON! ENSURING SHOW UPDATE BUTTON IS VISIBLE!", LogType.WarnLog);

            // Check for our updates now.
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
            if (!this.GitHubUpdateHelper.CheckForUpdate(FulcrumVersionInfo.InjectorVersionString, out this._latestInjectorVersion) && !ForceUpdate)
            {
                // Log out that no update is ready and that we've constructed a view model correctly
                this.ViewModelLogger.WriteLog("NO UPDATE FOUND! MOVING ON TO MAIN EXECUTION ROUTINE", LogType.WarnLog);
                this.ViewModelLogger.WriteLog("NOT CONFIGURING UPDATE EVENT ROUTINES FOR OUR UPDATER OBJECT!", LogType.WarnLog);
                return;
            }

            // Now setup view content for update ready and exit out 
            this.ViewModelLogger.WriteLog("STORED UPDATE READY STATE TO TRUE!", LogType.InfoLog);
            this.UpdateReady = true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Invokes a new download on the git hub helper to pull in the newest release of the injector
        /// </summary>
        /// <returns>The path to the downloaded injector installer</returns>
        /// <exception cref="InvalidOperationException">Thrown when the version requested can not be found</exception>
        public string InvokeInjectorDownload()
        {
            // Start by finding the version of the injector we wish to update to
            this.IsDownloading = true;
            string AssetDownloadUrl = this.GitHubUpdateHelper.GetInjectorInstallerUrl(this.LatestInjectorVersion);
            if (string.IsNullOrWhiteSpace(AssetDownloadUrl))
                throw new InvalidOperationException($"Error! Failed to find injector version {this.LatestInjectorVersion}!");

            // Build our download path for the pulled asset/installer version
            string InstallerName = Path.ChangeExtension(AssetDownloadUrl.Split('/').Last(), "msi");
            string InstallersFolder = Path.Combine(RegistryControl.InjectorInstallPath, "FulcrumInstallers");
            string DownloadFilePath = Path.Combine(InstallersFolder, InstallerName);
            this.ViewModelLogger.WriteLog($"PULLING IN RELEASE VERSION {this.LatestInjectorVersion.Split('_').Last()} NOW...", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"ASSET DOWNLOAD URL IS {AssetDownloadUrl}", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"PULLING DOWNLOADED MSI INTO TEMP FILE {DownloadFilePath}", LogType.InfoLog);

            // Make sure our installer download directory exists here 
            if (!Directory.Exists(InstallersFolder)) {
                this.ViewModelLogger.WriteLog("WARNING! INSTALLERS FOLDER DID NOT EXIST! BUILDING IT NOW...", LogType.WarnLog);
                Directory.CreateDirectory(InstallersFolder);
            }

            // Return the URL of the path to download here
            Stopwatch DownloadTimer = new Stopwatch();
            WebClient AssetDownloadHelper = new WebClient();
            AssetDownloadHelper.DownloadDataCompleted += (Sender, Args) => this.OnUpdaterComplete.Invoke(this, Args);
            AssetDownloadHelper.DownloadProgressChanged += (Sender, Args) =>
            {
                // Invoke the event for progress changed if it's not null
                this.OnUpdaterProgress?.Invoke(this, Args);

                // Find our approximate time left
                var ApproximateMillisLeft = DownloadTimer.ElapsedMilliseconds * Args.TotalBytesToReceive / Args.BytesReceived;
                TimeSpan ApproximateToSpan = TimeSpan.FromMilliseconds(ApproximateMillisLeft);
                this.DownloadTimeElapsed = DownloadTimer.Elapsed.ToString("mm:ss");
                this.DownloadTimeRemaining = ApproximateToSpan.ToString("mm:ss");
            };

            // Log done building setup and download the version output here
            DownloadTimer.Start();
            this.ViewModelLogger.WriteLog("BUILT NEW WEB CLIENT FOR DOWNLOADING ASSETS OK! STARTING DOWNLOAD NOW...", LogType.InfoLog);
            AssetDownloadHelper.DownloadFile(AssetDownloadUrl, DownloadFilePath); DownloadTimer.Stop();
            this.ViewModelLogger.WriteLog($"TOTAL DOWNLOAD TIME TAKEN: {this.DownloadTimeElapsed}");

            // Log done downloading and return the path
            this.ViewModelLogger.WriteLog($"DOWNLOADED RELEASE {this.LatestInjectorVersion} TO PATH {DownloadFilePath} OK!", LogType.InfoLog);
            this.IsDownloading = false;
            return DownloadFilePath;
        }
        /// <summary>
        /// Pulls in the release notes for the latest injector version and builds a flow document to show on the UI for the updater
        /// </summary>
        /// <returns>A flow document holding release notes for the injector version requested</returns>
        /// <exception cref="InvalidOperationException">Thrown when the conversion to a flow document fails</exception>
        public FlowDocument BuildInjectorReleaseNotes()
        {
            // Pull the release notes for our latest release version and build a new XML document for them
            this.ViewModelLogger.WriteLog($"PULLING IN RELEASE NOTES FOR VERSION {this.LatestInjectorVersion} NOW...", LogType.InfoLog); 
            string ReleaseNotes = this.GitHubUpdateHelper.GetInjectorReleaseNotes(this.LatestInjectorVersion);
            var XamlReleaseNotes = Markdown.ToXaml(ReleaseNotes, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());

            // Now append the contents of the markdown into our output viewer
            using var MemStream = new MemoryStream(Encoding.UTF8.GetBytes(XamlReleaseNotes));
            using var XamlToXmlReader = new XamlXmlReader(MemStream, new MarkdownXamlSchemaContext());
            if (XamlReader.Load(XamlToXmlReader) is not FlowDocument OutputDocument)
                throw new InvalidOperationException("Error! Release notes converted were not seen to be a FlowDocument type!");

            // Return the built flow document object here
            this.ViewModelLogger.WriteLog($"BUILD AND SAVED RELEASE NOTES FOR VERSION {this.LatestInjectorVersion} CORRECTLY!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("RETURNING RELEASE NOTES FLOW DOCUMENT OBJECT NOW...", LogType.InfoLog);
            return OutputDocument;
        }
    }
}
