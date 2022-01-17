using System.Linq;
using FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews;
using FulcrumInjector.FulcrumViewSupport.AppStyleSupport.AvalonEditHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    public class FulcrumDllOutputLogViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorDllOutputViewModelLogger")) ?? new SubServiceLogger("InjectorDllOutputViewModelLogger");

        // Private control values
        private bool _hasOutput;
        private bool _usingRegex;
        private bool _noResultsOnSearch;
        
        // Public values for our view to bind onto 
        public bool HasOutput { get => _hasOutput; set => PropertyUpdated(value); }
        public bool UsingRegex { get => _usingRegex; set => PropertyUpdated(value); }
        public bool NoResultsOnSearch { get => _noResultsOnSearch; set => PropertyUpdated(value); }

        // Helper for editing Text box contents
        private readonly PropertyWatchdog _outputWatchdog;
        public AvalonEditFilteringHelpers LogContentHelper;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumDllOutputLogViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP INJECTOR TEST VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Build watchdogs.
            this.HasOutput = false;
            this._outputWatchdog = new PropertyWatchdog(100);
            ViewModelLogger.WriteLog("BUILT NEW WATCHDOG OBJECT HELPER FOR VIEW OUTPUT ON DEBUG LOG PLACEHOLDER CONTENT!", LogType.InfoLog);

            // Build log content helper and return
            ViewModelLogger.WriteLog("SETUP NEW DLL INJECTION OUTPUT LOG VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"STORED NEW VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// Boots watchdog for view content watching.
        /// </summary>
        public void SetupWatchdogs()
        {
            // Start watchdog objects here.
            this._outputWatchdog.StartUpdateTimer((_, _) =>
            {
                // Cast view output object and then pull value from it.
                var CastView = this.BaseViewControl as FulcrumDllOutputLogView;
                try { this.HasOutput = CastView.DebugRedirectOutputEdit.Text.Length != 0; } 
                catch {
                    // Ignore failure on this set method. 
                    // View is still updating as desired.
                }
            });
            ViewModelLogger.WriteLog("STARTED NEW WATCHDOG OBJECT HELPER FOR VIEW OUTPUT ON DEBUG LOG PLACEHOLDER CONTENT!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        internal void SearchForText(string TextToFind)
        {
            // Make sure transformer is built
            if (LogContentHelper == null) return;
            var OutputTransformer = this.LogContentHelper.SearchForText(TextToFind);

            // Store values here
            if (string.IsNullOrEmpty(TextToFind)) return;
            this.UsingRegex = OutputTransformer?.UseRegex ?? false;
            this.NoResultsOnSearch = OutputTransformer?.NoMatches ?? false;
        }
    }
}
