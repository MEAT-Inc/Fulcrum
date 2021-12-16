using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.AppLogic.DebugLogFormatters;
using FulcrumInjector.JsonHelpers;
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
            ViewModelLogger.WriteLog("SETUP NEW LOGGING REDIRECTION TARGET VALUES OK!", LogType.InfoLog);
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
        internal void SearchForText(string TextToFind, TextEditor EditObject)
        {
            try
            {
                // Remove existing search objects and add in new ones.
                var TransformerList = EditObject.TextArea.TextView.LineTransformers.ToList();
                TransformerList.RemoveAll(TransformerObj => TransformerObj.GetType() == typeof(DebugLogSelectMatchesColorFormatter));

                // Clear out transformers and append them in again
                EditObject.TextArea.TextView.LineTransformers.Clear();
                foreach (var TransformerObject in TransformerList)
                    EditObject.TextArea.TextView.LineTransformers.Add(TransformerObject);

                // Build new searching helper and apply it onto the view
                if (string.IsNullOrEmpty(TextToFind)) { EditObject.TextArea.TextView.Redraw(); }
                else
                {
                    // Apply Searcher. Store if using Regex or not.
                    var NewSearcher = new DebugLogSelectMatchesColorFormatter(TextToFind);
                    EditObject.TextArea.TextView.LineTransformers.Add(NewSearcher);

                    // Set Regex and match status
                    this.UsingRegex = NewSearcher.UseRegex;
                    this.NoResultsOnSearch = NewSearcher.NoMatches;
                }
            }
            catch (Exception Ex)
            {
                // Log this failure and return outupt.
                ViewModelLogger.WriteLog("ERROR! FAILED TO STORE NEW COLORING METHODS ONTO OUR DOCUMENT VIEW!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN DURING COLORING LINE ROUTINE!", Ex);
            }
        }
        /// <summary>
        /// Filters log lines by logger name
        /// </summary>
        /// <param name="LoggerName"></param>
        /// <param name="EditorObject"></param>
        internal void FilterByLoggerName(string LoggerName, TextEditor EditObject)
        {
            try
            {
                // Remove existing filtering transformers 
                var TransformerList = EditObject.TextArea.TextView.LineTransformers.ToList();
                TransformerList.RemoveAll(TransformerObj =>
                {
                    // If not a filtering type, return false to not remove
                    if (TransformerObj.GetType() != typeof(DebugLogLineFilteringColorFormatter))
                        return false;

                    // Cast and run the reset command here then remove
                    var FilteringObj = TransformerObj as DebugLogLineFilteringColorFormatter;
                    FilteringObj.ResetDocumentContent();
                    return true;
                });

                // Clear out transformers and append them in again
                EditObject.TextArea.TextView.LineTransformers.Clear();
                foreach (var TransformerObject in TransformerList)
                    EditObject.TextArea.TextView.LineTransformers.Add(TransformerObject);

                // Build new searching helper and apply it onto the view
                if (string.IsNullOrEmpty(LoggerName)) { EditObject.TextArea.TextView.Redraw(); }
                else
                {
                    // Apply Searcher. Store if using Regex or not.
                    var NewSearcher = new DebugLogLineFilteringColorFormatter(EditObject, LoggerName);
                    EditObject.TextArea.TextView.LineTransformers.Add(NewSearcher);
                }
            }
            catch (Exception Ex)
            {
                // Log this failure and return outupt.
                ViewModelLogger.WriteLog("ERROR! FAILED TO STORE NEW FILTERING METHODS ONTO OUR DOCUMENT VIEW!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN DURING FILTERING ROUTINE!", Ex);
            }
        }
    }
}