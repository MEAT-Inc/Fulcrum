using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FilteringFormatters;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// View model used to help render and register logging redirects
    /// </summary>
    internal class FulcrumDebugLoggingViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Helper for editing Text box contents
        public LogOutputFilteringHelper LogContentHelper;

        // Private backing fields for our public properties
        private bool _usingRegex;
        private bool _noResultsOnSearch;
        private List<string> _loggerNamesFound;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public bool UsingRegex { get => _usingRegex; set => PropertyUpdated(value); }
        public bool NoResultsOnSearch { get => _noResultsOnSearch; set => PropertyUpdated(value); }
        public List<string> LoggerNamesFound { get => _loggerNamesFound; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="DebugLoggingUserControl">User control which holds the view content for our debug logging view</param>
        public FulcrumDebugLoggingViewModel(UserControl DebugLoggingUserControl) : base(DebugLoggingUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Log information and store values 
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("SETTING UP DEBUG LOG TARGETS FOR UI LOGGING NOW...", LogType.WarnLog);

            // Store logger names here
            this.LoggerNamesFound = this.BuildLoggerNamesList();
            this.ViewModelLogger.WriteLog("DONE CONFIGURING VIEW BINDING VALUES!", LogType.InfoLog);

            // Log completed setup.
            this.ViewModelLogger.WriteLog("SETUP NEW LOGGING VIEW AND MODIFICATION VALUES OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"STORED NEW VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Generates a list of logger names from the log broker object
        /// </summary>
        /// <returns>Names of all loggers</returns>
        public List<string> BuildLoggerNamesList()
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("PULLING LOGGER NAMES FROM QUEUE NOW TO POPULATE DEBUG DROPDOWN...");

            // Build filtering list and return them
            var PulledNames = new List<string>() { "--- Select A Logger ---" };
            PulledNames.AddRange(SharpLogBroker.LoggerPool.Select(LoggerObj => LoggerObj.LoggerName));

            // Return them here
            this.ViewModelLogger.WriteLog($"PULLED A TOTAL OF {PulledNames.Count} LOGGER NAMES OK!", LogType.InfoLog);
            return PulledNames;
        }
        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        public void SearchForText(string TextToFind)
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
        public void FilterByLoggerName(string LoggerName) { this.LogContentHelper?.FilterByText(LoggerName); }
    }
}