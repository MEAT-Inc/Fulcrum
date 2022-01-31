using System;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.UniversalFormatters;
using ICSharpCode.AvalonEdit;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers
{
    /// <summary>
    /// Contains a set of methods which are able to help us modify avEdit objects by filtering
    /// </summary>
    public class LogOutputFilteringHelper
    {
        // Logger object.
        private static SubServiceLogger FilterLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("AvalonEditFilteringLogger")) ?? new SubServiceLogger("AvalonEditFilteringLogger");

        // Editor object to use and Session GUID
        public readonly Guid SessionGuid;
        public readonly TextEditor SessionEditor;

        /// <summary>
        /// Builds a new instance of this helper class object
        /// </summary>
        /// <param name="EditorInput"></param>
        public LogOutputFilteringHelper(TextEditor EditorInput)
        {
            // Store class values and log information
            this.SessionEditor = EditorInput;
            this.SessionGuid = Guid.NewGuid();
            FilterLogger.WriteLog($"BUILT NEW AVALON EDIT HELPER WITH SESSION ID {SessionGuid} OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches the AvalonEdit object for text matching what we want.
        /// </summary>
        /// <param name="TextToFind"></param>
        public SelectMatchesColorFormatter SearchForText(string TextToFind)
        {
            try
            {
                // Remove existing search objects and add in new ones.
                return SessionEditor.Dispatcher.Invoke(() =>
                {
                    var TransformerList = SessionEditor.TextArea.TextView.LineTransformers.ToList();
                    TransformerList.RemoveAll(TransformerObj => TransformerObj.GetType() == typeof(SelectMatchesColorFormatter));

                    // Clear out transformers and append them in again
                    SessionEditor.TextArea.TextView.LineTransformers.Clear();
                    foreach (var TransformerObject in TransformerList)
                        SessionEditor.TextArea.TextView.LineTransformers.Add(TransformerObject);

                    // Build new searching helper and apply it onto the view
                    if (string.IsNullOrEmpty(TextToFind))
                    {
                        SessionEditor.TextArea.TextView.Redraw();
                        return null;
                    }

                    // Apply Searcher. Store if using Regex or not.
                    var NewSearcher = new SelectMatchesColorFormatter(TextToFind);
                    SessionEditor.TextArea.TextView.LineTransformers.Add(NewSearcher);
                    return NewSearcher;
                });
            }
            catch (Exception Ex)
            {
                // Log this failure and return outupt.
                FilterLogger.WriteLog("ERROR! FAILED TO STORE NEW COLORING METHODS ONTO OUR DOCUMENT VIEW!", LogType.ErrorLog);
                FilterLogger.WriteLog("EXCEPTION THROWN DURING COLORING LINE ROUTINE!", Ex);

                // Set output to failed
                return null;
            }
        }
        /// <summary>
        /// Filters log lines by logger name
        /// </summary>
        /// <param name="FilteringText">Name of logger to filter with</param>
        public FilteringColorFormatter FilterByText(string FilteringText)
        {
            try
            {
                // Remove existing filtering transformers 
                return SessionEditor.Dispatcher.Invoke(() =>
                {
                    var TransformerList = SessionEditor.TextArea.TextView.LineTransformers.ToList();
                    TransformerList.RemoveAll(TransformerObj =>
                    {
                        // If not a filtering type, return false to not remove
                        if (TransformerObj.GetType() != typeof(FilteringColorFormatter))
                            return false;

                        // Cast and run the reset command here then remove
                        var FilteringObj = TransformerObj as FilteringColorFormatter;
                        FilteringObj.ResetDocumentContent();
                        return true;
                    });

                    // Clear out transformers and append them in again
                    SessionEditor.TextArea.TextView.LineTransformers.Clear();
                    foreach (var TransformerObject in TransformerList)
                        SessionEditor.TextArea.TextView.LineTransformers.Add(TransformerObject);

                    // Build new searching helper and apply it onto the view
                    if (string.IsNullOrEmpty(FilteringText)) { SessionEditor.TextArea.TextView.Redraw(); return null; }

                    // Apply Searcher. Store if using Regex or not.
                    var NewSearcher = new FilteringColorFormatter(SessionEditor, FilteringText);
                    SessionEditor.TextArea.TextView.LineTransformers.Add(NewSearcher);
                    return NewSearcher;
                });
            }
            catch (Exception Ex)
            {
                // Log this failure and return outupt.
                FilterLogger.WriteLog("ERROR! FAILED TO STORE NEW FILTERING METHODS ONTO OUR DOCUMENT VIEW!", LogType.ErrorLog);
                FilterLogger.WriteLog("EXCEPTION THROWN DURING FILTERING ROUTINE!", Ex);
                return null;
            }
        }
    }
}
