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
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Static instances of the color formatters for use on UI controls
        private InjectorDocFormatterBase _typeTimeFormatter;
        private InjectorDocFormatterBase _commandParameterFormatter;
        private InjectorDocFormatterBase _messageDataReadFormatter;
        private InjectorDocFormatterBase _messageSentDataFormatter;
        private InjectorDocFormatterBase _messageFilterFormatter;

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

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

            // Always consume UI contents inside the dispatcher
            this.OutputEditor.Dispatcher.Invoke(() =>
            {
                // Build new formatter objects here
                this._typeTimeFormatter = new TypeAndTimeColorFormatter(this);
                this._commandParameterFormatter = new CommandParameterColorFormatter(this);
                this._messageDataReadFormatter = new MessageDataReadColorFormatter(this);
                this._messageSentDataFormatter = new MessageDataSentColorFormatter(this);
                this._messageFilterFormatter = new MessageFilterDataColorFormatter(this);
            });
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Turns on coloring output for highlighting on the output view
        /// </summary>
        public override void StartColorHighlighting()
        {
            // Stop old format helpers and clear them.
            if (this.IsHighlighting) this.StopColorHighlighting();
            _formatLogger.WriteLog("BUILDING NEW HIGHLIGHT HELPER OUTPUT NOW...", LogType.WarnLog);

            // Make sure to control our UI contents on the dispatcher
            this.OutputEditor.Dispatcher.Invoke(() =>
            {
                // Append all the color helpers into our TextBox here
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(this._typeTimeFormatter);
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(this._commandParameterFormatter);
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(this._messageDataReadFormatter);
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(this._messageSentDataFormatter);
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(this._messageFilterFormatter);
            });
        }
        /// <summary>
        /// Clears out the color helpers from the main input doc object.
        /// </summary>
        public override void StopColorHighlighting()
        {
            // Make sure we need to stop first 
            if (!this.IsHighlighting) return;

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
