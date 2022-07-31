using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.FulcrumUpdater;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorMiscViewModels
{
    /// <summary>
    /// View model content for the Updater view on the injector application
    /// </summary>
    public class FulcrumUpdaterViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("UpdaterViewModelLogger")) ?? new SubServiceLogger("UpdaterViewModelLogger");

        // Private control values
        private bool _updateReady;                // Sets if there's an update ready or not.
        private bool _isDownloading;              // Sets if the updater is currently pulling in a file or not.
        private double _downloadProgress;         // Progress for when downloads are in the works
        private string _downloadTimeElapsed;      // Time downloading spent so far

        // Public values for our view to bind onto 
        public readonly InjectorUpdater GitHubUpdateHelper;
        public bool UpdateReady { get => _updateReady; set => PropertyUpdated(value); }
        public bool IsDownloading { get => _isDownloading; set => PropertyUpdated(value); }
        public double DownloadProgress { get => _downloadProgress; set => PropertyUpdated(value); }
        public string DownloadTimeElapsed { get => _downloadTimeElapsed; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumUpdaterViewModel()
        {
            // Log information and store values
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP TITLE VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Build new update helper
            this.GitHubUpdateHelper = new InjectorUpdater();
            GitHubUpdateHelper.RefreshInjectorVersions();
            ViewModelLogger.WriteLog("BUILT NEW UPDATE HELPER OK! UPDATE CHECK HAS PASSED! READY TO INVOKE NEW UPDATE IF NEEDED", LogType.InfoLog);

            // Check for force update toggle
            bool ForceUpdate = ValueLoaders.GetConfigValue<bool>("FulcrumInjectorConstants.InjectorUpdates.ForceUpdateReady");
            if (ForceUpdate) ViewModelLogger.WriteLog("WARNING! FORCING UPDATES IS ON! ENSURING SHOW UPDATE BUTTON IS VISIBLE!", LogType.WarnLog);

            // Check for our updates now.
            if (!GitHubUpdateHelper.CheckAgainstVersion(FulcrumConstants.InjectorVersions.InjectorVersionString) && !ForceUpdate) {
                ViewModelLogger.WriteLog("NO UPDATE FOUND! MOVING ON TO MAIN EXECUTION ROUTINE", LogType.WarnLog);
                return;
            }

            // Now setup view content for update ready.
            this.UpdateReady = true;
            this.SetupUpdaterClientEvents();
        }
        /// <summary>
        /// Configures updater event objects for when downloads are in the works
        /// </summary>
        private void SetupUpdaterClientEvents()
        {
            // Build action for downloading in progress
            ViewModelLogger.WriteLog("BUILDING PROGRESS UPDATE EVENT FOR DOWNLOADS NOW", LogType.WarnLog);
            this.GitHubUpdateHelper.UpdateDownloadProgressAction += new Action<DownloadProgressChangedEventArgs>((ProgressArgs) =>
            {
                // Start by getting the current progress update value and set is downloading to true
                this.IsDownloading = true;
                this.DownloadProgress = ProgressArgs.ProgressPercentage;
                this.DownloadTimeElapsed = GitHubUpdateHelper.DownloadTimeElapsed;

                // Log the current byte count output
                string CurrentSize = ProgressArgs.BytesReceived.ToString();
                string TotalSize = ProgressArgs.TotalBytesToReceive.ToString();
                ViewModelLogger.WriteLog($"CURRENT DOWNLOAD PROGRESS: {CurrentSize} OF {TotalSize}");
            });

            // Build action for downloading completed
            ViewModelLogger.WriteLog("BUILDING PROGRESS DONE EVENT FOR DOWNLOADS NOW", LogType.WarnLog);
            this.GitHubUpdateHelper.DownloadCompleteProgressAction += new Action<DownloadDataCompletedEventArgs>((ProgressArgs) =>
            {
                // Update current progress to 100% and set is downloading state to false.
                this.IsDownloading = false;
                this.DownloadProgress = 100;
                this.DownloadTimeElapsed = GitHubUpdateHelper.DownloadTimeElapsed;

                // Log done downloading and update values for the view model
                ViewModelLogger.WriteLog("DOWNLOADING COMPLETED WITHOUT ISSUES!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"TOTAL DOWNLOAD TIME ELAPSED: {GitHubUpdateHelper.DownloadTimeElapsed}");
            });

            // Log done and exit routine
            ViewModelLogger.WriteLog("BUILT EVENTS FOR PROGRESS MONITORING CORRECTLY!", LogType.InfoLog);
            ViewModelLogger.WriteLog("DOWNLOAD PROGRESS WILL BE TRACKED AND UPDATED AS FILES ARE PULLED IN", LogType.InfoLog);
        }
    }
}
