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

        // Public values for our view to bind onto 
        public bool UsingRegex { get => _usingRegex; set => PropertyUpdated(value); }
        public bool NoResultsOnSearch { get => _noResultsOnSearch; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumDebugLoggingViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP DEBUG LOG TARGETS FOR UI LOGGING NOW...", LogType.WarnLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW LOGGING REDIRECTION TARGET VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        internal void SearchForText(string TextToFind, TextEditor EditObject)
        {
            // Remove existing search objects and add in new ones.
            var TransformerList = EditObject.TextArea.TextView.LineTransformers.ToList();
            TransformerList.RemoveAll(TransformerObj => TransformerObj.GetType() == typeof(DebugLogSelectMatchesColorFormatter));

            // Clear out transformers and append them in again
            EditObject.TextArea.TextView.LineTransformers.Clear();
            foreach (var TransformerObject in TransformerList) 
                EditObject.TextArea.TextView.LineTransformers.Add(TransformerObject);

            // Build new searching helper and apply it onto the view
            if (TextToFind == string.Empty) { EditObject.TextArea.TextView.Redraw(); }
            else
            {
                // Apply Searcher. Store if using Regex or not.
                var NewSearcher = new DebugLogSelectMatchesColorFormatter(TextToFind);
                EditObject.TextArea.TextView.LineTransformers.Add(NewSearcher);
                this.UsingRegex = NewSearcher.UseRegex;
            }
        }
    }
}
