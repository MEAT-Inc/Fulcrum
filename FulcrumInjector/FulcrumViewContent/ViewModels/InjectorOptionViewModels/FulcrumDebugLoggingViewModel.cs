using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FIlteringFormatters;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// View model used to help render and register logging redirects
    /// </summary>
    public class FulcrumDebugLoggingViewModel : FulcrumViewModelBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("DebugLoggingViewModelLogger", LoggerActions.SubServiceLogger);

        // Private control values
        private bool _usingRegex;
        private bool _noResultsOnSearch;
        private List<string> _loggerNamesFound;

        // Public values for our view to bind onto 
        public bool UsingRegex { get => _usingRegex; set => PropertyUpdated(value); }
        public bool NoResultsOnSearch { get => _noResultsOnSearch; set => PropertyUpdated(value); }
        public List<string> LoggerNamesFound { get => _loggerNamesFound; set => PropertyUpdated(value); }

        // Helper for editing Text box contents
        public LogOutputFilteringHelper LogContentHelper;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumDebugLoggingViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP DEBUG LOG TARGETS FOR UI LOGGING NOW...", LogType.WarnLog);

            // Store logger names here
            this.LoggerNamesFound = this.BuildLoggerNamesList();
            ViewModelLogger.WriteLog("DONE CONFIGURING VIEW BINDING VALUES!", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW LOGGING VIEW AND MODIFICATION VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"STORED NEW VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Generates a list of logger names from the log broker object
        /// </summary>
        /// <returns>Names of all loggers</returns>
        internal List<string> BuildLoggerNamesList()
        {
            // Pull the names and return them here.
            ViewModelLogger.WriteLog("PULLING LOGGER NAMES FROM QUEUE NOW TO POPULATE DEBUG DROPDOWN...");

            // Build filtering list and return them
            var PulledNames = new List<string>() { "--- Select A Logger ---" };
            PulledNames.AddRange(LoggerQueue.GetLoggers().Select(LoggerObj => LoggerObj.LoggerName).ToList());

            // Return them here
            ViewModelLogger.WriteLog($"PULLED A TOTAL OF {PulledNames.Count} LOGGER NAMES OK!", LogType.InfoLog);
            return PulledNames;
        }

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
        /// <summary>
        /// Filters log lines by logger name
        /// </summary>
        /// <param name="LoggerName"></param>
        /// <param name="EditorObject"></param>
        internal void FilterByLoggerName(string LoggerName) { this.LogContentHelper?.FilterByText(LoggerName); }
    }
}