using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using FulcrumInjector.FulcrumViewContent;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters.CommandParamFormatters;
using FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters.MessageDataFormatters;
using ICSharpCode.AvalonEdit;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.InjectorSyntaxFormatters
{
    /// <summary>
    /// Target for redirecting logging configuration on our output
    /// </summary>
    internal sealed class InjectorOutputSyntaxHelper : OutputFormatHelperBase
    {
        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public InjectorOutputSyntaxHelper(TextEditor EditorObject)
        {
            // Build document transforming helper objects now and build formatters.
            base.OutputEditor = EditorObject;
            _formatLogger.WriteLog("STORED NEW CONTENT VALUES FOR USER CONTROL AND EDITOR INPUT OK!", LogType.InfoLog);

            // Build color objects here.
            base.BuildColorFormatValues(FulcrumConstants.FulcrumSettings.InjectorDllSyntaxFulcrumSettings.ToArray());
            _formatLogger.WriteLog("PULLED COLOR VALUES IN CORRECTLY AND BEGAN OUTPUT FORMATTING ON THIS EDITOR!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Turns on coloring output for highlighting on the output view
        /// </summary>
        public override void StartColorHighlighting()
        {
            // Stop old format helpers and clear them.
            this.StopColorHighlighting(); 
            _formatLogger.WriteLog("BUILDING NEW HIGHLIGHT HELPER OUTPUT NOW...", LogType.WarnLog);

            // Build our new formatter objects and add them to our UI controls
            List<InjectorDocFormatterBase> ColorFormatters = new List<InjectorDocFormatterBase>()
            {
                new TypeAndTimeColorFormatter(this),
                new CommandParameterColorFormatter(this),
                new MessageDataReadColorFormatter(this),
                new MessageDataSentColorFormatter(this),
                new MessageFilterDataColorFormatter(this)
            };

            // Make sure to control our UI contents on the dispatcher
            this.OutputEditor.Dispatcher.Invoke(() =>
            {
                // Append all the color helpers into our TextBox here
                foreach (var ColorFormatter in ColorFormatters)
                    this.OutputEditor.TextArea.TextView.LineTransformers.Add(ColorFormatter);
            });
        }
        /// <summary>
        /// Clears out the color helpers from the main input doc object.
        /// </summary>
        public override void StopColorHighlighting()
        {
            // Remove all previous transformers and return out.
            _formatLogger.WriteLog("STOPPING OUTPUT FORMAT!", LogType.WarnLog);
            this.OutputEditor.Dispatcher.Invoke(() =>
            {
                // Log information, find transformers to remove, and remove them
                _formatLogger.WriteLog("STOPPING OUTPUT FORMAT!", LogType.WarnLog);
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
