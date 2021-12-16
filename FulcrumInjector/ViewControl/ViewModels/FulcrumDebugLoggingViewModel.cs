using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.AppLogic;
using FulcrumInjector.AppLogic.AvalonEditHelpers;
using FulcrumInjector.JsonHelpers;
using FulcrumInjector.ViewControl.Views;
using ICSharpCode.AvalonEdit;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.ViewModels
{
    /// <summary>
    /// View model used to help render and register logging redirects
    /// </summary>
    public class FulcrumDebugLoggingViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("DebugLoggingViewModelLogger")) ?? new SubServiceLogger("DebugLoggingViewModelLogger");

        // Private control values
        private bool _usingRegex;
        private bool _noResultsOnSearch;
        private List<string> _loggerNamesFound;

        // Public values for our view to bind onto 
        public bool UsingRegex { get => _usingRegex; set => PropertyUpdated(value); }
        public bool NoResultsOnSearch { get => _noResultsOnSearch; set => PropertyUpdated(value); }
        public List<string> LoggerNamesFound { get => _loggerNamesFound; set => PropertyUpdated(value); }

        // Helper for editing Text box contents
        public readonly AvalonEditFilteringHelpers LogContentHelper;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumDebugLoggingViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP DEBUG LOG TARGETS FOR UI LOGGING NOW...", LogType.WarnLog);

            // Pull editor off the view base
            var DebugViewCast = this.BaseViewControl as FulcrumDebugLoggingView;
            var PulledEditBox = DebugViewCast.DebugRedirectOutputEdit;
            ViewModelLogger.WriteLog("CAST VIEW TO TYPE OF DEBUG LOG VIEWER AND EXTRACTED TEXTEDIT OK!", LogType.InfoLog);

            // Store logger names here
            this.LoggerNamesFound = this.BuildLoggerNamesList();
            this.LogContentHelper = new AvalonEditFilteringHelpers(PulledEditBox);
            ViewModelLogger.WriteLog("DONE CONFIGURING VIEW BINDING VALUES!", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW LOGGING VIEW AND MODIFICATION VALUES OK!", LogType.InfoLog);
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
            PulledNames.AddRange(LogBroker.LoggerQueue.GetLoggers().Select(LoggerObj => LoggerObj.LoggerName).ToList());

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
            // Setup new transformer helper
            var OutputTransformer = this.LogContentHelper.SearchForText(TextToFind);

            // Store values here
            this.UsingRegex = OutputTransformer.UseRegex;
            this.NoResultsOnSearch = OutputTransformer.NoMatches;
        }
        /// <summary>
        /// Filters log lines by logger name
        /// </summary>
        /// <param name="LoggerName"></param>
        /// <param name="EditorObject"></param>
        internal void FilterByLoggerName(string LoggerName) { this.LogContentHelper.FilterByText(LoggerName); }
    }
}