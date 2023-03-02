using System.Linq;
using FulcrumInjector.FulcrumViewContent;
using ICSharpCode.AvalonEdit;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumLogFormatters.DebugLogFormatters
{
    /// <summary>
    /// Target for redirecting logging configuration on our output
    /// </summary>
    [Target("DebugLoggingRedirectTarget")]
    internal sealed class DebugLoggingRedirectTarget : OutputFormatHelperBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties

        // Edit Object which we will be using to write into.
        [RequiredParameter]
        public new TextEditor OutputEditor { get; set; }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of our redirecting target object.
        /// </summary>
        public DebugLoggingRedirectTarget(TextEditor EditorObject)
        {
            // Store UserControl and Exit box
            this.OutputEditor = EditorObject;
            _formatLogger.WriteLog("STORED NEW CONTENT VALUES FOR USER CONTROL AND EDITOR INPUT OK!", LogType.InfoLog);

            // Setup our Layout and define the default color formatting rules
            string LoggerMessage = "${message}";
            string LoggerLevel = "${level:uppercase=true}";
            string LoggerDate = "${date:format=hh\\:mm\\:ss}";
            string LoggerMethod = "${scope-property:calling-method-short:whenEmpty=NO_METHOD_FOUND}";
            this.Layout = $"[{LoggerDate}][{LoggerLevel}][{LoggerMethod}] ::: {LoggerMessage}";

            // Finally build our color values for the logging entries we find
            base.BuildColorFormatValues(FulcrumConstants.FulcrumSettings.InjectorDebugSyntaxFulcrumSettings.ToArray());
            _formatLogger.WriteLog("BUILT LAYOUT FORMAT CORRECTLY! READY TO PULL COLORS", LogType.InfoLog);
            _formatLogger.WriteLog("PULLED COLOR VALUES IN CORRECTLY AND BEGAN OUTPUT FORMATTING ON THIS EDITOR!", LogType.InfoLog);

            // Start output formatting here.
            this.StartColorHighlighting();
            _formatLogger.WriteLog("FORMAT OUTPUT HAS BEEN BUILT AND KICKED OFF OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Begins writing output for highlighting syntax
        /// </summary>
        public override void StartColorHighlighting()
        {
            // Build all the color formatting helpers here. Clear out existing ones first.
            this.StopColorHighlighting();

            // Invoke this on a background thread to avoid access issues.
            _formatLogger.WriteLog("STARTING NEW FORMAT HELPERS NOW...", LogType.WarnLog);
            this.OutputEditor.Dispatcher.Invoke(() =>
            {
                // Now build all our new color format helpers.
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(new TimeColorFormatter(this));
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(new LogLevelColorFormatter(this));
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(new CallStackColorFormatter(this));
                this.OutputEditor.TextArea.TextView.LineTransformers.Add(new LoggerNameColorFormatter(this));
            });
        }
        /// <summary>
        /// Clears out the color helpers from the main input doc object.
        /// </summary>
        public override void StopColorHighlighting()
        {
            // Log information, find transformers to remove, and remove them
            _formatLogger.WriteLog("STOPPING OUTPUT FORMAT!", LogType.WarnLog);
            this.OutputEditor.Dispatcher.Invoke(() =>
            {
                // Get the transformers to pull away from this editor
                var TransformersToRemove = this.OutputEditor.TextArea.TextView.LineTransformers
                    .Where(TransformHelper => TransformHelper.GetType().BaseType == typeof(InjectorDocFormatterBase))
                    .ToArray();

                // Now apply the new transformers onto the editor
                this.OutputEditor.TextArea.TextView.LineTransformers.Clear();
                foreach (var TransformHelper in TransformersToRemove)
                    this.OutputEditor.TextArea.TextView.LineTransformers.Add(TransformHelper);
            });
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes the message out to our Logging box.
        /// </summary>
        /// <param name="LogEvent"></param>
        protected override void Write(LogEventInfo LogEvent)
        {
            // Write output using dispatcher to avoid threading issues.
            string RenderedText = this.Layout.Render(LogEvent);
            this.OutputEditor.Dispatcher.InvokeAsync(() => OutputEditor.Text += RenderedText + "\n");
        }
    }
}
