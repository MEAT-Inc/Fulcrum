using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private bool _injectorUpdateReady;        // Sets if there's an update ready or not.

        // Public values for our view to bind onto 
        public readonly InjectorUpdater GitHubUpdateHelper;
        public bool InjectorUpdateReady { get => _injectorUpdateReady; set => PropertyUpdated(value); }

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
            this.InjectorUpdateReady = true;
        }
    }
}
