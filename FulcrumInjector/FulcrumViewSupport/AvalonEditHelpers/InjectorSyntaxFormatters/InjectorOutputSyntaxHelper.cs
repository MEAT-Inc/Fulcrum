using System.Linq;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using ICSharpCode.AvalonEdit;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters
{
    /// <summary>
    /// Target for redirecting logging configuration on our output
    /// </summary>
    public sealed class InjectorOutputSyntaxHelper : OutputFormatHelperBase
    {
        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public InjectorOutputSyntaxHelper(TextEditor EditorObject)
        {
            // Build document transforming helper objects now and build formatters.
            base.OutputEditor = EditorObject;
            FormatLogger.WriteLog("STORED NEW CONTENT VALUES FOR USER CONTROL AND EDITOR INPUT OK!", LogType.InfoLog);

            // Build color objects here.
            base.BuildColorFormatValues(FulcrumSettingsShare.InjectorDllSyntaxSettings.SettingsEntries);
            FormatLogger.WriteLog("PULLED COLOR VALUES IN CORRECTLY AND BEGAN OUTPUT FORMATTING ON THIS EDITOR!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Turns on coloring output for highlighting on the output view
        /// </summary>
        public override void StartColorHighlighting()
        {
            // Stop old format helpers and clear them.
            this.StopColorHighlighting(); 
            FormatLogger.WriteLog("BUILDING NEW HIGHLIGHT HELPER OUTPUT NOW...", LogType.WarnLog);

            // Invoke this on a background thread
            this.OutputEditor.Dispatcher.Invoke(() =>
            {
                // Add in our output objects now.
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(new TypeAndTimeColorFormatter(this));          // Time coloring
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(new MessageDataColorFormatter(this));          // Message data coloring
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(new CommandParameterColorFormatter(this));     // Command value coloring 
            });
        }
        /// <summary>
        /// Clears out the color helpers from the main input doc object.
        /// </summary>
        public override void StopColorHighlighting()
        {
            // Remove all previous transformers and return out.
            FormatLogger.WriteLog("STOPPING OUTPUT FORMAT!", LogType.WarnLog);
            this.OutputEditor.Dispatcher.Invoke(() =>
            {
                // Log information, find transformers to remove, and remove them
                FormatLogger.WriteLog("STOPPING OUTPUT FORMAT!", LogType.WarnLog);
                var TransformersToRemove = this.OutputEditor.TextArea.TextView.LineTransformers
                    .Where(TransformHelper => TransformHelper.GetType().BaseType == typeof(InjectorDocFormatterBase))
                    .ToArray();

                // Now apply the new transformers onto the editor
                foreach (var TransformHelper in TransformersToRemove) 
                    this.OutputEditor.TextArea.TextView.LineTransformers.Remove(TransformHelper);
            });
        }
    }
}
